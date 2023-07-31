using GTA.Math;
using GTA.Native;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to the mind control feature
    /// </summary>
    public static class MindControl
    {

        public static SpawnedGangMember currentlyControlledMember = null;
        public static bool hasDiedWithChangedBody = false;
        public static Ped theOriginalPed;

        private static int moneyFromLastProtagonist = 0;
        private static int defaultMaxHealth = 200;

        private static int originalProtagonistRelationGroup;
        public static int spectatorProtagonistRelationGroup;

        /// <summary>
        /// the character currently controlled by the player. 
        /// Can be a mind-controlled member or one of the protagonists
        /// </summary>
        public static Ped CurrentPlayerCharacter
        {
            get
            {
                if (HasChangedBody)
                {
                    return currentlyControlledMember.watchedPed;
                }
                else
                {
                    return Game.Player.Character;
                }
            }
        }

        private static RaycastResult rayResult;

        /// <summary>
        /// usually returns the player's pos, but if the cur. player char is flying,
        /// gets a safe spot on the ground instead of the player pos
        /// </summary>
        public static Vector3 SafePositionNearPlayer
        {
            get
            {
                if (CurrentPlayerCharacter.IsInAir || CurrentPlayerCharacter.IsInFlyingVehicle)
                {
                    rayResult = World.Raycast(CurrentPlayerCharacter.Position, Vector3.WorldDown, 99999.0f, IntersectOptions.Map);
                    if (rayResult.DitHitAnything)
                    {
                        Logger.Log("SafePositionNearPlayer: ray ok!", 4);
                        return rayResult.HitCoords;
                    }
                    else
                    {
                        Logger.Log("SafePositionNearPlayer: Eddlmizing!", 4);
                        Vector3 safePos = CurrentPlayerCharacter.Position;
                        safePos.Z = 0;
                        return SpawnManager.GenerateSpawnPos(
                        safePos, SpawnManager.Nodetype.AnyRoad, false);
                    }
                }
                else
                {
                    return CurrentPlayerCharacter.Position;
                }
            }
        }





        public static void SetupData()
        {
            defaultMaxHealth = Game.Player.Character.MaxHealth;

            //set up the peaceful spectator relation group
            spectatorProtagonistRelationGroup = World.AddRelationshipGroup("GangAndTurfModSpectatorProtagonist");

            //player shouldn't be attacked by the ai-controlled protagonist while mind controlling a member
            World.SetRelationshipBetweenGroups(Relationship.Companion, spectatorProtagonistRelationGroup, Game.Player.Character.RelationshipGroup);
            World.SetRelationshipBetweenGroups(Relationship.Companion, Game.Player.Character.RelationshipGroup, spectatorProtagonistRelationGroup);
        }

        public static void Tick()
        {
            if (HasChangedBody)
            {
                TickMindControl();
            }

        }


        /// <summary>
        /// the addition to the tick methods when the player is in control of a member
        /// </summary>
        private static void TickMindControl()
        {
            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true))
            {
                UI.ShowSubtitle(Localization.GetTextByKey("subtitle_member_was_arrested", "Your member has been arrested!"));
                RestorePlayerBody();
                return;
            }

            if (!theOriginalPed.IsAlive)
            {
                RestorePlayerBody();
                Game.Player.Character.Kill();
                return;
            }

            if (CurrentPlayerCharacter.Health > 4000 && CurrentPlayerCharacter.Health != CurrentPlayerCharacter.MaxHealth)
            {
                CurrentPlayerCharacter.Armor -= (CurrentPlayerCharacter.MaxHealth - CurrentPlayerCharacter.Health);
            }

            CurrentPlayerCharacter.Health = CurrentPlayerCharacter.MaxHealth;

            if (CurrentPlayerCharacter.Armor <= 0) //dead!
            {
                if (!(CurrentPlayerCharacter.IsRagdoll) && hasDiedWithChangedBody)
                {
                    CurrentPlayerCharacter.Weapons.Select(WeaponHash.Unarmed, true);
                    CurrentPlayerCharacter.Task.ClearAllImmediately();
                    CurrentPlayerCharacter.CanRagdoll = true;
                    Function.Call((Hash)0xAE99FB955581844A, CurrentPlayerCharacter.Handle, -1, -1, 0, 0, 0, 0);
                }
                else
                {
                    if (!hasDiedWithChangedBody)
                    {
                        if (GangWarManager.instance.focusedWar != null)
                        {
                            GangWarManager.instance.focusedWar.MemberHasDiedNearWar(currentlyControlledMember.myGang);
                        }
                    }
                    hasDiedWithChangedBody = true;
                    CurrentPlayerCharacter.Weapons.Select(WeaponHash.Unarmed, true);
                    //in a war, this counts as a casualty in our team

                    Function.Call((Hash)0xAE99FB955581844A, CurrentPlayerCharacter.Handle, -1, -1, 0, 0, 0, 0);
                    
                    Game.Player.IgnoredByEveryone = true;
                }

                //RestorePlayerBody();
            }
        }

        /// <summary>
        /// attempts to change the player's body.
        /// if the player has already changed body, the original body is restored
        /// </summary>
        public static void TryBodyChange()
        {
            if (!HasChangedBody)
            {
                List<Ped> playerGangMembers = SpawnManager.instance.GetSpawnedPedsOfGang
                    (GangManager.instance.PlayerGang);
                for (int i = 0; i < playerGangMembers.Count; i++)
                {
                    if (Game.Player.IsTargetting(playerGangMembers[i]))
                    {
                        if (playerGangMembers[i].IsAlive)
                        {
                            theOriginalPed = CurrentPlayerCharacter;
                            originalProtagonistRelationGroup = CurrentPlayerCharacter.RelationshipGroup;
                            //adds a blip to the protagonist so that we know where we left him
                            Blip protagonistBlip = theOriginalPed.AddBlip();
                            protagonistBlip.Sprite = BlipSprite.Creator;
                            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, Localization.GetTextByKey("blip_last_used_protagonist", "Last Used Protagonist"));
                            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, protagonistBlip);


                            defaultMaxHealth = theOriginalPed.MaxHealth;
                            moneyFromLastProtagonist = Game.Player.Money;
                            TakePedBody(playerGangMembers[i]);

                            if (ModOptions.instance.protagonistsAreSpectators)
                            {
                                theOriginalPed.RelationshipGroup = spectatorProtagonistRelationGroup;
                                theOriginalPed.Task.ClearAll();
                            }

                            GangManager.instance.RefreshPlayerRelationsWithAiGangs();

                            break;
                        }
                    }
                }
            }
            else
            {
                RestorePlayerBody();
            }

        }

        private static void TakePedBody(Ped targetPed)
        {
            targetPed.Task.ClearAllImmediately();
            Game.Player.MaxArmor = targetPed.Armor + targetPed.MaxHealth - 100;
            targetPed.Armor += targetPed.Health - 100;
            //UI.Notify("health: " + targetPed.Health.ToString());
            //UI.Notify("our armor is " + targetPed.Armor.ToString());
            //UI.Notify("max armor is " + Game.Player.MaxArmor.ToString());
            //UI.Notify("calculation: " + (targetPed.Armor + targetPed.MaxHealth - 100).ToString());
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, targetPed, true, true);
            targetPed.MaxHealth = 5000;
            targetPed.Health = 5000;
            currentlyControlledMember = SpawnManager.instance.GetTargetMemberAI(targetPed);

            Game.Player.CanControlCharacter = true;
        }

        /// <summary>
        /// makes the body the player was using become dead for real
        /// </summary>
        /// <param name="theBody"></param>
        private static void DiscardDeadBody(Ped theBody)
        {
            hasDiedWithChangedBody = false;
            theBody.RelationshipGroup = GangManager.instance.PlayerGang.relationGroupIndex;
            theBody.IsInvincible = false;
            theBody.Health = 0;
            theBody.Kill();
        }

        /// <summary>
        /// takes control of a random gang member in the vicinity.
        /// if there isnt any, creates one parachuting.
        /// you can only respawn if you have died as a gang member
        /// </summary>
        public static void RespawnIfPossible()
        {
            if (hasDiedWithChangedBody)
            {
                Ped oldPed = CurrentPlayerCharacter;

                List<Ped> respawnOptions = SpawnManager.instance.GetSpawnedPedsOfGang
                    (GangManager.instance.PlayerGang);

                for (int i = 0; i < respawnOptions.Count; i++)
                {
                    if (respawnOptions[i].IsAlive && !respawnOptions[i].IsInVehicle())
                    {
                        //we have a new body then
                        TakePedBody(respawnOptions[i]);

                        DiscardDeadBody(oldPed);
                        return;
                    }
                }

                //lets parachute if no one outside a veh is around
                SpawnedGangMember spawnedPara = SpawnManager.instance.SpawnGangMember
                    (GangManager.instance.PlayerGang,
                   CurrentPlayerCharacter.Position + Vector3.WorldUp * 70);
                if (spawnedPara != null)
                {
                    TakePedBody(spawnedPara.watchedPed);
                    spawnedPara.watchedPed.Weapons.Give(WeaponHash.Parachute, 1, true, true);
                    DiscardDeadBody(oldPed);
                }


            }
        }

        public static void RestorePlayerBody()
        {
            Ped oldPed = CurrentPlayerCharacter;
            //return to original body
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, theOriginalPed, true, true);
            Game.Player.MaxArmor = 100;
            theOriginalPed.CurrentBlip.Remove();
            theOriginalPed.MaxHealth = defaultMaxHealth;
            if (theOriginalPed.Health > theOriginalPed.MaxHealth) theOriginalPed.Health = theOriginalPed.MaxHealth;
            theOriginalPed.Task.ClearAllImmediately();
            theOriginalPed.RelationshipGroup = originalProtagonistRelationGroup;

            GangManager.instance.RefreshPlayerRelationsWithAiGangs();

            oldPed.IsInvincible = false;

            if (hasDiedWithChangedBody)
            {

                oldPed.Health = 0;
                oldPed.MarkAsNoLongerNeeded();
                oldPed.Kill();
            }
            else
            {
                oldPed.Health = oldPed.Armor + 100;
                oldPed.RelationshipGroup = GangManager.instance.PlayerGang.relationGroupIndex;
                oldPed.Task.ClearAllImmediately();
            }

            hasDiedWithChangedBody = false;
            Game.Player.Money = moneyFromLastProtagonist;
            Game.Player.IgnoredByEveryone = false;
            currentlyControlledMember = null;
        }

        public static bool HasChangedBody
        {
            get
            {
                return currentlyControlledMember != null;
            }
        }

        /// <summary>
        /// adds the value, or checks if it's possible to do so, to the currently controlled protagonist
        /// (or the last controlled protagonist if the player is mind-controlling a member)
        /// </summary>
        /// <param name="valueToAdd"></param>
        /// <returns></returns>
        public static bool AddOrSubtractMoneyToProtagonist(int valueToAdd, bool onlyCheck = false)
        {
            if (HasChangedBody)
            {
                if (valueToAdd > 0 || moneyFromLastProtagonist >= RandoMath.Abs(valueToAdd))
                {
                    if (!onlyCheck) moneyFromLastProtagonist += valueToAdd;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (valueToAdd > 0 || Game.Player.Money >= RandoMath.Abs(valueToAdd))
                {
                    if (!onlyCheck) Game.Player.Money += valueToAdd;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


    }

}
