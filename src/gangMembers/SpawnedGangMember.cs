using GTA.Math;
using GTA.Native;
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
            inVehicle
        }

        public const int ticksBetweenIdleChange = 10;
        private int ticksSinceLastIdleChange = 0;

        public MemberStatus curStatus = MemberStatus.none;

        public bool hasDriveByGun = false;

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

            if ((watchedPed.IsInAir) || MindControl.CurrentPlayerCharacter == watchedPed)
            {
                Logger.Log("member update: end (isPlayer or etc)", 5);
                return;
            }

            if (!watchedPed.IsAlive)
            {
                if (GangWarManager.instance.focusedWar != null)
                {
                    GangWarManager.instance.focusedWar.MemberHasDiedNearWar(myGang);
                }
                Die(allowPreserving: true);
                Logger.Log("member update: end (dead)", 5);
                return;
            }

            if (curStatus != MemberStatus.inVehicle)
            {
                watchedPed.BlockPermanentEvents = false;

                if (watchedPed.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
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
                        //instead of idling while in a war, members should head for one of the key locations
                        
                        if(moveDestination == Vector3.Zero || watchedPed.Position.DistanceTo(moveDestination) < ModOptions.instance.distanceToCaptureWarControlPoint)
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
                        ticksSinceLastIdleChange = 0;
                    }
                    else
                    {
                        if (curStatus != MemberStatus.onFootThinking || ticksSinceLastIdleChange > ticksBetweenIdleChange)
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
                            ticksSinceLastIdleChange = 0 - RandoMath.CachedRandom.Next(10);
                        }
                        else
                        {
                            ticksSinceLastIdleChange++;
                        }
                    }
                }
            }
            else
            {
                if (watchedPed.IsInVehicle())
                {
                    Vehicle curVehicle = watchedPed.CurrentVehicle;
                    if (!curVehicle.IsPersistent) //if our vehicle has reached its destination (= no longer persistent, no longer with driver AI attached)...
                    {
                        if (watchedPed.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
               ModOptions.instance.maxDistanceCarSpawnFromPlayer * 3)
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
                            if (!Function.Call<bool>(Hash.DOES_VEHICLE_HAVE_WEAPONS, curVehicle))
                            {
                                watchedPed.Task.LeaveVehicle();
                                stuckCounter = 0;
                            }
                        }
                        else
                        {
                            //even if we're inside an awesome vehicle, we should probably leave it if we appear to be stuck
                            if(!watchedPed.IsInCombat && curVehicle.Speed < 20)
                            {
                                stuckCounter++;

                                if(stuckCounter > STUCK_COUNTER_LIMIT)
                                {
                                    watchedPed.Task.LeaveVehicle();
                                    
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
