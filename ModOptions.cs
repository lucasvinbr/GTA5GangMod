using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using System.Windows.Forms;

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

                this.maxGangMemberHealth = loadedOptions.maxGangMemberHealth;
                this.maxGangMemberArmor = loadedOptions.maxGangMemberArmor;
                this.maxGangMemberAccuracy = loadedOptions.maxGangMemberAccuracy;

                this.emptyZoneDuringWar = loadedOptions.emptyZoneDuringWar;
                this.baseNumKillsBeforeWarVictory = loadedOptions.baseNumKillsBeforeWarVictory;

                this.ticksBetweenTurfRewards = loadedOptions.ticksBetweenTurfRewards;
                this.ticksBetweenGangAIUpdates = loadedOptions.ticksBetweenGangAIUpdates;
                this.ticksBetweenGangMemberAIUpdates = loadedOptions.ticksBetweenGangMemberAIUpdates;
                this.baseRewardPerZoneOwned = loadedOptions.baseRewardPerZoneOwned;

                this.rewardMultiplierPerZone = loadedOptions.rewardMultiplierPerZone;

                this.costToTakeNeutralTurf = loadedOptions.costToTakeNeutralTurf;
                this.rewardForTakingEnemyTurf = loadedOptions.rewardForTakingEnemyTurf;
                this.costToCallBackupCar = loadedOptions.costToCallBackupCar;
                this.costToCallParachutingMember = loadedOptions.costToCallParachutingMember;
                this.ticksCooldownBackupCar = loadedOptions.ticksCooldownBackupCar;
                this.ticksCooldownParachutingMember = loadedOptions.ticksCooldownParachutingMember;

                this.wantedFactorWhenInGangTurf = loadedOptions.wantedFactorWhenInGangTurf;
                this.maxWantedLevelInGangTurf = loadedOptions.maxWantedLevelInGangTurf;

                this.spawnedMembersBeforeAmbientGenStops = loadedOptions.spawnedMembersBeforeAmbientGenStops;
                this.spawnedMemberLimit = loadedOptions.spawnedMemberLimit;
                this.minDistanceCarSpawnFromPlayer = loadedOptions.minDistanceCarSpawnFromPlayer;
                this.minDistanceMemberSpawnFromPlayer = loadedOptions.minDistanceMemberSpawnFromPlayer;
                this.maxDistanceCarSpawnFromPlayer = loadedOptions.maxDistanceCarSpawnFromPlayer;
                this.maxDistanceMemberSpawnFromPlayer = loadedOptions.maxDistanceMemberSpawnFromPlayer;
                SaveOptions();
            }
            else
            {
                SetWeaponListDefaultValues();
                SetNameListsDefaultValues();
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

        public int ticksBetweenTurfRewards = 50000;
        public int ticksBetweenGangAIUpdates = 30000;
        public int ticksBetweenGangMemberAIUpdates = 100;
        public int baseRewardPerZoneOwned = 500;

        /// <summary>
        /// percentage sum, per zone owned, over the total reward received.
        /// for example, if the gang owns 2 zones and the multiplier is 0.2, the reward percentage will be 140%
        /// </summary>
        public float rewardMultiplierPerZone = 0.2f;

        public int costToTakeNeutralTurf = 10000;
        public int rewardForTakingEnemyTurf = 10000;
        public int costToCallBackupCar = 900;
        public int costToCallParachutingMember = 250;
        public int ticksCooldownBackupCar = 1000;
        public int ticksCooldownParachutingMember = 600;

        public float wantedFactorWhenInGangTurf = 0.2f;
        public int maxWantedLevelInGangTurf = 1;

        public int spawnedMembersBeforeAmbientGenStops = 20;
        public int spawnedMemberLimit = 30; //max number of living gang members at any time
        public int minDistanceMemberSpawnFromPlayer = 50;
        public int maxDistanceMemberSpawnFromPlayer = 130;
        public int minDistanceCarSpawnFromPlayer = 80;
        public int maxDistanceCarSpawnFromPlayer = 190;


        public List<BuyableWeapon> buyableWeapons;

        public List<string> possibleGangFirstNames;

        public List<string> possibleGangLastNames;

        public List<GangColorTranslation> similarColors;

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

            public GangColorTranslation()
            {
                vehicleColors = new List<VehicleColor>();
            }

            public GangColorTranslation(PotentialGangMember.memberColor baseColor, List<VehicleColor> vehicleColors)
            {
                this.baseColor = baseColor;
                this.vehicleColors = vehicleColors;
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
                maxDistanceMemberSpawnFromPlayer = minDistanceMemberSpawnFromPlayer + 1;
                SaveOptions(false);
            }
            return RandomUtil.CachedRandom.Next(minDistanceMemberSpawnFromPlayer, maxDistanceMemberSpawnFromPlayer);
        }

        public int GetAcceptableCarSpawnDistance()
        {
            if(maxDistanceCarSpawnFromPlayer <= minDistanceCarSpawnFromPlayer)
            {
                maxDistanceCarSpawnFromPlayer = minDistanceCarSpawnFromPlayer + 1;
                SaveOptions(false);
            }
            return RandomUtil.CachedRandom.Next(minDistanceCarSpawnFromPlayer, maxDistanceCarSpawnFromPlayer);
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

            ticksBetweenTurfRewards = 50000;
            ticksBetweenGangAIUpdates = 30000;
            ticksBetweenGangMemberAIUpdates = 100;
            baseRewardPerZoneOwned = 500;

            rewardMultiplierPerZone = 0.2f;

            costToTakeNeutralTurf = 10000;
            rewardForTakingEnemyTurf = 10000;
            costToCallBackupCar = 900;
            costToCallParachutingMember = 250;
            ticksCooldownBackupCar = 1000;
            ticksCooldownParachutingMember = 600;

            wantedFactorWhenInGangTurf = 0.2f;
            maxWantedLevelInGangTurf = 1;

            spawnedMembersBeforeAmbientGenStops = 20;
            spawnedMemberLimit = 30; //max number of living gang members at any time
            minDistanceMemberSpawnFromPlayer = 50;
            maxDistanceMemberSpawnFromPlayer = 130;
            minDistanceCarSpawnFromPlayer = 80;
            maxDistanceCarSpawnFromPlayer = 190;

            buyableWeapons.Clear();

            SetWeaponListDefaultValues();

            SaveOptions();

            GangManager.instance.ResetGangUpdateIntervals();
        }

        public void SetWeaponListDefaultValues()
        {
            buyableWeapons = new List<BuyableWeapon>()
        {
            new BuyableWeapon(WeaponHash.AdvancedRifle, 20000),
            new BuyableWeapon(WeaponHash.APPistol, 5000),
            new BuyableWeapon(WeaponHash.AssaultRifle, 12000),
            new BuyableWeapon(WeaponHash.AssaultShotgun, 25000),
            new BuyableWeapon(WeaponHash.AssaultSMG, 19000),
            new BuyableWeapon(WeaponHash.BullpupRifle, 23000),
            new BuyableWeapon(WeaponHash.BullpupShotgun, 26500),
            new BuyableWeapon(WeaponHash.CarbineRifle, 15000),
            new BuyableWeapon(WeaponHash.CombatMG, 22000),
            new BuyableWeapon(WeaponHash.CombatPDW, 20500),
            new BuyableWeapon(WeaponHash.CombatPistol, 5000),
            new BuyableWeapon(WeaponHash.CompactRifle, 17500),
            new BuyableWeapon(WeaponHash.DoubleBarrelShotgun, 21000),
            new BuyableWeapon(WeaponHash.GrenadeLauncher, 28000),
            new BuyableWeapon(WeaponHash.Gusenberg, 10000),
            new BuyableWeapon(WeaponHash.HeavyPistol, 4000),
            new BuyableWeapon(WeaponHash.HeavyShotgun, 18000),
            new BuyableWeapon(WeaponHash.HeavySniper, 30000),
            new BuyableWeapon(WeaponHash.MachinePistol, 5500),
            new BuyableWeapon(WeaponHash.MarksmanPistol, 5000),
            new BuyableWeapon(WeaponHash.MarksmanRifle, 20000),
            new BuyableWeapon(WeaponHash.MG, 19000),
            new BuyableWeapon(WeaponHash.MicroSMG, 7000),
            new BuyableWeapon(WeaponHash.Minigun, 40000),
            new BuyableWeapon(WeaponHash.Musket, 5000),
            new BuyableWeapon(WeaponHash.Pistol, 1000),
            new BuyableWeapon(WeaponHash.Pistol50, 3000),
            new BuyableWeapon(WeaponHash.PumpShotgun, 10000),
            new BuyableWeapon(WeaponHash.Railgun, 50000),
            new BuyableWeapon(WeaponHash.Revolver, 8000),
            new BuyableWeapon(WeaponHash.RPG, 32000),
            new BuyableWeapon(WeaponHash.SawnOffShotgun, 8000),
            new BuyableWeapon(WeaponHash.SMG, 11000),
            new BuyableWeapon(WeaponHash.SniperRifle, 23000),
            new BuyableWeapon(WeaponHash.SNSPistol, 900),
            new BuyableWeapon(WeaponHash.SpecialCarbine, 23000),
            new BuyableWeapon(WeaponHash.VintagePistol, 5000)
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
            }
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
            }
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
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.pink, new List<VehicleColor> {
                 VehicleColor.HotPink,
                VehicleColor.MetallicVermillionPink,
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.purple, new List<VehicleColor> {
                 VehicleColor.MatteDarkPurple,
                VehicleColor.MattePurple,
                VehicleColor.MetallicPurple,
                VehicleColor.MetallicPurpleBlue,
            }
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
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.white, new List<VehicleColor> {
                 VehicleColor.MatteWhite,
                VehicleColor.MetallicFrostWhite,
                VehicleColor.MetallicWhite,
                VehicleColor.PureWhite,
                VehicleColor.UtilOffWhite,
                VehicleColor.WornOffWhite,
                VehicleColor.WornWhite,
            }
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
            }
           ),
            new GangColorTranslation(PotentialGangMember.memberColor.gray, new List<VehicleColor> {
                 VehicleColor.MatteGray,
               VehicleColor.MatteLightGray,
               VehicleColor.MetallicAnthraciteGray,
               VehicleColor.MetallicSteelGray,
               VehicleColor.WornSilverGray,
            }
           )
        };
        }
                
    }
}
