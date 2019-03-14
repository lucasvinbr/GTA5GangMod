using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;


/// <summary>
/// a script that checks the player's location once in a while in order to update blips and other stuff
/// </summary>
namespace GTA.GangAndTurfMod {
	class WorldLocChecker : Script {

		public static WorldLocChecker instance;

		int offroadAttempts = 0;
		Vector3 offroadCheckVector;
		Vector3 playerPos;

		public static bool PlayerIsAwayFromRoads {
			get{
				if(instance != null) {
					return instance.playerIsAwayFromRoads;
				}
				else {
					return true;
				}
			}
		}

		/// <summary>
		/// the way spawns are made changes in some cases according to this
		/// </summary>
		private bool playerIsAwayFromRoads = false;


		void OnTick(object sender, EventArgs e)
        {
            Wait(3000 + RandoMath.CachedRandom.Next(1000));
            ZoneManager.instance.RefreshZoneBlips();
			//check if we're offroad
			playerIsAwayFromRoads = false;
			offroadAttempts = 0;
			playerPos = MindControl.CurrentPlayerCharacter.Position;
			offroadCheckVector = World.GetNextPositionOnStreet
						  (playerPos + RandoMath.RandomDirection(true) * ModOptions.instance.maxDistanceCarSpawnFromPlayer);
			//UI.Notify(playerPos.ToString() + " from " + offroadCheckVector.ToString());
			while(offroadAttempts < 3 && 
				World.GetDistance(playerPos, offroadCheckVector) > ModOptions.instance.maxDistanceCarSpawnFromPlayer * 1.3f) {
				offroadAttempts++;
				offroadCheckVector = World.GetNextPositionOnStreet
						  (playerPos + RandoMath.RandomDirection(true) *
						  ModOptions.instance.maxDistanceCarSpawnFromPlayer);
			}

			if(offroadAttempts >= 3) {
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
