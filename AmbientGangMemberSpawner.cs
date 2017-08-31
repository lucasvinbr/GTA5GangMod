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

        public static AmbientGangMemberSpawner instance;

        /// <summary>
        /// some circumstances, such as wars, may disable ambient spawning, even if it is active in the mod options
        /// </summary>
        public bool enabled = true;

        public int postWarBackupsRemaining = 0;

        void OnTick(object sender, EventArgs e)
        {
            Wait(4000 + RandoMath.CachedRandom.Next(2000));
            ZoneManager.instance.RefreshZoneBlips(); //since this runs once in a while, let's also refresh the zone blips

            TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
            if (curTurfZone != null)
            {
                // also reduce police influence
                if (enabled)
                {
                    Game.WantedMultiplier = (1.0f / (curTurfZone.value + 1)) + ModOptions.instance.minWantedFactorWhenInGangTurf;
                    Game.MaxWantedLevel = RandoMath.Max(CalculateMaxWantedLevelInTurf(curTurfZone.value), ModOptions.instance.maxWantedLevelInMaxedGangTurf);
                }
                
                if (Game.Player.WantedLevel > Game.MaxWantedLevel) Game.Player.WantedLevel--;

                if(postWarBackupsRemaining > 0 && curTurfZone == GangWarManager.instance.warZone)
                {
                    Vector3 playerPos = Game.Player.Character.Position;
                    GangManager.instance.SpawnParachutingMember(GangManager.instance.PlayerGang,
                       playerPos + Math.Vector3.WorldUp * 50, playerPos);
                    GangManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                        GangManager.instance.FindGoodSpawnPointForCar(), playerPos, true, false, true);
                    postWarBackupsRemaining--;
                }

                //if spawning is enabled, lets try to spawn the current zone's corresponding gang members!
                if (ModOptions.instance.ambientSpawningEnabled && enabled)
                {

                    Gang curGang = GangManager.instance.GetGangByName(curTurfZone.ownerGangName);
                    if (GangWarManager.instance.isOccurring && GangWarManager.instance.enemyGang == curGang) return; //we want enemies of this gang to spawn only when close to the war

                    if (curTurfZone.ownerGangName != "none" && curGang != null) //only spawn if there really is a gang in control here
                    {
                        if (GangManager.instance.livingMembersCount < ModOptions.instance.spawnedMembersBeforeAmbientGenStops)
                        {
                            Vehicle playerVehicle = Game.Player.Character.CurrentVehicle;
                            if ((playerVehicle != null && playerVehicle.Speed < 70) || playerVehicle == null)
                            {
                                Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForMember();
                                Ped newMember = GangManager.instance.SpawnGangMember(curGang, spawnPos);
                                if (newMember != null)
                                {
                                    newMember.Task.GoTo(World.GetNextPositionOnSidewalk(newMember.Position));
                                }
                            }
                            if (RandoMath.CachedRandom.Next(0, 5) < 3)
                            {
                                Wait(100 + RandoMath.CachedRandom.Next(300));
                                Vehicle spawnedVehicle = GangManager.instance.SpawnGangVehicle(curGang,
                                GangManager.instance.FindGoodSpawnPointForCar(), Vector3.Zero, true, false, false);
                                if (spawnedVehicle != null)
                                {
                                    GangManager.instance.TryPlaceVehicleOnStreet(spawnedVehicle, spawnedVehicle.Position);
                                    Ped driver = spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver);
                                    if (driver != null) //if, for some reason, we don't have a driver, do nothing
                                    {
                                        driver.Task.CruiseWithVehicle(spawnedVehicle, 20, (int)DrivingStyle.Rushed);
                                    }
                                }
                            }

                            Wait(1000 + RandoMath.CachedRandom.Next(40000) / (curTurfZone.value * curTurfZone.value + 1));
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

        public int CalculateMaxWantedLevelInTurf(int curTurfValue)
        {
            int maxTurfValue = ModOptions.instance.maxTurfValue;
            float turfProgressPercent = (float) curTurfValue / maxTurfValue;
            return 6 - (int) (6 * turfProgressPercent);
        }
    }
}
