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


		void OnTick(object sender, EventArgs e) {
			for (int i = 0; i < driverList.Count; i++) {
				if (driverList[i].watchedPed != null && driverList[i].vehicleIAmDriving != null) {
					driverList[i].ticksSinceLastUpdate++;
					if (driverList[i].ticksSinceLastUpdate >= driverList[i].ticksBetweenUpdates) {
						//max is one vehicle update per frame in order to avoid crashes
						driverList[i].Update();
						driverList[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(driverList[i].ticksBetweenUpdates / 3);
					}
					Wait(35);
				}
				
			}
		}

		public static void Initialize() {
			instance.driverList = GangManager.instance.livingDrivingMembers;
			instance.enabled = true;
		}

		
		public GangVehicleUpdater() {
			this.Tick += OnTick;
			instance = this;
		}

	}
}
