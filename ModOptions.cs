using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using System.Windows.Forms;
using System.Xml.Serialization;

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

                this.maxGangMemberHealth = loadedOptions.maxGangMemberHealth;
                this.maxGangMemberArmor = loadedOptions.maxGangMemberArmor;
                this.maxGangMemberAccuracy = loadedOptions.maxGangMemberAccuracy;

                this.emptyZoneDuringWar = loadedOptions.emptyZoneDuringWar;
                this.baseNumKillsBeforeWarVictory = loadedOptions.baseNumKillsBeforeWarVictory;
                this.extraKillsPerTurfValue = loadedOptions.extraKillsPerTurfValue;

                this.ticksBetweenTurfRewards = loadedOptions.ticksBetweenTurfRewards;
                this.ticksBetweenGangAIUpdates = loadedOptions.ticksBetweenGangAIUpdates;
                this.ticksBetweenGangMemberAIUpdates = loadedOptions.ticksBetweenGangMemberAIUpdates;
                this.baseRewardPerZoneOwned = loadedOptions.baseRewardPerZoneOwned;
                this.maxTurfValue = loadedOptions.maxTurfValue;

                this.rewardMultiplierPerZone = loadedOptions.rewardMultiplierPerZone;

                this.costToTakeTurf = loadedOptions.costToTakeTurf;
                this.rewardForTakingEnemyTurf = loadedOptions.rewardForTakingEnemyTurf;

                this.baseCostToUpgradeGeneralGangTurfValue = loadedOptions.baseCostToUpgradeGeneralGangTurfValue;
                this.baseCostToUpgradeSingleTurfValue = loadedOptions.baseCostToUpgradeSingleTurfValue;
                this.costToCallBackupCar = loadedOptions.costToCallBackupCar;
                this.costToCallParachutingMember = loadedOptions.costToCallParachutingMember;
                this.ticksCooldownBackupCar = loadedOptions.ticksCooldownBackupCar;
                this.ticksCooldownParachutingMember = loadedOptions.ticksCooldownParachutingMember;

                this.minWantedFactorWhenInGangTurf = loadedOptions.minWantedFactorWhenInGangTurf;
                this.maxWantedLevelInMaxedGangTurf = loadedOptions.maxWantedLevelInMaxedGangTurf;

                this.gangsStartWithPistols = loadedOptions.gangsStartWithPistols;

                this.notificationsEnabled = loadedOptions.notificationsEnabled;
                this.fightingEnabled = loadedOptions.fightingEnabled;
                this.warAgainstPlayerEnabled = loadedOptions.warAgainstPlayerEnabled;
                this.ambientSpawningEnabled = loadedOptions.ambientSpawningEnabled;
                this.forceSpawnCars = loadedOptions.forceSpawnCars;
                this.joypadControls = loadedOptions.joypadControls;

                this.maxCoexistingGangs = loadedOptions.maxCoexistingGangs;
                this.spawnedMembersBeforeAmbientGenStops = loadedOptions.spawnedMembersBeforeAmbientGenStops;
                this.spawnedMemberLimit = loadedOptions.spawnedMemberLimit;
                this.minDistanceCarSpawnFromPlayer = loadedOptions.minDistanceCarSpawnFromPlayer;
                this.minDistanceMemberSpawnFromPlayer = loadedOptions.minDistanceMemberSpawnFromPlayer;
                this.maxDistanceCarSpawnFromPlayer = loadedOptions.maxDistanceCarSpawnFromPlayer;
                this.maxDistanceMemberSpawnFromPlayer = loadedOptions.maxDistanceMemberSpawnFromPlayer;

                if (similarColors[0].blipColor == 0)
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
            PersistenceHandler.SaveToFile<ModOptions>(this, "ModOptions", notifyMsg);
        }

        public Keys openGangMenuKey = Keys.B,
            openZoneMenuKey = Keys.N,
            mindControlKey = Keys.J,
            addToGroupKey = Keys.H;

        public enum gangMemberAggressivenessMode
        {
            veryAgressive,
            agressive,
            defensive
        }

        public gangMemberAggressivenessMode gangMemberAggressiveness = gangMemberAggressivenessMode.veryAgressive;

        public int maxGangMemberHealth = 400;
        public int maxGangMemberArmor = 100;
        public int maxGangMemberAccuracy = 75;

        public bool emptyZoneDuringWar = true;
        public int baseNumKillsBeforeWarVictory = 25;
        public int extraKillsPerTurfValue = 15;

        public int ticksBetweenTurfRewards = 45000;
        public int ticksBetweenGangAIUpdates = 15000;
        public int ticksBetweenGangMemberAIUpdates = 100;
        public int baseRewardPerZoneOwned = 1200;
        public int maxTurfValue = 10;

        /// <summary>
        /// percentage sum, per zone owned, over the total reward received.
        /// for example, if the gang owns 2 zones and the multiplier is 0.2, the reward percentage will be 140%
        /// </summary>
        public float rewardMultiplierPerZone = 0.2f;

        public int costToTakeTurf = 1000;
        public int rewardForTakingEnemyTurf = 3000;

        public int baseCostToUpgradeGeneralGangTurfValue = 1000000;
        public int baseCostToUpgradeSingleTurfValue = 15000;
        public int costToCallBackupCar = 900;
        public int costToCallParachutingMember = 250;
        public int ticksCooldownBackupCar = 1000;
        public int ticksCooldownParachutingMember = 600;

        public bool gangsStartWithPistols = true;

        public bool notificationsEnabled = true;
        public bool fightingEnabled = true, warAgainstPlayerEnabled = true, ambientSpawningEnabled = true;
        public bool forceSpawnCars = false;
        public bool joypadControls = false;

        public float minWantedFactorWhenInGangTurf = 0.0f;
        public int maxWantedLevelInMaxedGangTurf = 0;

        public int maxCoexistingGangs = 7;
        public int spawnedMembersBeforeAmbientGenStops = 20;
        public int spawnedMemberLimit = 30; //max number of living gang members at any time
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
            WeaponHash.Pistol50,
            WeaponHash.Revolver,
            WeaponHash.SawnOffShotgun,
            WeaponHash.SNSPistol,
            WeaponHash.VintagePistol,
            WeaponHash.MicroSMG
        };

        [XmlIgnore]
        public List<WeaponHash> meleeWeapons = new List<WeaponHash>()
        {
            WeaponHash.Bat,
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
            WeaponHash.SwitchBlade,
        };

        public List<BuyableWeapon> buyableWeapons;

        public List<string> possibleGangFirstNames;

        public List<string> possibleGangLastNames;

        public List<GangColorTranslation> similarColors;

        public List<VehicleColor> extraPlayerExclusiveColors;

        //XMLserializer does not like dictionaries
        [System.Serializable]
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

        [System.Serializable]
        public class GangColorTranslation
        {
            public List<VehicleColor> vehicleColors;
            public PotentialGangMember.memberColor baseColor;
            public int blipColor;

            public GangColorTranslation()
            {
                vehicleColors = new List<VehicleColor>();
            }

            public GangColorTranslation(PotentialGangMember.memberColor baseColor, List<VehicleColor> vehicleColors, int blipColor)
            {
                this.baseColor = baseColor;
                this.vehicleColors = vehicleColors;
                this.blipColor = blipColor;
            }
        }

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

        public GangColorTranslation GetGangColorTranslation(PotentialGangMember.memberColor baseColor)
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

        public PotentialGangMember.memberColor TranslateVehicleToMemberColor(VehicleColor vehColor)
        {
            for(int i = 0; i < similarColors.Count; i++)
            {
                if (similarColors[i].vehicleColors.Contains(vehColor))
                {
                    return similarColors[i].baseColor;
                }
            }

            return PotentialGangMember.memberColor.white;
        }

        public int GetAcceptableMemberSpawnDistance()
        {
            if (maxDistanceMemberSpawnFromPlayer <= minDistanceMemberSpawnFromPlayer)
            {
                maxDistanceMemberSpawnFromPlayer = minDistanceMemberSpawnFromPlayer + 2;
                SaveOptions(false);
            }
            return RandoMath.CachedRandom.Next(minDistanceMemberSpawnFromPlayer, maxDistanceMemberSpawnFromPlayer);
        }

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

        public void SetKey(MenuScript.changeableKeyBinding keyToChange, Keys newKey)
        {
            if(newKey == Keys.Escape || newKey == Keys.ShiftKey ||
                newKey == Keys.Insert || newKey == Keys.ControlKey)
            {
                UI.ShowSubtitle("That key can't be used because some settings would become unaccessible due to conflicts.");
                return;
            }

            //verify if this key isn't being used by the other commands from this mod
            //if not, set the chosen key as the new one for the command!
            List<Keys> curKeys = new List<Keys>();
            
            curKeys.Add(openGangMenuKey);
            curKeys.Add(openZoneMenuKey);
            curKeys.Add(mindControlKey);
            curKeys.Add(addToGroupKey);

            if (curKeys.Contains(newKey))
            {
                UI.ShowSubtitle("That key is already being used by this mod's commands.");
                return;
            }
            else
            {
                switch (keyToChange)
                {
                    case MenuScript.changeableKeyBinding.AddGroupBtn:
                        addToGroupKey = newKey;
                        break;
                    case MenuScript.changeableKeyBinding.GangMenuBtn:
                        openGangMenuKey = newKey;
                        break;
                    case MenuScript.changeableKeyBinding.MindControlBtn:
                        mindControlKey = newKey;
                        break;
                    case MenuScript.changeableKeyBinding.ZoneMenuBtn:
                        openZoneMenuKey = newKey;
                        break;
                }

                UI.ShowSubtitle("Key changed!");
                SaveOptions();
            }
        }

        public void SetMemberAggressiveness(gangMemberAggressivenessMode newMode)
        {
            gangMemberAggressiveness = newMode;
            //makes everyone hate cops if set to very aggressive
            GangManager.instance.SetCopRelations(newMode == gangMemberAggressivenessMode.veryAgressive);
            MenuScript.instance.aggOption.Index = (int)newMode;

            SaveOptions(false);
        }

        /// <summary>
        /// resets all values, except for the first and last gang names and the color translations
        /// </summary>
        public void SetAllValuesToDefault()
        {
            openGangMenuKey = Keys.B;
            openZoneMenuKey = Keys.N;
            mindControlKey = Keys.J;
            addToGroupKey = Keys.H;

            gangMemberAggressiveness = gangMemberAggressivenessMode.veryAgressive;

            maxGangMemberHealth = 400;
            maxGangMemberArmor = 100;
            maxGangMemberAccuracy = 75;

            emptyZoneDuringWar = true;
            baseNumKillsBeforeWarVictory = 25;
            extraKillsPerTurfValue = 15;

            ticksBetweenTurfRewards = 45000;
            ticksBetweenGangAIUpdates = 15000;
            ticksBetweenGangMemberAIUpdates = 100;
            baseRewardPerZoneOwned = 1200;
            maxTurfValue = 10;

            rewardMultiplierPerZone = 0.2f;

            costToTakeTurf = 4000;
            rewardForTakingEnemyTurf = 5000;

            baseCostToUpgradeGeneralGangTurfValue = 1000000;
            baseCostToUpgradeSingleTurfValue = 15000;
            costToCallBackupCar = 900;
            costToCallParachutingMember = 250;
            ticksCooldownBackupCar = 1000;
            ticksCooldownParachutingMember = 600;

            minWantedFactorWhenInGangTurf = 0.0f;
            maxWantedLevelInMaxedGangTurf = 0;

            gangsStartWithPistols = true;

            notificationsEnabled = true;
            fightingEnabled = true;
            warAgainstPlayerEnabled = true;
            ambientSpawningEnabled = true;
            forceSpawnCars = false;
            joypadControls = false;

            maxCoexistingGangs = 7;
            spawnedMembersBeforeAmbientGenStops = 20;
            spawnedMemberLimit = 30; //max number of living gang members at any time
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
            new BuyableWeapon(WeaponHash.SwitchBlade, 1100),
            //--guns
            new BuyableWeapon(WeaponHash.AdvancedRifle, 200000),
            new BuyableWeapon(WeaponHash.APPistol, 60000),
            new BuyableWeapon(WeaponHash.AssaultRifle, 120000),
            new BuyableWeapon(WeaponHash.AssaultShotgun, 250000),
            new BuyableWeapon(WeaponHash.AssaultSMG, 190000),
            new BuyableWeapon(WeaponHash.BullpupRifle, 230000),
            new BuyableWeapon(WeaponHash.BullpupShotgun, 265000),
            new BuyableWeapon(WeaponHash.CarbineRifle, 150000),
            new BuyableWeapon(WeaponHash.CombatMG, 220000),
            new BuyableWeapon(WeaponHash.CombatPDW, 205000),
            new BuyableWeapon(WeaponHash.CombatPistol, 50000),
            new BuyableWeapon(WeaponHash.CompactRifle, 175000),
            new BuyableWeapon(WeaponHash.DoubleBarrelShotgun, 210000),
            new BuyableWeapon(WeaponHash.GrenadeLauncher, 550000),
            new BuyableWeapon(WeaponHash.Gusenberg, 200000),
            new BuyableWeapon(WeaponHash.HeavyPistol, 55000),
            new BuyableWeapon(WeaponHash.HeavyShotgun, 180000),
            new BuyableWeapon(WeaponHash.HeavySniper, 300000),
            new BuyableWeapon(WeaponHash.MachinePistol, 65000),
            new BuyableWeapon(WeaponHash.MarksmanPistol, 50000),
            new BuyableWeapon(WeaponHash.MarksmanRifle, 250000),
            new BuyableWeapon(WeaponHash.MG, 290000),
            new BuyableWeapon(WeaponHash.MicroSMG, 90000),
            new BuyableWeapon(WeaponHash.Minigun, 400000),
            new BuyableWeapon(WeaponHash.Musket, 70000),
            new BuyableWeapon(WeaponHash.Pistol, 30000),
            new BuyableWeapon(WeaponHash.Pistol50, 70000),
            new BuyableWeapon(WeaponHash.PumpShotgun, 100000),
            new BuyableWeapon(WeaponHash.Railgun, 1000000),
            new BuyableWeapon(WeaponHash.Revolver, 80000),
            new BuyableWeapon(WeaponHash.RPG, 720000),
            new BuyableWeapon(WeaponHash.SawnOffShotgun, 95000),
            new BuyableWeapon(WeaponHash.SMG, 115000),
            new BuyableWeapon(WeaponHash.SniperRifle, 230000),
            new BuyableWeapon(WeaponHash.SNSPistol, 27000),
            new BuyableWeapon(WeaponHash.SpecialCarbine, 230000),
            new BuyableWeapon(WeaponHash.VintagePistol, 50000)
        };
        }


        public void SetNameListsDefaultValues()
        {

            possibleGangFirstNames = new List<string>
        {
            "Rocket",
            "Magic",
            "High",
            "Miami",
            "Vicious",
            "Electric",
            "Bloody",
            "Brazilian",
            "French",
            "Italian",
            "Greek",
            "Roman",
            "Serious",
            "Japanese",
            "Holy",
            "Colombian",
            "American",
            "Sweet",
            "Cute",
            "Killer",
            "Merciless",
            "666",
            "Street",
            "Business",
            "Beach",
            "Night",
            "Laughing",
            "Watchful",
            "Vigilant",
            "Laser",
            "Swedish",
            "Fire",
            "Ice",
            "Snow",
            "Gothic",
            "Gold",
            "Silver",
            "Iron",
            "Steel",
            "Robot",
            "Chemical",
            "New Wave",
            "Nihilist",
            "Legendary",
            "Epic",
            "Crazy"
        };

            possibleGangLastNames = new List<string>
        {
            "League",
            "Sword",
            "Vice",
            "Gang",
            "Eliminators",
            "Kittens",
            "Cats",
            "Murderers",
            "Mob",
            "Invaders",
            "Mafia",
            "Skull",
            "Ghosts",
            "Dealers",
            "People",
            "Tigers",
            "Triad",
            "Watchers",
            "Vigilantes",
            "Militia",
            "Wolves",
            "Bears",
            "Infiltrators",
            "Barbarians",
            "Goths",
            "Gunners",
            "Hunters",
            "Sharks",
            "Unicorns",
            "Pegasi",
            "Industry Leaders",
            "Gangsters",
            "Hobos",
            "Hookers",
            "Reapers",
            "Dogs",
            "Soldiers",
            "Mobsters",
            "Company",
            "Friends",
            "Monsters",
            "Fighters",
        };
        }

        public void SetColorTranslationDefaultValues()
        {
                similarColors = new List<GangColorTranslation>
            {
                new GangColorTranslation(PotentialGangMember.memberColor.black, new List<VehicleColor> {
                     VehicleColor.BrushedBlackSteel,
                    VehicleColor.MatteBlack,
                    VehicleColor.MetallicBlack,
                    VehicleColor.MetallicGraphiteBlack,
                    VehicleColor.UtilBlack,
                    VehicleColor.WornBlack,
                    VehicleColor.ModshopBlack1
                }, 29
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.blue, new List<VehicleColor> {
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
                }, 3
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.green, new List<VehicleColor> {
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
                }, 2
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.pink, new List<VehicleColor> {
                     VehicleColor.HotPink,
                    VehicleColor.MetallicVermillionPink,
                }, 23
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.purple, new List<VehicleColor> {
                     VehicleColor.MatteDarkPurple,
                    VehicleColor.MattePurple,
                    VehicleColor.MetallicPurple,
                    VehicleColor.MetallicPurpleBlue,
                }, 19
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.red, new List<VehicleColor> {
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
                }, 1
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.white, new List<VehicleColor> {
                     VehicleColor.MatteWhite,
                    VehicleColor.MetallicFrostWhite,
                    VehicleColor.MetallicWhite,
                    VehicleColor.PureWhite,
                    VehicleColor.UtilOffWhite,
                    VehicleColor.WornOffWhite,
                    VehicleColor.WornWhite,
                    VehicleColor.MetallicDarkIvory,
                }, 0
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.yellow, new List<VehicleColor> {
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
                }, 66
               ),
                new GangColorTranslation(PotentialGangMember.memberColor.gray, new List<VehicleColor> {
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
                }, 20
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
