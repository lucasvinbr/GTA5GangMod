using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    public class GangWarManager : Script
    {

        public int enemyReinforcements, alliedReinforcements;

        public bool isOccurring = false;

        public enum WarType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public enum AttackStrength
        {
            light,
            medium,
            large,
            massive
        }

        public WarType curWarType = WarType.attackingEnemy;

        private int curTicksAwayFromBattle = 0;

        /// <summary>
        /// numbers closer to 1 for player advantage, less than 0.5 for enemy advantage.
        /// this advantage affects the member respawns:
        /// whoever has the greater advantage tends to have priority when spawning
        /// </summary>
        private float reinforcementsAdvantage = 0.0f;

        private float alliedPercentOfSpawnedMembers;

        private const int MIN_TICKS_BETWEEN_CAR_SPAWNS = 10;
        private const int MS_BETWEEN_WAR_TICKS = 200;

        //balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
        //we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
        private const int TICKS_BETWEEN_BALANCE_CHECKS = 14;

        private int ticksSinceLastCarSpawn = 0;
           
        //private int ticksSinceLastEnemyRelocation = 0;



        private int ticksSinceLastBalanceCheck = 0;

        private int timeLastWarAgainstPlayer = 0;

        private int maxSpawnedAllies, maxSpawnedEnemies;

        private int spawnedAllies = 0, spawnedEnemies = 0;

        public TurfZone warZone;

        public bool playerNearWarzone = false;

        public Gang enemyGang;

        private Blip warBlip;

        //private Blip enemySpawnBlip;

        /// <summary>
        /// index 0 is the area around the zone blip; 1 is the area around the player when the war starts
        /// (this one may not be used if the war was started by the AI and the player was away)
        /// </summary>
        private readonly Blip[] warAreaBlips;

        //private readonly Blip[] alliedSpawnBlips;

        public List<WarControlPoint> enemySpawnPoints, alliedSpawnPoints;

        //private bool spawnPointsSet = false;

        private AttackStrength curWarAtkStrength = AttackStrength.light;

        public UIResText alliedNumText, enemyNumText;

        public bool shouldDisplayReinforcementsTexts = false;

        public static GangWarManager instance;

        public List<WarControlPoint> controlPoints = new List<WarControlPoint>();

        private readonly List<WarControlPoint> pooledControlPoints = new List<WarControlPoint>();

        private List<Vector3> availableNearbyPresetSpawns;

        private int desiredNumberOfControlPointsForThisWar = 0;
        private int nextCPIndexToCheckForCapture = 0;

        public const int MAX_EXTRA_CONTROL_POINTS = 5;

        public const float MIN_SPAWNS_DISTANCE_TO_CONTROL_POINT = 60;

        public GangWarManager()
        {
            instance = this;
            this.Tick += OnTick;
            this.Aborted += OnAbort;
            //alliedSpawnBlips = new Blip[3];
            warAreaBlips = new Blip[2];


            alliedNumText = new UIResText("400", new Point(), 0.5f, Color.CadetBlue);
            enemyNumText = new UIResText("400", new Point(), 0.5f, Color.Red);

            alliedNumText.Outline = true;
            enemyNumText.Outline = true;

            alliedNumText.TextAlignment = UIResText.Alignment.Centered;
            enemyNumText.TextAlignment = UIResText.Alignment.Centered;
        }


        #region start/end/skip war
        public bool StartWar(Gang enemyGang, TurfZone warZone, WarType theWarType, AttackStrength attackStrength)
        {
            if (!isOccurring || enemyGang == GangManager.instance.PlayerGang)
            {
                this.enemyGang = enemyGang;
                this.warZone = warZone;
                this.curWarType = theWarType;
                curWarAtkStrength = attackStrength;
                playerNearWarzone = false;
                //spawnPointsSet = false;

                warBlip = World.CreateBlip(warZone.zoneBlipPosition);
                warBlip.IsFlashing = true;
                warBlip.Sprite = BlipSprite.Deathmatch;
                warBlip.Color = BlipColor.Red;

                enemySpawnPoints = new List<WarControlPoint>();
                alliedSpawnPoints = new List<WarControlPoint>();

                warAreaBlips[0] = World.CreateBlip(warZone.zoneBlipPosition,
                    ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
                warAreaBlips[0].Sprite = BlipSprite.BigCircle;
                warAreaBlips[0].Color = BlipColor.Red;
                warAreaBlips[0].Alpha = 175;

                bool alreadyInsideWarzone = IsPositionInsideWarzone(MindControl.SafePositionNearPlayer);

                //set the second war blip at the player pos if it'll help "staying inside the war"
                //(for example, player started the war at the border of the zone)
                if (alreadyInsideWarzone)
                {
                    warAreaBlips[1] = World.CreateBlip(MindControl.SafePositionNearPlayer,
                    ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
                    warAreaBlips[1].Sprite = BlipSprite.BigCircle;
                    warAreaBlips[1].Color = BlipColor.Red;
                    warAreaBlips[1].Alpha = 175;
                }
                else
                {
                    if (warAreaBlips[1] != null)
                    {
                        warAreaBlips[1].Remove();
                        warAreaBlips[1] = null;
                    }
                }


                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;

                if (theWarType == WarType.attackingEnemy)
                {
                    alliedReinforcements = GangCalculations.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, attackStrength);
                    enemyReinforcements = GangCalculations.CalculateDefenderReinforcements(enemyGang, warZone);
                }
                else
                {
                    alliedReinforcements = GangCalculations.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone);
                    enemyReinforcements = GangCalculations.CalculateAttackerReinforcements(enemyGang, attackStrength);
                }

                float screenRatio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;

                int proportionalScreenWidth = (int)(1080 * screenRatio); //nativeUI UIResText works with 1080p height

                alliedNumText.Position = new Point((proportionalScreenWidth / 2) - 120, 10);
                enemyNumText.Position = new Point((proportionalScreenWidth / 2) + 120, 10);

                alliedNumText.Caption = alliedReinforcements.ToString();
                enemyNumText.Caption = enemyReinforcements.ToString();

                reinforcementsAdvantage = alliedReinforcements / (float)(enemyReinforcements + alliedReinforcements);

                spawnedAllies = SpawnManager.instance.GetSpawnedMembersOfGang(GangManager.instance.PlayerGang).Count;
                spawnedEnemies = SpawnManager.instance.GetSpawnedMembersOfGang(enemyGang).Count;

                maxSpawnedAllies = (int)RandoMath.Max(RandoMath.Min(
                    (ModOptions.instance.spawnedMemberLimit) * reinforcementsAdvantage,
                    ModOptions.instance.spawnedMemberLimit - ModOptions.instance.minSpawnsForEachSideDuringWars),
                    ModOptions.instance.minSpawnsForEachSideDuringWars);

                maxSpawnedEnemies = RandoMath.Max
                    (ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies, ModOptions.instance.minSpawnsForEachSideDuringWars);

                Logger.Log(string.Concat("war started! Reinf advantage: ", reinforcementsAdvantage.ToString(),
                    " maxAllies: ", maxSpawnedAllies.ToString(), " maxEnemies: ", maxSpawnedEnemies.ToString()), 3);

                isOccurring = true;

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (theWarType == WarType.attackingEnemy)
                {
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");
                }
                else
                {
                    if (ModOptions.instance.notificationsEnabled)
                        UI.Notify(string.Concat("The ", enemyGang.name, " are attacking ", warZone.zoneName, "! They are ",
                        GangCalculations.CalculateAttackerReinforcements(enemyGang, attackStrength).ToString(),
                        " against our ",
                        GangCalculations.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone).ToString()));
                }

                if (alreadyInsideWarzone)
                {
                    //if we are inside the warzone already, set spawns around the player!
                    PrepareAndSetupInitialSpawnPoint(MindControl.SafePositionNearPlayer);
                }
                else
                {
                    //this number may change once we're inside the zone and PrepareAndSetupInitialSpawnPoint is run
                    desiredNumberOfControlPointsForThisWar = 2;
                }
                

                SetHateRelationsBetweenGangs();




                return true;
            }
            else
            {
                return false;
            }


        }

        /// <summary>
        /// checks both gangs' situations and the amount of reinforcements left for each side.
        /// also considers their strength (with variations) in order to decide the likely outcome of this battle.
        /// returns true for a player victory and false for a defeat
        /// </summary>
        public bool GetSkippedWarResult(float playerGangStrengthFactor = 1.0f)
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
                    battleReport = string.Concat(battleReport, "We had the upper hand!");
                }
                else if (strengthProportion > 1.5f)
                {
                    battleReport = string.Concat(battleReport, "We fought well!");
                }
                else if (strengthProportion > 1.25f)
                {
                    battleReport = string.Concat(battleReport, "It was a bit tough, but we won!");
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
                    battleReport = string.Concat(battleReport, "We had no chance!");
                }
                else if (strengthProportion < 0.75f)
                {
                    battleReport = string.Concat(battleReport, "We just couldn't beat them!");
                }
                else if (strengthProportion < 0.875f)
                {
                    battleReport = string.Concat(battleReport, "We fought hard, but they pushed us back.");
                }
                else
                {
                    battleReport = string.Concat(battleReport, "We almost won, but they got us in the end.");
                }
            }

            if (ModOptions.instance.notificationsEnabled)
                UI.Notify(battleReport);

            return itsAVictory;
        }

        public void EndWar(bool playerVictory)
        {
            bool weWereAttacking = curWarType == WarType.attackingEnemy;
            if (playerVictory)
            {
                int battleProfit = GangCalculations.CalculateBattleRewards(enemyGang, weWereAttacking ? warZone.value : (int)curWarAtkStrength, weWereAttacking);
                MindControl.instance.AddOrSubtractMoneyToProtagonist
                    (battleProfit);

                if (ModOptions.instance.notificationsEnabled)
                    UI.Notify("Victory rewards: $" + battleProfit.ToString());

                if (weWereAttacking)
                {
                    GangManager.instance.PlayerGang.TakeZone(warZone);

                    UI.ShowSubtitle(warZone.zoneName + " is now ours!");
                }
                else
                {
                    UI.ShowSubtitle(warZone.zoneName + " remains ours!");

                }

                AmbientGangMemberSpawner.instance.postWarBackupsRemaining = ModOptions.instance.postWarBackupsAmount;
            }
            else
            {
                enemyGang.moneyAvailable += (int)
                    (GangCalculations.CalculateBattleRewards(GangManager.instance.PlayerGang, !weWereAttacking ? warZone.value : (int)curWarAtkStrength, !weWereAttacking) *
                    ModOptions.instance.extraProfitForAIGangsFactor);
                if (curWarType == WarType.attackingEnemy)
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
            for(int i = 0; i < warAreaBlips.Length; i++)
            {
                if (warAreaBlips[i] != null)
                {
                    warAreaBlips[i].Remove();
                    warAreaBlips[i] = null;
                }
                    
            }
            shouldDisplayReinforcementsTexts = false;
            isOccurring = false;
            playerNearWarzone = false;
            AmbientGangMemberSpawner.instance.enabled = true;

            if (!weWereAttacking)
            {
                //prevent the player from being attacked again too early
                timeLastWarAgainstPlayer = ModCore.curGameTime;
            }

            //if (enemySpawnBlip != null)
            //{
            //    enemySpawnBlip.Remove();
            //}

            //foreach (Blip alliedBlip in alliedSpawnBlips)
            //{
            //    if (alliedBlip != null)
            //    {
            //        alliedBlip.Remove();
            //    }
            //}

            PoolAllControlPoints();

            //reset relations to whatever is set in modoptions
            GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);

        }

        public bool CanStartWarAgainstPlayer
        {
            get
            {
                return (ModCore.curGameTime - timeLastWarAgainstPlayer > ModOptions.instance.minMsTimeBetweenAttacksOnPlayerTurf) &&
                    MindControl.CurrentPlayerCharacter.IsAlive; //starting a war against the player when we're in the "wasted" screen would instantly end it
            }
        }

        #endregion

        #region spawn point setup

        /// <summary>
        /// stores nearby preset spawn points and attempts to set the allied spawn, returning true if it succeeded
        /// </summary>
        /// <param name="initialReferencePoint"></param>
        private bool PrepareAndSetupInitialSpawnPoint(Vector3 initialReferencePoint)
        {
            Logger.Log("setSpawnPoints: begin", 3);
            //spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos

            availableNearbyPresetSpawns = PotentialSpawnsForWars.GetAllPotentialSpawnsInRadiusFromPos
                (initialReferencePoint, ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar / 2);

            desiredNumberOfControlPointsForThisWar = RandoMath.ClampValue(availableNearbyPresetSpawns.Count,
                2,
                2 + (int)(warZone.GetUpgradePercentage() * MAX_EXTRA_CONTROL_POINTS));

            if(availableNearbyPresetSpawns.Count < 2)
            {
                UI.Notify("Less than 2 preset potential spawns were found nearby. One or both teams' spawns will be generated.");
            }


            if (availableNearbyPresetSpawns.Count > 0)
            {
                //find the closest preset spawn and set it as the allied CP
                int indexOfClosestSpawn = 0;
                float smallestDistance = ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar;

                for(int i = 0; i < availableNearbyPresetSpawns.Count; i++)
                {
                    float candidateDistance = initialReferencePoint.DistanceTo(availableNearbyPresetSpawns[i]);
                    if (candidateDistance < smallestDistance)
                    {
                        smallestDistance = candidateDistance;
                        indexOfClosestSpawn = i;
                    }
                }

                if(SetupAControlPoint(availableNearbyPresetSpawns[indexOfClosestSpawn], GangManager.instance.PlayerGang))
                {
                    availableNearbyPresetSpawns.RemoveAt(indexOfClosestSpawn);
                }
            }
            else
            {

                SetupAControlPoint(SpawnManager.instance.FindCustomSpawnPoint
                                (initialReferencePoint,
                                ModOptions.instance.GetAcceptableMemberSpawnDistance(10),
                                10,
                                5),
                                GangManager.instance.PlayerGang);
            }

            Logger.Log("setSpawnPoints: end", 3);

            return controlPoints.Count > 0;

        }

        /// <summary>
        /// if spawns are set, returns a random spawn that can be allied or hostile
        /// </summary>
        /// <returns></returns>
        public Vector3 GetRandomSpawnPoint()
        {
            if (controlPoints.Count > 0)
            {
                WarControlPoint pickedCP;

                if (RandoMath.RandomBool())
                {
                    pickedCP = RandoMath.RandomElement(alliedSpawnPoints);
                }
                else
                {
                    pickedCP =  RandoMath.RandomElement(enemySpawnPoints);
                }

                return pickedCP.position;
            }
            else
            {
                return Vector3.Zero;
            }

        }


        /// <summary>
        /// returns true if the distance to the target position from any of the player's (or enemy's) gang spawn points is below the distanceLimit
        /// </summary>
        /// <param name="position"></param>
        /// <param name="isEnemyTeam"></param>
        /// <param name="distanceLimit"></param>
        /// <returns></returns>
        //public bool IsPositionCloseToAnySpawnOfTeam(Vector3 position, bool isEnemyTeam, float distanceLimit = 0.5f)
        //{
        //    if (!spawnPointsSet) return false;

        //    List<Vector3> consideredSpawns = isEnemyTeam ? enemySpawnPoints : alliedSpawnPoints;

        //    foreach (Vector3 spawn in consideredSpawns)
        //    {
        //        if (World.GetDistance(position, spawn) <= distanceLimit)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        #endregion

        #region control point related

        /// <summary>
        /// returns true if the provided position is not zero
        /// </summary>
        private bool SetupAControlPoint(Vector3 targetPos, Gang ownerGang)
        {
            //if (!spawnPointsSet) return false;
            //for now, we're generating random control points, but they should be manually prepared in the future for better placement!
            //(and maybe then we'll load them all at once)

            
            //Vector3 possiblePointPos = SpawnManager.instance.FindCustomSpawnPoint(
            //    RandoMath.CenterOfVectors(alliedSpawnPoints[0], enemySpawnPoints[0]),
            //    ModOptions.instance.GetAcceptableMemberSpawnDistance(10),
            //    10,
            //    5);
            if (targetPos != Vector3.Zero)
            {
                WarControlPoint newPoint;
                if (pooledControlPoints.Count > 0)
                {
                    newPoint = pooledControlPoints[0];
                    pooledControlPoints.RemoveAt(0);
                }
                else
                {
                    newPoint = new WarControlPoint();
                }

                newPoint.SetupAtPosition(targetPos, ownerGang);
                if (ownerGang != null) ControlPointHasBeenCaptured(newPoint);
                controlPoints.Add(newPoint);
                return true;
            }

            return false;
        }

        public void ControlPointHasBeenCaptured(WarControlPoint capturedCP)
        {
            if(capturedCP.ownerGang == enemyGang)
            {
                alliedSpawnPoints.Remove(capturedCP);
            }
            else
            {
                enemySpawnPoints.Remove(capturedCP);
            }

            capturedCP.onCaptureCooldown = true;

        }

        /// <summary>
        /// a CP must "cool down" before being used as a spawn point
        /// </summary>
        /// <param name="capturedCP"></param>
        public void ControlPointHasCooledDown(WarControlPoint capturedCP)
        {
            if (capturedCP.ownerGang == enemyGang)
            {
                enemySpawnPoints.Add(capturedCP);
            }
            else
            {
                alliedSpawnPoints.Add(capturedCP);
            }

            capturedCP.onCaptureCooldown = false;

        }

        private void PoolAllControlPoints()
        {
            foreach (WarControlPoint cp in controlPoints)
            {
                cp.Disable();
                pooledControlPoints.Add(cp);
            }

            controlPoints.Clear();
        }

        /// <summary>
        /// gets a neutral or enemy point's position for this gang's members to head to
        /// </summary>
        /// <param name="gang"></param>
        /// <returns></returns>
        public Vector3 GetMoveTargetForGang(Gang gang)
        {
            WarControlPoint targetPoint = null;

            for (int i = 0; i < controlPoints.Count; i++)
            {
                if (targetPoint == null ||
                    (controlPoints[i].ownerGang != gang && targetPoint.ownerGang == gang) ||
                    (controlPoints[i].ownerGang != gang && targetPoint.ownerGang != gang && RandoMath.RandomBool()))
                {
                    targetPoint = controlPoints[i];
                }
            }

            return targetPoint.position;
        }

        /// <summary>
        /// returns the position of one of the control points for the provided gang
        /// </summary>
        /// <param name="gang"></param>
        /// <returns></returns>
        public Vector3 GetSpawnPositionForGang(Gang gang)
        {
            WarControlPoint pickedPoint = gang == enemyGang ? enemySpawnPoints.RandomElement() : alliedSpawnPoints.RandomElement();

            if(pickedPoint != null)
            {
                return pickedPoint.position;
            }
            else
            {
                return Vector3.Zero;
            }
        }

        #endregion

        /// <summary>
        ///    the battle was unfair if the player's gang had guns and the enemy gang hadn't
        ///    in this case, there is a possibility of the defeated gang instantly getting pistols
        ///    in order to at least not get decimated all the time
        /// </summary>
        private void CheckIfBattleWasUnfair()
        {
            if (enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
                GangManager.instance.PlayerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) != WeaponHash.Unarmed)
            {
                if (RandoMath.RandomBool())
                {
                    enemyGang.gangWeaponHashes.Add(RandoMath.RandomElement(ModOptions.instance.driveByWeapons));
                    GangManager.instance.SaveGangData(false);
                }
            }
        }

        #region spawn/death/culling handlers
        /// <summary>
        /// spawns a vehicle that has the player as destination
        /// </summary>
        public SpawnedDrivingGangMember SpawnAngryVehicle(bool isFriendly)
        {

            if (SpawnManager.instance.HasThinkingDriversLimitBeenReached()) return null;

            Math.Vector3 playerPos = MindControl.SafePositionNearPlayer,
                spawnPos = SpawnManager.instance.FindGoodSpawnPointForCar(playerPos);

            if (spawnPos == Vector3.Zero) return null;

            SpawnedDrivingGangMember spawnedVehicle = null;
            if (!isFriendly && spawnedEnemies - 4 < maxSpawnedEnemies)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(enemyGang,
                    spawnPos, playerPos, false, false, IncrementEnemiesCount);
            }
            else if (spawnedAllies - 4 < maxSpawnedAllies)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                    spawnPos, playerPos, false, false, IncrementAlliesCount);
            }

            return spawnedVehicle;
        }

        public void SpawnMember(bool isFriendly)
        {
            Vector3 spawnPos = GetSpawnPositionForGang(isFriendly ? GangManager.instance.PlayerGang : enemyGang);

            if (spawnPos == default) return; //this means we don't have spawn points set yet

            SpawnedGangMember spawnedMember = null;

            if (isFriendly)
            {
                if (spawnedAllies < maxSpawnedAllies)
                {
                    spawnedMember = SpawnManager.instance.SpawnGangMember(GangManager.instance.PlayerGang, spawnPos, onSuccessfulMemberSpawn: IncrementAlliesCount);
                }
                else return;

            }
            else
            {
                if (spawnedEnemies < maxSpawnedEnemies)
                {
                    spawnedMember = SpawnManager.instance.SpawnGangMember(enemyGang, spawnPos, onSuccessfulMemberSpawn: IncrementEnemiesCount);
                }
                else return;
            }
        }

        private void IncrementAlliesCount() { spawnedAllies++; }

        private void IncrementEnemiesCount() { spawnedEnemies++; }

        public void OnEnemyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (playerNearWarzone)
            {
                enemyReinforcements--;

                //have we lost too many? its a victory for the player then
                if (enemyReinforcements <= 0)
                {
                    EndWar(true);
                }
                else
                {
                    enemyNumText.Caption = enemyReinforcements.ToString();
                    //if we've lost too many people since the last time we changed spawn points,
                    //change them again!
                    //if (initialEnemyReinforcements - enemyReinforcements > 0 &&
                    //    ModOptions.instance.killsBetweenEnemySpawnReplacement > 0 &&
                    //    enemyReinforcements % ModOptions.instance.killsBetweenEnemySpawnReplacement == 0)
                    //{
                    //    if (spawnPointsSet)
                    //    {
                    //        ReplaceEnemySpawnPoint();
                    //    }

                    //}
                }

            }
        }

        public void OnAllyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (playerNearWarzone)
            {
                alliedReinforcements--;

                if (alliedReinforcements <= 0)
                {
                    EndWar(false);
                }
                else
                {
                    alliedNumText.Caption = alliedReinforcements.ToString();
                }
            }
        }

        public void TryWarBalancing(bool cullFriendlies)
        {
            Logger.Log("war balancing: start", 3);
            List<SpawnedGangMember> membersFromTargetGang =
                SpawnManager.instance.GetSpawnedMembersOfGang(cullFriendlies ? GangManager.instance.PlayerGang : enemyGang);

            for (int i = 0; i < membersFromTargetGang.Count; i++)
            {
                if (membersFromTargetGang[i].watchedPed == null) continue;
                //don't attempt to cull a friendly driving member because they could be a backup car called by the player...
                //and the player can probably take more advantage of any stuck friendly vehicle than the AI can
                if ((!cullFriendlies || !Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, membersFromTargetGang[i].watchedPed, false)) &&
                    !membersFromTargetGang[i].watchedPed.IsOnScreen)
                {
                    membersFromTargetGang[i].Die(true);
                    //make sure we don't exagerate!
                    //stop if we're back inside the limits
                    if ((cullFriendlies && spawnedAllies < maxSpawnedAllies) ||
                        (!cullFriendlies && spawnedEnemies < maxSpawnedEnemies))
                    {
                        break;
                    }
                }

                Yield();
            }

            Logger.Log("war balancing: end", 3);
        }


        public void DecrementSpawnedsNumber(bool memberWasFriendly)
        {
            if (memberWasFriendly)
            {
                spawnedAllies--;
                if (spawnedAllies < 0) spawnedAllies = 0;
            }
            else
            {
                spawnedEnemies--;
                if (spawnedEnemies < 0) spawnedEnemies = 0;
            }
        }

        #endregion

        /// <summary>
        /// true if the player is in the war zone or close enough to one of the war area blips
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerCloseToWar()
        {
            return IsPositionInsideWarzone(MindControl.CurrentPlayerCharacter.Position);
        }

        /// <summary>
        /// true if the position is in the war zone or close enough to one of the war area blips
        /// </summary>
        /// <returns></returns>
        public bool IsPositionInsideWarzone(Vector3 position)
        {
            if (World.GetZoneName(position) == warZone.zoneName) return true;

            foreach (Blip warAreaBlip in warAreaBlips)
            {
                if (warAreaBlip != null && warAreaBlip.Position != default)
                {
                    if (warAreaBlip.Position.DistanceTo2D(position) < ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// forces the hate relation level between the involved gangs (includes the player, if not a spectator)
        /// </summary>
        public void SetHateRelationsBetweenGangs()
        {
            World.SetRelationshipBetweenGroups(Relationship.Hate, enemyGang.relationGroupIndex, GangManager.instance.PlayerGang.relationGroupIndex);
            World.SetRelationshipBetweenGroups(Relationship.Hate, GangManager.instance.PlayerGang.relationGroupIndex, enemyGang.relationGroupIndex);

            if (!ModOptions.instance.playerIsASpectator)
            {
                World.SetRelationshipBetweenGroups(Relationship.Hate, enemyGang.relationGroupIndex, Game.Player.Character.RelationshipGroup);
                World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, enemyGang.relationGroupIndex);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                if (IsPlayerCloseToWar())
                {
                    Logger.Log("warmanager inside war tick: begin. spAllies: " + spawnedAllies.ToString() + " spEnemies: " + spawnedEnemies.ToString(), 5);
                    playerNearWarzone = true;
                    shouldDisplayReinforcementsTexts = true;
                    ticksSinceLastCarSpawn++;
                    ticksSinceLastBalanceCheck++;
                    //ticksSinceLastEnemyRelocation++;
                    curTicksAwayFromBattle = 0;

                    if (ModOptions.instance.freezeWantedLevelDuringWars)
                    {
                        Game.WantedMultiplier = 0;
                    }

                    AmbientGangMemberSpawner.instance.enabled = false;


                    if (ticksSinceLastCarSpawn > MIN_TICKS_BETWEEN_CAR_SPAWNS && RandoMath.RandomBool())
                    {
                        SpawnAngryVehicle(RandoMath.RandomBool());

                        ticksSinceLastCarSpawn = 0;
                    }

                    if (ticksSinceLastBalanceCheck > TICKS_BETWEEN_BALANCE_CHECKS)
                    {
                        ticksSinceLastBalanceCheck = 0;

                        int maxSpawns = ModOptions.instance.spawnedMemberLimit - ModOptions.instance.minSpawnsForEachSideDuringWars;
                        //control max spawns, so that a gang with 5 tickets won't spawn as much as before
                        reinforcementsAdvantage = alliedReinforcements / (float)(enemyReinforcements + alliedReinforcements);

                        maxSpawnedAllies = RandoMath.ClampValue((int)(ModOptions.instance.spawnedMemberLimit * reinforcementsAdvantage),
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue(alliedReinforcements, ModOptions.instance.minSpawnsForEachSideDuringWars, maxSpawns));

                        maxSpawnedEnemies = RandoMath.ClampValue(ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies,
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue
                                (enemyReinforcements,
                                ModOptions.instance.minSpawnsForEachSideDuringWars,
                                ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies));

                        if (spawnedAllies > maxSpawnedAllies)
                        {
                            //try removing some members that can't currently be seen by the player or are far enough
                            TryWarBalancing(true);
                        }
                        else if (spawnedEnemies > maxSpawnedEnemies)
                        {
                            TryWarBalancing(false);
                        }

                    }


                    if (controlPoints.Count < desiredNumberOfControlPointsForThisWar)
                    {
                        if (controlPoints.Count > 0)
                        {
                            if(availableNearbyPresetSpawns.Count > 0)
                            {
                                int presetSpawnIndex = RandoMath.CachedRandom.Next(availableNearbyPresetSpawns.Count);
                                if(SetupAControlPoint(availableNearbyPresetSpawns[presetSpawnIndex],
                                    enemySpawnPoints.Count >= desiredNumberOfControlPointsForThisWar * warZone.GetUpgradePercentage() ? null : enemyGang))
                                {
                                    availableNearbyPresetSpawns.RemoveAt(presetSpawnIndex);
                                }
                            }
                            else
                            {
                                SetupAControlPoint(SpawnManager.instance.FindCustomSpawnPoint
                                    (controlPoints[0].position,
                                    ModOptions.instance.GetAcceptableMemberSpawnDistance(10),
                                    10,
                                    5),
                                    enemySpawnPoints.Count >= desiredNumberOfControlPointsForThisWar * warZone.GetUpgradePercentage() ? null : enemyGang);
                            }
                        }
                        else
                        {
                            if (PrepareAndSetupInitialSpawnPoint(MindControl.SafePositionNearPlayer))
                            {
                                //if setting spawns succeeded this time, place the second war area here if it still hasn't been placed
                                if (warAreaBlips[1] == null)
                                {
                                    warAreaBlips[1] = World.CreateBlip(MindControl.SafePositionNearPlayer,
                                    ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
                                    warAreaBlips[1].Sprite = BlipSprite.BigCircle;
                                    warAreaBlips[1].Color = BlipColor.Red;
                                    warAreaBlips[1].Alpha = 175;
                                }
                            }
                        }

                    }


                    alliedPercentOfSpawnedMembers = spawnedAllies / RandoMath.Max(spawnedAllies + spawnedEnemies, 1.0f);

                    if (SpawnManager.instance.livingMembersCount < ModOptions.instance.spawnedMemberLimit)
                    {
                        SpawnMember(alliedPercentOfSpawnedMembers < reinforcementsAdvantage && spawnedAllies < maxSpawnedAllies);
                    }

                    //check one of the control points for capture
                    if (controlPoints.Count > 0)
                    {
                        if (nextCPIndexToCheckForCapture >= controlPoints.Count)
                        {
                            nextCPIndexToCheckForCapture = 0;
                        }

                        controlPoints[nextCPIndexToCheckForCapture].CheckIfHasBeenCaptured();

                        nextCPIndexToCheckForCapture++;
                    }


                    Logger.Log("warmanager inside war tick: end", 5);
                    Wait(MS_BETWEEN_WAR_TICKS);
                }
                else
                {
                    playerNearWarzone = false;
                    shouldDisplayReinforcementsTexts = false;
                    curTicksAwayFromBattle++;
                    AmbientGangMemberSpawner.instance.enabled = true;
                    if (curTicksAwayFromBattle > ModOptions.instance.ticksBeforeWarEndWithPlayerAway)
                    {
                        EndWar(GetSkippedWarResult(0.65f));
                    }
                }
                //if the player's gang leader is dead...
                if (!Game.Player.IsAlive && !MindControl.instance.HasChangedBody)
                {
                    //the war ends, but the outcome depends more on how well the player's side was doing
                    EndWar(GetSkippedWarResult(0.9f));
                    return;
                }
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            if (warBlip != null)
            {
                warBlip.Remove();

                foreach (Blip areaBlip in warAreaBlips)
                {
                    if (areaBlip != null)
                        areaBlip.Remove();
                }

            }

            PoolAllControlPoints();
        }
    }
}
