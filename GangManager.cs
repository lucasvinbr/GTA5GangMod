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

        public bool fightingEnabled = true, warAgainstPlayerEnabled = true;

        /// <summary>
        /// the number of currently alive members.
        /// (the number of entries in LivingMembers isn't the same as this)
        /// </summary>
        public int livingMembersCount = 0;

        public bool hasChangedBody = false;
        public bool hasDiedWithChangedBody = false;
        public Ped theOriginalPed;
        private int moneyFromLastProtagonist = 0;

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

            livingMembers = new List<SpawnedGangMember>();
            livingDrivingMembers = new List<SpawnedDrivingGangMember>();
            enemyGangs = new List<GangAI>();

            new ModOptions(); //just start the options, we can call it by its instance later

            gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (gangData == null)
            {
                gangData = new GangData();

                Gang playerGang = new Gang("Player's Gang", VehicleColor.BrushedGold, true);
                //setup gangs
                gangData.gangs.Add(playerGang);

                playerGang.blipColor = (int) BlipColor.Yellow;

                CreateNewEnemyGang();
            }

            if (gangData.gangs.Count == 1)
            {
                //we're alone.. add an enemy!
                CreateNewEnemyGang();
            }

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
                    //since we're checking each gangs situation...
                    //lets check if we don't have any member variation, which could be a problem
                    if (gangData.gangs[i].memberVariations.Count == 0)
                    {
                        GetMembersForGang(gangData.gangs[i]);
                    }

                    //lets also see if their colors are consistent
                    gangData.gangs[i].EnforceGangColorConsistency();

                    //if we're not player owned, we hate the player!
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);
                    
                    //add this gang to the enemy gangs
                    //and start the AI for it
                    enemyGangs.Add(new GangAI(gangData.gangs[i]));
                }

            }

            //and the relations themselves
            for (int i = gangData.gangs.Count - 1; i > -1; i--)
            {
                for(int j = 0; j < i; j++)
                {
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, gangData.gangs[j].relationGroupIndex);
                    World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[j].relationGroupIndex, gangData.gangs[i].relationGroupIndex);
                }
            }

            //all gangs hate cops if set to very aggressive
            SetCopRelations(ModOptions.instance.gangMemberAggressiveness == ModOptions.gangMemberAggressivenessMode.veryAgressive);
        }

        public void SetCopRelations(bool hate)
        {
            int copHash = Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            int relationLevel = 3; //neutral
            if (hate) relationLevel = 5; //hate

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, copHash, gangData.gangs[i].relationGroupIndex);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, gangData.gangs[i].relationGroupIndex, copHash);
            }
        }

        public void SaveGangData(bool notifySuccess = true)
        {
            PersistenceHandler.SaveToFile<GangData>(gangData, "GangData", notifySuccess);
        }
        #endregion

        public void Tick()
        {
            //tick living members...
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

            //tick living driving members...
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

            TickGangs();

            if (hasChangedBody)
            {
                TickMindControl();
            }
        }

        #region gang general control stuff

        /// <summary>
        /// this controls the gang AI decisions and rewards for the player and AI gangs
        /// </summary>
        void TickGangs()
        {
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
        }

        public Gang CreateNewEnemyGang(bool notifyMsg = true)
        {
            if(PotentialGangMember.MemberPool.memberList.Count <= 0)
            {
                UI.Notify("Enemy gang creation failed: bad/empty/not found memberPool file. Try adding peds as potential members for AI gangs");
                return null;
            }
            //set gang name from options
            string gangName = "Gang";
            do
            {
                gangName = string.Concat(RandomUtil.GetRandomElementFromList(ModOptions.instance.possibleGangFirstNames), " ",
                RandomUtil.GetRandomElementFromList(ModOptions.instance.possibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.memberColor gangColor = (PotentialGangMember.memberColor)RandomUtil.CachedRandom.Next(9);

            Gang newGang = new Gang(gangName, RandomUtil.GetRandomElementFromList(ModOptions.instance.GetGangColorTranslation(gangColor).vehicleColors), false);

            newGang.blipColor = ModOptions.instance.GetGangColorTranslation(gangColor).blipColor;

            GetMembersForGang(newGang);

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

        public void GetMembersForGang(Gang targetGang)
        {
            PotentialGangMember.memberColor gangColor = ModOptions.instance.TranslateVehicleToMemberColor(targetGang.vehicleColor);
            PotentialGangMember.dressStyle gangStyle = (PotentialGangMember.dressStyle)RandomUtil.CachedRandom.Next(3);
            for (int i = 0; i < RandomUtil.CachedRandom.Next(2, 6); i++)
            {
                PotentialGangMember newMember = PotentialGangMember.GetMemberFromPool(gangStyle, gangColor);
                if (newMember != null)
                {
                    targetGang.AddMemberVariation(newMember);
                }
                else
                {
                    break;
                }

            }
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

                        AddOrSubtractMoneyToProtagonist(zoneReward);
                        
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

        public static int CalculateHealthUpgradeCost(int currentMemberHealth)
        {
            return (currentMemberHealth + 20) * (20 * (currentMemberHealth / 20) + 1);
        }

        public static int CalculateArmorUpgradeCost(int currentMemberArmor)
        {
            return 2000 + (currentMemberArmor + 20) * (50 * (currentMemberArmor / 25) + 1);
        }

        public static int CalculateAccuracyUpgradeCost(int currentMemberAcc)
        {
            return (currentMemberAcc + 10) * 2500;
        }

        #endregion

        #region gang member mind control

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

            if (Game.Player.Character.Health > 4000 && Game.Player.Character.Health != 4900)
            {
                Game.Player.Character.Armor -= (4900 - Game.Player.Character.Health);
            }

            Game.Player.Character.Health = 5000;

            if (Game.Player.Character.Armor <= 0) //dead!
            {
                if (!(Game.Player.Character.IsRagdoll) && hasDiedWithChangedBody)
                {
                    Game.Player.Character.Weapons.Select(WeaponHash.Unarmed, true);
                    Game.Player.Character.Task.ClearAllImmediately();
                    Game.Player.Character.Euphoria.ShotFallToKnees.Start();
                }
                else
                {
                    hasDiedWithChangedBody = true;
                    //Game.Player.CanControlCharacter = false;
                    Game.Player.Character.CanRagdoll = true;
                    Game.Player.Character.Euphoria.ShotFallToKnees.Start(20000);
                    Game.Player.IgnoredByEveryone = true;
                }

                //RestorePlayerBody();
            }
        }

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
                            moneyFromLastProtagonist = Game.Player.Money;
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
            Game.Player.MaxArmor = targetPed.Armor + targetPed.Health;
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
            hasDiedWithChangedBody = false;
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
            if (hasDiedWithChangedBody)
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
                   Game.Player.Character.Position + Vector3.WorldUp * 70);
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
            Game.Player.MaxArmor = 100;
            if (theOriginalPed.Health > theOriginalPed.MaxHealth) theOriginalPed.Health = theOriginalPed.MaxHealth;
            hasChangedBody = false;
            //Game.Player.CanControlCharacter = true;
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
                oldPed.RelationshipGroup = GetPlayerGang().relationGroupIndex;
                oldPed.Task.FightAgainstHatedTargets(80);
            }

            hasDiedWithChangedBody = false;
            Game.Player.Money = moneyFromLastProtagonist;
        }

        /// <summary>
        /// adds the value to the currently controlled protagonist
        /// (or the last controlled protagonist if the player is mind-controlling a member)
        /// </summary>
        /// <param name="valueToAdd"></param>
        /// <returns></returns>
        public bool AddOrSubtractMoneyToProtagonist(int valueToAdd, bool onlyCheck = false)
        {
            if (hasChangedBody)
            {
                if (valueToAdd > 0 || moneyFromLastProtagonist >= valueToAdd)
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
                if (valueToAdd > 0 || Game.Player.Money >= valueToAdd)
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

        public Ped[] GetEnemyGangMembers(Gang friendlyGang)
        {
            //gets all members who are enemies of friendlyGang
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i].watchedPed.RelationshipGroup != friendlyGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            return returnedList.ToArray();
        }

        public Ped[] GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            foreach(Ped ped in detectedPeds)
            {
                if (referencePed.RelationshipGroup != ped.RelationshipGroup)
                {
                    if (World.GetRelationshipBetweenGroups(referencePed.RelationshipGroup, ped.RelationshipGroup) == Relationship.Hate)
                    {
                        if (ped.IsAlive)
                        {
                            hostilePeds.Add(ped);
                        }
                        
                    }
                }
               
            }
            return hostilePeds.ToArray();
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember()
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 playerPos = Game.Player.Character.Position;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(World.GetNextPositionOnStreet(Game.Player.Character.Position + RandomUtil.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableMemberSpawnDistance()));
            float distFromPlayer = World.GetDistance(Game.Player.Character.Position, chosenPos);
            while ((distFromPlayer > ModOptions.instance.maxDistanceMemberSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceMemberSpawnFromPlayer) && attempts <= 5)
            {
                // UI.Notify("too far"); or too close
                chosenPos = World.GetNextPositionOnSidewalk(Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 
                    ModOptions.instance.GetAcceptableMemberSpawnDistance());
                distFromPlayer = World.GetDistance(Game.Player.Character.Position, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        public Vector3 FindGoodSpawnPointForCar()
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 playerPos = Game.Player.Character.Position;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnStreet
                          (Game.Player.Character.Position + RandomUtil.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableCarSpawnDistance());
            float distFromPlayer = World.GetDistance(Game.Player.Character.Position, chosenPos);

            while ((distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer) && attempts < 5)
            {
                // UI.Notify("too far"); or too close
                //just spawn it then, don't mind being on the street because the player might be on the mountains or the desert
                chosenPos = World.GetNextPositionOnSidewalk(Game.Player.Character.Position + RandomUtil.RandomDirection(true) *
                    ModOptions.instance.GetAcceptableCarSpawnDistance());
                distFromPlayer = World.GetDistance(Game.Player.Character.Position, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// makes a few attempts to place the target vehicle on a street.
        /// if it fails, the vehicle is returned to its original position
        /// </summary>
        /// <param name="targetVehicle"></param>
        /// <param name="originalPos"></param>
        public void TryPlaceVehicleOnStreet(Vehicle targetVehicle, Vector3 originalPos)
        {
            targetVehicle.PlaceOnNextStreet();
            int attemptsPlaceOnStreet = 0;
            float distFromPlayer = World.GetDistance(Game.Player.Character.Position, targetVehicle.Position);
            while((distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer) && attemptsPlaceOnStreet < 3)
            {
                targetVehicle.Position = FindGoodSpawnPointForCar();
                targetVehicle.PlaceOnNextStreet();
                distFromPlayer = World.GetDistance(Game.Player.Character.Position, targetVehicle.Position);
                attemptsPlaceOnStreet++;
            }

            if(distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer)
            {
                targetVehicle.Position = originalPos;
            }
        }

        public Ped SpawnGangMember(Gang ownerGang, Vector3 spawnPos, bool isImportant = true, bool deactivatePersistent = false)
        {
            if(livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.memberVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }
            if (ownerGang.memberVariations.Count > 0)
            {
                PotentialGangMember chosenMember =
                    RandomUtil.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if(newPed != null)
                {
                    SetPedAppearance(newPed, chosenMember);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    //set the blip
                    newPed.AddBlip();
                    newPed.CurrentBlip.IsShortRange = true;
                    newPed.CurrentBlip.Scale = 0.65f;
                    Function.Call(Hash.SET_BLIP_COLOUR, newPed.CurrentBlip, ownerGang.blipColor);

                    //set blip name - got to use native, the c# blip.name returns error ingame
                    Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, ownerGang.name + " member");
                    Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);


                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        //get one weap from each type... if possible
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.meleeWeapons), 1000, false, true);
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons), 1000, false, true);
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.primaryWeapons), 1000, false, true);

                        //and one extra
                        newPed.Weapons.Give(RandomUtil.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, false, true);
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

                    newPed.BlockPermanentEvents = true;

                    Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, newPed, false, false); //cannot attack friendlies
                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, newPed, 1); //average combat ability
                    Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, newPed, 0, 0); //clears the flee attributes?

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // alwaysFight = true and canFightArmedWhenNotArmed. which one is which is unknown
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 5, true);
                    Function.Call(Hash.SET_PED_COMBAT_RANGE, newPed, 2); //combatRange = far
                   
                    newPed.CanSwitchWeapons = true;
                    newPed.CanWrithe = false; //no early dying
                    //newPed.AlwaysKeepTask = true;


                    //enlist this new gang member in the spawned list!
                    if (livingMembers.Count > 0)
                    {
                        bool couldEnlistWithoutAdding = false;
                        for (int i = 0; i < livingMembers.Count; i++)
                        {
                            if (livingMembers[i].watchedPed == null)
                            {
                                livingMembers[i].watchedPed = newPed;
                                livingMembers[i].myGang = ownerGang;
                                couldEnlistWithoutAdding = true;
                                break;
                            }
                        }
                        if (!couldEnlistWithoutAdding)
                        {
                            if (livingMembers.Count < ModOptions.instance.spawnedMemberLimit)
                            {
                                SpawnedGangMember newAddition = new SpawnedGangMember(newPed);
                                newAddition.myGang = ownerGang;
                                livingMembers.Add(newAddition);
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
                            SpawnedGangMember newAddition = new SpawnedGangMember(newPed);
                            newAddition.myGang = ownerGang;
                            livingMembers.Add(newAddition);
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

                livingMembersCount++;
                return newPed;
            }

            return null;
        }

        public Vehicle SpawnGangVehicle(Gang ownerGang, Vector3 spawnPos, Vector3 destPos, bool isImportant = false, bool deactivatePersistent = false, bool playerIsDest = false)
        {
            if (livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.carVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }

            if (ownerGang.carVariations.Count > 0)
            {
                Vehicle newVehicle = World.CreateVehicle(RandomUtil.GetRandomElementFromList(ownerGang.carVariations).modelHash, spawnPos);
                newVehicle.PrimaryColor = ownerGang.vehicleColor;

                if (!isImportant)
                {
                    newVehicle.MarkAsNoLongerNeeded();
                }

                Ped driver = SpawnGangMember(ownerGang, spawnPos, true);
                if (driver != null)
                {
                    driver.SetIntoVehicle(newVehicle, VehicleSeat.Driver);

                    int passengerCount = newVehicle.PassengerSeats;
                    if (destPos == Vector3.Zero && passengerCount > 4) passengerCount = 4; //limit ambient passengers in order to have less impact in ambient spawning

                    for (int i = 0; i < passengerCount; i++)
                    {
                        Ped passenger = SpawnGangMember(ownerGang, spawnPos, true);
                        if (passenger != null)
                        {
                            passenger.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                        }
                    }

                    EnlistDrivingMember(driver, newVehicle, destPos, playerIsDest);

                    if (deactivatePersistent)
                    {
                        newVehicle.IsPersistent = false;
                    }

                    newVehicle.AddBlip();
                    newVehicle.CurrentBlip.IsShortRange = true;

                    Function.Call(Hash.SET_BLIP_COLOUR, newVehicle.CurrentBlip, ownerGang.blipColor);

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

        void EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, Vector3 destPos, bool playerIsDest = false)
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
                        livingDrivingMembers[i].playerAsDest = playerIsDest;
                        couldEnlistWithoutAdding = true;
                        break;
                    }
                }
                if (!couldEnlistWithoutAdding)
                {
                    SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos, playerIsDest);
                    newDriver.SetWatchedPassengers();
                    livingDrivingMembers.Add(newDriver);
                }

            }
            else
            {
                SpawnedDrivingGangMember newDriver = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos, playerIsDest);
                newDriver.SetWatchedPassengers();
                livingDrivingMembers.Add(newDriver);
            }


        }

        /// <summary>
        /// this method sets the ped's appearance according to what's been specified in the potential gang member info.
        /// then, it checks for inconsistencies in the ped's appearance (differences in skin tone, for example, between the ped's body parts).
        /// if it finds one, it tries to find a valid variation of the inconsistent part
        /// </summary>
        void SetPedAppearance(Ped targetPed, PotentialGangMember pedAppearanceInfo)
        {
            bool hasChangedLegs = false, hasChangedTorso = false;
            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 0);

            if (pedAppearanceInfo.torsoDrawableIndex != -1)
            {
                int torsoTexIndex = pedAppearanceInfo.torsoTextureIndex;
                if (torsoTexIndex == -1)
                {
                    torsoTexIndex = RandomUtil.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, targetPed, 3, pedAppearanceInfo.torsoDrawableIndex));
                }
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 3, pedAppearanceInfo.torsoDrawableIndex, torsoTexIndex, pedPalette);
                hasChangedTorso = true;
            }

            if (pedAppearanceInfo.legsDrawableIndex != -1)
            {
                int legsTexIndex = pedAppearanceInfo.legsTextureIndex;
                if (legsTexIndex == -1)
                {
                    legsTexIndex = RandomUtil.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, targetPed, 4, pedAppearanceInfo.legsDrawableIndex));
                }
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 4, pedAppearanceInfo.legsDrawableIndex, legsTexIndex, pedPalette);
                hasChangedLegs = true;
            }

            if (hasChangedTorso || hasChangedLegs)
            {
                //check if the head is valid with the changes that have been made
                //also, find out if the torso or the legs haven't been changed
                //because, in that case, we'll have to enforce consistency for them too

                int unchangedComponentIndex = -1, changedComponentIndex = 0;
                int changedDrawableIndex = 0;
                

                if (!hasChangedTorso)
                {
                    unchangedComponentIndex = 3;
                    changedComponentIndex = 4;
                    changedDrawableIndex = pedAppearanceInfo.legsDrawableIndex;
                }

                if (!hasChangedLegs)
                {
                    unchangedComponentIndex = 4;
                    changedComponentIndex = 3;
                    changedDrawableIndex = pedAppearanceInfo.torsoDrawableIndex;
                }

                int changedTexIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, changedComponentIndex);
                //if it's aleady valid, lets just stop here
                if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, targetPed, changedComponentIndex, changedDrawableIndex, changedTexIndex))
                {
                    UI.ShowSubtitle("already valid!");
                    return;
                }

               

                //for each head variation, check if it matches the rest
                //if we havent changed torso or legs, do that for them too
                for(int i = 0; i < Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, targetPed, 0); i++)
                {
                    for(int j = 0; j < Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, targetPed, 0, i); j++)
                    {
                        if (unchangedComponentIndex != -1)
                        {
                            for (int k = 0; k < Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, targetPed, unchangedComponentIndex); k++)
                            {
                                for (int l = 0; l < Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, targetPed, unchangedComponentIndex, k); l++)
                                {
                                    //set the unchanged body to this new variation we're testing
                                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, unchangedComponentIndex, k, l, pedPalette);
                                }
                            }
                        }

                        //check the head again if it's consistent...
                        if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, targetPed, 0, i, j))
                        {
                            //if this one is, set the head to this variation!
                            UI.ShowSubtitle("found valid!");
                            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 0, i, j, pedPalette);
                            return;
                        }
                    }
                }
            }
        }
        #endregion
    }

}
