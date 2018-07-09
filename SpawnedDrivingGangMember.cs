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
		public bool mustReachDest = false;

        public override void Update()
        {
            if (vehicleIAmDriving.IsAlive && watchedPed.IsAlive)
            {
				if (mustReachDest) {
					watchedPed.BlockPermanentEvents = true; //our driver shouldn't get distracted
				}

                if(destination != Vector3.Zero)
                {
                    //stop tracking this driver/vehicle if he/she leaves the vehicle
                    if (!watchedPed.IsInVehicle(vehicleIAmDriving))
                    {
                        ClearAllRefs();
                    }
                    else
                    {
                        RideToDest();
                    }
                }
                else
                {
					//we are just wandering arond
					//if we get too far from the player, despawn
					if (World.GetDistance(vehicleIAmDriving.Position, GangManager.CurrentPlayerCharacter.Position) >
							ModOptions.instance.maxDistanceCarSpawnFromPlayer * 2.5f) {

						for (int i = 0; i < myPassengers.Count; i++) {
							if (myPassengers[i] != null && myPassengers[i].IsAlive && !myPassengers[i].IsPlayer) {
								SpawnedGangMember memberAI = GangManager.instance.GetTargetMemberAI(myPassengers[i]);
								if (memberAI != null) memberAI.Die();
							}

						}

						if (!watchedPed.IsPlayer) {
							SpawnedGangMember memberAI = GangManager.instance.GetTargetMemberAI(watchedPed);
							if (memberAI != null) memberAI.Die();
						}
						ClearAllRefs();
						return;
					}

					//if there is a war going on and we're in the war zone, get to the player to help/kill him!
					if (GangWarManager.instance.isOccurring && GangWarManager.instance.playerNearWarzone)
                    {
                        playerAsDest = true;
                        updatesWhileGoingToDest = 0;
                        RideToDest();
                        return;
                    }

                    //stop tracking this driver/vehicle if he/she leaves the vehicle or something goes wrong
                    if (!watchedPed.IsInVehicle())
                    {
						ClearAllRefs();
                        return;
                    }
                }
            }
            else
            {
				//our vehicle has been destroyed/ our driver has died

				ClearAllRefs();
            }
            
        }

        void RideToDest()
        {

            if (playerAsDest) destination = GangManager.CurrentPlayerCharacter.Position;
            bool playerInVehicle = GangManager.CurrentPlayerCharacter.IsInVehicle();

            //if we're close to the destination...
            if (vehicleIAmDriving.Position.DistanceTo(destination) < 25) //tweaked to match my changes below -- zix
            {
                //leave the vehicle if we are a backup vehicle and the player's on foot or if we just had to get somewhere
				if(!playerAsDest || (playerAsDest && !playerInVehicle)) {
					if (mustReachDest) {
						DriverLeaveVehicle();
					}
					else {
						ClearAllRefs();
					}
				}
            }
            else
            {
                updatesWhileGoingToDest++;

                //give up, drop passengers and go away... but only if we're not chasing the player
                //and he/she isn't on a vehicle
                if (updatesWhileGoingToDest > updateLimitWhileGoing &&
                    (!playerAsDest || !playerInVehicle))
                {
                    if (playerAsDest && mustReachDest) //zix - extra config options
                    {
                        //if we took too long to get to the player and can't be currently seen by the player, lets just teleport close by
                        //...this should only happen with friendly vehicles, or else the player may be blitzkrieg-ed in a not funny way
                        if (!vehicleIAmDriving.IsOnScreen && ModOptions.instance.forceSpawnCars &&
                            watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex)
                        {
                            vehicleIAmDriving.Position = World.GetNextPositionOnStreet(GangManager.CurrentPlayerCharacter.Position, true);
                        }
                        
                    }
					if (playerAsDest) {
						DriverLeaveVehicle();
					}
					else {
						ClearAllRefs();
					}
                }
                else
                {
                    if (!watchedPed.IsPlayer)
                    {
                        if (playerAsDest && playerInVehicle)
                        {
                            watchedPed.Task.ClearAll();
                            if (isFriendlyToPlayer)
                            {
                                Function.Call(Hash.TASK_VEHICLE_ESCORT, watchedPed, vehicleIAmDriving, GangManager.CurrentPlayerCharacter.CurrentVehicle, -1, -1, ModOptions.instance.driverWithDestinationDrivingStyle, 30, 0, 35);
                            }
                            else
                            {
                                watchedPed.Task.VehicleChase(GangManager.CurrentPlayerCharacter);
                            }
                        }
                        else
                        {
                            watchedPed.Task.ClearAll();
                            watchedPed.Task.DriveTo(vehicleIAmDriving, destination, 15, 50, ModOptions.instance.driverWithDestinationDrivingStyle);
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

        public SpawnedDrivingGangMember(Ped watchedPed, Vehicle vehicleIAmDriving, Vector3 destination, bool isFriendlyToPlayer, bool playerAsDest = false, bool mustReachDest = false)
        {
            this.ticksBetweenUpdates = 50;
            AttachData(watchedPed, vehicleIAmDriving, destination, isFriendlyToPlayer, playerAsDest, mustReachDest);
        }

        public void AttachData(Ped targetPed, Vehicle targetVehicle, Vector3 theDest, bool isFriendlyToPlayer, bool playerIsDest, bool mustReachDest)
        {
            this.watchedPed = targetPed;
            this.vehicleIAmDriving = targetVehicle;
            this.destination = theDest;
            this.playerAsDest = playerIsDest;
			this.mustReachDest = mustReachDest;
            this.isFriendlyToPlayer = isFriendlyToPlayer;
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
