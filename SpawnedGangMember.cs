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

        public enum memberStatus
        {
            none,
            idle,
            combat,
            inVehicle
        }

        public memberStatus curStatus = memberStatus.none;

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
                    if(curStatus != memberStatus.idle)
                    {
                        curStatus = memberStatus.idle;
                        watchedPed.Task.WanderAround();
                        //Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, watchedPed, "WORLD_HUMAN_AA_COFFEE", -1, false);
                    }
                    
                    
                }

                
                if (((RandoMath.RandomBool() && ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.gangMemberAggressivenessMode.defensive) || Game.Player.IsTargetting(watchedPed) || 
                    watchedPed.HasBeenDamagedBy(Game.Player.Character))
                    && !watchedPed.IsInGroup && ModOptions.instance.fightingEnabled)
                {
                    PickATarget();
                }

                //call for aid of nearby friendly members if we're in combat
                if (watchedPed.IsInCombat && ModOptions.instance.fightingEnabled)
                {
                    foreach (Ped member in GangManager.instance.GetSpawnedPedsOfGang
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

                        if (!watchedPed.IsInCombat && ModOptions.instance.fightingEnabled)
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
            }
            else
            {
                curStatus = memberStatus.inVehicle;
                Ped vehicleDriver = watchedPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver);
                if (vehicleDriver != watchedPed)
                {
                    if (vehicleDriver != null)
                    {
                        if (!vehicleDriver.IsAlive || !vehicleDriver.IsInVehicle())
                        {
                            //TODO check for mounted weapons
                            watchedPed.Task.LeaveVehicle();
                            curStatus = memberStatus.none;
                        }
                        else
                        {
                            if (ModOptions.instance.gangMemberAggressiveness !=
                    ModOptions.gangMemberAggressivenessMode.defensive && ModOptions.instance.fightingEnabled)
                            {
                                PickATarget(100);
                                curStatus = memberStatus.inVehicle;
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
                    }else if(watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex)
                    {
                        //ally down
                        GangWarManager.instance.OnAllyDeath();
                    }
                }
                Die();
            }

        }

        public void Die()
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
            watchedPed.CurrentBlip.Remove();
            this.watchedPed.MarkAsNoLongerNeeded();
            this.myGang = null;
            this.watchedPed = null;
            curStatus = memberStatus.none;
            GangManager.instance.livingMembersCount--;

        }

        public bool PickATarget(float radius = 50)
        {
            //a method that tries to make the target idle melee fighter pick other idle fighters as targets (by luck)
            //in order to stop them from just staring at a 1 on 1 fight or just picking the player as target all the time

            //get a random ped from the hostile ones nearby
            List<Ped> hostileNearbyPeds = GangManager.instance.GetHostilePedsAround(watchedPed.Position, watchedPed, radius);

            if(hostileNearbyPeds != null && hostileNearbyPeds.Count > 0)
            {
                watchedPed.Task.FightAgainst(RandoMath.GetRandomElementFromList(hostileNearbyPeds));
                curStatus = memberStatus.combat;
                return true;
            }

            return false;
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangMemberAIUpdates + RandoMath.CachedRandom.Next(100);
        }

        public SpawnedGangMember(Ped watchedPed)
        {
            this.watchedPed = watchedPed;
            ResetUpdateInterval();
        }

    }
}
