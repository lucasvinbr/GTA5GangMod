using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

namespace GTA.GangAndTurfMod {
	class GangVehicleUpdater : Script {

		public static GangVehicleUpdater instance;

		/// <summary>
		/// the updater must wait for the gangManager before it starts looking for members to update
		/// </summary>
		public bool enabled = false;

		public List<SpawnedDrivingGangMember> driverList;

		private bool updateRanThisFrame = false;

		void OnTick(object sender, EventArgs e) {
			updateRanThisFrame = false;
			for (int i = 0; i < driverList.Count; i++) {
				if (driverList[i].watchedPed != null && driverList[i].vehicleIAmDriving != null) {
					driverList[i].ticksSinceLastUpdate++;
					if (!updateRanThisFrame && driverList[i].ticksSinceLastUpdate >= driverList[i].ticksBetweenUpdates) {
						//max is one vehicle update per frame in order to avoid crashes
						updateRanThisFrame = true;
						driverList[i].Update();
						driverList[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(driverList[i].ticksBetweenUpdates / 3);
					}
				}
				
			}
		}

		public static bool Initialize() {
			if (instance != null) {
				instance.driverList = SpawnManager.instance.livingDrivingMembers;
				instance.enabled = true;
				return true;
			}
			else {
				return false;
			}
		}

		
		public GangVehicleUpdater() {
			this.Tick += OnTick;
			instance = this;
		}

	}
}
