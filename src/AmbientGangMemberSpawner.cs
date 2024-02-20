using GTA.Math;
using GTA.Native;
using System;

/// <summary>
/// this script takes care of spawning roaming gang members and vehicles in gang zones.
/// It also regulates police influence according to the settings in ModOptions
/// </summary>
namespace GTA.GangAndTurfMod
{
    internal class AmbientGangMemberSpawner : Script
    {

        public static AmbientGangMemberSpawner instance;

        /// <summary>
        /// some circumstances, such as wars, may disable ambient spawning, even if it is active in the mod options
        /// </summary>
        public bool enabled = true;

        public int postWarBackupsRemaining = 0;

        private void OnTick(object sender, EventArgs e)
        {
            Wait(3000 + RandoMath.CachedRandom.Next(1000));
            Logger.Log("ambient spawner tick: begin", 5);

            TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
            if (curTurfZone != null)
            {
                // also reduce police influence
                if (enabled)
                {
                    Function.Call(Hash.SET_WANTED_LEVEL_MULTIPLIER, (1.0f / (curTurfZone.value + 1)) + ModOptions.instance.minWantedFactorWhenInGangTurf);
                    Game.MaxWantedLevel = RandoMath.Max(CalculateMaxWantedLevelInTurf(curTurfZone.value), ModOptions.instance.maxWantedLevelInMaxedGangTurf);
                }

                if (Game.Player.WantedLevel > Game.MaxWantedLevel) Game.Player.WantedLevel--;

                if (postWarBackupsRemaining > 0)
                {
                    Vector3 playerPos = MindControl.CurrentPlayerCharacter.Position,
                        safePlayerPos = MindControl.SafePositionNearPlayer;
                    if (SpawnManager.instance.SpawnParachutingMember(GangManager.instance.PlayerGang,
                       playerPos + Vector3.WorldUp * 50, safePlayerPos) == null &&
                       !SpawnManager.instance.HasThinkingDriversLimitBeenReached())
                    {
                        SpawnManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                        SpawnManager.instance.FindGoodSpawnPointForCar(safePlayerPos), safePlayerPos);
                    }
                    postWarBackupsRemaining--;
                }

                //if spawning is enabled, lets try to spawn the current zone's corresponding gang members!
                if (ModOptions.instance.ambientSpawningEnabled && enabled)
                {

                    Gang curGang = GangManager.instance.GetGangByName(curTurfZone.ownerGangName);

                    if (curTurfZone.ownerGangName != "none" && curGang != null) //only spawn if there really is a gang in control here
                    {
                        if (SpawnManager.instance.livingMembersCount < ModOptions.instance.spawnedMembersBeforeAmbientGenStops)
                        {
                            //randomize spawned gang if "members spawn anywhere"
                            if (ModOptions.instance.ignoreTurfOwnershipWhenAmbientSpawning)
                            {
                                curGang = GangManager.instance.gangData.gangs.RandomElement();
                            }

                            Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                            if ((playerVehicle != null && playerVehicle.Speed < 30) || playerVehicle == null)
                            {
                                SpawnAmbientMember(curGang);
                            }
                            if (RandoMath.CachedRandom.Next(0, 5) < 3 && !SpawnManager.instance.HasThinkingDriversLimitBeenReached())
                            {
                                Wait(100 + RandoMath.CachedRandom.Next(300));
                                SpawnAmbientVehicle(curGang);
                            }

                            Wait(1 + RandoMath.CachedRandom.Next(RandoMath.Max(1, ModOptions.instance.msBaseIntervalBetweenAmbientSpawns / 2), ModOptions.instance.msBaseIntervalBetweenAmbientSpawns) / (curTurfZone.value + 1));
                        }
                    }
                    else
                    {
                        Function.Call(Hash.SET_WANTED_LEVEL_MULTIPLIER, 1.0f);
                        Game.MaxWantedLevel = 6;
                    }

                }
            }

            Logger.Log("ambient spawner tick: end", 5);
        }

        public void SpawnAmbientMember(Gang curGang)
        {
            Vector3 spawnPos = SpawnManager.instance.FindGoodSpawnPointForMember
                (MindControl.CurrentPlayerCharacter.Position);
            SpawnManager.instance.SpawnGangMember(curGang, spawnPos);
        }

        public void SpawnAmbientVehicle(Gang curGang)
        {
            Vector3 vehSpawnPoint = SpawnManager.instance.FindGoodSpawnPointForCar
                (MindControl.CurrentPlayerCharacter.Position);
            SpawnedDrivingGangMember spawnedVehicleAI = SpawnManager.instance.SpawnGangVehicle(curGang,
                                vehSpawnPoint, Vector3.Zero, true);
            
            if (spawnedVehicleAI != null)
            {
                Ped driver = spawnedVehicleAI.watchedPed;
                if (driver != null)
                {
                    Vehicle spawnedVehicle = spawnedVehicleAI.vehicleIAmDriving;

                    if (spawnedVehicle.Model.IsCar)
                    {
                        SpawnManager.instance.TryPlaceVehicleOnStreet(spawnedVehicleAI.vehicleIAmDriving, vehSpawnPoint);
                        driver.Task.CruiseWithVehicle(spawnedVehicleAI.vehicleIAmDriving, 20, (DrivingStyle)ModOptions.instance.wanderingDriverDrivingStyle);
                    }
                    else if (spawnedVehicle.Model.IsHelicopter)
                    {
                        // flee from player character!
                        driver.Task.StartHeliMission(spawnedVehicle, MindControl.CurrentPlayerCharacter, VehicleMissionType.Flee, 15.0f, 1.0f, 20, 20);
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
            float turfProgressPercent = (float)curTurfValue / maxTurfValue;
            return 6 - (int)(6 * turfProgressPercent);
        }
    }
}
