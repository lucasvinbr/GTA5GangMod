using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    class SpawnedDrivingGangMember : UpdatedClass
    {
        public Ped watchedPed;
        public List<Ped> myPassengers = new List<Ped>();
        public Vector3 destination;
        public Vehicle vehicleIAmDriving;
        public int updatesWhileGoingToDest;
        public int updateLimitWhileGoing = 5;

        public override void Update()
        {
            if (vehicleIAmDriving.IsAlive)
            {
                if (vehicleIAmDriving.Position.DistanceTo(destination) < 15)
                {
                    //leave vehicle, everyone stops being important
                    if (!watchedPed.IsPlayer)
                    {
                        watchedPed.Task.LeaveVehicle();
                        watchedPed.MarkAsNoLongerNeeded();
                    }
                   
                    for (int i = 0; i < myPassengers.Count; i++)
                    {
                        if (!myPassengers[i].IsPlayer)
                        {
                            myPassengers[i].MarkAsNoLongerNeeded();
                            myPassengers[i].Task.LeaveVehicle();
                        }
                    }

                    vehicleIAmDriving.IsPersistent = false;
                    vehicleIAmDriving = null;
                    watchedPed = null;
                }
                else
                {
                    updatesWhileGoingToDest++;
                    //give up, drop passengers and go away
                    if(updatesWhileGoingToDest > updateLimitWhileGoing)
                    {
                        //leave vehicle, everyone stops being important
                        if (!watchedPed.IsPlayer)
                        {
                            watchedPed.Task.LeaveVehicle();
                            watchedPed.MarkAsNoLongerNeeded();
                        }

                        for (int i = 0; i < myPassengers.Count; i++)
                        {
                            if (!myPassengers[i].IsPlayer)
                            {
                                myPassengers[i].MarkAsNoLongerNeeded();
                                myPassengers[i].Task.LeaveVehicle();
                            }
                        }

                        vehicleIAmDriving.IsPersistent = false;
                        vehicleIAmDriving = null;
                        watchedPed = null;
                    }
                    else
                    {
                        watchedPed.Task.DriveTo(vehicleIAmDriving, destination, 10, 50);
                    }
                }
            }
            else
            {
                vehicleIAmDriving.IsPersistent = false;
                watchedPed.MarkAsNoLongerNeeded();

                for (int i = 0; i < myPassengers.Count; i++)
                {
                    myPassengers[i].MarkAsNoLongerNeeded();
                }

                vehicleIAmDriving = null;
                watchedPed = null;
            }
            
        }

        public SpawnedDrivingGangMember(Ped watchedPed, Vehicle vehicleIAmDriving, Vector3 destination)
        {
            this.watchedPed = watchedPed;
            this.vehicleIAmDriving = vehicleIAmDriving;
            this.destination = destination;
            this.ticksBetweenUpdates = 600;
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
            }
        }

    }
}
