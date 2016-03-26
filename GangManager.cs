using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to gang behavior and relations.
    /// </summary>
    public class GangManager
    {
        List<SpawnedGangMember> livingMembers;
        List<SpawnedDrivingGangMember> livingDrivingMembers;
        List<GangAI> enemyGangs;
        public GangData gangData;
        public static GangManager instance;

        private int ticksSinceLastReward = 0;

        public bool hasChangedBody = false;
        private Ped theOriginalPed;
        private int profitWhileChangedBody = 0;

        #region setup/save stuff
        [System.Serializable]
        public class GangData
        {

            public GangData()
            {
                gangs = new List<Gang>();
            }

            public List<Gang> gangs;
        }
        public GangManager()
        {
            instance = this;

            new ModOptions(); //just start the options, we can call it by its instance later

            gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (gangData == null)
            {
                gangData = new GangData();

                //setup gangs
                gangData.gangs.Add(new Gang("Player's Gang", VehicleColor.BrushedGold, true));
                CreateNewEnemyGang();
            }

            if (gangData.gangs.Count == 1)
            {
                //we're alone.. add an enemy!
                CreateNewEnemyGang();
            }

            livingMembers = new List<SpawnedGangMember>();
            livingDrivingMembers = new List<SpawnedDrivingGangMember>();
            enemyGangs = new List<GangAI>();

            SetUpGangRelations();
        }
        /// <summary>
        /// basically makes all gangs hate each other
        /// </summary>
        void SetUpGangRelations()
        {
            //set up the relationshipgroups
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                gangData.gangs[i].relationGroupIndex = World.AddRelationshipGroup(gangData.gangs[i].name);
                //if the player owns this gang, we love him
                if (gangData.gangs[i].isPlayerOwned)
                {
                    World.SetRelationshipBetweenGroups(Relationship.Companion, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Companion, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);
                }
                else
                {
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);

                    //add this gang to the enemy gangs
                    //and start the AI for it
                    enemyGangs.Add(new GangAI(gangData.gangs[i]));
                }
            }

            //and the relations themselves
            for (int i = 0; i < gangData.gangs.Count - 1; i++)
            {
                World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, gangData.gangs[i + 1].relationGroupIndex);
                World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i + 1].relationGroupIndex, gangData.gangs[i].relationGroupIndex);
            }
        }

        public void SaveGangData(bool notifySuccess = true)
        {
            PersistenceHandler.SaveToFile<GangData>(gangData, "GangData", notifySuccess);
        }
        #endregion

        public void Tick()
        {
            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    livingMembers[i].ticksSinceLastUpdate++;
                    if (livingMembers[i].ticksSinceLastUpdate >= livingMembers[i].ticksBetweenUpdates)
                    {
                        livingMembers[i].Update();
                        livingMembers[i].ticksSinceLastUpdate = 0;
                    }
                }
            }

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed != null && livingDrivingMembers[i].vehicleIAmDriving != null)
                {
                    livingDrivingMembers[i].ticksSinceLastUpdate++;
                    if (livingDrivingMembers[i].ticksSinceLastUpdate >= livingDrivingMembers[i].ticksBetweenUpdates)
                    {
                        livingDrivingMembers[i].Update();
                        livingDrivingMembers[i].ticksSinceLastUpdate = 0;
                    }
                }
            }

            for (int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ticksSinceLastUpdate++;
                if (enemyGangs[i].ticksSinceLastUpdate >= enemyGangs[i].ticksBetweenUpdates)
                {
                    enemyGangs[i].ticksSinceLastUpdate = 0;
                    enemyGangs[i].Update();

                    //lets also check if there aren't too many gangs around
                    //if there aren't, we might create a new one...
                    if (enemyGangs.Count < 7)
                    {
                        if (RandomUtil.CachedRandom.Next(enemyGangs.Count) == 0)
                        {
                            Gang createdGang = CreateNewEnemyGang();
                            if (createdGang != null)
                            {
                                enemyGangs.Add(new GangAI(createdGang));
                            }

                        }
                    }
                }
            }

            ticksSinceLastReward++;
            if (ticksSinceLastReward >= ModOptions.instance.ticksBetweenTurfRewards)
            {
                ticksSinceLastReward = 0;
                //each gang wins money according to the amount of owned zones and their values
                for (int i = 0; i < enemyGangs.Count; i++)
                {
                    GiveTurfRewardToGang(enemyGangs[i].watchedGang);
                }

                //this also counts for the player's gang
                GiveTurfRewardToGang(GetPlayerGang());
            }

            if (hasChangedBody)
            {

                if(Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true))
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

                if(Game.Player.Character.Health > 4000 && Game.Player.Character.Health != 4900)
                {
                    Game.Player.Character.Armor -= (4900 - Game.Player.Character.Health);
                }

                Game.Player.Character.Health = 5000;

                if (Game.Player.Character.Armor <= 0) //dead!
                {
                    if (!(Game.Player.Character.IsRagdoll) && Game.Player.IsInvincible)
                    {
                        Game.Player.Character.Euphoria.ShotFallToKnees.Start();
                    }
                    else
                    {
                        Game.Player.IsInvincible = true;
                        Game.Player.Character.CanRagdoll = true;
                        Game.Player.Character.Euphoria.ShotFallToKnees.Start(20000);
                        Game.Player.IgnoredByEveryone = true;
                    }
                    
                    //RestorePlayerBody();
                }
            }
        }

        #region gang general control stuff

        public Gang CreateNewEnemyGang(bool notifyMsg = true)
        {
            //set gang name from options
            string gangName = "Gang";
            do
            {
                gangName = string.Concat(RandomUtil.GetRandomElementFromList(ModOptions.instance.possibleGangFirstNames), " ",
                RandomUtil.GetRandomElementFromList(ModOptions.instance.possibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.dressStyle gangStyle = (PotentialGangMember.dressStyle)RandomUtil.CachedRandom.Next(3);
            PotentialGangMember.memberColor gangColor = (PotentialGangMember.memberColor)RandomUtil.CachedRandom.Next(9);

            Gang newGang = new Gang(gangName, RandomUtil.GetRandomElementFromList(ModOptions.instance.GetGangColorTranslation(gangColor).vehicleColors), false);

            for (int i = 0; i < RandomUtil.CachedRandom.Next(2, 6); i++)
            {
                newGang.AddMemberVariation(PotentialGangMember.GetMemberFromPool(gangStyle, gangColor));
            }

            //relations...
            newGang.relationGroupIndex = World.AddRelationshipGroup(gangName);

            World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, Game.Player.Character.RelationshipGroup);
            World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, newGang.relationGroupIndex);

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, newGang.relationGroupIndex);
                World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, gangData.gangs[i].relationGroupIndex);
            }

            gangData.gangs.Add(newGang);

            SaveGangData();
            if (notifyMsg)
            {
                UI.Notify("The " + gangName + " have entered San Andreas!");
            }
            

            return newGang;
        }

        public void KillGang(GangAI aiWatchingTheGang)
        {
            UI.Notify("The " + aiWatchingTheGang.watchedGang.name + " have been wiped out!");
            enemyGangs.Remove(aiWatchingTheGang);
            gangData.gangs.Remove(aiWatchingTheGang.watchedGang);
            if(enemyGangs.Count == 0)
            {
                //create a new gang right away... but do it silently to not demotivate the player too much
                CreateNewEnemyGang(false);
            }
            SaveGangData(false);
        }

        public void GiveTurfRewardToGang(Gang targetGang)
        {

            TurfZone[] curGangZones = ZoneManager.instance.GetZonesControlledByGang(targetGang.name);
            if (targetGang.isPlayerOwned)
            {
                if (curGangZones.Length > 0)
                {
                    int rewardedCash = 0;

                    for (int i = 0; i < curGangZones.Length; i++)
                    {
                        int zoneReward = (int)((curGangZones[i].value + 1) * ModOptions.instance.baseRewardPerZoneOwned *
                        (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Length));
                        if (hasChangedBody)
                        {
                            profitWhileChangedBody += zoneReward;
                        }
                        else
                        {
                            Game.Player.Money += zoneReward;
                        }
                        
                        rewardedCash += zoneReward;
                    }

                    UI.Notify("Money won from controlled zones: " + rewardedCash.ToString());
                }
            }
            else
            {
                for (int j = 0; j < curGangZones.Length; j++)
                {
                    targetGang.moneyAvailable += (int)((curGangZones[j].value + 1) *
                        ModOptions.instance.baseRewardPerZoneOwned *
                        (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Length));
                }

            }

        }

        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetGangUpdateIntervals()
        {
            for(int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ResetUpdateInterval();
            }

            for (int i = 0; i < livingMembers.Count; i++)
            {
                livingMembers[i].ResetUpdateInterval();
            }
        }

        #endregion

        #region gang member mind control
        public void TryBodyChange()
        {
            if (!hasChangedBody)
            {
                Ped[] playerGangMembers = GetSpawnedMembersOfGang(GetPlayerGang());
                for (int i = 0; i < playerGangMembers.Length; i++)
                {
                    if (Game.Player.IsTargetting(playerGangMembers[i]))
                    {
                        if (playerGangMembers[i].IsAlive)
                        {
                            theOriginalPed = Game.Player.Character;
                            //theOriginalPed.IsInvincible = true;
                            hasChangedBody = true;
                            TakePedBody(playerGangMembers[i]);
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
            targetPed.Armor += targetPed.Health;
            targetPed.MaxHealth = 5000;
            targetPed.Health = 5000;
            Game.Player.CanControlCharacter = true;
        }

        /// <summary>
        /// makes the body the player was using become dead for real
        /// </summary>
        /// <param name="theBody"></param>
        void DiscardDeadBody(Ped theBody)
        {
            theBody.IsInvincible = false;
            theBody.Health = 0;
            theBody.MarkAsNoLongerNeeded();
            theBody.Kill();
        }

        /// <summary>
        /// takes control of a random gang member in the vicinity.
        /// if there isnt any, creates one parachuting.
        /// you can only respawn if you have died as a gang member
        /// </summary>
        public void RespawnIfPossible()
        {
            if (Game.Player.Character.IsRagdoll && Game.Player.Character.IsInvincible)
            {
                Ped oldPed = Game.Player.Character;

                Ped[] respawnOptions = GetSpawnedMembersOfGang(GetPlayerGang());

                for(int i = 0; i < respawnOptions.Length; i++)
                {
                    if (respawnOptions[i].IsAlive)
                    {
                        //we have a new body then
                        TakePedBody(respawnOptions[i]);

                        DiscardDeadBody(oldPed);
                        return;
                    }
                }

                //lets parachute if no one is around
                Ped spawnedPed = GangManager.instance.SpawnGangMember(GangManager.instance.GetPlayerGang(),
                   Game.Player.Character.Position + Math.Vector3.WorldUp * 70);
                if (spawnedPed != null)
                {
                    TakePedBody(spawnedPed);
                    spawnedPed.Task.UseParachute();
                    DiscardDeadBody(oldPed);
                }


            }
        }

        void RestorePlayerBody()
        {
            Ped oldPed = Game.Player.Character;
            //return to original body
            Function.Call(Hash.CHANGE_PLAYER_PED, Game.Player, theOriginalPed, true, true);
            hasChangedBody = false;
            Game.Player.CanControlCharacter = true;
            theOriginalPed.IsInvincible = false;

            Game.Player.Money += profitWhileChangedBody;
            profitWhileChangedBody = 0;
            
            if (oldPed.IsInvincible) //means he's dead
            {
                oldPed.IsInvincible = false;
                oldPed.Health = 0;
                oldPed.MarkAsNoLongerNeeded();
                oldPed.Kill();
            }
            else
            {
                oldPed.MarkAsNoLongerNeeded();
                oldPed.Health = oldPed.Armor + 100;
                oldPed.RelationshipGroup = GetPlayerGang().relationGroupIndex;
                oldPed.Task.FightAgainstHatedTargets(80);
            }            
        }

        #endregion

        #region getters
        public Gang GetGangByName(string name)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].name == name)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public Gang GetGangByRelGroup(int relGroupIndex)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].relationGroupIndex == relGroupIndex)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the player's gang
        /// </summary>
        /// <returns></returns>
        public Gang GetPlayerGang()
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].isPlayerOwned)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public Ped[] GetSpawnedMembersOfGang(Gang desiredGang)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            return returnedList.ToArray();
        }
        #endregion

        #region spawner methods
        public Ped SpawnGangMember(Gang ownerGang, GTA.Math.Vector3 spawnPos, bool isImportant = false, bool deactivatePersistent = false)
        {
            if (ownerGang.memberVariations.Count > 0)
            {
                PotentialGangMember chosenMember =
                    RandomUtil.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if(newPed != null)
                {
                    int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, newPed, 1);

                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 3, chosenMember.torsoDrawableIndex, chosenMember.torsoTextureIndex, pedPalette);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 4, chosenMember.legsDrawableIndex, chosenMember.legsTextureIndex, pedPalette);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    //set the blip
                    newPed.AddBlip();
                    newPed.CurrentBlip.IsShortRange = true;
                    newPed.CurrentBlip.Sprite = BlipSprite.Pistol;
                    if (ownerGang.isPlayerOwned)
                    {
                        newPed.CurrentBlip.Color = BlipColor.Green;
                    }
                    else
                    {
                        newPed.CurrentBlip.Color = BlipColor.Red;
                    }


                    //set blip name - got to use native, the c# blip.name returns error ingame
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, ownerGang.name + " member");
                    Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);


                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        newPed.Weapons.Give(RandomUtil.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, true, true);
                    }

                    //set the relationship group
                    newPed.RelationshipGroup = ownerGang.relationGroupIndex;



                    if (!isImportant)
                    {
                        newPed.MarkAsNoLongerNeeded();
                    }

                    if (deactivatePersistent)
                    {
                        newPed.IsPersistent = false;
                    }

                    newPed.NeverLeavesGroup = true;

                    //Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, newPed, false, false); //cannot attack friendlies
                    //Function.Call(Hash.SET_PED_COMBAT_ABILITY, newPed, 1); //average combat ability
                    //Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, newPed, 0, false); //clears the flee attributes?

                    //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // alwaysFight = true and canFightArmedWhenNotArmed. which one is 17 and 46 is unknown
                    //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 17, true); 
                    //Function.Call(Hash.SET_PED_COMBAT_RANGE, newPed, 2); //combatRange = far

                    newPed.BlockPermanentEvents = true;
                    newPed.CanSwitchWeapons = true;


                    //enlist this new gang member in the spawned list!
                    if (livingMembers.Count > 0)
                    {
                        bool couldEnlistWithoutAdding = false;
                        for (int i = 0; i < livingMembers.Count; i++)
                        {
                            if (livingMembers[i].watchedPed == null)
                            {
                                livingMembers[i].watchedPed = newPed;
                                couldEnlistWithoutAdding = true;
                                break;
                            }
                        }
                        if (!couldEnlistWithoutAdding)
                        {
                            if (livingMembers.Count < ModOptions.instance.spawnedMemberLimit)
                            {
                                livingMembers.Add(new SpawnedGangMember(newPed));
                            }
                            else
                            {
                                newPed.Delete();
                                return null;
                            }
                        }

                    }
                    else
                    {
                        if (livingMembers.Count < ModOptions.instance.spawnedMemberLimit)
                        {
                            livingMembers.Add(new SpawnedGangMember(newPed));
                        }
                        else
                        {
                            newPed.Delete();
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }

                return newPed;
            }

            return null;
        }

        public Vehicle SpawnGangVehicle(Gang ownerGang, GTA.Math.Vector3 spawnPos, GTA.Math.Vector3 destPos, bool isImportant = false, bool deactivatePersistent = false)
        {
            if (ownerGang.gangVehicleHash != -1)
            {
                Vehicle newVehicle = World.CreateVehicle(ownerGang.gangVehicleHash, spawnPos);
                newVehicle.PrimaryColor = ownerGang.color;

                if (!isImportant)
                {
                    newVehicle.MarkAsNoLongerNeeded();
                }

                Ped driver = SpawnGangMember(ownerGang, spawnPos, true);
                if (driver != null)
                {
                    driver.SetIntoVehicle(newVehicle, VehicleSeat.Driver);
                    Function.Call(Hash.SET_PED_STEERS_AROUND_OBJECTS, driver, true);
                    Function.Call(Hash.SET_PED_STEERS_AROUND_PEDS, driver, true);
                    Function.Call(Hash.SET_PED_STEERS_AROUND_VEHICLES, driver, true);

                    for (int i = 0; i < newVehicle.PassengerSeats; i++)
                    {
                        Ped passenger = SpawnGangMember(ownerGang, spawnPos, true);
                        if (passenger != null)
                        {
                            passenger.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                        }
                    }

                    EnlistDrivingMember(driver, newVehicle, destPos);

                    if (deactivatePersistent)
                    {
                        newVehicle.IsPersistent = false;
                    }

                    newVehicle.AddBlip();
                    newVehicle.CurrentBlip.IsShortRange = true;

                    if (ownerGang.isPlayerOwned)
                    {
                        newVehicle.CurrentBlip.Color = BlipColor.Green;
                    }
                    else
                    {
                        newVehicle.CurrentBlip.Color = BlipColor.Red;
                    }

                    return newVehicle;
                }
                else
                {
                    newVehicle.Delete();
                    return null;
                }

            }

            return null;
        }

        void EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, GTA.Math.Vector3 destPos)
        {
            //enlist this new gang member in the spawned list!
            if (livingDrivingMembers.Count > 0)
            {
                bool couldEnlistWithoutAdding = false;
                for (int i = 0; i < livingDrivingMembers.Count; i++)
                {
                    if (livingDrivingMembers[i].watchedPed == null)
                    {
                        livingDrivingMembers[i].watchedPed = pedToEnlist;
                        livingDrivingMembers[i].vehicleIAmDriving = vehicleDriven;
                        livingDrivingMembers[i].destination = destPos;
                        livingDrivingMembers[i].updatesWhileGoingToDest = 0;
                        livingDrivingMembers[i].SetWatchedPassengers();
                        couldEnlistWithoutAdding = true;
                        break;
                    }
                }
                if (!couldEnlistWithoutAdding)
                {
                    SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos);
                    newDriver.SetWatchedPassengers();
                    livingDrivingMembers.Add(newDriver);
                }

            }
            else
            {
                SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos);
                newDriver.SetWatchedPassengers();
                livingDrivingMembers.Add(newDriver);
            }


        }
        #endregion
    }

}
