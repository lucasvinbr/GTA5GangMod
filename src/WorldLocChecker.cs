using GTA.Math;
using System;


/// <summary>
/// a script that checks the player's location once in a while in order to update blips and other stuff
/// </summary>
namespace GTA.GangAndTurfMod
{
    internal class WorldLocChecker : Script
    {

        public static WorldLocChecker instance;
        private int offroadAttempts = 0;
        private Vector3 offroadCheckVector;
        private Vector3 playerPos;

        public static bool PlayerIsAwayFromRoads
        {
            get
            {
                if (instance != null)
                {
                    return instance.playerIsAwayFromRoads;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// the way spawns are made changes in some cases according to this
        /// </summary>
        private bool playerIsAwayFromRoads = false;

        private void OnTick(object sender, EventArgs e)
        {
            Wait(3000 + RandoMath.CachedRandom.Next(1000));
            ZoneManager.instance.RefreshZoneBlips();

            playerPos = MindControl.CurrentPlayerCharacter.Position;

            if (PotentialSpawnsForWars.showingBlips)
            {
                PotentialSpawnsForWars.UpdateBlipDisplay(playerPos);
            }

            //check if we're offroad
            playerIsAwayFromRoads = false;
            offroadAttempts = 0;
            offroadCheckVector = World.GetNextPositionOnStreet
                          (playerPos + RandoMath.RandomDirection(true) * ModOptions.instance.maxDistanceCarSpawnFromPlayer);
            //UI.Notification.Show(playerPos.ToString() + " from " + offroadCheckVector.ToString());
            while (offroadAttempts < 3 &&
                World.GetDistance(playerPos, offroadCheckVector) > ModOptions.instance.maxDistanceCarSpawnFromPlayer * 1.3f)
            {
                offroadAttempts++;
                offroadCheckVector = World.GetNextPositionOnStreet
                          (playerPos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.maxDistanceCarSpawnFromPlayer);
            }

            if (offroadAttempts >= 3)
            {
                playerIsAwayFromRoads = true;
            }
        }

        public WorldLocChecker()
        {
            this.Tick += OnTick;
            instance = this;
        }

    }
}
