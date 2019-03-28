using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Resolvers;

namespace GTA.GangAndTurfMod
{
    [System.Serializable]
    public class ModOptions
    {
        public static ModOptions instance;

        public ModOptions()
        {
            if(instance == null)
            {
                instance = this;
                LoadOptions();
                SetupPrimaryWeapons();
            }
        }

        public void LoadOptions()
        {
            ModOptions loadedOptions = PersistenceHandler.LoadFromFile<ModOptions>("ModOptions");
            if (loadedOptions != null)
            {
				//get the loaded options
				this.msAutoSaveInterval = loadedOptions.msAutoSaveInterval;

                this.gangMemberAggressiveness = loadedOptions.gangMemberAggressiveness;

                this.addToGroupKey = loadedOptions.addToGroupKey;
                this.mindControlKey = loadedOptions.mindControlKey;
                this.openGangMenuKey = loadedOptions.openGangMenuKey;
                this.openZoneMenuKey = loadedOptions.openZoneMenuKey;

                this.possibleGangFirstNames = loadedOptions.possibleGangFirstNames;
                this.possibleGangLastNames = loadedOptions.possibleGangLastNames;
                this.buyableWeapons = loadedOptions.buyableWeapons;
                this.similarColors = loadedOptions.similarColors;
                this.extraPlayerExclusiveColors = loadedOptions.extraPlayerExclusiveColors;

                this.startingGangMemberHealth = loadedOptions.startingGangMemberHealth;
                this.maxGangMemberHealth = loadedOptions.maxGangMemberHealth;
                this.maxGangMemberArmor = loadedOptions.maxGangMemberArmor;
                this.maxGangMemberAccuracy = loadedOptions.maxGangMemberAccuracy;

                this.emptyZoneDuringWar = loadedOptions.emptyZoneDuringWar;
                this.maxDistToWarBlipBeforePlayerLeavesWar = loadedOptions.maxDistToWarBlipBeforePlayerLeavesWar;
				this.ticksBeforeWarEndWithPlayerAway = loadedOptions.ticksBeforeWarEndWithPlayerAway;
				this.postWarBackupsAmount = loadedOptions.postWarBackupsAmount;
                this.baseNumKillsBeforeWarVictory = loadedOptions.baseNumKillsBeforeWarVictory;
                this.extraKillsPerTurfValue = loadedOptions.extraKillsPerTurfValue;
                this.killsBetweenEnemySpawnReplacement = loadedOptions.killsBetweenEnemySpawnReplacement;
                this.ticksBetweenEnemySpawnReplacement = loadedOptions.ticksBetweenEnemySpawnReplacement;

                this.msTimeBetweenTurfRewards = loadedOptions.msTimeBetweenTurfRewards;
                this.ticksBetweenGangAIUpdates = loadedOptions.ticksBetweenGangAIUpdates;
                this.minMsTimeBetweenAttacksOnPlayerTurf = loadedOptions.minMsTimeBetweenAttacksOnPlayerTurf;
                this.ticksBetweenGangMemberAIUpdates = loadedOptions.ticksBetweenGangMemberAIUpdates;
                this.baseRewardPerZoneOwned = loadedOptions.baseRewardPerZoneOwned;
                this.maxTurfValue = loadedOptions.maxTurfValue;

                this.rewardMultiplierPerZone = loadedOptions.rewardMultiplierPerZone;

                this.baseCostToTakeTurf = loadedOptions.baseCostToTakeTurf;
                this.rewardForTakingEnemyTurf = loadedOptions.rewardForTakingEnemyTurf;

                this.baseCostToUpgradeGeneralGangTurfValue = loadedOptions.baseCostToUpgradeGeneralGangTurfValue;
                this.baseCostToUpgradeSingleTurfValue = loadedOptions.baseCostToUpgradeSingleTurfValue;
                this.baseCostToUpgradeArmor = loadedOptions.baseCostToUpgradeArmor;
                this.baseCostToUpgradeHealth = loadedOptions.baseCostToUpgradeHealth;
                this.baseCostToUpgradeAccuracy = loadedOptions.baseCostToUpgradeAccuracy;

				this.wanderingDriverDrivingStyle = loadedOptions.wanderingDriverDrivingStyle;
				this.driverWithDestinationDrivingStyle = loadedOptions.driverWithDestinationDrivingStyle;

                this.numUpgradesUntilMaxMemberAttribute = loadedOptions.numUpgradesUntilMaxMemberAttribute;
                this.costToCallBackupCar = loadedOptions.costToCallBackupCar;
                this.costToCallParachutingMember = loadedOptions.costToCallParachutingMember;
                this.ticksCooldownBackupCar = loadedOptions.ticksCooldownBackupCar;
                this.ticksCooldownParachutingMember = loadedOptions.ticksCooldownParachutingMember;

                this.minWantedFactorWhenInGangTurf = loadedOptions.minWantedFactorWhenInGangTurf;
                this.maxWantedLevelInMaxedGangTurf = loadedOptions.maxWantedLevelInMaxedGangTurf;
				this.freezeWantedLevelDuringWars = loadedOptions.freezeWantedLevelDuringWars;

				this.notificationsEnabled = loadedOptions.notificationsEnabled;
				this.loggerEnabled = loadedOptions.loggerEnabled;
				this.preventAIExpansion = loadedOptions.preventAIExpansion;
                this.membersSpawnWithMeleeOnly = loadedOptions.membersSpawnWithMeleeOnly;
                this.warAgainstPlayerEnabled = loadedOptions.warAgainstPlayerEnabled;
                this.ambientSpawningEnabled = loadedOptions.ambientSpawningEnabled;
                this.forceSpawnCars = loadedOptions.forceSpawnCars;
                this.joypadControls = loadedOptions.joypadControls;

				this.showGangMemberBlips = loadedOptions.showGangMemberBlips;

                this.gangsStartWithPistols = loadedOptions.gangsStartWithPistols;
                this.gangsCanBeWipedOut = loadedOptions.gangsCanBeWipedOut;
                this.maxCoexistingGangs = loadedOptions.maxCoexistingGangs;
                this.extraProfitForAIGangsFactor = loadedOptions.extraProfitForAIGangsFactor;
                this.spawnedMembersBeforeAmbientGenStops = loadedOptions.spawnedMembersBeforeAmbientGenStops;
                this.msBaseIntervalBetweenAmbientSpawns = loadedOptions.msBaseIntervalBetweenAmbientSpawns;
                this.spawnedMemberLimit = loadedOptions.spawnedMemberLimit;
                this.numSpawnsReservedForCarsDuringWars = loadedOptions.numSpawnsReservedForCarsDuringWars;
                this.minDistanceCarSpawnFromPlayer = loadedOptions.minDistanceCarSpawnFromPlayer;
                this.minDistanceMemberSpawnFromPlayer = loadedOptions.minDistanceMemberSpawnFromPlayer;
                this.maxDistanceCarSpawnFromPlayer = loadedOptions.maxDistanceCarSpawnFromPlayer;
                this.maxDistanceMemberSpawnFromPlayer = loadedOptions.maxDistanceMemberSpawnFromPlayer;

                if (similarColors[0].blipColors == null)
                {
                    SetColorTranslationDefaultValues();
                }
                SaveOptions();
            }
            else
            {
                SetAllValuesToDefault();
                SetNameListsDefaultValues();
                SetColorTranslationDefaultValues();
                PersistenceHandler.SaveToFile(this, "ModOptions");
            }
        }

        public void SaveOptions(bool notifyMsg = true)
        {
            PersistenceHandler.SaveToFile(this, "ModOptions", notifyMsg);
        }

        public Keys openGangMenuKey = Keys.B,
            openZoneMenuKey = Keys.N,
            mindControlKey = Keys.J,
            addToGroupKey = Keys.H;

        public enum GangMemberAggressivenessMode
        {
            veryAgressive = 0, //legacy typos... hahaha
			veryAggressive = 0,
            agressive = 1,
			aggressive = 1,
            defensive = 2
        }

		public int msAutoSaveInterval = 3000;

        public GangMemberAggressivenessMode gangMemberAggressiveness = GangMemberAggressivenessMode.veryAgressive;

        public int startingGangMemberHealth = 20;
        public int maxGangMemberHealth = 120;
        public int maxGangMemberArmor = 100;
        public int maxGangMemberAccuracy = 30;

        public bool emptyZoneDuringWar = true;
        public int maxDistToWarBlipBeforePlayerLeavesWar = 300;
		public int ticksBeforeWarEndWithPlayerAway = 30000;
		public int postWarBackupsAmount = 5;
        public int baseNumKillsBeforeWarVictory = 25;
        public int extraKillsPerTurfValue = 15;
        public int killsBetweenEnemySpawnReplacement = 25;
        public int ticksBetweenEnemySpawnReplacement = 3600;

        public int msTimeBetweenTurfRewards = 180000;
        public int ticksBetweenGangAIUpdates = 15000;
        public int minMsTimeBetweenAttacksOnPlayerTurf = 600000;
        public int ticksBetweenGangMemberAIUpdates = 100;
        public int baseRewardPerZoneOwned = 1200;
        public int maxTurfValue = 10;

        /// <summary>
        /// percentage sum, per zone owned, over the total reward received.
        /// for example, if the gang owns 2 zones and the multiplier is 0.2, the reward percentage will be 140%
        /// </summary>
        public float rewardMultiplierPerZone = 0.2f;

        public int baseCostToTakeTurf = 4000;
        public int rewardForTakingEnemyTurf = 5000;

        public int baseCostToUpgradeGeneralGangTurfValue = 1000000;
        public int baseCostToUpgradeSingleTurfValue = 15000;
        public int baseCostToUpgradeArmor = 35000;
        public int baseCostToUpgradeHealth = 20000;
        public int baseCostToUpgradeAccuracy = 40000;

		//special thanks to Eddlm for the driving style data! 
		//more info here: https://gtaforums.com/topic/822314-guide-driving-styles/
		public int wanderingDriverDrivingStyle = 1 + 2 + 8 + 32 + 128 + 256;
		public int driverWithDestinationDrivingStyle = 2 + 4 + 8 + 32 + 512 + 262144;

		public int numUpgradesUntilMaxMemberAttribute = 10;
        public int costToCallBackupCar = 900;
        public int costToCallParachutingMember = 250;
        public int ticksCooldownBackupCar = 1000;
        public int ticksCooldownParachutingMember = 600;

        public bool notificationsEnabled = true;
		public bool loggerEnabled = false;
        public bool preventAIExpansion = true, membersSpawnWithMeleeOnly = false, warAgainstPlayerEnabled = true, ambientSpawningEnabled = true;
        public bool forceSpawnCars = false;
        public bool joypadControls = false;

		public bool showGangMemberBlips = true;

        public float minWantedFactorWhenInGangTurf = 0.0f;
        public int maxWantedLevelInMaxedGangTurf = 0;
		public bool freezeWantedLevelDuringWars = true;

		public bool gangsStartWithPistols = true;
        public bool gangsCanBeWipedOut = true;
        public int maxCoexistingGangs = 7;
        public float extraProfitForAIGangsFactor = 1.5f;
        public int spawnedMembersBeforeAmbientGenStops = 20;
        public int msBaseIntervalBetweenAmbientSpawns = 15000;
        public int spawnedMemberLimit = 30; //max number of living gang members at any time
        public int numSpawnsReservedForCarsDuringWars = 1;
        public int minDistanceMemberSpawnFromPlayer = 50;
        public int maxDistanceMemberSpawnFromPlayer = 130;
        public int minDistanceCarSpawnFromPlayer = 80;
        public int maxDistanceCarSpawnFromPlayer = 190;

        [XmlIgnore]
        public List<WeaponHash> primaryWeapons = new List<WeaponHash>();

        [XmlIgnore]
        public List<WeaponHash> driveByWeapons = new List<WeaponHash>()
        {
            WeaponHash.APPistol,
            WeaponHash.CombatPistol,
            WeaponHash.HeavyPistol,
            WeaponHash.MachinePistol,
            WeaponHash.MarksmanPistol,
            WeaponHash.Pistol,
            WeaponHash.PistolMk2,
            WeaponHash.Pistol50,
            WeaponHash.Revolver,
            WeaponHash.DoubleActionRevolver,
            WeaponHash.RevolverMk2,
            WeaponHash.SawnOffShotgun,
            WeaponHash.SNSPistol,
            WeaponHash.SNSPistolMk2,
            WeaponHash.VintagePistol,
            WeaponHash.MicroSMG
        };

        [XmlIgnore]
        public List<WeaponHash> meleeWeapons = new List<WeaponHash>()
        {
            WeaponHash.Bat,
            WeaponHash.BattleAxe,
            WeaponHash.Bottle,
            WeaponHash.Crowbar,
            WeaponHash.Dagger,
            WeaponHash.GolfClub,
            WeaponHash.Hammer,
            WeaponHash.Hatchet,
            WeaponHash.Knife,
            WeaponHash.KnuckleDuster,
            WeaponHash.Machete,
            WeaponHash.Nightstick,
            WeaponHash.PoolCue,
            WeaponHash.SwitchBlade,
            WeaponHash.Wrench,
        };

        public List<BuyableWeapon> buyableWeapons;

        public List<string> possibleGangFirstNames;

        public List<string> possibleGangLastNames;

        public List<GangColorTranslation> similarColors;

        public List<VehicleColor> extraPlayerExclusiveColors;

        //XMLserializer does not like dictionaries
        public class BuyableWeapon
        {
            public WeaponHash wepHash;
            public int price;

            public BuyableWeapon()
            {
                wepHash = WeaponHash.SNSPistol;
                price = 500;
            }

            public BuyableWeapon(WeaponHash wepHash, int price)
            {
                this.wepHash = wepHash;
                this.price = price;
            }
        }

        public class GangColorTranslation
        {
            public List<VehicleColor> vehicleColors;
            public PotentialGangMember.MemberColor baseColor;
            public int[] blipColors;

            public GangColorTranslation()
            {
                vehicleColors = new List<VehicleColor>();
            }

            public GangColorTranslation(PotentialGangMember.MemberColor baseColor, List<VehicleColor> vehicleColors, int[] blipColors)
            {
                this.baseColor = baseColor;
                this.vehicleColors = vehicleColors;
                this.blipColors = blipColors;
            }
        }

        #region getters

        public BuyableWeapon GetBuyableWeaponByHash(WeaponHash wepHash)
        {
            for (int i = 0; i < buyableWeapons.Count; i++)
            {
                if (buyableWeapons[i].wepHash == wepHash)
                {
                    return buyableWeapons[i];
                }
            }

            return null;
        }

        public GangColorTranslation GetGangColorTranslation(PotentialGangMember.MemberColor baseColor)
        {
            for (int i = 0; i < similarColors.Count; i++)
            {
                if (similarColors[i].baseColor == baseColor)
                {
                    return similarColors[i];
                }
            }

            return null;
        }

        /// <summary>
        /// returns a random distance between the minimum and maximum distances that a member can spawn from the player
        /// </summary>
        /// <returns></returns>
        public int GetAcceptableMemberSpawnDistance(int paddingFromLimits = 0)
        {
            if (maxDistanceMemberSpawnFromPlayer <= minDistanceMemberSpawnFromPlayer)
            {
                maxDistanceMemberSpawnFromPlayer = minDistanceMemberSpawnFromPlayer + 2;
                SaveOptions(false);
            }
            return RandoMath.CachedRandom.Next(minDistanceMemberSpawnFromPlayer,
				RandoMath.Max(minDistanceCarSpawnFromPlayer + paddingFromLimits, maxDistanceMemberSpawnFromPlayer - paddingFromLimits));
        }

        /// <summary>
        /// returns a random distance between the minimum and maximum distances that a car can spawn from the player
        /// </summary>
        /// <returns></returns>
        public int GetAcceptableCarSpawnDistance()
        {
            if(maxDistanceCarSpawnFromPlayer <= minDistanceCarSpawnFromPlayer)
            {
                maxDistanceCarSpawnFromPlayer = minDistanceCarSpawnFromPlayer + 2;
                SaveOptions(false);
            }
            return RandoMath.CachedRandom.Next(minDistanceCarSpawnFromPlayer, maxDistanceCarSpawnFromPlayer);
        }

        /// <summary>
        /// gets how much the member accuracy increases with each upgrade (this depends on maxGangMemberAccuracy and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetAccuracyUpgradeIncrement()
        {
            return RandoMath.Max(1, maxGangMemberAccuracy / numUpgradesUntilMaxMemberAttribute);
        }

        /// <summary>
        /// gets how much the member health increases with each upgrade (this depends on maxGangMemberHealth and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetHealthUpgradeIncrement()
        {
            return RandoMath.Max(1, maxGangMemberHealth / numUpgradesUntilMaxMemberAttribute);
        }

        /// <summary>
        /// gets how much the member armor increases with each upgrade (this depends on maxGangMemberArmor and numUpgradesUntilMaxMemberAttribute)
        /// </summary>
        /// <returns></returns>
        public int GetArmorUpgradeIncrement()
        {
            return RandoMath.Max(1, maxGangMemberArmor / numUpgradesUntilMaxMemberAttribute);
        }

        /// <summary>
        /// gets a weapon from a list and check if it is in the buyables list.
        /// if it isn't, get another or get a random one from the buyables
        /// </summary>
        /// <param name="theWeaponList"></param>
        /// <returns></returns>
        public WeaponHash GetWeaponFromListIfBuyable(List<WeaponHash> theWeaponList)
        {
            for (int attempts = 0; attempts < 5; attempts++)
            {
                WeaponHash chosenWeapon = RandoMath.GetRandomElementFromList(theWeaponList);
                if (GetBuyableWeaponByHash(chosenWeapon) != null)
                {
                    return chosenWeapon;
                }
            }

            return RandoMath.GetRandomElementFromList(buyableWeapons).wepHash;
        }

        #endregion

        public PotentialGangMember.MemberColor TranslateVehicleToMemberColor(VehicleColor vehColor)
        {
            for (int i = 0; i < similarColors.Count; i++)
            {
                if (similarColors[i].vehicleColors.Contains(vehColor))
                {
                    return similarColors[i].baseColor;
                }
            }

            return PotentialGangMember.MemberColor.white;
        }

        public void SetupPrimaryWeapons()
        {
            if (primaryWeapons != null)
            {
                primaryWeapons.Clear();
            }
            else
            {
                primaryWeapons = new List<WeaponHash>();
            }

            if (buyableWeapons == null)
            {
                SetWeaponListDefaultValues();
                SaveOptions(false);
            }
            //primary weapons are the ones that are not melee and cannot be used to drive-by (the bigger weapons, like rifles)
            for(int i = 0; i < buyableWeapons.Count; i++)
            {
                if(!meleeWeapons.Contains(buyableWeapons[i].wepHash) &&
                    !driveByWeapons.Contains(buyableWeapons[i].wepHash)){
                    primaryWeapons.Add(buyableWeapons[i].wepHash);
                }
            }
        }

        public void SetKey(MenuScript.ChangeableKeyBinding keyToChange, Keys newKey)
        {
            if(newKey == Keys.Escape || newKey == Keys.ShiftKey ||
                newKey == Keys.Insert || newKey == Keys.ControlKey)
            {
                UI.ShowSubtitle("That key can't be used because some settings would become unaccessible due to conflicts.");
                return;
            }

			//verify if this key isn't being used by the other commands from this mod
			//if not, set the chosen key as the new one for the command!
			List<Keys> curKeys = new List<Keys> {
				openGangMenuKey,
				openZoneMenuKey,
				mindControlKey,
				addToGroupKey
			};

			if (curKeys.Contains(newKey))
            {
                UI.ShowSubtitle("That key is already being used by this mod's commands.");
                return;
            }
            else
            {
                switch (keyToChange)
                {
                    case MenuScript.ChangeableKeyBinding.AddGroupBtn:
                        addToGroupKey = newKey;
                        break;
                    case MenuScript.ChangeableKeyBinding.GangMenuBtn:
                        openGangMenuKey = newKey;
                        break;
                    case MenuScript.ChangeableKeyBinding.MindControlBtn:
                        mindControlKey = newKey;
                        break;
                    case MenuScript.ChangeableKeyBinding.ZoneMenuBtn:
                        openZoneMenuKey = newKey;
                        break;
                }

                UI.ShowSubtitle("Key changed!");
                SaveOptions();
            }
        }

        public void SetMemberAggressiveness(GangMemberAggressivenessMode newMode)
        {
            gangMemberAggressiveness = newMode;
			GangManager.instance.SetGangRelationsAccordingToAggrLevel(newMode);
            //makes everyone hate cops if set to very aggressive
            GangManager.instance.SetCopRelations(newMode == GangMemberAggressivenessMode.veryAgressive);
            MenuScript.instance.aggOption.Index = (int)newMode;

            SaveOptions(false);
        }

        /// <summary>
        /// resets all values, except for the first and last gang names and the color translations
        /// </summary>
        public void SetAllValuesToDefault()
        {
			msAutoSaveInterval = 3000;

            openGangMenuKey = Keys.B;
            openZoneMenuKey = Keys.N;
            mindControlKey = Keys.J;
            addToGroupKey = Keys.H;

            gangMemberAggressiveness = GangMemberAggressivenessMode.veryAgressive;

            startingGangMemberHealth = 20;
            maxGangMemberHealth = 120;
            maxGangMemberArmor = 100;
            maxGangMemberAccuracy = 30;

            emptyZoneDuringWar = true;
            maxDistToWarBlipBeforePlayerLeavesWar = 300;
			ticksBeforeWarEndWithPlayerAway = 30000;
            postWarBackupsAmount = 5;
            baseNumKillsBeforeWarVictory = 25;
            extraKillsPerTurfValue = 15;
            killsBetweenEnemySpawnReplacement = 25;
            ticksBetweenEnemySpawnReplacement = 3600;

            msTimeBetweenTurfRewards = 180000;
            ticksBetweenGangAIUpdates = 15000;
			minMsTimeBetweenAttacksOnPlayerTurf = 600000;
            
            ticksBetweenGangMemberAIUpdates = 100;
            baseRewardPerZoneOwned = 1200;
            maxTurfValue = 10;

            rewardMultiplierPerZone = 0.2f;

            baseCostToTakeTurf = 4000;
            rewardForTakingEnemyTurf = 5000;

            baseCostToUpgradeGeneralGangTurfValue = 1000000;
            baseCostToUpgradeSingleTurfValue = 15000;
            baseCostToUpgradeArmor = 35000;
            baseCostToUpgradeHealth = 20000;
            baseCostToUpgradeAccuracy = 40000;

			wanderingDriverDrivingStyle = 1 + 2 + 8 + 16 + 32 + 128 + 256;
			driverWithDestinationDrivingStyle = 4 + 8 + 16 + 32 + 512 + 262144;

			numUpgradesUntilMaxMemberAttribute = 10;
            costToCallBackupCar = 900;
            costToCallParachutingMember = 250;
            ticksCooldownBackupCar = 1000;
            ticksCooldownParachutingMember = 600;

            minWantedFactorWhenInGangTurf = 0.0f;
            maxWantedLevelInMaxedGangTurf = 0;
			freezeWantedLevelDuringWars = true;

            notificationsEnabled = true;
			loggerEnabled = false;

            preventAIExpansion = true;
            membersSpawnWithMeleeOnly = false;
            warAgainstPlayerEnabled = true;
            ambientSpawningEnabled = true;
            forceSpawnCars = false;
            joypadControls = false;

			showGangMemberBlips = true;

            gangsStartWithPistols = true;
            gangsCanBeWipedOut = true;
            maxCoexistingGangs = 7;
            extraProfitForAIGangsFactor = 1.5f;
            spawnedMembersBeforeAmbientGenStops = 20;
            msBaseIntervalBetweenAmbientSpawns = 15000;
            spawnedMemberLimit = 30; //max number of living gang members at any time
            numSpawnsReservedForCarsDuringWars = 1;
            minDistanceMemberSpawnFromPlayer = 50;
            maxDistanceMemberSpawnFromPlayer = 130;
            minDistanceCarSpawnFromPlayer = 80;
            maxDistanceCarSpawnFromPlayer = 190;

            SaveOptions();

            GangManager.instance.ResetGangUpdateIntervals();
        }

        public void SetWeaponListDefaultValues()
        {
            buyableWeapons = new List<BuyableWeapon>()
        {
            //--melee
			
            new BuyableWeapon(WeaponHash.Bat, 1000),
            new BuyableWeapon(WeaponHash.BattleAxe, 4500),
            new BuyableWeapon(WeaponHash.Bottle, 500),
            new BuyableWeapon(WeaponHash.Crowbar, 800),
            new BuyableWeapon(WeaponHash.Dagger, 4000),
            new BuyableWeapon(WeaponHash.GolfClub, 3000),
            new BuyableWeapon(WeaponHash.Hammer, 800),
            new BuyableWeapon(WeaponHash.Hatchet, 1100),
            new BuyableWeapon(WeaponHash.Knife, 1000),
            new BuyableWeapon(WeaponHash.KnuckleDuster, 650),
            new BuyableWeapon(WeaponHash.Machete, 1050),
            new BuyableWeapon(WeaponHash.Nightstick, 700),
            new BuyableWeapon(WeaponHash.PoolCue, 730),
            new BuyableWeapon(WeaponHash.SwitchBlade, 1100),
            new BuyableWeapon(WeaponHash.Wrench, 560),
			//--guns
            new BuyableWeapon(WeaponHash.AdvancedRifle, 200000),
            new BuyableWeapon(WeaponHash.APPistol, 60000),
            new BuyableWeapon(WeaponHash.AssaultRifle, 120000),
            new BuyableWeapon(WeaponHash.AssaultrifleMk2, 195000),
            new BuyableWeapon(WeaponHash.AssaultShotgun, 250000),
            new BuyableWeapon(WeaponHash.AssaultSMG, 190000),
            new BuyableWeapon(WeaponHash.BullpupRifle, 230000),
            new BuyableWeapon(WeaponHash.BullpupRifleMk2, 285000),
            new BuyableWeapon(WeaponHash.BullpupShotgun, 265000),
            new BuyableWeapon(WeaponHash.CarbineRifle, 150000),
            new BuyableWeapon(WeaponHash.CarbineRifleMk2, 210000),
            new BuyableWeapon(WeaponHash.CombatMG, 220000),
            new BuyableWeapon(WeaponHash.CombatMGMk2, 245000),
            new BuyableWeapon(WeaponHash.CombatPDW, 205000),
            new BuyableWeapon(WeaponHash.CombatPistol, 50000),
			new BuyableWeapon(WeaponHash.CompactGrenadeLauncher, 1000000),
			new BuyableWeapon(WeaponHash.CompactRifle, 175000),
            new BuyableWeapon(WeaponHash.DoubleActionRevolver, 120000),
            new BuyableWeapon(WeaponHash.DoubleBarrelShotgun, 210000),
            new BuyableWeapon(WeaponHash.Firework, 1000000),
            new BuyableWeapon(WeaponHash.FlareGun, 600000),
			new BuyableWeapon(WeaponHash.GrenadeLauncher, 950000),
            new BuyableWeapon(WeaponHash.Gusenberg, 200000),
            new BuyableWeapon(WeaponHash.HeavyPistol, 55000),
            new BuyableWeapon(WeaponHash.HeavyShotgun, 180000),
            new BuyableWeapon(WeaponHash.HeavySniper, 300000),
            new BuyableWeapon(WeaponHash.HeavySniperMk2, 380000),
            new BuyableWeapon(WeaponHash.HomingLauncher, 2100000),
            new BuyableWeapon(WeaponHash.MachinePistol, 65000),
            new BuyableWeapon(WeaponHash.MarksmanPistol, 50000),
            new BuyableWeapon(WeaponHash.MarksmanRifle, 250000),
            new BuyableWeapon(WeaponHash.MarksmanRifleMk2, 310000),
            new BuyableWeapon(WeaponHash.MG, 290000),
            new BuyableWeapon(WeaponHash.MicroSMG, 90000),
            new BuyableWeapon(WeaponHash.Minigun, 400000),
            new BuyableWeapon(WeaponHash.MiniSMG, 100000),
            new BuyableWeapon(WeaponHash.Musket, 70000),
            new BuyableWeapon(WeaponHash.Pistol, 30000),
            new BuyableWeapon(WeaponHash.Pistol50, 70000),
            new BuyableWeapon(WeaponHash.PistolMk2, 65000),
            new BuyableWeapon(WeaponHash.PumpShotgun, 100000),
            new BuyableWeapon(WeaponHash.PumpShotgunMk2, 135000),
			new BuyableWeapon(WeaponHash.Railgun, 5100000),
            new BuyableWeapon(WeaponHash.Revolver, 80000),
            new BuyableWeapon(WeaponHash.RevolverMk2, 100000),
            new BuyableWeapon(WeaponHash.RPG, 1200000),
            new BuyableWeapon(WeaponHash.SawnOffShotgun, 95000),
            new BuyableWeapon(WeaponHash.SMG, 115000),
            new BuyableWeapon(WeaponHash.SMGMk2, 155000),
            new BuyableWeapon(WeaponHash.SniperRifle, 230000),
            new BuyableWeapon(WeaponHash.SNSPistol, 27000),
            new BuyableWeapon(WeaponHash.SNSPistolMk2, 38000),
            new BuyableWeapon(WeaponHash.SpecialCarbine, 230000),
            new BuyableWeapon(WeaponHash.SpecialCarbineMk2, 290000),
            new BuyableWeapon(WeaponHash.StunGun, 45000),
            new BuyableWeapon(WeaponHash.SweeperShotgun, 230000),
			new BuyableWeapon(WeaponHash.UnholyHellbringer, 5100000),
			new BuyableWeapon(WeaponHash.UpNAtomizer, 4100000),
			new BuyableWeapon(WeaponHash.VintagePistol, 50000),
			new BuyableWeapon(WeaponHash.Widowmaker, 5100000),
		};
        }


        public void SetNameListsDefaultValues()
        {

            possibleGangFirstNames = new List<string>
            {
                "666",
                "American",
                "Angry",
                "Artful",
                "Beach",
                "Big",
                "Bloody",
                "Brazilian",
                "Bright",
                "Brilliant",
                "Business",
                "Canadian",
                "Chemical",
                "Chinese",
                "Colombian",
                "Corrupt",
                "Countryside",
                "Crazy",
                "Cursed",
                "Cute",
                "Desert",
                "Dishonored",
                "Disillusioned",
                "Egyptian",
                "Electric",
                "Epic",
                "Fake",
                "Fallen",
                "Fire",
                "Forbidden",
                "Forgotten",
                "French",
                "Gold",
                "Gothic",
                "Grave",
                "Greedy",
                "Greek",
                "Happy",
                "High",
                "High Poly",
                "Holy",
                "Ice",
                "Ice Cold",
                "Indian",
                "Irish",
                "Iron",
                "Italian",
                "Japanese",
                "Killer",
                "Laser",
                "Laughing",
                "Legendary",
                "Lordly",
                "Lost",
                "Low Poly",
                "Magic",
                "Manic",
                "Mercenary",
                "Merciless",
                "Mexican",
                "Miami",
                "Mighty",
                "Mountain",
                "Neon",
                "New",
                "New Wave",
                "Night",
                "Nihilist",
                "Nordic",
                "Original",
                "Power",
                "Poisonous",
                "Rabid",
                "Roman",
                "Robot",
                "Rocket",
                "Russian",
                "Sad",
                "Scottish",
                "Seaside",
                "Serious",
                "Shadowy",
                "Silver",
                "Snow",
                "Soviet",
                "Steel",
                "Street",
                "Swedish",
                "Sweet",
                "Tundra",
                "Turkish",
                "Vicious",
                "Vigilant",
                "Wise",
            };

            possibleGangLastNames = new List<string>
            {
                "Bandits",
                "Barbarians",
                "Bears",
                "Cats",
                "Champions",
                "Company",
                "Coyotes",
                "Dealers",
                "Dogs",
                "Eliminators",
                "Fighters",
                "Friends",
                "Gang",
                "Gangsters",
                "Ghosts",
                "Gringos",
                "Group",
                "Gunners",
                "Hobos",
                "Hookers",
                "Hunters",
                "Industry Leaders",
                "Infiltrators",
                "Invaders",
                "Kittens",
                "League",
                "Mafia",
                "Militia",
                "Mob",
                "Mobsters",
                "Monsters",
                "Murderers",
                "Pegasi",
                "People",
                "Pirates",
                "Puppies",
                "Raiders",
                "Reapers",
                "Robbers",
                "Sailors",
                "Sharks",
                "Skull",
                "Soldiers",
                "Sword",
                "Thieves",
                "Tigers",
                "Triad",
                "Unicorns",
                "Vice",
                "Vigilantes",
                "Vikings",
                "Warriors",
                "Watchers",
                "Wolves",
                "Zaibatsu",
            };
        }

        public void SetColorTranslationDefaultValues()
        {
                similarColors = new List<GangColorTranslation>
            {
                new GangColorTranslation(PotentialGangMember.MemberColor.black, new List<VehicleColor> {
                     VehicleColor.BrushedBlackSteel,
                    VehicleColor.MatteBlack,
                    VehicleColor.MetallicBlack,
                    VehicleColor.MetallicGraphiteBlack,
                    VehicleColor.UtilBlack,
                    VehicleColor.WornBlack,
                    VehicleColor.ModshopBlack1
                }, new int[]{40}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.blue, new List<VehicleColor> {
                     VehicleColor.Blue,
                    VehicleColor.EpsilonBlue,
                    VehicleColor.MatteBlue,
                    VehicleColor.MatteDarkBlue,
                    VehicleColor.MatteMidnightBlue,
                    VehicleColor.MetaillicVDarkBlue,
                    VehicleColor.MetallicBlueSilver,
                    VehicleColor.MetallicBrightBlue,
                    VehicleColor.MetallicDarkBlue,
                    VehicleColor.MetallicDiamondBlue,
                    VehicleColor.MetallicHarborBlue,
                    VehicleColor.MetallicMarinerBlue,
                    VehicleColor.UtilBlue,
                    VehicleColor.MetallicUltraBlue
                }, new int[]{3, 12, 15, 18, 26, 30, 38, 42, 54, 57, 63, 67, 68, 74, 77, 78, 84}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.green, new List<VehicleColor> {
                     VehicleColor.Green,
                    VehicleColor.HunterGreen,
                    VehicleColor.MatteFoliageGreen,
                    VehicleColor.MatteForestGreen,
                    VehicleColor.MatteGreen,
                    VehicleColor.MatteLimeGreen,
                    VehicleColor.MetallicDarkGreen,
                    VehicleColor.MetallicGreen,
                    VehicleColor.MetallicRacingGreen,
                    VehicleColor.UtilDarkGreen,
                    VehicleColor.MetallicOliveGreen,
                    VehicleColor.WornGreen,
                }, new int[]{2, 11, 25, 43, 52, 69, 82}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.pink, new List<VehicleColor> {
                     VehicleColor.HotPink,
                    VehicleColor.MetallicVermillionPink,
                }, new int[]{8, 23, 34, 35, 41, 48, 61 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.purple, new List<VehicleColor> {
                     VehicleColor.MatteDarkPurple,
                    VehicleColor.MattePurple,
                    VehicleColor.MetallicPurple,
                    VehicleColor.MetallicPurpleBlue,
                }, new int[]{19, 7, 27, 50, 58, 83}
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.red, new List<VehicleColor> {
                     VehicleColor.MatteDarkRed,
                    VehicleColor.MatteRed,
                    VehicleColor.MetallicBlazeRed,
                    VehicleColor.MetallicCabernetRed,
                    VehicleColor.MetallicCandyRed,
                    VehicleColor.MetallicDesertRed,
                    VehicleColor.MetallicFormulaRed,
                    VehicleColor.MetallicGarnetRed,
                    VehicleColor.MetallicGracefulRed,
                    VehicleColor.MetallicLavaRed,
                    VehicleColor.MetallicRed,
                    VehicleColor.MetallicTorinoRed,
                    VehicleColor.UtilBrightRed,
                    VehicleColor.UtilGarnetRed,
                    VehicleColor.UtilRed,
                    VehicleColor.WornDarkRed,
                    VehicleColor.WornGoldenRed,
                    VehicleColor.WornRed,
                }, new int[]{1, 6, 49, 59, 75, 76 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.white, new List<VehicleColor> {
                     VehicleColor.MatteWhite,
                    VehicleColor.MetallicFrostWhite,
                    VehicleColor.MetallicWhite,
                    VehicleColor.PureWhite,
                    VehicleColor.UtilOffWhite,
                    VehicleColor.WornOffWhite,
                    VehicleColor.WornWhite,
                    VehicleColor.MetallicDarkIvory,
                }, new int[]{0, 4, 13, 37, 45 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.yellow, new List<VehicleColor> {
                     VehicleColor.MatteYellow,
                    VehicleColor.MetallicRaceYellow,
                    VehicleColor.MetallicTaxiYellow,
                    VehicleColor.MetallicYellowBird,
                    VehicleColor.WornTaxiYellow,
                    VehicleColor.BrushedGold,
                    VehicleColor.MetallicClassicGold,
                    VehicleColor.PureGold,
                    VehicleColor.MetallicGoldenBrown,
                    VehicleColor.MatteDesertTan,
                }, new int[]{66, 5, 28, 16, 36, 33, 46, 56, 60, 70, 71, 73, 81 }
               ),
                new GangColorTranslation(PotentialGangMember.MemberColor.gray, new List<VehicleColor> {
                     VehicleColor.MatteGray,
                   VehicleColor.MatteLightGray,
                   VehicleColor.MetallicAnthraciteGray,
                   VehicleColor.MetallicSteelGray,
                   VehicleColor.WornSilverGray,
                   VehicleColor.BrushedAluminium,
                    VehicleColor.BrushedSteel,
                    VehicleColor.Chrome,
                    VehicleColor.WornGraphite,
                    VehicleColor.WornShadowSilver,
                    VehicleColor.MetallicDarkSilver,
                    VehicleColor.MetallicMidnightSilver,
                    VehicleColor.MetallicShadowSilver,
                }, new int[]{20, 39, 55, 65}
               )
            };

            extraPlayerExclusiveColors = new List<VehicleColor>()
            {
                VehicleColor.MatteOliveDrab,
                VehicleColor.MatteOrange,
                VehicleColor.MetallicBeachSand,
                VehicleColor.MetallicBeechwood,
                VehicleColor.MetallicBistonBrown,
                VehicleColor.MetallicBronze,
                VehicleColor.MetallicChampagne,
                VehicleColor.MetallicChocoBrown,
                VehicleColor.MetallicChocoOrange,
                VehicleColor.MetallicCream,
                VehicleColor.MetallicDarkBeechwood,
                VehicleColor.MetallicGunMetal,
                VehicleColor.MetallicLime,
                VehicleColor.MetallicMossBrown,
                VehicleColor.MetallicOrange,
                VehicleColor.MetallicPuebloBeige,
                VehicleColor.MetallicStrawBeige,
                VehicleColor.MetallicSunBleechedSand,
                VehicleColor.MetallicSunriseOrange,
                VehicleColor.Orange,
                VehicleColor.WornLightOrange,
                VehicleColor.WornOrange,
                VehicleColor.WornSeaWash,
            };
        }
    }
}
