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
                    ModOptions.gangMemberAggressivenessMode.defensive) || Game.Player.IsTargetting(watchedPed)) &&
                    !watchedPed.IsInCombat && !watchedPed.IsInGroup && GangManager.instance.fightingEnabled)
                {
                    watchedPed.Task.FightAgainstHatedTargets(100);
                }

                //call for aid of nearby friendly members
                if (watchedPed.IsInCombat && GangManager.instance.fightingEnabled)
                {
                    Gang pedGang = GangManager.instance.GetGangByRelGroup(watchedPed.RelationshipGroup);
                    if (pedGang != null)
                        foreach (Ped member in GangManager.instance.GetSpawnedMembersOfGang
                            (pedGang))
                        {
                            if (!member.IsInCombat && !member.IsInAir && !member.IsPlayer && !member.IsInVehicle())
                            {
                                member.Task.FightAgainstHatedTargets(200);
                            }

                        }
                }
                else
                {
                    if (watchedPed.RelationshipGroup == GangManager.instance.GetPlayerGang().relationGroupIndex)
                    {

                        if (!watchedPed.IsInCombat && GangManager.instance.fightingEnabled)
                        {
                            //help the player if he's in trouble and we're not
                            foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 100))
                            {
                                if (ped.IsInCombat && (ped.RelationshipGroup != GangManager.instance.GetPlayerGang().relationGroupIndex &&
                                    ped.RelationshipGroup != Game.Player.Character.RelationshipGroup))
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
                ModOptions.instance.maxDistanceMemberSpawnFromPlayer && watchedPed.CurrentVehicle == null)
            {
                //we're too far to be important
                watchedPed.CurrentBlip.Remove();
                this.watchedPed.MarkAsNoLongerNeeded();
                this.watchedPed = null;
                GangManager.instance.livingMembersCount--;
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
                watchedPed.CurrentBlip.Remove();
                this.watchedPed.MarkAsNoLongerNeeded();
                this.watchedPed = null;
                GangManager.instance.livingMembersCount--;
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
