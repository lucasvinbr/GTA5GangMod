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

        public int attackerReinforcements, defenderReinforcements;

        /// <summary>
        /// numbers closer to 1 for defender advantage, less than 0.5 for attacker advantage.
        /// this advantage affects the member respawns:
        /// whoever has the greater advantage tends to have priority when spawning
        /// </summary>
        private float defenderReinforcementsAdvantage = 0.0f;

        private const int MS_TIME_BETWEEN_CAR_SPAWNS = 6000;

        //balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
        //we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
        private const int MS_TIME_BETWEEN_BALANCE_CHECKS = 5000;

        private const int MIN_LOSSES_PER_AUTORESOLVE_STEP = 6, MAX_LOSSES_PER_AUTORESOLVE_STEP = 17;

        private int msTimeWarStarted;

        private int msTimeOfLastAutoResolveStep = 0;

        private int msTimeOfLastCarSpawn = 0;

        private int msTimeOfLastBalanceCheck = 0;

        private int msTimeOfLastNoSpawnsPunishment = 0;

        private int maxSpawnedDefenders, maxSpawnedAttackers;

        private int spawnedDefenders = 0, spawnedAttackers = 0;

        public TurfZone warZone;

        public bool playerNearWarzone = false, isFocused = false;

        public Gang attackingGang, defendingGang;

        private Blip warBlip;

        /// <summary>
        /// index 0 is the area around the zone blip; 1 is the area around the player when the war starts
        /// (this one may not be used if the war was started by the AI and the player was away)
        /// </summary>
        private readonly Blip[] warAreaBlips;

        public List<WarControlPoint> attackerSpawnPoints, defenderSpawnPoints;

        private GangWarManager.AttackStrength curWarAtkStrength = GangWarManager.AttackStrength.light;

        public List<WarControlPoint> controlPoints = new List<WarControlPoint>();

        private List<Vector3> availableNearbyPresetSpawns;

        private int desiredNumberOfControlPointsForThisWar = 0;
        private int nextCPIndexToCheckForCapture = 0;

        private int allowedSpawnLimit = 0;

        /// <summary>
        /// TODO modoption?
        /// </summary>
        public const float PERCENT_SPAWNS_TO_USE_IN_AI_WAR = 0.8f;

        public Action<GangWar> OnReinforcementsChanged;
        public Action<GangWar> OnPlayerEnteredWarzone;
        public Action<GangWar> OnPlayerLeftWarzone;

        public delegate void OnWarEnded(GangWar endedWar, bool defenderVictory);

        public OnWarEnded onWarEnded;

        public GangWar()
        {
            warAreaBlips = new Blip[2];
            ResetUpdateInterval();
        }


        #region start/end/skip war
        public bool StartWar(Gang attackerGang, Gang defenderGang, TurfZone warZone, GangWarManager.AttackStrength attackStrength)
        {
            attackingGang = attackerGang;
            defendingGang = defenderGang;
            this.warZone = warZone;
            curWarAtkStrength = attackStrength;
            playerNearWarzone = false;

            warBlip = World.CreateBlip(warZone.zoneBlipPosition);
            warBlip.Sprite = BlipSprite.Deathmatch;
            warBlip.Color = BlipColor.Red;


            attackerSpawnPoints = new List<WarControlPoint>();
            defenderSpawnPoints = new List<WarControlPoint>();

            if (warAreaBlips[1] != null)
            {
                warAreaBlips[1].Remove();
                warAreaBlips[1] = null;
            }

            bool playerGangInvolved = IsPlayerGangInvolved();

            spawnedDefenders = SpawnManager.instance.GetSpawnedMembersOfGang(defenderGang).Count;
            spawnedAttackers = SpawnManager.instance.GetSpawnedMembersOfGang(attackerGang).Count;

            defenderReinforcements = GangCalculations.CalculateDefenderReinforcements(defenderGang, warZone);
            attackerReinforcements = GangCalculations.CalculateAttackerReinforcements(attackerGang, attackStrength);

            //if it's an AIvsAI fight, add the number of currently spawned members to the tickets!
            //this should prevent large masses of defenders from going poof when defending their newly taken zone
            if (!playerGangInvolved)
            {
                defenderReinforcements += spawnedDefenders;
                attackerReinforcements += spawnedAttackers;
            }

            defenderReinforcementsAdvantage = defenderReinforcements / (float)(attackerReinforcements + defenderReinforcements);


            warAreaBlips[0] = World.CreateBlip(warZone.zoneBlipPosition,
                ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
            warAreaBlips[0].Sprite = BlipSprite.BigCircle;
            warAreaBlips[0].Color = BlipColor.Red;
            warAreaBlips[0].Alpha = playerGangInvolved ? 175 : 25;

            if (playerGangInvolved)
            {
                warBlip.IsShortRange = false;
                warBlip.IsFlashing = true;

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (ModOptions.instance.notificationsEnabled && defenderGang == GangManager.instance.PlayerGang)
                {
                    //if the player is defending and already was inside the zone, we should take their current spawned members in consideration

                    defenderReinforcements = RandoMath.Max(defenderReinforcements, spawnedDefenders);

                    UI.Notify(string.Concat("The ", attackerGang.name, " are attacking ", warZone.zoneName, "! They are ",
                    attackerReinforcements.ToString(),
                    " against our ",
                    defenderReinforcements.ToString()));
                }
                    

                GangWarManager.instance.timeLastWarAgainstPlayer = ModCore.curGameTime;
                allowedSpawnLimit = ModOptions.instance.spawnedMemberLimit;
            }
            else
            {
                warBlip.IsShortRange = true;
                allowedSpawnLimit = (int)RandoMath.Max(ModOptions.instance.spawnedMemberLimit * PERCENT_SPAWNS_TO_USE_IN_AI_WAR,
                    ModOptions.instance.minSpawnsForEachSideDuringWars * 2);
            }

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War (", attackerGang.name, " attacking ", defenderGang.name + ")"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

            msTimeOfLastAutoResolveStep = ModCore.curGameTime;
            msTimeWarStarted = ModCore.curGameTime;
            

            maxSpawnedDefenders = (int)RandoMath.ClampValue(allowedSpawnLimit * defenderReinforcementsAdvantage,
                ModOptions.instance.minSpawnsForEachSideDuringWars,
                allowedSpawnLimit - ModOptions.instance.minSpawnsForEachSideDuringWars);

            maxSpawnedAttackers = RandoMath.Max
                (allowedSpawnLimit - maxSpawnedDefenders, ModOptions.instance.minSpawnsForEachSideDuringWars);

            Logger.Log(string.Concat("war started! Reinf advantage: ", defenderReinforcementsAdvantage.ToString(),
                " maxDefenders: ", maxSpawnedDefenders.ToString(), " maxAttackers: ", maxSpawnedAttackers.ToString()), 3);

            //this number may change once we're inside the zone and PrepareAndSetupInitialSpawnPoint is run
            desiredNumberOfControlPointsForThisWar = 2;

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
            float defenderBaseStr = defendingGang.GetGangVariedStrengthValue(),
                attackerBaseStr = attackingGang.GetGangVariedStrengthValue();

            //the amount of reinforcements counts here
            float totalDefenderStrength = defenderReinforcements / attackerBaseStr,
                totalAttackerStrength = attackerReinforcements / defenderBaseStr;

            bool playerGangInvolved = IsPlayerGangInvolved();

            if (playerGangInvolved)
            {
                if (defendingGang == GangManager.instance.PlayerGang)
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

                if (playerWon)
                {
                    //player gang was involved and won
                    AmbientGangMemberSpawner.instance.postWarBackupsRemaining = ModOptions.instance.postWarBackupsAmount;

                    MindControl.instance.AddOrSubtractMoneyToProtagonist
                        (battleProfit);

                    if (ModOptions.instance.notificationsEnabled)
                        UI.Notify("Victory rewards: $" + battleProfit.ToString());

                    if (defenderVictory)
                    {
                        UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                    }
                    else
                    {
                        UI.ShowSubtitle(warZone.zoneName + " is ours!");
                    }
                }
                else
                {
                    //player was involved and lost!
                    if (defenderVictory)
                    {
                        UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                    }
                    else
                    {
                        UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + attackingGang.name + "!");
                    }
                }

                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ScreenFlash", "WastedSounds");
            }
            else
            {
                if (defenderVictory)
                {
                    defendingGang.moneyAvailable += battleProfit;
                }
                else
                {
                    attackingGang.moneyAvailable += battleProfit;
                }
            }

            if (warBlip != null)
            {
                warBlip.Remove();

                foreach (Blip areaBlip in warAreaBlips)
                {
                    if (areaBlip != null)
                        areaBlip.Remove();
                }
            }

            playerNearWarzone = false;
            OnPlayerLeftWarzone?.Invoke(this);

            PoolAllControlPoints();

            if (!defenderVictory)
            {
                attackingGang.TakeZone(warZone);
            }

            onWarEnded?.Invoke(this, defenderVictory);

            


            //reset relations to whatever is set in modoptions
            GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);


        }

        /// <summary>
        /// reduces reinforcements on both sides (optionally applying the multiplier on the player gang if it's involved) and then checks if the war should end
        /// </summary>
        /// <param name="lossMultiplierOnPlayerGang"></param>
        public void RunAutoResolveStep(float lossMultiplierOnPlayerGang = 1.0f)
        {
            float defenderLosses = RandoMath.CachedRandom.Next(MIN_LOSSES_PER_AUTORESOLVE_STEP, MAX_LOSSES_PER_AUTORESOLVE_STEP);

            float attackerLosses = RandoMath.CachedRandom.Next(MIN_LOSSES_PER_AUTORESOLVE_STEP, MAX_LOSSES_PER_AUTORESOLVE_STEP);

            float biasTowardDefenders = defendingGang.GetGangVariedStrengthValue() / attackingGang.GetGangVariedStrengthValue();

            defenderLosses = RandoMath.ClampValue(defenderLosses / biasTowardDefenders, MIN_LOSSES_PER_AUTORESOLVE_STEP, MAX_LOSSES_PER_AUTORESOLVE_STEP);
            attackerLosses = RandoMath.ClampValue(attackerLosses * biasTowardDefenders, MIN_LOSSES_PER_AUTORESOLVE_STEP, MAX_LOSSES_PER_AUTORESOLVE_STEP);

            if (defendingGang.isPlayerOwned) defenderLosses *= lossMultiplierOnPlayerGang;
            if (attackingGang.isPlayerOwned) attackerLosses *= lossMultiplierOnPlayerGang;

            attackerReinforcements -= (int)attackerLosses;
            defenderReinforcements -= (int)defenderLosses;

            if (attackerReinforcements <= 0 || defenderReinforcements <= 0)
            {
                EndWar(attackerReinforcements <= 0); //favor the defenders if both sides ran out of reinforcements
            }
            else
            {
                //alliedNumText.Caption = defenderReinforcements.ToString();
                OnReinforcementsChanged?.Invoke(this);
            }

            msTimeOfLastAutoResolveStep = ModCore.curGameTime;
        }

        #endregion

        #region control point related

        /// <summary>
        /// stores nearby preset spawn points and attempts to set the allied spawn, returning true if it succeeded
        /// </summary>
        /// <param name="initialReferencePoint"></param>
        private bool PrepareAndSetupInitialSpawnPoint(Vector3 initialReferencePoint)
        {
            Logger.Log("setSpawnPoints: begin", 3);
            //spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos

            availableNearbyPresetSpawns = PotentialSpawnsForWars.GetAllPotentialSpawnsInRadiusFromPos
                (initialReferencePoint, ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar * 0.75f);

            desiredNumberOfControlPointsForThisWar = RandoMath.ClampValue(availableNearbyPresetSpawns.Count,
                RandoMath.Max(ModOptions.instance.warsMinNumControlPoints, 2),
                ModOptions.instance.warsMinNumControlPoints + (int)(warZone.GetUpgradePercentage() * ModOptions.instance.warsMaxExtraControlPoints));

            //if (availableNearbyPresetSpawns.Count < 2)
            //{
            //    UI.Notify("Less than 2 preset potential spawns were found nearby. One or both teams' spawns will be generated.");
            //}


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

                if (TrySetupAControlPoint(availableNearbyPresetSpawns[indexOfClosestSpawn], IsPlayerGangInvolved() ? GangManager.instance.PlayerGang : defendingGang))
                {
                    availableNearbyPresetSpawns.RemoveAt(indexOfClosestSpawn);
                }
            }
            else
            {

                TrySetupAControlPoint(SpawnManager.instance.FindCustomSpawnPoint
                                (initialReferencePoint,
                                ModOptions.instance.GetAcceptableMemberSpawnDistance(10),
                                10,
                                5),
                                IsPlayerGangInvolved() ? GangManager.instance.PlayerGang : defendingGang);
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
                    pickedCP = RandoMath.RandomElement(defenderSpawnPoints);
                }
                else
                {
                    pickedCP = RandoMath.RandomElement(attackerSpawnPoints);
                }

                return pickedCP.position;
            }
            else
            {
                return Vector3.Zero;
            }

        }


        /// <summary>
        /// returns true if the provided position is not zero and not too close to existing spawns
        /// and the point was set up successfully
        /// </summary>
        private bool TrySetupAControlPoint(Vector3 targetPos, Gang ownerGang)
        {
            if (targetPos != Vector3.Zero)
            {
                foreach(WarControlPoint cp in controlPoints)
                {
                    if(cp.position.DistanceTo(targetPos) < ModOptions.instance.minDistanceBetweenWarSpawns)
                    {
                        return false;
                    }
                }

                WarControlPoint newPoint = GangWarManager.instance.GetUnusedWarControlPoint();

                newPoint.SetupAtPosition(targetPos, ownerGang, this);
                if (ownerGang != null) ControlPointHasBeenCaptured(newPoint);
                controlPoints.Add(newPoint);
                return true;
            }

            return false;
        }

        public void ControlPointHasBeenCaptured(WarControlPoint capturedCP)
        {
            if (capturedCP.ownerGang != defendingGang)
            {
                defenderSpawnPoints.Remove(capturedCP);
            }

            if (capturedCP.ownerGang != attackingGang)
            {
                attackerSpawnPoints.Remove(capturedCP);
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
                attackerSpawnPoints.Add(capturedCP);
            }
            else if (capturedCP.ownerGang == defendingGang)
            {
                defenderSpawnPoints.Add(capturedCP);
            }

            capturedCP.onCaptureCooldown = false;

        }

        private void PoolAllControlPoints()
        {
            foreach (WarControlPoint cp in controlPoints)
            {
                GangWarManager.instance.PoolControlPoint(cp);
            }

            controlPoints.Clear();
        }

        private void HideAllControlPoints()
        {
            foreach (WarControlPoint cp in controlPoints)
            {
                cp.HideBlip();
            }
        }

        private void UpdateDisplayForAllControlPoints()
        {
            foreach (WarControlPoint cp in controlPoints)
            {
                cp.UpdateBlipAppearance();
            }
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

            if (targetPoint == null)
            {
                return MindControl.SafePositionNearPlayer;
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
            WarControlPoint pickedPoint = gang == attackingGang ? attackerSpawnPoints.RandomElement() : defenderSpawnPoints.RandomElement();

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
        public SpawnedDrivingGangMember SpawnAngryVehicle(bool isDefender)
        {

            if (SpawnManager.instance.HasThinkingDriversLimitBeenReached()) return null;

            Math.Vector3 playerPos = MindControl.SafePositionNearPlayer,
                spawnPos = SpawnManager.instance.FindGoodSpawnPointForCar(playerPos);

            if (spawnPos == Vector3.Zero) return null;

            SpawnedDrivingGangMember spawnedVehicle = null;
            if (!isDefender && spawnedAttackers - 4 < maxSpawnedAttackers)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(attackingGang,
                    spawnPos, playerPos, false, false, IncrementAttackersCount);
            }
            else if (spawnedDefenders - 4 < maxSpawnedDefenders)
            {
                spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(defendingGang,
                    spawnPos, playerPos, false, false, IncrementDefendersCount);
            }

            return spawnedVehicle;
        }

        public SpawnedGangMember SpawnMember(bool isDefender)
        {
            Vector3 spawnPos = GetSpawnPositionForGang(isDefender ? defendingGang : attackingGang);

            if (spawnPos == default) return null; //this means we don't have spawn points set yet

            if (isDefender)
            {
                if (spawnedDefenders < maxSpawnedDefenders)
                {
                    return SpawnManager.instance.SpawnGangMember(defendingGang, spawnPos, onSuccessfulMemberSpawn: IncrementDefendersCount);
                }
                else return null;

            }
            else
            {
                if (spawnedAttackers < maxSpawnedAttackers)
                {
                    return SpawnManager.instance.SpawnGangMember(attackingGang, spawnPos, onSuccessfulMemberSpawn: IncrementAttackersCount);
                }
                else return null;
            }
        }

        private void IncrementDefendersCount() { spawnedDefenders++; }

        private void IncrementAttackersCount() { spawnedAttackers++; }

        public void MemberHasDiedNearWar(Gang memberGang)
        {
            if (memberGang == defendingGang)
            {
                DecrementDefenderReinforcements();
            }
            else if (memberGang == attackingGang)
            {
                DecrementAttackerReinforcements();
            }
        }

        public void DecrementAttackerReinforcements()
        {
            attackerReinforcements--;

            //have we lost too many? its a victory for the defenders then
            if (attackerReinforcements <= 0)
            {
                EndWar(true);
            }
            else
            {
                //attack.Caption = attackerReinforcements.ToString();
                OnReinforcementsChanged?.Invoke(this);
            }
        }

        public void DecrementDefenderReinforcements()
        {
            defenderReinforcements--;

            if (defenderReinforcements <= 0)
            {
                EndWar(false);
            }
            else
            {
                //alliedNumText.Caption = defenderReinforcements.ToString();
                OnReinforcementsChanged?.Invoke(this);
            }
        }

        public void DecrementSpawnedsFromGang(Gang gang)
        {
            if(gang == defendingGang)
            {
                DecrementSpawnedsNumber(true);
            }
            else if(gang == attackingGang)
            {
                DecrementSpawnedsNumber(false);
            }
        }

        public void DecrementSpawnedsNumber(bool memberWasDefender)
        {
            if (memberWasDefender)
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

        /// <summary>
        /// if one of the involved gangs has too many or too few members,
        /// attempts to remove exceeding members from the involved gangs or any "interfering" ones
        /// </summary>
        public void ReassureWarBalance()
        {
            Logger.Log("war balancing: start", 3);
            List<SpawnedGangMember> allLivingMembers =
                SpawnManager.instance.GetAllLivingMembers();

            int minSpawns = ModOptions.instance.minSpawnsForEachSideDuringWars;

            foreach (SpawnedGangMember member in allLivingMembers)
            {
                if((spawnedAttackers >= minSpawns && spawnedAttackers <= maxSpawnedAttackers) &&
                   (spawnedDefenders >= minSpawns && spawnedDefenders <= maxSpawnedDefenders))
                {
                    break;
                }

                if (member.watchedPed == null) continue;
                //don't attempt to cull a friendly driving member because they could be a backup car called by the player...
                //and the player can probably take more advantage of any stuck friendly vehicle than the AI can
                if ((!member.myGang.isPlayerOwned || !Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, member.watchedPed, false)) &&
                    !member.watchedPed.IsOnScreen)
                {
                    //ok, it's fine to cull this member...
                    //but is it necessary right now?
                    if((member.myGang == attackingGang && spawnedAttackers > maxSpawnedAttackers) ||
                       (member.myGang == defendingGang && spawnedDefenders > maxSpawnedDefenders) ||
                       (!IsGangFightingInThisWar(member.myGang) && SpawnManager.instance.livingMembersCount >= allowedSpawnLimit &&
                            (spawnedAttackers < ModOptions.instance.minSpawnsForEachSideDuringWars ||
                             spawnedDefenders < ModOptions.instance.minSpawnsForEachSideDuringWars)))
                    {
                        member.Die(true);
                    }
                }
            }

            Logger.Log("war balancing: end", 3);
        }


        #endregion

        

        /// <summary>
        /// true if the position is in the war zone or close enough to one of the war area blips
        /// </summary>
        /// <returns></returns>
        public bool IsPositionInsideWarzone(Vector3 position)
        {
            if (warZone.IsLocationInside(World.GetZoneName(position), position)) return true;

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

        public bool IsGangFightingInThisWar(Gang gang)
        {
            return defendingGang == gang || attackingGang == gang;
        }

        /// <summary>
        /// true if the player is in the war zone or close enough to one of the war area blips
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerCloseToWar()
        {
            return IsPositionInsideWarzone(MindControl.CurrentPlayerCharacter.Position);
        }


        public void OnBecameFocusedWar()
        {
            isFocused = true;
            UpdateDisplayForAllControlPoints();
        }

        public void OnNoLongerFocusedWar()
        {
            isFocused = false;
            HideAllControlPoints();
            //hide the "redder" area blip
            if (warAreaBlips[1] != null)
            {
                warAreaBlips[1].Remove();
                warAreaBlips[1] = null;
            }
        }

        public override void Update()
        {
            if (IsPlayerCloseToWar())
            {
                Logger.Log("warmanager inside war tick: begin. spDefenders: " + spawnedDefenders.ToString() + " spAttackers: " + spawnedAttackers.ToString(), 5);
                if (!playerNearWarzone)
                {
                    OnPlayerEnteredWarzone?.Invoke(this);
                }
                playerNearWarzone = true;

                if (isFocused)
                {
                    int curTime = ModCore.curGameTime;
                    msTimeOfLastAutoResolveStep = curTime;

                    if (ModOptions.instance.freezeWantedLevelDuringWars)
                    {
                        Game.WantedMultiplier = 0;
                    }


                    if (curTime - msTimeOfLastCarSpawn > MS_TIME_BETWEEN_CAR_SPAWNS && RandoMath.RandomBool())
                    {
                        SpawnAngryVehicle(spawnedDefenders < maxSpawnedDefenders);

                        msTimeOfLastCarSpawn = curTime;
                    }

                    if (curTime - msTimeOfLastBalanceCheck > MS_TIME_BETWEEN_BALANCE_CHECKS)
                    {
                        msTimeOfLastBalanceCheck = curTime;

                        int maxSpawns = allowedSpawnLimit - ModOptions.instance.minSpawnsForEachSideDuringWars;
                        //control max spawns, so that a gang with 5 tickets won't spawn as much as before
                        defenderReinforcementsAdvantage = defenderReinforcements / (float)(attackerReinforcements + defenderReinforcements);

                        maxSpawnedDefenders = RandoMath.ClampValue((int)(maxSpawns * defenderReinforcementsAdvantage),
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue(defenderReinforcements, ModOptions.instance.minSpawnsForEachSideDuringWars, maxSpawns));

                        maxSpawnedAttackers = RandoMath.ClampValue(allowedSpawnLimit - maxSpawnedDefenders,
                            ModOptions.instance.minSpawnsForEachSideDuringWars,
                            RandoMath.ClampValue
                                (attackerReinforcements,
                                ModOptions.instance.minSpawnsForEachSideDuringWars,
                                maxSpawns));

                        ReassureWarBalance();

                    }


                    if (controlPoints.Count < desiredNumberOfControlPointsForThisWar)
                    {
                        if (controlPoints.Count > 0)
                        {
                            if (availableNearbyPresetSpawns.Count > 0)
                            {
                                int presetSpawnIndex = RandoMath.CachedRandom.Next(availableNearbyPresetSpawns.Count);

                                TrySetupAControlPoint(availableNearbyPresetSpawns[presetSpawnIndex],
                                    attackerSpawnPoints.Count >= 1 ? defendingGang : attackingGang);
                                
                                //remove this potential spawn, even if we fail,
                                //so that we don't spend time testing (and failing) again
                                availableNearbyPresetSpawns.RemoveAt(presetSpawnIndex);
                            }
                            else
                            {
                                TrySetupAControlPoint(SpawnManager.instance.FindCustomSpawnPoint
                                    (controlPoints[0].position,
                                    ModOptions.instance.GetAcceptableMemberSpawnDistance(10),
                                    ModOptions.instance.minDistanceMemberSpawnFromPlayer,
                                    5),
                                    attackerSpawnPoints.Count >= desiredNumberOfControlPointsForThisWar * warZone.GetUpgradePercentage() ? defendingGang : attackingGang);
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
                    else
                    {
                        if(curTime - msTimeOfLastNoSpawnsPunishment > ModOptions.instance.msTimeBetweenWarPunishingForNoSpawns)
                        {
                            msTimeOfLastNoSpawnsPunishment = curTime;

                            //decrement reinforcements of any side with no spawn points!
                            if(attackerSpawnPoints.Count == 0)
                            {
                                DecrementAttackerReinforcements();
                            }

                            if(defenderSpawnPoints.Count == 0)
                            {
                                DecrementDefenderReinforcements();
                            }
                        }
                    }


                    if (SpawnManager.instance.livingMembersCount < allowedSpawnLimit)
                    {
                        SpawnMember(spawnedDefenders < maxSpawnedDefenders && defenderSpawnPoints.Count > 0);
                    }

                    //check one of the control points for capture
                    if (controlPoints.Count > 0)
                    {
                        if (nextCPIndexToCheckForCapture >= controlPoints.Count)
                        {
                            nextCPIndexToCheckForCapture = 0;
                        }

                        WarControlPoint curCheckedCP = controlPoints[nextCPIndexToCheckForCapture];

                        bool pointIsSafe = (((ModCore.curGameTime - msTimeWarStarted < ModOptions.instance.msTimeBeforeEnemySpawnsCanBeCaptured) && curCheckedCP.ownerGang != null) ||
                            !curCheckedCP.CheckIfHasBeenCaptured());

                        if (pointIsSafe && curCheckedCP.onCaptureCooldown)
                        {
                            ControlPointHasCooledDown(curCheckedCP);
                        }

                        nextCPIndexToCheckForCapture++;
                    }
                }

                Logger.Log("warmanager inside war tick: end", 5);
            }
            else
            {
                if (playerNearWarzone)
                {
                    OnPlayerLeftWarzone?.Invoke(this);
                }

                playerNearWarzone = false;
                if (ModCore.curGameTime - msTimeOfLastAutoResolveStep > ModOptions.instance.msTimeBetweenWarAutoResolveSteps)
                {
                    RunAutoResolveStep(1.15f);
                }
            }
            //if the player's gang leader is dead...
            if (!Game.Player.IsAlive && !MindControl.instance.HasChangedBody)
            {
                RunAutoResolveStep(1.05f);
                return;
            }
        }

        public override void ResetUpdateInterval()
        {
            ticksBetweenUpdates = GangWarManager.TICKS_BETWEEN_WAR_UPDATES;
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
    }
}
