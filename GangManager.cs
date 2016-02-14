using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;

/// <summary>
/// this script controls most things related to gang behavior and relations.
/// </summary>
public class GangManager : Script
{
    PotentialGangMember pedToStore = null;
    List<SpawnedGangMember> livingMembers;
    List<SpawnedDrivingGangMember> livingDrivingMembers;
    List<GangAI> enemyGangs;
    public GangData gangData;
    public static GangManager instance;

    private int ticksSinceLastReward = 0;

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
        this.KeyUp += onKeyUp;
        this.Tick += onTick;
        instance = this;

        new ModOptions(); //just start the options, we can call it by its instance later

        pedToStore = PersistenceHandler.LoadFromFile<PotentialGangMember>("testPedData");
        if (pedToStore == null)
        {
            pedToStore = new PotentialGangMember();
        }

        gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
        if (gangData == null)
        {
            gangData = new GangData();

            //setup test gangs
            gangData.gangs.Add(new Gang("Player's Gang", VehicleColor.BrushedGold, true));
            CreateNewEnemyGang();
        }

        if(gangData.gangs.Count == 1)
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

    public void KillGang(GangAI aiWatchingTheGang)
    {
        UI.Notify("The " + aiWatchingTheGang.watchedGang.name + " have been wiped out!");
        enemyGangs.Remove(aiWatchingTheGang);
        gangData.gangs.Remove(aiWatchingTheGang.watchedGang);
        SaveGangData(false);
    }

    public Gang CreateNewEnemyGang()
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

        gangData.gangs.Add(newGang);

        SaveGangData();
        UI.Notify("The " + gangName + " have entered San Andreas!");

        return newGang;
    }

    public void SaveGangData(bool notifySuccess = true)
    {
        PersistenceHandler.SaveToFile<GangData>(gangData, "GangData", notifySuccess);
    }
    #endregion

    private void onKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.H)
        {
            Ped[] playerGangMembers = GetSpawnedMembersOfGang(GetPlayerGang());
            for(int i = 0; i < playerGangMembers.Length; i++)
            {
                if(Game.Player.IsTargetting(playerGangMembers[i])){
                    int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);
                    
                    if (playerGangMembers[i].IsInGroup){
                        Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                        UI.Notify("A member has left your group");
                    }
                    else
                    {
                        Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                        UI.Notify("A member has joined your group");
                    }
                    break;
                }
            }
            
        }

    }

    void onTick(object sender, EventArgs e)
    {
        for(int i = 0; i < livingMembers.Count; i++)
        {
            if(livingMembers[i].watchedPed != null)
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

        for(int i = 0; i < enemyGangs.Count; i++)
        {
            enemyGangs[i].ticksSinceLastUpdate++;
            if (enemyGangs[i].ticksSinceLastUpdate >= enemyGangs[i].ticksBetweenUpdates)
            {
                enemyGangs[i].Update();
                enemyGangs[i].ticksSinceLastUpdate = 0;

                //lets also check if there aren't too many gangs around
                //if there aren't, we might create a new one...
                if(enemyGangs.Count < 7)
                {
                    if(RandomUtil.CachedRandom.Next(10) == 0)
                    {
                        Gang createdGang = CreateNewEnemyGang();
                        enemyGangs.Add(new GangAI(createdGang));
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
                TurfZone[] curGangZones = ZoneManager.instance.GetZonesControlledByGang(enemyGangs[i].watchedGang.name);
                for(int j = 0; j < curGangZones.Length; j++)
                {
                    enemyGangs[i].watchedGang.moneyAvailable += (int) ((curGangZones[j].value + 1) * 
                        ModOptions.instance.baseRewardPerZoneOwned * 
                        (1 + ModOptions.instance.rewardMultiplierPerZone));
                }
            }

            //this also counts for the player's gang
            int rewardedCash = 0;
            TurfZone[] playerZones = ZoneManager.instance.GetZonesControlledByGang(GetPlayerGang().name);
            for (int i = 0; i < playerZones.Length; i++)
            {
                int zoneReward = (playerZones[i].value + 1) * ModOptions.instance.baseRewardPerZoneOwned;
                Game.Player.Money += zoneReward;
                rewardedCash += zoneReward;
            }

            if(rewardedCash != 0)
            {
                UI.Notify("Money won from controlled zones: " + rewardedCash.ToString());
            }
            
        }
    }

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
            if(ownerGang.gangWeaponHashes.Count > 0)
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
                        UI.Notify("reuse");
                        livingMembers[i].watchedPed = newPed;
                        couldEnlistWithoutAdding = true;
                        break;
                    }
                }
                if (!couldEnlistWithoutAdding)
                {
                    UI.Notify("add new");
                    livingMembers.Add(new SpawnedGangMember(newPed));
                }

            }
            else
            {
                UI.Notify("add new");
                livingMembers.Add(new SpawnedGangMember(newPed));
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
            driver.SetIntoVehicle(newVehicle, VehicleSeat.Driver);
            
            for (int i = 0; i < newVehicle.PassengerSeats; i++)
            {
                SpawnGangMember(ownerGang, spawnPos, true).SetIntoVehicle(newVehicle, VehicleSeat.Any);
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
                    UI.Notify("reuse driver");
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
                UI.Notify("add new drivet");
                SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos);
                newDriver.SetWatchedPassengers();
                livingDrivingMembers.Add(newDriver);
            }

        }
        else
        {
            UI.Notify("add new driver");
            SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos);
            newDriver.SetWatchedPassengers();
            livingDrivingMembers.Add(newDriver);
        }


    }
    #endregion







}
