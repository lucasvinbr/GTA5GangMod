using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    public class GangWarManager : Script
    {

        public int enemyReinforcements, alliedReinforcements;

        public bool isOccurring = false;

        public enum warType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public enum attackStrength
        {
            light,
            medium,
            large,
            massive
        }

        public enum playerBattleParticipation
        {
            foughtAndSurvived,
            fellInBattle,
            didNotParticipate
        }

        public warType curWarType = warType.attackingEnemy;

        private int curTicksAwayFromBattle = 0, ticksSinceLastCarSpawn = 0;

        /// <summary>
        /// numbers greater than 1 for player advantage, lesser for enemy advantage.
        /// this advantage affects the member respawns:
        /// whoever has the greater advantage tends to have priority when spawning
        /// </summary>
        private float reinforcementsAdvantage = 0.0f;

        private float spawnedMembersProportion;

        private int ticksBeforeAutoResolution = 30000, minTicksBetweenCarSpawns = 1000;

        private float spawnedAllies = 0, spawnedEnemies = 0;

        public TurfZone warZone;

        public Gang enemyGang;

        public static GangWarManager instance;

        private Blip warBlip, alliedSpawnBlip, enemySpawnBlip;

        private Vector3[] enemySpawnPoints, alliedSpawnPoints;

        private bool spawnPointsSet = false;

        public GangWarManager()
        {
            instance = this;
            this.Tick += OnTick;
            enemySpawnPoints = new Vector3[3];
            alliedSpawnPoints = new Vector3[3];
        }

        

        public bool StartWar(Gang enemyGang, TurfZone warZone, warType theWarType, attackStrength attackStrength)
        {
            //TODO disable wars during missions
            if (!isOccurring)
            {
                this.enemyGang = enemyGang;
                this.warZone = warZone;
                this.curWarType = theWarType;

                spawnPointsSet = false;

                warBlip = World.CreateBlip(warZone.zoneBlipPosition);
                warBlip.IsFlashing = true;
                warBlip.Sprite = BlipSprite.Deathmatch;
                warBlip.Color = BlipColor.Red;
                
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;

                if(theWarType == warType.attackingEnemy)
                {
                    alliedReinforcements = GangManager.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, attackStrength);
                    enemyReinforcements = GangManager.CalculateDefenderReinforcements(enemyGang, warZone);
                }
                else
                {
                    alliedReinforcements = GangManager.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone);
                    enemyReinforcements = GangManager.CalculateAttackerReinforcements(enemyGang, attackStrength);
                }

                reinforcementsAdvantage = alliedReinforcements / (float) enemyReinforcements;

                spawnedAllies = 0;
                spawnedEnemies = 0;

                isOccurring = true;

                if(World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 100)
                {
                    SetSpawnPoints(warZone.zoneBlipPosition);
                }

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (theWarType == warType.attackingEnemy)
                {
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");
                    Wait(10000);
                }
                else
                {
                    UI.Notify(string.Concat("The " , enemyGang.name , " are attacking " , warZone.zoneName , "! They are ",
                        GangManager.CalculateAttackerReinforcements(enemyGang, attackStrength).ToString(),
                        " against our ",
                        GangManager.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone).ToString()));
                }

                return true;
            }
            else
            {
                return false;
            }
            

        }

        void SetSpawnPoints(Vector3 initialReferencePoint)
        {
            //spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos
            //the defenders' spawn point should be closer to the war blip than the attacker
            if(curWarType == warType.defendingFromEnemy)
            {
                alliedSpawnPoints[0] = GangManager.instance.FindGoodSpawnPointForMember(initialReferencePoint);
                enemySpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
                30, alliedSpawnPoints[0], ModOptions.instance.maxDistanceMemberSpawnFromPlayer);
            }
            else
            {
                enemySpawnPoints[0] = GangManager.instance.FindGoodSpawnPointForMember(initialReferencePoint);
                alliedSpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
                30, enemySpawnPoints[0], ModOptions.instance.maxDistanceMemberSpawnFromPlayer);
            }
            
            for (int i = 1; i < 3; i++)
            {
                alliedSpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(alliedSpawnPoints[0], 20, 10, 20);
            }

            
            for (int i = 1; i < 3; i++)
            {
                enemySpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(enemySpawnPoints[0], 20, 10, 20);
            }

            //and the spawn point blips, so that we don't have to hunt where our troops will come from
            alliedSpawnBlip = World.CreateBlip(alliedSpawnPoints[0]);
            alliedSpawnBlip.Scale = 1.15f;
            Function.Call(Hash.SET_BLIP_COLOUR, alliedSpawnBlip, GangManager.instance.PlayerGang.blipColor);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War: ", GangManager.instance.PlayerGang.name, " spawn point"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, alliedSpawnBlip);

            enemySpawnBlip = World.CreateBlip(enemySpawnPoints[0]);
            alliedSpawnBlip.Scale = 1.15f;
            Function.Call(Hash.SET_BLIP_COLOUR, enemySpawnBlip, enemyGang.blipColor);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War: ", enemyGang.name, " spawn point"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, enemySpawnBlip);

            if(alliedSpawnPoints[0] != Vector3.Zero &&
                enemySpawnPoints[0] != Vector3.Zero)
            {
                spawnPointsSet = true;
            }
            else
            {
                //we probably failed to place spawn points properly.
                //we will try placing the spawn points again in the next tick
                alliedSpawnBlip.Remove();
                enemySpawnBlip.Remove();
            }
            
        }

        /// <summary>
        /// checks both gangs' situations and the amount of reinforcements left for each side.
        /// also considers their strength (with variations) in order to decide the likely outcome of this battle.
        /// returns true for a player victory and false for a defeat
        /// </summary>
        public bool SkipWar(float playerGangStrengthFactor = 1.0f)
        {
            //if the player was out of reinforcements, it's a defeat, no matter what
            if (alliedReinforcements <= 0)
            {
                return false;
            }

            int alliedBaseStr = GangManager.instance.PlayerGang.GetGangVariedStrengthValue(),
                enemyBaseStr = enemyGang.GetGangVariedStrengthValue();
            //the amount of reinforcements counts here
            float totalAlliedStrength = alliedBaseStr * playerGangStrengthFactor +
                RandoMath.Max(4, alliedBaseStr / 100) * alliedReinforcements,
                totalEnemyStrength = enemyBaseStr +
                RandoMath.Max(4, enemyBaseStr / 100) * enemyReinforcements;

            bool itsAVictory = totalAlliedStrength > totalEnemyStrength;

            float strengthProportion = totalAlliedStrength / totalEnemyStrength;

            string battleReport = "Battle report: We";

            //we attempt to provide a little report on what happened
            if (itsAVictory)
            {
                battleReport = string.Concat(battleReport, " won the battle against the ", enemyGang.name, "! ");

                if (strengthProportion > 2f)
                {
                    battleReport = string.Concat(battleReport, "They were crushed!");
                }
                else if (strengthProportion > 1.75f)
                {
                    battleReport = string.Concat(battleReport, "We had the upper hand and they didn't have much of a chance!");
                }
                else if (strengthProportion > 1.5f)
                {
                    battleReport = string.Concat(battleReport, "We fought well and took them down.");
                }
                else if (strengthProportion > 1.25f)
                {
                    battleReport = string.Concat(battleReport, "They tried to resist, but we got them.");
                }
                else
                {
                    battleReport = string.Concat(battleReport, "It was a tough battle, but we prevailed in the end.");
                }
            }
            else
            {
                battleReport = string.Concat(battleReport, " lost the battle against the ", enemyGang.name, ". ");

                if (strengthProportion < 0.5f)
                {
                    battleReport = string.Concat(battleReport, "We were crushed!");
                }
                else if (strengthProportion < 0.625f)
                {
                    battleReport = string.Concat(battleReport, "They had the upper hand and we had no chance!");
                }
                else if (strengthProportion < 0.75f)
                {
                    battleReport = string.Concat(battleReport, "They fought well and we had to retreat.");
                }
                else if (strengthProportion < 0.875f)
                {
                    battleReport = string.Concat(battleReport, "We did our best, but couldn't put them down.");
                }
                else
                {
                    battleReport = string.Concat(battleReport, "We almost won, but in the end, we were defeated.");
                }
            }

            UI.Notify(battleReport);

            return itsAVictory;
        }

        public void EndWar(bool playerVictory)
        {
            if (playerVictory)
            {
                GangManager.instance.AddOrSubtractMoneyToProtagonist
                    (GangManager.CalculateBattleRewards(enemyGang, curWarType == warType.attackingEnemy));
                if (curWarType == warType.attackingEnemy)
                {
                    GangManager.instance.PlayerGang.TakeZone(warZone);
                    
                    UI.ShowSubtitle(warZone.zoneName + " is now ours!");
                }
                else
                {
                    UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                    
                }
            }
            else
            {
                enemyGang.moneyAvailable +=
                    GangManager.CalculateBattleRewards(GangManager.instance.PlayerGang, curWarType != warType.attackingEnemy);
                if (curWarType == warType.attackingEnemy)
                {
                    UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                }
                else
                {
                    enemyGang.TakeZone(warZone);
                    UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
                }
            }

            CheckIfBattleWasUnfair();
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ScreenFlash", "WastedSounds");
            warBlip.Remove();
            alliedSpawnBlip.Remove();
            enemySpawnBlip.Remove();
            isOccurring = false;
            Game.WantedMultiplier = 1;
            AmbientGangMemberSpawner.instance.enabled = true;
        }

        void CheckIfBattleWasUnfair()
        {
            //the battle was unfair if the player's gang had guns and the enemy gang hadn't
            //in this case, there is a possibility of the defeated gang instantly getting pistols
            //in order to at least not get decimated all the time

            if(enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
                GangManager.instance.PlayerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) != WeaponHash.Unarmed)
            {
                if (RandoMath.RandomBool())
                {
                    enemyGang.gangWeaponHashes.Add(RandoMath.GetRandomElementFromList(ModOptions.instance.driveByWeapons));
                    enemyGang.gangWeaponHashes.Sort(enemyGang.CompareGunsByPrice);
                    GangManager.instance.SaveGangData(false);
                }
            }
        }

        /// <summary>
        /// spawns a vehicle that has the player as destination
        /// </summary>
        public void SpawnAngryVehicle(bool isFriendly)
        {
            Math.Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForCar();
            Vehicle spawnedVehicle = null;
            if (!isFriendly)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(enemyGang,
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true);
            }
            else
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true);
            }
            
            if (spawnedVehicle != null)
            {
                spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver).Task.DriveTo(spawnedVehicle, Game.Player.Character.Position, 25, 100);
                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver), 4457020); //ignores roads, avoids obstacles

                ticksSinceLastCarSpawn = RandoMath.CachedRandom.Next(-minTicksBetweenCarSpawns, 0);
            }
        }

        public void SpawnMember(bool isFriendly)
        {
            Vector3 spawnPos = isFriendly ? 
                RandoMath.GetRandomElementFromArray(alliedSpawnPoints) : RandoMath.GetRandomElementFromArray(enemySpawnPoints);
            Ped spawnedMember = 
                GangManager.instance.SpawnGangMember(isFriendly ? GangManager.instance.PlayerGang : enemyGang, spawnPos);

            if (spawnedMember != null)
            {
                if (isFriendly)
                {
                    spawnedMember.Task.RunTo(Game.Player.Character.Position);
                    spawnedAllies++;
                }
                else
                {
                    spawnedMember.Task.FightAgainst(Game.Player.Character);
                    spawnedEnemies++;
                }
                
            }
        }

        public void OnEnemyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 600){
                enemyReinforcements--;
                spawnedEnemies--; //reducing this assures both sides will keep spawning

                //have we lost too many? its a victory for the player then
                if(enemyReinforcements <= 0)
                {
                    EndWar(true);
                }
                else
                {
                    UI.ShowSubtitle(enemyReinforcements.ToString() + " kills remaining!", 900);
                }

            }
        }

        public void OnAllyDeath(bool itWasThePlayer = false)
        {
            //check if the player was in or near the warzone when the death happened 
            if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 600)
            {
                alliedReinforcements--;
                spawnedAllies--; //reducing this assures both sides will keep spawning

                //we can't lose by running out of reinforcements only.
                //the player must fall or the war be skipped for it to end as a defeat

                if (alliedReinforcements >= 0)
                {
                    UI.ShowSubtitle(alliedReinforcements.ToString() + " of us remain!", 900);
                }
                else
                {
                    if (itWasThePlayer)
                    {
                        //then it's a defeat
                        EndWar(false);
                    }
                }
            }
        }

        void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                ticksSinceLastCarSpawn++;
                if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 300)
                {
                    curTicksAwayFromBattle = 0;
                    Game.WantedMultiplier = 0;

                    AmbientGangMemberSpawner.instance.enabled = false;

                    if (ModOptions.instance.emptyZoneDuringWar)
                    {
                        Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                        Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                    }

                    if (ticksSinceLastCarSpawn >= minTicksBetweenCarSpawns)
                    {
                        SpawnAngryVehicle(false);

                        if(curWarType == warType.defendingFromEnemy && RandoMath.RandomBool())
                        {
                            SpawnAngryVehicle(true); //automatic backup for us
                        }
                    }

                    if (!spawnPointsSet) SetSpawnPoints(warZone.zoneBlipPosition);

                    spawnedMembersProportion = spawnedAllies / RandoMath.Max(spawnedEnemies, 1.0f);

                    //if the allied side is out of reinforcements, no more allies will be spawned by this system.
                    //it won't be a defeat, however, until the player dies
                    SpawnMember(alliedReinforcements > 0 && spawnedMembersProportion < reinforcementsAdvantage);
                    Wait(400);
                       
                }
                else
                {
                    curTicksAwayFromBattle++;
                    AmbientGangMemberSpawner.instance.enabled = true;
                    if (curTicksAwayFromBattle > ticksBeforeAutoResolution)
                    {
                        EndWar(SkipWar(0.75f));
                    }
                }
                //if the player's gang leader is dead...
                if (!Game.Player.IsAlive && !GangManager.instance.hasChangedBody)
                {
                    //the war ends, but the outcome depends on how well the player's side was doing
                    EndWar(SkipWar());
                    return;
                }
            }
        }
    }
}
