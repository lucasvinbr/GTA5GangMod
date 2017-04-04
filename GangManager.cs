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

        public Gang PlayerGang
        {
            get
            {
                if(cachedPlayerGang == null)
                {
                    cachedPlayerGang = GetPlayerGang();
                }

                return cachedPlayerGang;
            }
        }

        private Gang cachedPlayerGang;

        private int ticksSinceLastReward = 0;

        /// <summary>
        /// the number of currently alive members.
        /// (the number of entries in LivingMembers isn't the same as this)
        /// </summary>
        public int livingMembersCount = 0;

        public bool hasChangedBody = false;
        public bool hasDiedWithChangedBody = false;
        public Ped theOriginalPed;
        private int moneyFromLastProtagonist = 0;
        private int defaultMaxHealth = 200;

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

            defaultMaxHealth = Game.Player.Character.MaxHealth;

            gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (gangData == null)
            {
                gangData = new GangData();

                Gang playerGang = new Gang("Player's Gang", VehicleColor.BrushedGold, true);
                //setup gangs
                gangData.gangs.Add(playerGang);

                playerGang.blipColor = (int) BlipColor.Yellow;

                if (ModOptions.instance.gangsStartWithPistols)
                {
                    playerGang.gangWeaponHashes.Add(WeaponHash.Pistol);
                }

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

                    ////also, make the player gang friendly to mission characters
                    //for(int missionIndex = 2; missionIndex < 9; missionIndex++)
                    //{
                    //    int specialHash = Function.Call<int>(Hash.GET_HASH_KEY, "MISSION" + missionIndex.ToString());
                    //    World.SetRelationshipBetweenGroups(Relationship.Respect, gangData.gangs[i].relationGroupIndex, specialHash);
                    //    World.SetRelationshipBetweenGroups(Relationship.Respect, specialHash, gangData.gangs[i].relationGroupIndex);
                    //}
                    
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
                    if (enemyGangs.Count < ModOptions.instance.maxCoexistingGangs)
                    {
                        if (RandoMath.CachedRandom.Next(enemyGangs.Count) == 0)
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
                GiveTurfRewardToGang(PlayerGang);
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
                gangName = string.Concat(RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangFirstNames), " ",
                RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.memberColor gangColor = (PotentialGangMember.memberColor)RandoMath.CachedRandom.Next(9);

            Gang newGang = new Gang(gangName, RandoMath.GetRandomElementFromList(ModOptions.instance.GetGangColorTranslation(gangColor).vehicleColors), false, GetWealthiestGang().moneyAvailable * 2);

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

            newGang.GetPistolIfOptionsRequire();

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
            PotentialGangMember.dressStyle gangStyle = (PotentialGangMember.dressStyle)RandoMath.CachedRandom.Next(3);
            for (int i = 0; i < RandoMath.CachedRandom.Next(2, 6); i++)
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
                Gang createdGang = CreateNewEnemyGang(false);
                if (createdGang != null)
                {
                    enemyGangs.Add(new GangAI(createdGang));
                }
            }
            SaveGangData(false);
        }

        public void GiveTurfRewardToGang(Gang targetGang)
        {

            List<TurfZone> curGangZones = ZoneManager.instance.GetZonesControlledByGang(targetGang.name);
            if (targetGang.isPlayerOwned)
            {
                if (curGangZones.Count > 0)
                {
                    int rewardedCash = 0;

                    for (int i = 0; i < curGangZones.Count; i++)
                    {
                        int zoneReward = (int)((ModOptions.instance.baseRewardPerZoneOwned * 
                            (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count)) +
                            ((curGangZones[i].value + 1) * ModOptions.instance.baseRewardPerZoneOwned * 0.25f) );

                        AddOrSubtractMoneyToProtagonist(zoneReward);
                        
                        rewardedCash += zoneReward;
                    }
                    Function.Call(Hash.PLAY_SOUND, -1, "Virus_Eradicated", "LESTER1A_SOUNDS", 0, 0, 1);
                    UI.Notify("Money won from controlled zones: " + rewardedCash.ToString());
                }
            }
            else
            {
                for (int j = 0; j < curGangZones.Count; j++)
                {
                    targetGang.moneyAvailable += (int)((curGangZones[j].value + 1) *
                        ModOptions.instance.baseRewardPerZoneOwned *
                        (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count));
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

        #region Gang Upgrade/War Calculations

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

        public static int CalculateGangValueUpgradeCost(int currentGangValue)
        {
            return (currentGangValue + 1) * ModOptions.instance.baseCostToUpgradeGeneralGangTurfValue;
        }

        public static int CalculateTurfValueUpgradeCost(int currentTurfValue)
        {
            return (currentTurfValue + 1) * ModOptions.instance.baseCostToUpgradeSingleTurfValue;
        }

        public static int CalculateAttackCost(Gang attackerGang, GangWarManager.attackStrength attackType)
        {
            int attackTypeInt = (int) attackType;
            int pow2NonZeroAttackType = (attackTypeInt * attackTypeInt + 1);
            return ModOptions.instance.costToTakeTurf + ModOptions.instance.costToTakeTurf * attackTypeInt * attackTypeInt +
                attackerGang.GetFixedStrengthValue() * pow2NonZeroAttackType;
        }

        public static GangWarManager.attackStrength CalculateRequiredAttackStrength(Gang attackerGang, int defenderStrength)
        {
            GangWarManager.attackStrength requiredAtk = GangWarManager.attackStrength.light;

            int attackerGangStrength = attackerGang.GetFixedStrengthValue();

            for (int i = 0; i < 3; i++)
            {
                if (attackerGangStrength * (i * i + 1) > defenderStrength)
                {
                    break;
                }
                else
                {
                    requiredAtk++;
                }
            }

            return requiredAtk;
        }

        public static int CalculateAttackerReinforcements(Gang attackerGang, GangWarManager.attackStrength attackType)
        {
            return ModOptions.instance.extraKillsPerTurfValue * ((int) (attackType + 1) * (int) (attackType + 1)) +  ModOptions.instance.baseNumKillsBeforeWarVictory / 2 +
                attackerGang.GetReinforcementsValue() / 100;
        }

        public static int CalculateDefenderStrength(Gang defenderGang, TurfZone contestedZone)
        {
            return defenderGang.GetFixedStrengthValue() * contestedZone.value;
        }

        public static int CalculateDefenderReinforcements(Gang defenderGang, TurfZone targetZone)
        {
            return ModOptions.instance.extraKillsPerTurfValue * targetZone.value + ModOptions.instance.baseNumKillsBeforeWarVictory +
                defenderGang.GetReinforcementsValue() / 100;
        }

        /// <summary>
        /// uses the base reward for taking enemy turf (half if it was just a battle for defending)
        /// and the enemy strength (with variation) to define the "loot"
        /// </summary>
        /// <returns></returns>
        public static int CalculateBattleRewards(Gang ourEnemy, bool weWereAttacking)
        {
            int baseReward = ModOptions.instance.rewardForTakingEnemyTurf;
            if(weWereAttacking)
            {
                baseReward /= 2;
            }
            return baseReward + ourEnemy.GetGangVariedStrengthValue();
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
                    Game.Player.Character.CanRagdoll = true;
                    Function.Call((Hash)0xAE99FB955581844A, Game.Player.Character.Handle, -1, -1, 0, 0, 0, 0);
                    //Game.Player.Character.Euphoria.ShotFallToKnees.Start();
                }
                else
                {
                    if (!hasDiedWithChangedBody)
                    {
                        if (GangWarManager.instance.isOccurring)
                        {
                            GangWarManager.instance.OnAllyDeath(true);
                        }
                    }
                    hasDiedWithChangedBody = true;
                    //Game.Player.CanControlCharacter = false;
                    //Game.Player.Character.Euphoria.ShotFallToKnees.Start(20000);
                    Game.Player.Character.Weapons.Select(WeaponHash.Unarmed, true);
                    //in a war, this counts as a casualty in our team
                    
                    Function.Call((Hash)0xAE99FB955581844A, Game.Player.Character.Handle, -1, -1, 0, 0, 0, 0);
                    Game.Player.IgnoredByEveryone = true;
                }

                //RestorePlayerBody();
            }
        }

        public void TryBodyChange()
        {
            if (!hasChangedBody)
            {
                List<Ped> playerGangMembers = GetSpawnedMembersOfGang(PlayerGang);
                for (int i = 0; i < playerGangMembers.Count; i++)
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

                List<Ped> respawnOptions = GetSpawnedMembersOfGang(PlayerGang);

                for(int i = 0; i < respawnOptions.Count; i++)
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
                Ped spawnedPed = GangManager.instance.SpawnGangMember(GangManager.instance.PlayerGang,
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
            theOriginalPed.MaxHealth = defaultMaxHealth;
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
                oldPed.RelationshipGroup = PlayerGang.relationGroupIndex;
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

        public List<Ped> GetSpawnedMembersOfGang(Gang desiredGang)
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

            return returnedList;
        }

        public List<Ped> GetEnemyGangMembers(Gang friendlyGang)
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

            return returnedList;
        }

        public List<Ped> GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            foreach (Ped ped in detectedPeds)
            {
                if (referencePed.RelationshipGroup != ped.RelationshipGroup && ped.IsAlive)
                {
                    int pedRelation = (int) World.GetRelationshipBetweenGroups(ped.RelationshipGroup, referencePed.RelationshipGroup);
                    //if the relationship between them is hate or they were neutral and our reference ped has been hit by this ped...
                    if (pedRelation == 5 ||
                        (pedRelation >= 3 && referencePed.HasBeenDamagedBy(ped))) 
                    {
                        hostilePeds.Add(ped);
                    }
                }
               
            }
            return hostilePeds;
        }

        /// <summary>
        /// returns the gang with the most stocked money
        /// </summary>
        /// <returns></returns>
        public Gang GetWealthiestGang()
        {
            Gang pickedGang = null;

            for(int i = 0; i < gangData.gangs.Count; i++)
            {
                if (pickedGang != null) {
                    if (gangData.gangs[i].moneyAvailable > pickedGang.moneyAvailable)
                        pickedGang = gangData.gangs[i];
                }
                else
                {
                    pickedGang = gangData.gangs[i];
                }
            }

            return pickedGang;
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player or referencePosition (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember(Vector3? referencePosition = null)
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 referencePos = Game.Player.Character.Position;

            if(referencePosition != null)
            {
                referencePos = referencePosition.Value;
            }

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(World.GetNextPositionOnStreet(referencePos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableMemberSpawnDistance()));
            float distFromRef = World.GetDistance(referencePos, chosenPos);
            while ((distFromRef > ModOptions.instance.maxDistanceMemberSpawnFromPlayer ||
                distFromRef < ModOptions.instance.minDistanceMemberSpawnFromPlayer) && attempts <= 5)
            {
                // UI.Notify("too far"); or too close
                chosenPos = World.GetNextPositionOnSidewalk(referencePos + RandoMath.RandomDirection(true) * 
                    ModOptions.instance.GetAcceptableMemberSpawnDistance());
                distFromRef = World.GetDistance(referencePos, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor
        /// </summary>
        /// <returns></returns>
        public Vector3 FindCustomSpawnPoint(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            Vector3 chosenPos = Vector3.Zero;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(World.GetNextPositionOnStreet(referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference));
            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 5 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {
                // UI.Notify("too far"); or too close
                chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                    averageDistanceFromReference);
                distFromRef = World.GetDistance(referencePoint, chosenPos);
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
                          (playerPos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableCarSpawnDistance());
            float distFromPlayer = World.GetDistance(playerPos, chosenPos);

            while ((distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer) && attempts < 5)
            {
                // UI.Notify("too far"); or too close
                //just spawn it then, don't mind being on the street because the player might be on the mountains or the desert
                chosenPos = World.GetNextPositionOnSidewalk(playerPos + RandoMath.RandomDirection(true) *
                    ModOptions.instance.GetAcceptableCarSpawnDistance());
                distFromPlayer = World.GetDistance(playerPos, chosenPos);
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
                    RandoMath.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if(newPed != null)
                {
                    int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, newPed, 1);
                    //if we're not a legacy registration, set the head and hair data too
                    if(chosenMember.hairDrawableIndex != -1)
                    {
                        int randomHairTex = RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                            newPed, 2, chosenMember.hairDrawableIndex));
                        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 0, chosenMember.headDrawableIndex, chosenMember.headTextureIndex, pedPalette);
                        Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 2, chosenMember.hairDrawableIndex, randomHairTex, pedPalette);
                    }
                    
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 3, chosenMember.torsoDrawableIndex, chosenMember.torsoTextureIndex, pedPalette);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, newPed, 4, chosenMember.legsDrawableIndex, chosenMember.legsTextureIndex, pedPalette);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    newPed.Money = RandoMath.CachedRandom.Next(60);

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
                        newPed.Weapons.Give(RandoMath.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, false, true);
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
                Vehicle newVehicle = World.CreateVehicle(RandoMath.GetRandomElementFromList(ownerGang.carVariations).modelHash, spawnPos);
                if(newVehicle != null)
                {
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
        #endregion
    }

}
