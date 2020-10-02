using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    public class GangWar : UpdatedClass
    {

        public enum AttackStrength
        {
            light,
            medium,
            large,
            massive
        }

        public int attackerReinforcements, defenderReinforcements;

        public bool isOccurring = false;

        private int ticksSinceLastAutoResolveStep = 0;

        /// <summary>
        /// numbers closer to 1 for defender advantage, less than 0.5 for attacker advantage.
        /// this advantage affects the member respawns:
        /// whoever has the greater advantage tends to have priority when spawning
        /// </summary>
        private float defenderReinforcementsAdvantage = 0.0f;

        private float alliedPercentOfSpawnedMembers;

        private const int UPDATES_BETWEEN_CAR_SPAWNS = 10;

        //balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
        //we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
        private const int TICKS_BETWEEN_BALANCE_CHECKS = 14;

        private int ticksSinceLastCarSpawn = 0;

        private int ticksSinceLastBalanceCheck = 0;

        private int timeLastWarAgainstPlayer = 0;

        private int maxSpawnedDefenders, maxSpawnedAttackers;

        private int spawnedDefenders = 0, spawnedAttackers = 0;

        public TurfZone warZone;

        public bool playerNearWarzone = false;

        public Gang attackingGang, defendingGang;

        private Blip warBlip;

        /// <summary>
        /// index 0 is the area around the zone blip; 1 is the area around the player when the war starts
        /// (this one may not be used if the war was started by the AI and the player was away)
        /// </summary>
        private readonly Blip[] warAreaBlips;

        public List<WarControlPoint> enemySpawnPoints, alliedSpawnPoints;

        private AttackStrength curWarAtkStrength = AttackStrength.light;

        public bool shouldDisplayReinforcementsTexts = false;

        public List<WarControlPoint> controlPoints = new List<WarControlPoint>();

        private List<Vector3> availableNearbyPresetSpawns;

        private int desiredNumberOfControlPointsForThisWar = 0;
        private int nextCPIndexToCheckForCapture = 0;

        private int allowedSpawnLimit = 0;

        /// <summary>
        /// TODO modoption?
        /// </summary>
        public const float PERCENT_SPAWNS_TO_USE_IN_AI_WAR = 0.75f;

        public GangWar()
        {
            warAreaBlips = new Blip[2];
        }


        #region start/end/skip war
        public bool StartWar(Gang attackerGang, Gang defenderGang, TurfZone warZone, AttackStrength attackStrength)
        {
            if (isOccurring) return false;


            attackingGang = attackerGang;
            defendingGang = defenderGang;
            this.warZone = warZone;
            curWarAtkStrength = attackStrength;
            playerNearWarzone = false;

            warBlip = World.CreateBlip(warZone.zoneBlipPosition);
            warBlip.IsFlashing = true;
            warBlip.Sprite = BlipSprite.Deathmatch;
            warBlip.Color = BlipColor.Red;
            

            enemySpawnPoints = new List<WarControlPoint>();
            alliedSpawnPoints = new List<WarControlPoint>();

            if (warAreaBlips[1] != null)
            {
                warAreaBlips[1].Remove();
                warAreaBlips[1] = null;
            }

            bool alreadyInsideWarzone = IsPositionInsideWarzone(MindControl.SafePositionNearPlayer);
            bool playerGangInvolved = IsPlayerGangInvolved();

            defenderReinforcements = GangCalculations.CalculateDefenderReinforcements(defenderGang, warZone);
            attackerReinforcements = GangCalculations.CalculateAttackerReinforcements(attackerGang, attackStrength);

            defenderReinforcementsAdvantage = defenderReinforcements / (float)(attackerReinforcements + defenderReinforcements);


            if (playerGangInvolved)
            {
                warBlip.IsShortRange = false;

                warAreaBlips[0] = World.CreateBlip(warZone.zoneBlipPosition,
                ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
                warAreaBlips[0].Sprite = BlipSprite.BigCircle;
                warAreaBlips[0].Color = BlipColor.Red;
                warAreaBlips[0].Alpha = 175;

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

                

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (ModOptions.instance.notificationsEnabled && defenderGang == GangManager.instance.PlayerGang)
                    UI.Notify(string.Concat("The ", attackerGang.name, " are attacking ", warZone.zoneName, "! They are ",
                    attackerReinforcements.ToString(),
                    " against our ",
                    defenderReinforcements.ToString()));
            }
            else
            {
                warBlip.IsShortRange = true;
                allowedSpawnLimit = (int) RandoMath.Max(ModOptions.instance.spawnedMemberLimit * PERCENT_SPAWNS_TO_USE_IN_AI_WAR,
                    ModOptions.instance.minSpawnsForEachSideDuringWars * 2);
            }

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War (", defenderGang.name, " versus ", attackerGang.name + ")"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

            ticksSinceLastAutoResolveStep = 0;

            spawnedDefenders = SpawnManager.instance.GetSpawnedMembersOfGang(defenderGang).Count;
            spawnedAttackers = SpawnManager.instance.GetSpawnedMembersOfGang(attackerGang).Count;

            maxSpawnedDefenders = (int) RandoMath.ClampValue(allowedSpawnLimit * defenderReinforcementsAdvantage,
                ModOptions.instance.minSpawnsForEachSideDuringWars,
                ModOptions.instance.spawnedMemberLimit - ModOptions.instance.minSpawnsForEachSideDuringWars);

            maxSpawnedAttackers = RandoMath.Max
                (allowedSpawnLimit - maxSpawnedDefenders, ModOptions.instance.minSpawnsForEachSideDuringWars);

            Logger.Log(string.Concat("war started! Reinf advantage: ", defenderReinforcementsAdvantage.ToString(),
                " maxDefenders: ", maxSpawnedDefenders.ToString(), " maxAttackers: ", maxSpawnedAttackers.ToString()), 3);

            isOccurring = true;

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

        /// <summary>
        /// checks both gangs' situations and the amount of reinforcements left for each side.
        /// also considers their strength (with variations) in order to decide the likely outcome of this battle.
        /// returns true for a defender victory, false if the attackers won
        /// </summary>
        public bool GetSkippedWarResult(float playerGangStrengthFactor = 1.0f)
        {
            int defenderBaseStr = defendingGang.GetGangVariedStrengthValue(),
                attackerBaseStr = attackingGang.GetGangVariedStrengthValue();

            //the amount of reinforcements counts here
            float totalDefenderStrength = defenderBaseStr +
                RandoMath.Max(4, defenderBaseStr / 100) * defenderReinforcements,
                totalAttackerStrength = attackerBaseStr +
                RandoMath.Max(4, attackerBaseStr / 100) * attackerReinforcements;

            bool playerGangInvolved = IsPlayerGangInvolved();

            if (playerGangInvolved)
            {
                if(defendingGang == GangManager.instance.PlayerGang)
                {
                    totalDefenderStrength *= playerGangStrengthFactor;
                }
                else
                {
                    totalAttackerStrength *= playerGangStrengthFactor;
                }
            }

            bool defenderVictory = totalDefenderStrength > totalAttackerStrength;

            if (playerGangInvolved)
            {
                Gang enemyGang = attackingGang;
                float strengthProportion = totalDefenderStrength / totalAttackerStrength;

                if (attackingGang.isPlayerOwned)
                {
                    enemyGang = defendingGang;
                    strengthProportion = totalAttackerStrength / totalDefenderStrength;
                }
                

                string battleReport = "Battle report: We";

                //we attempt to provide a little report on what happened
                if (defenderVictory)
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
            }

            

            return defenderVictory;
        }

        public void EndWar(bool defenderVictory)
        {
            Gang loserGang = defenderVictory ? attackingGang : defendingGang;

            int battleProfit = GangCalculations.CalculateBattleRewards
                (loserGang, defenderVictory ? (int)curWarAtkStrength : warZone.value, defenderVictory);

            if (IsPlayerGangInvolved())
            {
                bool playerWon = !loserGang.isPlayerOwned;

                AmbientGangMemberSpawner.instance.postWarBackupsRemaining = ModOptions.instance.postWarBackupsAmount;

                MindControl.instance.AddOrSubtractMoneyToProtagonist
                    (battleProfit);

                if (ModOptions.instance.notificationsEnabled)
                    UI.Notify("Victory rewards: $" + battleProfit.ToString());

                if (curWarType == WarType.attackingEnemy)
                {
                    UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                }
                else
                {
                    UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + attackingGang.name + "!");
                }

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ScreenFlash", "WastedSounds");
            }

            if (defenderVictory)
            {
                defendingGang.moneyAvailable += battleProfit;
                

                

                
            }
            else
            {
                attackingGang.TakeZone(warZone);

                attackingGang.moneyAvailable += battleProfit;
                
            }

            
            warBlip.Remove();
            for (int i = 0; i < warAreaBlips.Length; i++)
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

            if (availableNearbyPresetSpawns.Count < 2)
            {
                UI.Notify("Less than 2 preset potential spawns were found nearby. One or both teams' spawns will be generated.");
            }


            if (availableNearbyPresetSpawns.Count > 0)
            {
                //find the closest preset spawn and set it as the allied CP
                int indexOfClosestSpawn = 0;
                float smallestDistance = ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar;

                for (int i = 0; i < availableNearbyPresetSpawns.Count; i++)
                {
                    float candidateDistance = initialReferencePoint.DistanceTo(availableNearbyPresetSpawns[i]);
                    if (candidateDistance < smallestDistance)
                    {
                        smallestDistance = candidateDistance;
                        indexOfClosestSpawn = i;
                    }
                }

                if (SetupAControlPoint(availableNearbyPresetSpawns[indexOfClosestSpawn], GangManager.instance.PlayerGang))
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
                    pickedCP = RandoMath.RandomElement(enemySpawnPoints);
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
            if (capturedCP.ownerGang == attackingGang)
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
            if (capturedCP.ownerGang == attackingGang)
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
            WarControlPoint pickedPoint = gang == attackingGang ? enemySpawnPoints.RandomElement() : alliedSpawnPoints.RandomElement();

            if (pickedPoint != null)
            {
                return pickedPoint.position;
            }
            else
            {
                return Vector3.Zero;
            }
        }

        #endregion


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
            if (!isFriendly && spawnedAttackers - 4 < maxSpawnedAttackers)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(attackingGang,
                    spawnPos, playerPos, false, false, IncrementEnemiesCount);
            }
            else if (spawnedDefenders - 4 < maxSpawnedDefenders)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                    spawnPos, playerPos, false, false, IncrementAlliesCount);
            }

            return spawnedVehicle;
        }

        public void SpawnMember(bool isFriendly)
        {
            Vector3 spawnPos = GetSpawnPositionForGang(isFriendly ? GangManager.instance.PlayerGang : attackingGang);

            if (spawnPos == default) return; //this means we don't have spawn points set yet

            SpawnedGangMember spawnedMember = null;

            if (isFriendly)
            {
                if (spawnedDefenders < maxSpawnedDefenders)
                {
                    spawnedMember = SpawnManager.instance.SpawnGangMember(GangManager.instance.PlayerGang, spawnPos, onSuccessfulMemberSpawn: IncrementAlliesCount);
                }
                else return;

            }
            else
            {
                if (spawnedAttackers < maxSpawnedAttackers)
                {
                    spawnedMember = SpawnManager.instance.SpawnGangMember(attackingGang, spawnPos, onSuccessfulMemberSpawn: IncrementEnemiesCount);
                }
                else return;
            }
        }

        private void IncrementAlliesCount() { spawnedDefenders++; }

        private void IncrementEnemiesCount() { spawnedAttackers++; }

        public void OnEnemyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (playerNearWarzone)
            {
                attackerReinforcements--;

                //have we lost too many? its a victory for the player then
                if (attackerReinforcements <= 0)
                {
                    EndWar(true);
                }
                else
                {
                    enemyNumText.Caption = attackerReinforcements.ToString();
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
                defenderReinforcements--;

                if (defenderReinforcements <= 0)
                {
                    EndWar(false);
                }
                else
                {
                    alliedNumText.Caption = defenderReinforcements.ToString();
                }
            }
        }

        public void TryWarBalancing(bool cullFriendlies)
        {
            Logger.Log("war balancing: start", 3);
            List<SpawnedGangMember> membersFromTargetGang =
                SpawnManager.instance.GetSpawnedMembersOfGang(cullFriendlies ? GangManager.instance.PlayerGang : attackingGang);

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
                    if ((cullFriendlies && spawnedDefenders < maxSpawnedDefenders) ||
                        (!cullFriendlies && spawnedAttackers < maxSpawnedAttackers))
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
                spawnedDefenders--;
                if (spawnedDefenders < 0) spawnedDefenders = 0;
            }
            else
            {
                spawnedAttackers--;
                if (spawnedAttackers < 0) spawnedAttackers = 0;
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
            World.SetRelationshipBetweenGroups(Relationship.Hate, attackingGang.relationGroupIndex, defendingGang.relationGroupIndex);
            World.SetRelationshipBetweenGroups(Relationship.Hate, defendingGang.relationGroupIndex, attackingGang.relationGroupIndex);

            if (!ModOptions.instance.playerIsASpectator && IsPlayerGangInvolved())
            {
                Gang enemyGang = defendingGang == GangManager.instance.PlayerGang ? attackingGang : defendingGang;
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
                    Logger.Log("warmanager inside war tick: begin. spAllies: " + spawnedDefenders.ToString() + " spEnemies: " + spawnedAttackers.ToString(), 5);
                    playerNearWarzone = true;
                    shouldDisplayReinforcementsTexts = true;
                    ticksSinceLastCarSpawn++;
                    ticksSinceLastBalanceCheck++;
                    //ticksSinceLastEnemyRelocation++;
                    ticksSinceLastAutoResolveStep = 0;

                    if (ModOptions.instance.freezeWantedLevelDuringWars)
                    {
                        Game.WantedMultiplier = 0;
                    }

                    AmbientGangMemberSpawner.instance.enabled = false;


                    if (ticksSinceLastCarSpawn > UPDATES_BETWEEN_CAR_SPAWNS && RandoMath.RandomBool())
                    {
                        SpawnAngryVehicle(RandoMath.RandomBool());

                        ticksSinceLastCarSpawn = 0;
                    }

                    if (ticksSinceLastBalanceCheck > TICKS_BETWEEN_BALANCE_CHECKS)
                    {
                        ticksSinceLastBalanceCheck = 0;

                        int maxSpawns = ModOptions.instance.spawnedMemberLimit - ModOptions.instance.minSpawnsForEachSideDuringWars;
                        //control max spawns, so that a gang with 5 tickets won't spawn as much as before
                        defenderReinforcementsAdvantage = defenderReinforcements / (float)(attackerReinforcements + defenderReinforcements);

                        maxSpawnedDefenders = RandoMath.ClampValue((int)(ModOptions.instance.spawnedMemberLimit * defenderReinforcementsAdvantage),
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue(defenderReinforcements, ModOptions.instance.minSpawnsForEachSideDuringWars, maxSpawns));

                        maxSpawnedAttackers = RandoMath.ClampValue(ModOptions.instance.spawnedMemberLimit - maxSpawnedDefenders,
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue
                                (attackerReinforcements,
                                ModOptions.instance.minSpawnsForEachSideDuringWars,
                                ModOptions.instance.spawnedMemberLimit - maxSpawnedDefenders));

                        if (spawnedDefenders > maxSpawnedDefenders)
                        {
                            //try removing some members that can't currently be seen by the player or are far enough
                            TryWarBalancing(true);
                        }
                        else if (spawnedAttackers > maxSpawnedAttackers)
                        {
                            TryWarBalancing(false);
                        }

                    }


                    if (controlPoints.Count < desiredNumberOfControlPointsForThisWar)
                    {
                        if (controlPoints.Count > 0)
                        {
                            if (availableNearbyPresetSpawns.Count > 0)
                            {
                                int presetSpawnIndex = RandoMath.CachedRandom.Next(availableNearbyPresetSpawns.Count);
                                if (SetupAControlPoint(availableNearbyPresetSpawns[presetSpawnIndex],
                                    enemySpawnPoints.Count >= 1 + desiredNumberOfControlPointsForThisWar * warZone.GetUpgradePercentage() ? null : attackingGang))
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
                                    enemySpawnPoints.Count >= desiredNumberOfControlPointsForThisWar * warZone.GetUpgradePercentage() ? null : attackingGang);
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


                    alliedPercentOfSpawnedMembers = spawnedDefenders / RandoMath.Max(spawnedDefenders + spawnedAttackers, 1.0f);

                    if (SpawnManager.instance.livingMembersCount < ModOptions.instance.spawnedMemberLimit)
                    {
                        SpawnMember(alliedPercentOfSpawnedMembers < defenderReinforcementsAdvantage && spawnedDefenders < maxSpawnedDefenders);
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
                    Wait(EEN_WAR_TICKS);
                }
                else
                {
                    playerNearWarzone = false;
                    shouldDisplayReinforcementsTexts = false;
                    ticksSinceLastAutoResolveStep++;
                    AmbientGangMemberSpawner.instance.enabled = true;
                    if (ticksSinceLastAutoResolveStep > ModOptions.instance.ticksBetweenWarAutoResolveSteps)
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

        public void Abort()
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

        public bool IsPlayerGangInvolved()
        {
            return attackingGang == GangManager.instance.PlayerGang || defendingGang == GangManager.instance.PlayerGang;
        }

        /// <summary>
        /// always returns the defender if the player's gang isn't involved
        /// </summary>
        /// <returns></returns>
        public Gang GetEnemyGangIfPlayerInvolved()
        {
            return defendingGang == GangManager.instance.PlayerGang ? attackingGang : defendingGang;
        }

        public override void Update()
        {

        }
    }
}
