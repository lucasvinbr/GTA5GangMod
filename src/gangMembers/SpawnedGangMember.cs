using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace GTA.GangAndTurfMod
{
    public class SpawnedGangMember : UpdatedClass
    {

        public static string[] idleAnims =
        {
            "WORLD_HUMAN_SMOKING",
            "WORLD_HUMAN_HANG_OUT_STREET",
            "WORLD_HUMAN_DRINKING",
            "WORLD_HUMAN_GUARD_PATROL",
            "WORLD_HUMAN_STAND_GUARD",
            "WORLD_HUMAN_STAND_IMPATIENT",
            "WORLD_HUMAN_STAND_MOBILE",
        };

        public Ped watchedPed;

        public Gang myGang;

        /// <summary>
        /// used during wars
        /// </summary>
        public Vector3 moveDestination;


        public enum MemberStatus
        {
            none,
            onFootThinking,
            combat,
            inVehicle,
            parachuting
        }

        private const int UPDATES_BETWEEN_IDLE_CHANGE = 10;
        private int updatesSinceLastIdleChange = 0;

        public MemberStatus curStatus = MemberStatus.none;

        public bool hasDriveByGun = false;

        public int timeOfSpawn;

        /// <summary>
        /// does not trigger if the ped despawns due to being too far away
        /// </summary>
        public Action OnKilled;

        /// <summary>
        /// the position where we spawned (or at least the position we were in when the memberAI was attached to us)
        /// </summary>
        private Vector3 lastStuckCheckPosition;

        /// <summary>
        /// number of consecutive times this ped has failed the "is stuck" check
        /// </summary>
        private int stuckCounter = 0;

        private const int STUCK_COUNTER_LIMIT = 4;

        public override void Update()
        {
            Logger.Log("member update: start", 5);

            if (MindControl.CurrentPlayerCharacter == watchedPed)
            {
                Logger.Log("member update: end (isPlayer or etc)", 5);
                return;
            }

            float dist2DToPlyr = watchedPed.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position);

            if (curStatus == MemberStatus.parachuting)
            {
                if(!watchedPed.IsAlive || watchedPed.HeightAboveGround < 2.0f)
                {
                    //UI.ShowSubtitle("member no longer parachuting", 800);
                    watchedPed.BlockPermanentEvents = false;
                    watchedPed.AlwaysKeepTask = false;
                    watchedPed.IsCollisionProof = false;
                    curStatus = MemberStatus.none;
                }
                else
                {
                    Logger.Log("member update: end (still parachuting)", 5);

                    if (dist2DToPlyr > ModOptions.instance.maxDistanceMemberSpawnFromPlayer * 1.5f)
                    {
                        //we're too far to be important
                        Die();
                        Logger.Log("member update: end (parachuting: too far, despawn)", 5);
                        return;
                    }
                    return;
                }
            }

            if (!watchedPed.IsAlive)
            {
                if (GangWarManager.instance.focusedWar != null)
                {
                    GangWarManager.instance.focusedWar.MemberHasDiedNearWar(myGang);
                }
                OnKilled?.Invoke();
                Die(allowPreserving: watchedPed.IsOnScreen || dist2DToPlyr < ModOptions.instance.maxDistanceToPreserveKilledOffscreen);
                Logger.Log("member update: end (dead)", 5);
                return;
            }

            if (curStatus != MemberStatus.inVehicle)
            {
                watchedPed.BlockPermanentEvents = false;

                if (dist2DToPlyr >
               ModOptions.instance.maxDistanceMemberSpawnFromPlayer * 1.5f)
                {
                    //we're too far to be important
                    Die();
                    Logger.Log("member update: end (too far, despawn)", 5);
                    return;
                }

                if (RandoMath.RandomBool() && !watchedPed.IsInGroup && !watchedPed.IsInCombat)
                {
                    if (GangWarManager.instance.focusedWar != null)
                    {
                        //draw weapon!
                        watchedPed.Weapons.Select(watchedPed.Weapons.BestWeapon);
                        //instead of idling while in a war, members should head for one of the key locations
                        if (moveDestination == Vector3.Zero || watchedPed.Position.DistanceTo(moveDestination) < ModOptions.instance.distanceToCaptureWarControlPoint)
                            moveDestination = GangWarManager.instance.focusedWar.GetMoveTargetForGang(myGang, moveDestination);

                        if (moveDestination != Vector3.Zero)
                        {
                            watchedPed.Task.RunTo(moveDestination + RandoMath.RandomDirection(true));

                            if (lastStuckCheckPosition.DistanceTo(watchedPed.Position) <= 0.5f)
                            {
                                //maybe we're spawning inside a building?
                                stuckCounter++;
                                if (watchedPed.IsInWater)
                                {
                                    //in water and not moving, very likely to be a bad spawn!
                                    if (!watchedPed.IsOnScreen)
                                    {
                                        Logger.Log("member update: repositioning member stuck in water", 4);
                                        watchedPed.Position = SpawnManager.instance.FindGoodSpawnPointForMember();
                                        stuckCounter = 0;
                                        lastStuckCheckPosition = watchedPed.Position;
                                    }

                                }
                                else if (stuckCounter > STUCK_COUNTER_LIMIT)
                                {
                                    if (!watchedPed.IsOnScreen)
                                    {
                                        Logger.Log("member update: repositioning stuck member", 4);
                                        watchedPed.Position = SpawnManager.instance.FindGoodSpawnPointForMember();
                                        stuckCounter = 0;
                                        lastStuckCheckPosition = watchedPed.Position;
                                    }
                                }
                            }
                            else
                            {
                                lastStuckCheckPosition = watchedPed.Position;
                            }
                        }

                        curStatus = MemberStatus.onFootThinking;
                        updatesSinceLastIdleChange = 0;
                    }
                    else
                    {
                        if (curStatus != MemberStatus.onFootThinking || updatesSinceLastIdleChange > UPDATES_BETWEEN_IDLE_CHANGE)
                        {
                            curStatus = MemberStatus.onFootThinking;
                            if (RandoMath.RandomBool())
                            {
                                watchedPed.Task.WanderAround();
                            }
                            else
                            {
                                DoAnIdleAnim();
                            }
                            updatesSinceLastIdleChange = 0 - RandoMath.CachedRandom.Next(10);
                        }
                        else
                        {
                            updatesSinceLastIdleChange++;
                        }
                    }
                }
            }
            else
            {
                if (watchedPed.IsInVehicle())
                {
                    Vehicle curVehicle = watchedPed.CurrentVehicle;
                    if (!curVehicle.IsPersistent) //if our vehicle has reached its destination (no longer persistent, no longer with mod's driver AI attached)...
                    {
                        if (dist2DToPlyr >
               ModOptions.instance.roamingCarDespawnDistanceFromPlayer)
                        {
                            //we're too far to be important
                            Die();
                            Logger.Log("member update: end (in vehicle: too far, despawn)", 5);
                            return;
                        }


                        //if we were a driving member, we must be allowed to "be distracted" again
                        watchedPed.BlockPermanentEvents = false;

                        if (curVehicle.IsSeatFree(VehicleSeat.Driver))
                        {
                            //possibly leave the vehicle if the driver has left already
                            if (!watchedPed.IsUsingAnyVehicleWeapon())
                            {
                                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, watchedPed, 3, true); // BF_CanLeaveVehicle  

                                if (curVehicle.HeightAboveGround > 15.0f)
                                {
                                    StartParachuting(MindControl.SafePositionNearPlayer);
                                }
                                else
                                {
                                    watchedPed.Task.LeaveVehicle();
                                }

                                stuckCounter = 0;
                            }
                            else
                            {
                                watchedPed.Task.FightAgainstHatedTargets(200);
                            }
                        }
                        else
                        {
                            //even if we're inside an awesome vehicle, we should probably leave it if we appear to be stuck
                            if (curVehicle.Speed < 20)
                            {
                                stuckCounter++;

                                if (!watchedPed.IsInCombat)
                                {
                                    watchedPed.Task.FightAgainstHatedTargets(200);
                                }
                                else
                                {
                                    if (watchedPed.IsUsingAnyVehicleWeapon())
                                    {
                                        stuckCounter = 0;
                                    }
                                }

                                if (stuckCounter > STUCK_COUNTER_LIMIT)
                                {
                                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, watchedPed, 3, true); // BF_CanLeaveVehicle  

                                    if (curVehicle.Model.IsHelicopter)
                                    {
                                        if(watchedPed.SeatIndex != VehicleSeat.Driver)
                                        {
                                            StartParachuting(MindControl.SafePositionNearPlayer);
                                        }
                                    }
                                    else
                                    {
                                        watchedPed.Task.LeaveVehicle();
                                    }

                                    stuckCounter = 0;
                                }
                            }
                            else
                            {
                                stuckCounter = 0;
                            }
                        }
                    }

                }
                else
                {
                    curStatus = MemberStatus.none;
                }

            }

            Logger.Log("member update: end", 5);
        }

        /// <summary>
        /// decrements gangManager's living members count, also tells the war manager about it,
        /// clears this script's references, removes the ped's blip and marks the ped as no longer needed
        /// </summary>
        public void Die(bool alsoDelete = false, bool allowPreserving = false)
        {

            if (watchedPed != null)
            {

                if (GangWarManager.instance.focusedWar != null)
                {
                    GangWarManager.instance.focusedWar.DecrementSpawnedsFromGang(myGang);
                }
                if (watchedPed.CurrentBlip != null)
                {
                    watchedPed.CurrentBlip.Remove();
                }

                if (alsoDelete)
                {
                    watchedPed.Delete();
                }
                else
                {
                    if (allowPreserving)
                    {
                        SpawnManager.instance.PreserveDeadBody(watchedPed);
                    }
                    else
                    {
                        watchedPed.MarkAsNoLongerNeeded();
                    }
                }

            }

            OnKilled = null;
            myGang = null;

            curStatus = MemberStatus.none;
            stuckCounter = 0;
            moveDestination = Vector3.Zero;
            SpawnManager.instance.livingMembersCount--;
            hasDriveByGun = false;
            watchedPed = null;
        }


        /// <summary>
        /// does an ambient animation, like smoking
        /// </summary>
        public void DoAnIdleAnim()
        {
            Vector3 scenarioPos = World.GetNextPositionOnSidewalk(watchedPed.Position);
            Function.Call(Hash.TASK_START_SCENARIO_AT_POSITION, watchedPed, RandoMath.RandomElement(idleAnims),
                scenarioPos.X, scenarioPos.Y, scenarioPos.Z, RandoMath.RandomHeading(), 0, 0, 0);
        }

        public override void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangMemberAIUpdates + RandoMath.CachedRandom.Next(100);
        }

        public SpawnedGangMember(Ped watchedPed, Gang myGang, bool hasDriveByGun)
        {
            AttachData(watchedPed, myGang, hasDriveByGun);
            ResetUpdateInterval();
        }

        /// <summary>
        /// sets our watched ped, gang and other info that can be useful
        /// </summary>
        /// <param name="targetPed"></param>
        public void AttachData(Ped targetPed, Gang ourGang, bool hasDriveByGun)
        {
            this.watchedPed = targetPed;
            this.myGang = ourGang;
            this.hasDriveByGun = hasDriveByGun;
            this.lastStuckCheckPosition = targetPed.Position;

            timeOfSpawn = ModCore.curGameTime;
        }

        /// <summary>
        /// leaves our current vehicle (if any) and starts the parachuting procedure!
        /// (Cancels if the ped is currently controlled by the player)
        /// </summary>
        /// <param name="destination"></param>
        public void StartParachuting(Vector3 destination, int msWaitBeforeOpeningParachute = 0)
        {
            if (MindControl.currentlyControlledMember == this) return;

            if (destination == default || destination == Vector3.Zero) destination = MindControl.SafePositionNearPlayer;

            //UI.ShowSubtitle("member is parachuting!", 800);
            watchedPed.BlockPermanentEvents = true;
            watchedPed.AlwaysKeepTask = true;
            watchedPed.IsCollisionProof = ModOptions.instance.gangMembersAreFallproofWhileParachuting;
            //watchedPed.Task.LeaveVehicle();
            //watchedPed.Weapons.Give(WeaponHash.Parachute, 1, true, true);
            //watchedPed.Task.ParachuteTo(destination);
            watchedPed.Weapons.Give(WeaponHash.Parachute, 1, true, true);
            using (TaskSequence seq = new TaskSequence())
            {
                //seq.AddTask.Wait(msWaitBeforeOpeningParachute / 2);
                if (watchedPed.IsInVehicle())
                {
                    seq.AddTask.LeaveVehicle();
                }
                //seq.AddTask.Skydive();
                //seq.AddTask.Wait(msWaitBeforeOpeningParachute / 2);
                //seq.AddTask.UseParachute();
                seq.AddTask.ParachuteTo(destination);
                seq.Close();
                watchedPed.Task.PerformSequence(seq);
            };
            curStatus = MemberStatus.parachuting;
        }

        /// <summary>
        /// checks if we're not set to defensive (if a war is occurring and we're one of the involved gangs, that doesn't matter)
        /// </summary>
        /// <returns></returns>
        public bool ShouldStartFights()
        {
            return (ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.GangMemberAggressivenessMode.defensive ||
                    (GangWarManager.instance.focusedWar != null && (GangWarManager.instance.focusedWar.IsGangFightingInThisWar(myGang))));
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("SpawnedGangMember Data:");
            stringBuilder.AppendLine($"watchedPed is not null: {watchedPed != null}");
            stringBuilder.AppendLine($"My gang: {(myGang != null ? myGang.name : "NULL")}");
            stringBuilder.AppendLine($"Cur status: {curStatus}");
            stringBuilder.AppendLine($"Has DriveBy-usable gun: {hasDriveByGun}");
            stringBuilder.AppendLine($"War stuck counter: {this.stuckCounter}");
            stringBuilder.AppendLine($"War move destination: {moveDestination}");

            return stringBuilder.ToString();
        }

    }
}
