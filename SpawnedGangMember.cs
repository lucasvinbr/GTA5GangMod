using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    public class SpawnedGangMember : UpdatedClass
    {
        
        public Ped watchedPed;

        public Gang myGang;

        public override void Update()
        {
            if (watchedPed.IsInAir || watchedPed.IsPlayer)
            {
                return;
            }

            if(!Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, watchedPed, false))
            {
                if (RandomUtil.RandomBool() && !watchedPed.IsInGroup && !watchedPed.IsInCombat)
                {
                    watchedPed.Task.GoTo(World.GetNextPositionOnSidewalk(Game.Player.Character.Position +
                        RandomUtil.RandomDirection(true) * 25));
                }


                if (((RandomUtil.RandomBool() && ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.gangMemberAggressivenessMode.defensive) || Game.Player.IsTargetting(watchedPed) || 
                    watchedPed.HasBeenDamagedBy(Game.Player.Character)) &&
                    !watchedPed.IsInCombat && !watchedPed.IsInGroup && GangManager.instance.fightingEnabled)
                {
                    PickATarget();
                }

                //call for aid of nearby friendly members
                if (watchedPed.IsInCombat && GangManager.instance.fightingEnabled)
                {
                    
                    foreach (Ped member in GangManager.instance.GetSpawnedMembersOfGang
                        (myGang))
                    {
                        if (!member.IsInCombat && 
                            !member.IsInAir && !member.IsPlayer && !member.IsInVehicle())
                        {
                            member.Task.FightAgainstHatedTargets(100);
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
                                if (ped.IsInCombat && (myGang.relationGroupIndex != ped.RelationshipGroup &&
                            World.GetRelationshipBetweenGroups(myGang.relationGroupIndex, ped.RelationshipGroup) == Relationship.Hate))
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
                    if(vehicleDriver != null)
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
                                watchedPed.Task.FightAgainstHatedTargets(200);
                            }
                        }
                        
                    }
                   
                }
            }

            

            if(World.GetDistance(Game.Player.Character.Position, this.watchedPed.Position) > 
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

        public void PickATarget()
        {
            //a method that tries to make the target idle melee fighter pick other idle fighters as targets (by luck)
            //in order to stop them from just staring at a 1 on 1 fight or just picking the player as target all the time
            if (ModOptions.instance.meleeWeapons.Contains(watchedPed.Weapons.Current.Hash) ||
                watchedPed.Weapons.Current.Hash == WeaponHash.Unarmed)
            {
                //get a random ped from the hostile ones nearby
                Ped[] hostileNearbyPeds = GangManager.instance.GetHostilePedsAround(watchedPed.Position, watchedPed, 10);

                if(hostileNearbyPeds.Length > 0)
                {
                    watchedPed.Task.FightAgainst(RandomUtil.GetRandomElementFromArray(hostileNearbyPeds));
                    return;
                }
            }

            //no hostiles too close or we're using guns?
            //fight any hated target... if we're not in combat already
            if (!watchedPed.IsInCombat)
            {
                watchedPed.Task.FightAgainstHatedTargets(100);
            }
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
