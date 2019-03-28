using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;

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

        public enum MemberStatus
        {
            none,
            onFootThinking,
            combat,
            inVehicle
        }

        public const int ticksBetweenIdleChange = 10;

        int ticksSinceLastIdleChange = 0;

        public MemberStatus curStatus = MemberStatus.none;

        public bool hasDriveByGun = false;

        public override void Update()
        {
			Logger.Log("member update: start");
			if ((watchedPed.IsInAir) || MindControl.CurrentPlayerCharacter == watchedPed)
            {
				Logger.Log("member update: end (isPlayer or etc)");
				return;
            }
            if (curStatus != MemberStatus.inVehicle)
            {
				watchedPed.BlockPermanentEvents = false;
				if (watchedPed.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
			   ModOptions.instance.maxDistanceMemberSpawnFromPlayer * 1.5f) {
					//we're too far to be important
					Die();
					Logger.Log("member update: end (too far, despawn)");
					return;
				}

				if (RandoMath.RandomBool() && !watchedPed.IsInGroup && !watchedPed.IsInCombat)
                {
                    if (GangWarManager.instance.isOccurring && GangWarManager.instance.playerNearWarzone)
                    {
                        //instead of idling while in a war, members should head for one of the spawn points
                        if(myGang == GangManager.instance.PlayerGang)
                        {
                            if (GangWarManager.instance.enemySpawnPoints != null)
                            {
                                Vector3 ourDestination = RandoMath.GetRandomElementFromArray(GangWarManager.instance.enemySpawnPoints);
								if(ourDestination != Vector3.Zero) {
									watchedPed.Task.RunTo(ourDestination);
								}
								
                            }
                        }
                        else
                        {
                            if (GangWarManager.instance.alliedSpawnPoints != null)
                            {
                                Vector3 ourDestination = RandoMath.GetRandomElementFromArray(GangWarManager.instance.alliedSpawnPoints);
								if(ourDestination != Vector3.Zero) {
									watchedPed.Task.RunTo(ourDestination);
								}
                            }
                        }

                        curStatus = MemberStatus.onFootThinking;
                        ticksSinceLastIdleChange = 0;
					}
					else {
						if (curStatus != MemberStatus.onFootThinking || ticksSinceLastIdleChange > ticksBetweenIdleChange) {
							curStatus = MemberStatus.onFootThinking;
							if (RandoMath.RandomBool()) {
								watchedPed.Task.WanderAround();
							}
							else {
								DoAnIdleAnim();
							}
							ticksSinceLastIdleChange = 0 - RandoMath.CachedRandom.Next(10);
						}
						else {
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
			   ModOptions.instance.maxDistanceCarSpawnFromPlayer * 3) {
							//we're too far to be important
							Die();
							Logger.Log("member update: end (in vehicle: too far, despawn)");
							return;
						}

						if (curVehicle.IsSeatFree(VehicleSeat.Driver)) {
							//possibly leave the vehicle if the driver has left already
							if (RandoMath.RandomBool()) {
								watchedPed.Task.LeaveVehicle();
							}
						}
					}
                    
                }
                else
                {
                    curStatus = MemberStatus.none;
                }

            }

            if (!watchedPed.IsAlive)
            {
                if (GangWarManager.instance.isOccurring)
                {
                    if (watchedPed.RelationshipGroup == GangWarManager.instance.enemyGang.relationGroupIndex)
                    {
                        //enemy down
                        GangWarManager.instance.OnEnemyDeath();
                    }
                    else if (watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex)
                    {
                        //ally down
                        GangWarManager.instance.OnAllyDeath();
                    }
                }
                Die();
				Logger.Log("member update: end (dead)");
				return;
            }

			Logger.Log("member update: end");
		}

        /// <summary>
        /// decrements gangManager's living members count, also tells the war manager about it,
        /// clears this script's references, removes the ped's blip and marks the ped as no longer needed
        /// </summary>
        public void Die(bool alsoDelete = false)
        {
            
            if (watchedPed != null)
            {

                if (GangWarManager.instance.isOccurring)
                {
                    if (watchedPed.RelationshipGroup == GangWarManager.instance.enemyGang.relationGroupIndex)
                    {
                        //enemy down
                        GangWarManager.instance.DecrementSpawnedsNumber(false);
                    }
                    else if (watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex)
                    {
                        //ally down
                        GangWarManager.instance.DecrementSpawnedsNumber(true);
                    }
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
                    watchedPed.MarkAsNoLongerNeeded();

                }
                
            }

            this.myGang = null;
            this.watchedPed = null;
            curStatus = MemberStatus.none;
			SpawnManager.instance.livingMembersCount--;

        }

        /// <summary>
        /// does an ambient animation, like smoking
        /// </summary>
        public void DoAnIdleAnim()
        {
            Vector3 scenarioPos = World.GetNextPositionOnSidewalk(watchedPed.Position);
            Function.Call(Hash.TASK_START_SCENARIO_AT_POSITION, watchedPed, RandoMath.GetRandomElementFromArray(idleAnims),
                scenarioPos.X, scenarioPos.Y, scenarioPos.Z, RandoMath.RandomHeading(), 0, 0, 0);
        }

        public void ResetUpdateInterval()
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
        }

        /// <summary>
        /// checks if we're not set to defensive (if a war is occurring and we're one of the involved gangs, that doesn't matter)
        /// </summary>
        /// <returns></returns>
        public bool CanFight()
        {
            return (ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.GangMemberAggressivenessMode.defensive || 
                    (GangWarManager.instance.isOccurring && (myGang == GangWarManager.instance.enemyGang || myGang == GangManager.instance.PlayerGang)));
        }

    }
}
