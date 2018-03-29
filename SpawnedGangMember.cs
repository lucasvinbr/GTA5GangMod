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

        public enum memberStatus
        {
            none,
            idle,
            combat,
            inVehicle
        }

        public const int ticksBetweenIdleChange = 10;

        int ticksSinceLastIdleChange = 0;

        public memberStatus curStatus = memberStatus.none;

        public bool hasDriveByGun = false;

        public override void Update()
        {
            if ((myGang.isPlayerOwned && watchedPed.IsInAir) || watchedPed.IsPlayer)
            {
                return;
            }
            if (!Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, watchedPed, false))
            {
                if (RandoMath.RandomBool() && !watchedPed.IsInGroup && !watchedPed.IsInCombat)
                {
                    if(curStatus != memberStatus.idle || ticksSinceLastIdleChange > ticksBetweenIdleChange)
                    {
                        curStatus = memberStatus.idle;
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

                
                if (!watchedPed.IsInCombat && ModOptions.instance.fightingEnabled && !watchedPed.IsInGroup && ((RandoMath.RandomBool() && CanFight()) || Game.Player.IsTargetting(watchedPed) || 
                    watchedPed.HasBeenDamagedBy(Game.Player.Character)))
                {
                    PickATarget();
                }

                //if one enters combat, everyone does
                if (ModOptions.instance.fightingEnabled)
                {
                    if (watchedPed.IsInCombat)
                    {
                        foreach (SpawnedGangMember member in GangManager.instance.livingMembers)
                        {
                            if (member.watchedPed != null && member.watchedPed.IsAlive)
                            {
                                if (!member.watchedPed.IsInCombat &&
                                !member.watchedPed.IsInAir && !member.watchedPed.IsPlayer && !member.watchedPed.IsInVehicle())
                                {
                                    member.PickATarget();
                                }
                            }


                        }
                    }
                    else
                    {
                        if (myGang.isPlayerOwned)
                        {
                            //help the player if he's in trouble and we're not
                            foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 100))
                            {
                                if (ped.IsInCombatAgainst(Game.Player.Character) && (myGang.relationGroupIndex != ped.RelationshipGroup))
                                {
                                    watchedPed.Task.FightAgainst(ped);
                                    curStatus = memberStatus.combat;
                                    break;
                                }
                            }

                        }

                    }
                }

                if (World.GetDistance(Game.Player.Character.Position, watchedPed.Position) >
               ModOptions.instance.maxDistanceMemberSpawnFromPlayer)
                {
                    //we're too far to be important
                    Die();
                    return;
                }


            }
            else
            {
                curStatus = memberStatus.inVehicle;
                Vehicle curVehicle = watchedPed.CurrentVehicle;
                Ped vehicleDriver = curVehicle.GetPedOnSeat(VehicleSeat.Driver);
                if (vehicleDriver != watchedPed)
                {
                    if (vehicleDriver != null)
                    {
                        if (!vehicleDriver.IsAlive || !vehicleDriver.IsInVehicle())
                        {
                            if (!Function.Call<bool>(Hash.CONTROL_MOUNTED_WEAPON, watchedPed) && !hasDriveByGun)
                            {
                                watchedPed.Task.LeaveVehicle();
                                curStatus = memberStatus.none;
                            }
                        }
                        else
                        {
                            if (ModOptions.instance.fightingEnabled && CanFight())
                            {
                                //if we're not following the player, not inside a vehicle with a mounted weap,
                                //not equipped with a drive-by gun AND close to an enemy, leave the vehicle!
                                //...but don't leave vehicles while they are moving too fast
                                if(PickATarget(30) && !watchedPed.IsInGroup && !Function.Call<bool>(Hash.CONTROL_MOUNTED_WEAPON, watchedPed) &&
                                    !hasDriveByGun)
                                {
                                    if(curVehicle.Speed < 5) {
                                        watchedPed.Task.LeaveVehicle();
                                        curStatus = memberStatus.none;
                                    }
                                }
                                curStatus = memberStatus.inVehicle;
                            }
                        }
                        
                    }

                }else
                {
                    //if our vehicle has already been marked as deletable... maybe we can go too
                    if (!curVehicle.IsPersistent)
                    {
                        if (World.GetDistance(Game.Player.Character.Position, watchedPed.Position) >
               ModOptions.instance.maxDistanceMemberSpawnFromPlayer)
                        {
                            //we're too far to be important
                            Die();
                            return;
                        }
                        else
                        {
                            if (!Function.Call<bool>(Hash.CONTROL_MOUNTED_WEAPON, watchedPed))
                            {
                                watchedPed.Task.WarpOutOfVehicle(curVehicle);
                            }
                        }
                    }
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
                return;
            }

           
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
            curStatus = memberStatus.none;
            GangManager.instance.livingMembersCount--;

        }

        public bool PickATarget(float radius = 50)
        {
            //a method that tries to make the target fighter pick a random enemy as target
            //in order to stop them from just staring at a 1 on 1 fight or just picking the player as target all the time

            //get a random ped from the hostile ones nearby
            List<Ped> hostileNearbyPeds = GangManager.instance.GetHostilePedsAround(watchedPed.Position, watchedPed, radius);

            //add enemy gang members to the list, no matter where they are, so that we can attack the ones far away too
            hostileNearbyPeds.AddRange(GangManager.instance.GetMembersNotFromMyGang(myGang));

            if(hostileNearbyPeds != null && hostileNearbyPeds.Count > 0)
            {
                watchedPed.Task.FightAgainst(RandoMath.GetRandomElementFromList(hostileNearbyPeds));
                curStatus = memberStatus.combat;
                return true;
            }

            return false;
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
                    ModOptions.gangMemberAggressivenessMode.defensive || 
                    (GangWarManager.instance.isOccurring && (myGang == GangWarManager.instance.enemyGang || myGang == GangManager.instance.PlayerGang)));
        }

    }
}
