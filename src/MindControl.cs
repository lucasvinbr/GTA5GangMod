using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to the mind control feature
    /// </summary>
    public class MindControl {

        public static MindControl instance;

        public SpawnedGangMember currentlyControlledMember = null;
        public bool hasDiedWithChangedBody = false;
        public Ped theOriginalPed;

		/// <summary>
		/// the character currently controlled by the player. 
		/// Can be a mind-controlled member or one of the protagonists
		/// </summary>
		public static Ped CurrentPlayerCharacter
		{
			get
			{
				if (instance.HasChangedBody) {
					return instance.currentlyControlledMember.watchedPed;
				}
				else {
					return Game.Player.Character;
				}
			}
		}


        private int moneyFromLastProtagonist = 0;
        private int defaultMaxHealth = 200;


        public MindControl()
        {
            instance = this;

            defaultMaxHealth = Game.Player.Character.MaxHealth;

        }

        public void Tick()
        {
            if (HasChangedBody)
            {
                TickMindControl();
            }

        }


        /// <summary>
        /// the addition to the tick methods when the player is in control of a member
        /// </summary>
        void TickMindControl()
        {
            if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true))
            {
                UI.ShowSubtitle("Your member has been arrested!");
                RestorePlayerBody();
                return;
            }

            if (!theOriginalPed.IsAlive)
            {
                RestorePlayerBody();
                Game.Player.Character.Kill();
                return;
            }

            if (CurrentPlayerCharacter.Health > 4000 && CurrentPlayerCharacter.Health != 4900)
            {
                CurrentPlayerCharacter.Armor -= (4900 - CurrentPlayerCharacter.Health);
            }

            CurrentPlayerCharacter.Health = 5000;

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
                        if (GangWarManager.instance.isOccurring)
                        {
                            GangWarManager.instance.OnAllyDeath();
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
        public void TryBodyChange()
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
                            //adds a blip to the protagonist so that we know where we left him
                            Blip protagonistBlip = theOriginalPed.AddBlip();
                            protagonistBlip.Sprite = BlipSprite.Creator;
                            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Last Used Protagonist");
                            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, protagonistBlip);
                            

                            defaultMaxHealth = theOriginalPed.MaxHealth;
                            moneyFromLastProtagonist = Game.Player.Money;
                            TakePedBody(playerGangMembers[i]);
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

        void TakePedBody(Ped targetPed)
        {
            targetPed.Task.ClearAllImmediately();
            
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, targetPed, true, true);
            Game.Player.MaxArmor = targetPed.Armor + targetPed.MaxHealth;
            targetPed.Armor += targetPed.Health;
            targetPed.MaxHealth = 5000;
            targetPed.Health = 5000;
            currentlyControlledMember = SpawnManager.instance.GetTargetMemberAI(targetPed);

            Game.Player.CanControlCharacter = true;
        }

        /// <summary>
        /// makes the body the player was using become dead for real
        /// </summary>
        /// <param name="theBody"></param>
        void DiscardDeadBody(Ped theBody)
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
        public void RespawnIfPossible()
        {
            if (hasDiedWithChangedBody)
            {
                Ped oldPed = CurrentPlayerCharacter;

                List<Ped> respawnOptions = SpawnManager.instance.GetSpawnedPedsOfGang
					(GangManager.instance.PlayerGang);

                for(int i = 0; i < respawnOptions.Count; i++)
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

        public void RestorePlayerBody()
        {
            Ped oldPed = CurrentPlayerCharacter;
            //return to original body
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, theOriginalPed, true, true);
            Game.Player.MaxArmor = 100;
            theOriginalPed.CurrentBlip.Remove();
            theOriginalPed.MaxHealth = defaultMaxHealth;
            if (theOriginalPed.Health > theOriginalPed.MaxHealth) theOriginalPed.Health = theOriginalPed.MaxHealth;
            theOriginalPed.Task.ClearAllImmediately();
            
            if (hasDiedWithChangedBody)
            {
                oldPed.IsInvincible = false;
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

        public bool HasChangedBody
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
        public bool AddOrSubtractMoneyToProtagonist(int valueToAdd, bool onlyCheck = false)
        {
            if (HasChangedBody)
            {
                if (valueToAdd > 0 || moneyFromLastProtagonist >= RandoMath.Abs(valueToAdd))
                {
                    if(!onlyCheck) moneyFromLastProtagonist += valueToAdd;
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
