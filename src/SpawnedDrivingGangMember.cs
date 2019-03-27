using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    public class SpawnedDrivingGangMember : UpdatedClass
    {
        public Ped watchedPed;
        public bool isFriendlyToPlayer = false; //important in order to know if we should follow or chase (aggressively) the player
        public List<Ped> myPassengers = new List<Ped>();
        public Vector3 destination;
        public Vehicle vehicleIAmDriving;
        public int updatesWhileGoingToDest;
        public int updateLimitWhileGoing = 50;

        public bool playerAsDest = false;

		public const float MAX_SPEED = 50, SLOW_DOWN_DIST = 200;

		private float targetSpeed;
		private float distToDest;
		

		/// <summary>
		/// if true, this driver will focus on getting to their destination and, when there, will leave the car...
		/// unless it's a backup called by the player; 
		/// in that case, it will follow the player around if they're in a vehicle
		/// </summary>
		public bool deliveringCar = false;

		public override void Update() {
			if (vehicleIAmDriving.IsAlive && watchedPed.IsAlive) {
				if (deliveringCar) {
					watchedPed.BlockPermanentEvents = true; //our driver shouldn't get distracted
				}

				if (destination != Vector3.Zero) {
					//even if we are backup vehicles, we should despawn if TOO far
					if (vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
							ModOptions.instance.maxDistanceCarSpawnFromPlayer * 8) {
						DespawnProcedure();
						return;
					}

					//stop tracking this driver/vehicle if he/she leaves the vehicle
					if (!watchedPed.IsInVehicle(vehicleIAmDriving)) {
						ClearAllRefs();
						return;
					}
					else {
						RideToDest();
					}
				}
				else {
					//we are just wandering arond
					//if we get too far from the player, despawn
					if (vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
							ModOptions.instance.maxDistanceCarSpawnFromPlayer * 3) {

						DespawnProcedure();
						return;
					}

					//if there is a war going on and we're in the war zone, get to one of the spawn points;
					//it's probably close to the action
					if (GangWarManager.instance.isOccurring && GangWarManager.instance.playerNearWarzone) {
						deliveringCar = true;
						destination = GangWarManager.instance.GetRandomSpawnPoint();

						//if spawns still aren't set... try getting to the player
						if (destination == Vector3.Zero) {
							//no backup behavior, just a one-shot destination
							destination = MindControl.SafePositionNearPlayer;
						}

						updatesWhileGoingToDest = 0;
						RideToDest();
						return;
					}

					//stop tracking this driver/vehicle if he/she leaves the vehicle or something goes wrong
					if (!watchedPed.IsInVehicle()) {
						ClearAllRefs();
						return;
					}
				}
			}
			else {
				//our vehicle has been destroyed/ our driver has died

				ClearAllRefs();
			}

		}


		void RideToDest() {

			bool playerInVehicle = MindControl.CurrentPlayerCharacter.IsInVehicle();

			if (playerAsDest) {
				destination = MindControl.SafePositionNearPlayer;
			}

			distToDest = vehicleIAmDriving.Position.DistanceTo(destination);

			//if we're close to the destination...
			if (distToDest < 25) //tweaked to match my changes below -- zix
			{
				//leave the vehicle if we are a backup vehicle and the player's on foot or if we just had to get somewhere
				if (!playerAsDest || (playerAsDest && !playerInVehicle)) {
					if (deliveringCar) {
						DriverLeaveVehicle();
					}
					else {
						ClearAllRefs();
					}
				}
			}
			else {
				updatesWhileGoingToDest++;

				//give up, drop passengers and go away... but only if we're not chasing the player
				//and he/she isn't on a vehicle
				if (updatesWhileGoingToDest > updateLimitWhileGoing &&
					(!playerAsDest || !playerInVehicle)) {
					if (playerAsDest && deliveringCar) //zix - extra config options
					{
						//if we took too long to get to the player and can't be currently seen by the player, lets just teleport close by
						//...this should only happen with friendly vehicles, or else the player may be blitzkrieg-ed in a not funny way
						if (!vehicleIAmDriving.IsOnScreen && ModOptions.instance.forceSpawnCars &&
							watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex) {
							vehicleIAmDriving.Position = World.GetNextPositionOnStreet(MindControl.CurrentPlayerCharacter.Position, true);
						}

					}
					if (playerAsDest) {
						DriverLeaveVehicle();
					}
					else {
						ClearAllRefs();
					}
				}
				else {
					if (!watchedPed.IsPlayer) {
						if (playerAsDest && playerInVehicle) {

							//teleport if we're failing to escort due to staying too far
							//(should only happen with friendly vehicles and if forceSpawnCars is true)
							if(ModOptions.instance.forceSpawnCars &&
								watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex &&
								vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
								ModOptions.instance.maxDistanceCarSpawnFromPlayer * 3 &&
								!vehicleIAmDriving.IsOnScreen) {
								Logger.Log("backup driver: relocation!");
								vehicleIAmDriving.Position = SpawnManager.instance.FindGoodSpawnPointForCar(destination);
							}

							watchedPed.Task.ClearAll();
							if (MindControl.CurrentPlayerCharacter.IsInFlyingVehicle) {
								//just keep following on the ground in this case;
								//both allies and enemies should do it
								watchedPed.Task.DriveTo
									(vehicleIAmDriving, destination, 15, 50,
									ModOptions.instance.driverWithDestinationDrivingStyle);
							}
							else {
								if (isFriendlyToPlayer) {
									Function.Call(Hash.TASK_VEHICLE_ESCORT, watchedPed, vehicleIAmDriving,
										MindControl.CurrentPlayerCharacter.CurrentVehicle, -1, -1,
										ModOptions.instance.driverWithDestinationDrivingStyle, 30, 0, 35);
								}
								else {
									
									Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, 1.0f);
									watchedPed.Task.VehicleChase(MindControl.CurrentPlayerCharacter);
								}
							}
						}
						else {
							if(distToDest > SLOW_DOWN_DIST) {
								targetSpeed = MAX_SPEED;
							}
							else {
								targetSpeed = MAX_SPEED * (distToDest / SLOW_DOWN_DIST);
							}

							watchedPed.Task.ClearAll();
							watchedPed.Task.DriveTo(vehicleIAmDriving, destination, 15, targetSpeed, ModOptions.instance.driverWithDestinationDrivingStyle);
						}
					}
				}
			}
		}


		/// <summary>
		/// tells the driver to leave the vehicle, then clearAllRefs
		/// </summary>
		public void DriverLeaveVehicle()
        {
            //leave vehicle, everyone stops being important
            if (!watchedPed.IsPlayer)
            {
				watchedPed.Task.LeaveVehicle();
				watchedPed.BlockPermanentEvents = false;
            }

            ClearAllRefs();
        }

        /// <summary>
        /// nullifies/clears references to the vehicle, driver and passengers
        /// </summary>
        public void ClearAllRefs()
        {
            if (vehicleIAmDriving != null)
            {
                if (vehicleIAmDriving.CurrentBlip != null)
                {
                    vehicleIAmDriving.CurrentBlip.Remove();
                }

                vehicleIAmDriving.IsPersistent = false;
                vehicleIAmDriving = null;
            }
            
            watchedPed = null;
            myPassengers.Clear();
        }

		/// <summary>
		/// calls Die on the passengers' AI then clearsAllRefs
		/// </summary>
		public void DespawnProcedure() {
			for (int i = 0; i < myPassengers.Count; i++) {
				if (myPassengers[i] != null && myPassengers[i].IsAlive && !myPassengers[i].IsPlayer) {
					SpawnedGangMember memberAI = SpawnManager.instance.GetTargetMemberAI(myPassengers[i]);
					if (memberAI != null) memberAI.Die();
				}

			}

			if (!watchedPed.IsPlayer) {
				SpawnedGangMember memberAI = SpawnManager.instance.GetTargetMemberAI(watchedPed);
				if (memberAI != null) memberAI.Die();
			}
			ClearAllRefs();
		}

        public SpawnedDrivingGangMember(Ped watchedPed, Vehicle vehicleIAmDriving, Vector3 destination, bool isFriendlyToPlayer, bool playerAsDest = false, bool deliveringCar = false)
        {
            this.ticksBetweenUpdates = 50;
            AttachData(watchedPed, vehicleIAmDriving, destination, isFriendlyToPlayer, playerAsDest, deliveringCar);
        }

        public void AttachData(Ped targetPed, Vehicle targetVehicle, Vector3 theDest, bool isFriendlyToPlayer, bool playerIsDest, bool deliveringCar)
        {
            this.watchedPed = targetPed;
            this.vehicleIAmDriving = targetVehicle;
            this.destination = theDest;
            this.playerAsDest = playerIsDest;
			this.deliveringCar = deliveringCar;
            this.isFriendlyToPlayer = isFriendlyToPlayer;
			Function.Call(Hash.SET_DRIVER_ABILITY, watchedPed, 1.0f);
			updatesWhileGoingToDest = 0;
            SetWatchedPassengers();
        }

        /// <summary>
        /// remember who our passengers are so that we can erase them later, even if they leave the car
        /// </summary>
        public void SetWatchedPassengers()
        {
            myPassengers.Clear();
            for (int i = 0; i < vehicleIAmDriving.PassengerSeats; i++)
            {
                Ped memberInSeat = Function.Call<Ped>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicleIAmDriving, i);
                myPassengers.Add(memberInSeat);
                Function.Call(Hash.CONTROL_MOUNTED_WEAPON, memberInSeat);
            }
        }

    }
}
