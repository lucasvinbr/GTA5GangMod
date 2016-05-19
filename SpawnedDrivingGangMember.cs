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
        public int updateLimitWhileGoing = 50;

        public bool playerAsDest = false;

        public override void Update()
        {
            if (vehicleIAmDriving.IsAlive)
            {
                if (vehicleIAmDriving.Position.DistanceTo(destination) < 20)
                {
                    EveryoneLeaveVehicle();
                }
                else
                {
                    updatesWhileGoingToDest++;

                    if (playerAsDest) destination = Game.Player.Character.Position;

                    //give up, drop passengers and go away
                    if(updatesWhileGoingToDest > updateLimitWhileGoing)
                    {
                        EveryoneLeaveVehicle();
                    }
                    else
                    {
                        if (!watchedPed.IsPlayer)
                        {
                            watchedPed.Task.ClearAll();
                            watchedPed.Task.DriveTo(vehicleIAmDriving, destination, 10, 7 * Vector3.Distance(watchedPed.Position, destination));
                            Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, watchedPed, 4457020);
                        }
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
                vehicleIAmDriving.CurrentBlip.Remove();
                vehicleIAmDriving = null;
                watchedPed = null;
            }
            
        }

        public void EveryoneLeaveVehicle()
        {
            //leave vehicle, everyone stops being important
            if (!watchedPed.IsPlayer)
            {
                watchedPed.Task.ClearAll();
                watchedPed.Task.LeaveVehicle();
                Function.Call(Hash.RESET_PED_LAST_VEHICLE, watchedPed);
                watchedPed.MarkAsNoLongerNeeded();
            }

            for (int i = 0; i < myPassengers.Count; i++)
            {
                if (!myPassengers[i].IsPlayer)
                {
                    myPassengers[i].MarkAsNoLongerNeeded();
                    myPassengers[i].Task.ClearAll();
                    myPassengers[i].Task.LeaveVehicle();
                    Function.Call(Hash.RESET_PED_LAST_VEHICLE, myPassengers[i]);
                }
            }
            vehicleIAmDriving.CurrentBlip.Remove();
            vehicleIAmDriving.IsPersistent = false;
            vehicleIAmDriving = null;
            watchedPed = null;
        }

        public SpawnedDrivingGangMember(Ped watchedPed, Vehicle vehicleIAmDriving, Vector3 destination, bool playerAsDest = false)
        {
            this.watchedPed = watchedPed;
            this.vehicleIAmDriving = vehicleIAmDriving;
            this.destination = destination;
            this.ticksBetweenUpdates = 50;
            this.playerAsDest = playerAsDest;
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
