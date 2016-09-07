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
        
        public Ped watchedPed;

        public Gang myGang;

        private Vector3 currentWalkTarget;

        public override void Update()
        {
            if (watchedPed.IsInAir || watchedPed.IsPlayer)
            {
                return;
            }
            if (!Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, watchedPed, false))
            {
                if (RandomUtil.RandomBool() && !watchedPed.IsInGroup && !watchedPed.IsInCombat)
                {
                    currentWalkTarget = World.GetNextPositionOnSidewalk(Game.Player.Character.Position +
                        RandomUtil.RandomDirection(true) * 45);
                    watchedPed.Task.GoTo(currentWalkTarget);
                }

                
                if (((RandomUtil.RandomBool() && ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.gangMemberAggressivenessMode.defensive) || Game.Player.IsTargetting(watchedPed) || 
                    watchedPed.HasBeenDamagedBy(Game.Player.Character))
                    && !watchedPed.IsInGroup && GangManager.instance.fightingEnabled)
                {
                    PickATarget();
                }

                //call for aid of nearby friendly members if we're in combat
                if (watchedPed.IsInCombat && GangManager.instance.fightingEnabled)
                {
                    foreach (Ped member in GangManager.instance.GetSpawnedMembersOfGang
                        (myGang))
                    {
                        if (!member.IsInCombat && 
                            !member.IsInAir && !member.IsPlayer && !member.IsInVehicle())
                        {
                            PickATarget(100);
                        }

                    }
                }
                else
                {
                    if (myGang.isPlayerOwned)
                    {

                        if (!watchedPed.IsInCombat && GangManager.instance.fightingEnabled)
                        {
                            
                            //help the player if he's in trouble and we're not
                            foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 100))
                            {
                                if (ped.IsInCombatAgainst(Game.Player.Character) && (myGang.relationGroupIndex != ped.RelationshipGroup))
                                {
                                    watchedPed.Task.FightAgainst(ped);
                                    break;
                                }
                            }
                        }

                    }

                }
            }
            else
            {
                Ped vehicleDriver = watchedPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver);
                if (vehicleDriver != watchedPed)
                {
                    if (vehicleDriver != null)
                    {
                        if (!vehicleDriver.IsAlive || !vehicleDriver.IsInVehicle())
                        {
                            //TODO check for mounted weapons
                            watchedPed.Task.LeaveVehicle();
                        }
                        else
                        {
                            if (ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.gangMemberAggressivenessMode.defensive && GangManager.instance.fightingEnabled)
                            {
                                PickATarget(100);
                            }
                        }
                        
                    }
                   
                }

            }


            if (World.GetDistance(Game.Player.Character.Position, this.watchedPed.Position) > 
                ModOptions.instance.maxDistanceMemberSpawnFromPlayer &&
                !Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, watchedPed, false))
            {
                //we're too far to be important
                Die();
                return;
            }

            if (!watchedPed.IsAlive)
            {
                if (GangWarManager.instance.isOccurring)
                {
                    if(watchedPed.RelationshipGroup == GangWarManager.instance.enemyGang.relationGroupIndex)
                    {
                        //enemy down
                        GangWarManager.instance.OnEnemyDeath();
                    }
                }
                Die();
            }

        }

        public void Die()
        {

            watchedPed.CurrentBlip.Remove();
            this.watchedPed.MarkAsNoLongerNeeded();
            this.myGang = null;
            this.watchedPed = null;
            GangManager.instance.livingMembersCount--;

        }

        public bool PickATarget(float radius = 20)
        {
            //a method that tries to make the target idle melee fighter pick other idle fighters as targets (by luck)
            //in order to stop them from just staring at a 1 on 1 fight or just picking the player as target all the time

            //get a random ped from the hostile ones nearby
            Ped[] hostileNearbyPeds = GangManager.instance.GetHostilePedsAround(watchedPed.Position, watchedPed, radius);

            if(hostileNearbyPeds != null && hostileNearbyPeds.Length > 0)
            {
                watchedPed.Task.FightAgainst(RandomUtil.GetRandomElementFromArray(hostileNearbyPeds));
                return true;
            }

            return false;
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangMemberAIUpdates;
        }

        public SpawnedGangMember(Ped watchedPed)
        {
            this.watchedPed = watchedPed;
            ResetUpdateInterval();
        }

    }
}
