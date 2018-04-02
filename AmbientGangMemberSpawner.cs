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
            Wait(3000 + RandoMath.CachedRandom.Next(1000));
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

                if(postWarBackupsRemaining > 0 && GangWarManager.instance.playerNearWarzone)
                {
                    Vector3 playerPos = Game.Player.Character.Position;
                    GangManager.instance.SpawnParachutingMember(GangManager.instance.PlayerGang,
                       playerPos + Math.Vector3.WorldUp * 50, playerPos);
                    GangManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                        GangManager.instance.FindGoodSpawnPointForCar(), playerPos, true);
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
                                SpawnAmbientMember(curGang);
                            }
                            if (RandoMath.CachedRandom.Next(0, 5) < 3)
                            {
                                Wait(100 + RandoMath.CachedRandom.Next(300));
                                SpawnAmbientVehicle(curGang);
                            }

                            Wait(1 + RandoMath.CachedRandom.Next(RandoMath.Max(1, ModOptions.instance.msBaseIntervalBetweenAmbientSpawns / 2), ModOptions.instance.msBaseIntervalBetweenAmbientSpawns) / (curTurfZone.value + 1));
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

        public void SpawnAmbientMember(Gang curGang)
        {
            Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForMember();
            Ped newMember = GangManager.instance.SpawnGangMember(curGang, spawnPos).watchedPed;
            if (newMember != null)
            {
                newMember.Task.GoTo(World.GetNextPositionOnSidewalk(newMember.Position));
            }
        }

        public void SpawnAmbientVehicle(Gang curGang)
        {
            Vector3 vehSpawnPoint = GangManager.instance.FindGoodSpawnPointForCar();
            SpawnedDrivingGangMember spawnedVehicleAI = GangManager.instance.SpawnGangVehicle(curGang,
                                vehSpawnPoint , Vector3.Zero, true);
            if (spawnedVehicleAI != null)
            {
                GangManager.instance.TryPlaceVehicleOnStreet(spawnedVehicleAI.vehicleIAmDriving, vehSpawnPoint);
                Ped driver = spawnedVehicleAI.watchedPed;
                if (driver != null) //if, for some reason, we don't have a driver, do nothing
                {
                    driver.Task.CruiseWithVehicle(spawnedVehicleAI.vehicleIAmDriving, 8, (int) DrivingStyle.AvoidTraffic);
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
