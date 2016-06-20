using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    class AmbientGangMemberSpawner : Script
    {

        public bool enabled = true;
        public static AmbientGangMemberSpawner instance;


        void OnTick(object sender, EventArgs e)
        {
            Wait(4000 + RandomUtil.CachedRandom.Next(2000));
            ZoneManager.instance.RefreshZoneBlips(); //since this runs once in a while, let's also refresh the zone blips

            //lets try to spawn the current zone's corresponding gang members!
            if (enabled)
            {
                TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
                if (curTurfZone != null)
                {
                    Gang curGang = GangManager.instance.GetGangByName(curTurfZone.ownerGangName);
                    if (GangWarManager.instance.isOccurring && GangWarManager.instance.enemyGang == curGang) return; //we want enemies of this gang to spawn only when close to the war
                                                                                                                     
                    // also reduce police influence
                    Game.WantedMultiplier = ModOptions.instance.wantedFactorWhenInGangTurf;
                    Game.MaxWantedLevel = ModOptions.instance.maxWantedLevelInGangTurf;

                    if (curTurfZone.ownerGangName != "none" && curGang != null) //only spawn if there really is a gang in control here
                    {
                        if (GangManager.instance.livingMembersCount < ModOptions.instance.spawnedMembersBeforeAmbientGenStops)
                        {
                            Vehicle playerVehicle = Game.Player.Character.CurrentVehicle;
                            if ((playerVehicle != null && playerVehicle.Speed < 70) || playerVehicle == null)
                            {
                                Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForMember();
                                Ped newMember = GangManager.instance.SpawnGangMember(curGang, spawnPos);
                                if(newMember != null)
                                {
                                    newMember.Task.GoTo(World.GetNextPositionOnSidewalk(newMember.Position));
                                }
                            }
                            if (RandomUtil.CachedRandom.Next(0, 5) < 3)
                            {
                                Wait(100 + RandomUtil.CachedRandom.Next(300));
                                Vehicle spawnedVehicle = GangManager.instance.SpawnGangVehicle(curGang,
                                GangManager.instance.FindGoodSpawnPointForCar(), Vector3.Zero, true, false, false);
                                if (spawnedVehicle != null)
                                {
                                    GangManager.instance.TryPlaceVehicleOnStreet(spawnedVehicle, spawnedVehicle.Position);
                                    Ped driver = spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver);
                                    if(driver != null) //if, for some reason, we don't have a driver, do nothing
                                    {
                                        driver.Task.CruiseWithVehicle(spawnedVehicle, 20, (int) DrivingStyle.Rushed);
                                    }
                                }
                            }

                            Wait(1000 + RandomUtil.CachedRandom.Next(3000000) / curGang.GetGangAIStrengthValue());
                        }
                    }
                    else
                    {
                        Game.WantedMultiplier = 1;
                        Game.MaxWantedLevel = 6;
                    }

                }
            }
        }

        public AmbientGangMemberSpawner()
        {
            this.Tick += OnTick;
            instance = this;
        }
    }
}
