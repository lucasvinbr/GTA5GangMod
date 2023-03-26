using GTA.Math;
using GTA.Native;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    public enum VehicleType
    {
        car,
        heli,
        unsupported
    }
    public class SpawnedDrivingGangMember : UpdatedClass
    {
        public Ped watchedPed;
        public bool isFriendlyToPlayer = false; //important in order to know if we should follow or chase (aggressively) the player
        public List<Ped> myPassengers = new List<Ped>();
        public Vector3 destination;
        public Vehicle vehicleIAmDriving;
        public int updatesWhileGoingToDest;
        public int updatesWhileDroppingPassengers;

        public VehicleType vehicleType;
        public bool playerAsDest = false;

        public const float MAX_SPEED = 50, SLOW_DOWN_DIST = 120;
        

        private float targetSpeed;
        private float distToDest;


        private bool vehicleHasGuns = false;

        private int stuckCounter = 0;
        /// <summary>
        /// if the stuck counter gets to this value, we switch to another driving style in an attempt to get ourselves out of that position
        /// </summary>
        private const int CHANGE_DRIVESTYLE_STUCK_COUNTER_THRESHOLD = 6;
        /// <summary>
        /// if we're heading towards the destination and our current speed is equal or below this value, we consider ourselves to be stuck
        /// </summary>
        private const float STUCK_SPEED_LIMIT = 1.75f;

        /// <summary>
        /// driving style used when trying to "unstuck" the vehicle
        /// </summary>
        public const int DRIVESTYLE_REVERSE = 2 + 4 + 8 + 32 + 512 + 1024 + 262144;

        /// <summary>
        /// if true, this driver will focus on getting to their destination and, when there, will leave the car...
        /// unless it's a backup called by the player and the player is in a vehicle; 
        /// in that case, it will follow the player around
        /// </summary>
        public bool deliveringCar = false;

        private bool attemptingUnstuckVehicle = false;

        public override void Update()
        {
            if (MindControl.CurrentPlayerCharacter == watchedPed)
            {
                return;
            }

            if (vehicleIAmDriving.IsAlive && watchedPed.IsAlive)
            {
                if (deliveringCar || vehicleType != VehicleType.car)
                {
                    //since we want this vehicle to arrive (and/or not crash),
                    //our driver shouldn't get distracted with fights and stuff
                    watchedPed.BlockPermanentEvents = true;
                }

                if (destination != Vector3.Zero)
                {
                    //even if we are backup vehicles, we should despawn if TOO far
                    if (vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
                            ModOptions.instance.carWithDestinationDespawnDistanceFromPlayer)
                    {
                        DespawnProcedure();
                        return;
                    }

                    //stop tracking this driver/vehicle if he/she leaves the vehicle
                    if (!watchedPed.IsInVehicle(vehicleIAmDriving))
                    {
                        ClearAllRefs();
                        return;
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
                    if (vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
                            ModOptions.instance.roamingCarDespawnDistanceFromPlayer)
                    {

                        DespawnProcedure();
                        return;
                    }

                    //if there is a war going on and we're in the war zone, get to one of the relevant locations;
                    //it's probably close to the action
                    if (GangWarManager.instance.focusedWar != null)
                    {
                        //leave the vehicle after arrival only if it's unarmed and the modOption is active...
                        // or if the enemy needs a vehicle spawn slot
                        deliveringCar =  (!vehicleHasGuns && ModOptions.instance.warSpawnedMembersLeaveGunlessVehiclesOnArrival) ||
                            GangWarManager.instance.focusedWar.IsOneOfTheSidesInNeedOfACarSpawn(); 
                        destination = GangWarManager.instance.focusedWar.GetMoveTargetForGang(GangWarManager.instance.focusedWar.attackingGang);

                        //if spawns still aren't set... try getting to the player
                        if (destination == Vector3.Zero)
                        {
                            //no backup behavior, just a one-shot destination
                            destination = MindControl.SafePositionNearPlayer;
                        }

                        // we can stay around if we don't intend to leave the vehicle
                        if (!deliveringCar)
                        {
                            updatesWhileGoingToDest = 0;
                        }
                        
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

        private void RideToDest()
        {

            bool playerInVehicle = MindControl.CurrentPlayerCharacter.IsInVehicle();

            if (playerAsDest)
            {
                destination = MindControl.SafePositionNearPlayer;
            }

            if(vehicleType != VehicleType.heli)
            {
                distToDest = vehicleIAmDriving.Position.DistanceTo(destination);
            }
            else
            {
                distToDest = vehicleIAmDriving.Position.DistanceTo2D(destination);
            }
            

            //if we're close to the destination...
            if (distToDest < ModOptions.instance.driverDistanceToDestForArrival)
            {
                //if we were heading somewhere (not the player) or we're a backup vehicle and the player's on foot...
                if (!playerAsDest || (playerAsDest && !playerInVehicle))
                {
                    if (deliveringCar && vehicleType == VehicleType.car)
                    {
                        DriverLeaveVehicle();
                    }
                    else
                    {
                        if(GangWarManager.instance.focusedWar != null && GangWarManager.instance.focusedWar.IsOneOfTheSidesInNeedOfACarSpawn())
                        {
                            ClearAllRefs(true);
                        }
                        else
                        {
                            //stay around and keep dropping off passengers for a while
                            DropOffPassengers();

                            destination = Vector3.Zero;

                            updatesWhileDroppingPassengers++;

                            if(updatesWhileDroppingPassengers > ModOptions.instance.driverUpdateLimitWhileDroppingOffPassengers)
                            {
                                ClearAllRefs(true);
                            }
                            else if(vehicleHasGuns)
                            {
                                watchedPed.Task.FightAgainstHatedTargets(200);
                            }
                        }
                    }
                }
            }
            else
            {
                updatesWhileGoingToDest++;

                if(vehicleIAmDriving.Speed <= STUCK_SPEED_LIMIT)
                {
                    stuckCounter++;
                    if(stuckCounter >= CHANGE_DRIVESTYLE_STUCK_COUNTER_THRESHOLD)
                    {
                        //switch our driving style: if we were unstucking, return to normal, and vice-versa
                        attemptingUnstuckVehicle = !attemptingUnstuckVehicle;
                        stuckCounter = 0;
                    }
                }
                else
                {
                    attemptingUnstuckVehicle = false;
                    stuckCounter = 0;
                }

                //we've run out of time to reach the destination.
                //give up, drop passengers and go away... but only if we're not chasing the player
                //and he/she isn't on a vehicle
                if (updatesWhileGoingToDest > ModOptions.instance.driverUpdateLimitWhileGoingToDest &&
                    (!playerAsDest || !playerInVehicle))
                {
                    if (playerAsDest && deliveringCar)
                    {
                        //if we took too long to get to the player and can't be currently seen by the player, lets just teleport close by
                        //...this should only happen with friendly vehicles, or else the player may be blitzkrieg-ed in a not funny way
                        if (!vehicleIAmDriving.IsOnScreen && ModOptions.instance.forceSpawnCars &&
                            watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex)
                        {
                            Vector3 teleportDest = World.GetNextPositionOnStreet(MindControl.CurrentPlayerCharacter.Position, true);
                            if (vehicleType == VehicleType.heli)
                            {
                                vehicleIAmDriving.Position = teleportDest + Vector3.WorldUp * 80f;
                            }
                            else
                            {
                                vehicleIAmDriving.Position = teleportDest;
                            }
                        }

                    }
                    //wherever we were going, if we intended to leave the car there, let's just leave it here
                    if (deliveringCar && vehicleType != VehicleType.heli)
                    {
                        DriverLeaveVehicle();
                    }
                    else
                    {
                        ClearAllRefs(true);
                    }
                }
                else
                {
                    if (!watchedPed.IsPlayer)
                    {
                        if (playerAsDest && playerInVehicle)
                        {

                            //teleport if we're failing to escort due to staying too far
                            //(should only happen with friendly vehicles and if forceSpawnCars is true)
                            if (vehicleType != VehicleType.heli && ModOptions.instance.forceSpawnCars &&
                                watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relationGroupIndex &&
                                vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) >
                                ModOptions.instance.maxDistanceCarSpawnFromPlayer * 2 &&
                                !vehicleIAmDriving.IsOnScreen)
                            {

                                Logger.Log("backup driver: relocation!", 3);
                                vehicleIAmDriving.Position = SpawnManager.instance.FindGoodSpawnPointForCar(destination);

                            }

                            watchedPed.Task.ClearAll();
                            if (MindControl.CurrentPlayerCharacter.IsInFlyingVehicle)
                            {
                                if(vehicleType != VehicleType.heli)
                                {
                                    //just keep following on the ground in this case;
                                    //both allies and enemies should do it
                                    watchedPed.Task.DriveTo
                                        (vehicleIAmDriving, destination, ModOptions.instance.driverDistanceToDestForArrival, MAX_SPEED,
                                        GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest));
                                }
                                else
                                {
                                    // hover over destination (hopefully this means "hover over player's heli")
                                    Function.Call(Hash.TASK_HELI_MISSION, watchedPed, vehicleIAmDriving, 0, 0, destination.X, destination.Y, destination.Z, 9,
                                    MAX_SPEED / 2, ModOptions.instance.driverDistanceToDestForArrival / 2, 0.0f, -1, -1, -1, 0);
                                }
                            }
                            else
                            {
                                if (isFriendlyToPlayer)
                                {
                                    Function.Call(Hash.TASK_VEHICLE_ESCORT, watchedPed, vehicleIAmDriving,
                                        MindControl.CurrentPlayerCharacter.CurrentVehicle, -1, -1,
                                        GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest), 30, 0, 35);
                                }
                                else
                                {

                                    Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, 1.0f);
                                    watchedPed.Task.VehicleChase(MindControl.CurrentPlayerCharacter);
                                }
                            }
                        }
                        else
                        {
                            if (distToDest > SLOW_DOWN_DIST)
                            {
                                targetSpeed = MAX_SPEED;
                            }
                            else
                            {
                                targetSpeed = MAX_SPEED * (distToDest / SLOW_DOWN_DIST);
                            }

                            watchedPed.Task.ClearAll();
                            

                            if (vehicleType == VehicleType.heli)
                            {
                                // pilot ped, piloted heli, target veh, target ped, targed destination X, Y, Z, mission code (9 - circle around dest, 4 - go to dest),
                                // move speed, landing radius, target heading, ?, ?, ?, landing flags (32 - land on dest, 0 - hover over dest)
                                Function.Call(Hash.TASK_HELI_MISSION, watchedPed, vehicleIAmDriving, 0, 0, destination.X, destination.Y, destination.Z, 4,
                                    targetSpeed, 5, 0, -1, -1, -1, 32);
                            }
                            else
                            {
                                watchedPed.Task.DriveTo(vehicleIAmDriving, destination, ModOptions.instance.driverDistanceToDestForArrival / 2, targetSpeed,
                                    GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest));
                            }
                        }
                    }
                }
            }
        }


        private int GetAppropriateDrivingStyle(bool unstucking, float distToDest)
        {
            if (unstucking) return DRIVESTYLE_REVERSE;

            return distToDest <= SLOW_DOWN_DIST / 2 ?
                ModOptions.instance.nearbyDriverWithDestinationDrivingStyle :
                ModOptions.instance.driverWithDestinationDrivingStyle;
        }

        /// <summary>
        /// tells the driver to leave the vehicle, then clearAllRefs
        /// </summary>
        public void DriverLeaveVehicle()
        {
            //leave vehicle, everyone stops being important
            if (MindControl.CurrentPlayerCharacter != watchedPed)
            {
                watchedPed.Task.LeaveVehicle();
                watchedPed.BlockPermanentEvents = false;
            }

            ClearAllRefs();
        }

        /// <summary>
        /// attempts to make everyone not driving or operating a gun to leave the vehicle
        /// </summary>
        public void DropOffPassengers()
        {
            bool shouldParachute = vehicleIAmDriving.Model.IsHelicopter || vehicleIAmDriving.HeightAboveGround > 15.0f;
            int numParachuting = 0;
            for (int i = myPassengers.Count - 1; i >= 0; i--)
            {
                if (myPassengers[i] != watchedPed)
                {
                    //UI.ShowSubtitle(vehicleIAmDriving.FriendlyName + " is dropping off passenger " + myPassengers[i].SeatIndex + ". veh has guns? " + vehicleHasGuns, 800);
                    if (shouldParachute)
                    {
                        SpawnManager.instance.GetTargetMemberAI(myPassengers[i], true)?.StartParachuting(destination, 0 + numParachuting * 1200);
                        numParachuting++;
                        myPassengers.RemoveAt(i);
                    }
                    else
                    {
                        if(MindControl.CurrentPlayerCharacter != myPassengers[i])
                        {
                            myPassengers[i].Task.LeaveVehicle();
                            myPassengers.RemoveAt(i);
                        }
                    }
                    
                }
            }
        }

        /// <summary>
        /// nullifies/clears references to the vehicle, driver and passengers, 
        /// optionally telling the driver to roam
        /// </summary>
        public void ClearAllRefs(bool makeDriverRoamPostClear = false)
        {
            if (vehicleIAmDriving != null)
            {
                if (vehicleIAmDriving.CurrentBlip != null)
                {
                    vehicleIAmDriving.CurrentBlip.Remove();
                }

                vehicleIAmDriving.MarkAsNoLongerNeeded();
                vehicleIAmDriving = null;
            }

            if (makeDriverRoamPostClear && watchedPed.IsInVehicle() && MindControl.CurrentPlayerCharacter != watchedPed)
            {

                if (vehicleType != VehicleType.heli)
                {
                    watchedPed.Task.CruiseWithVehicle(watchedPed.CurrentVehicle, 15,
                        ModOptions.instance.wanderingDriverDrivingStyle);
                }
                else
                {
                    // flee from player character!
                    Function.Call(Hash.TASK_HELI_MISSION, watchedPed, watchedPed.CurrentVehicle, 0, MindControl.CurrentPlayerCharacter, 0, 0, 0, 8,
                                20.0f, 20.0f, 0.0f, -1, -1, -1, 32);
                }
                
            }
            watchedPed = null;
            myPassengers.Clear();

            SpawnManager.instance.thinkingDrivingMembersCount--;
        }

        /// <summary>
        /// calls Die on the passengers' AI then clearsAllRefs
        /// </summary>
        public void DespawnProcedure()
        {
            for (int i = 0; i < myPassengers.Count; i++)
            {
                if (myPassengers[i] != null && myPassengers[i].IsAlive && !myPassengers[i].IsPlayer)
                {
                    SpawnedGangMember memberAI = SpawnManager.instance.GetTargetMemberAI(myPassengers[i]);
                    if (memberAI != null) memberAI.Die();
                }

            }

            if (!watchedPed.IsPlayer)
            {
                SpawnedGangMember memberAI = SpawnManager.instance.GetTargetMemberAI(watchedPed);
                if (memberAI != null) memberAI.Die();
            }
            ClearAllRefs();
        }

        public SpawnedDrivingGangMember(Ped watchedPed, Vehicle vehicleIAmDriving, Vector3 destination, bool isFriendlyToPlayer, bool playerAsDest = false, bool deliveringCar = false)
        {
            ResetUpdateInterval();
            AttachData(watchedPed, vehicleIAmDriving, destination, isFriendlyToPlayer, playerAsDest, deliveringCar);
        }

        public void AttachData(Ped targetPed, Vehicle targetVehicle, Vector3 theDest, bool isFriendlyToPlayer, bool playerIsDest, bool deliveringCar)
        {
            watchedPed = targetPed;
            vehicleIAmDriving = targetVehicle;
            destination = theDest;
            playerAsDest = playerIsDest;
            this.deliveringCar = deliveringCar;
            this.isFriendlyToPlayer = isFriendlyToPlayer;
            Function.Call(Hash.SET_DRIVER_ABILITY, watchedPed, 1.0f);
            updatesWhileGoingToDest = 0;
            updatesWhileDroppingPassengers = 0;
            attemptingUnstuckVehicle = false;
            stuckCounter = 0;
            SetWatchedPassengers();

            vehicleHasGuns = Function.Call<bool>(Hash.DOES_VEHICLE_HAVE_WEAPONS, targetVehicle);
            if (vehicleIAmDriving.Model.IsHelicopter)
            {
                vehicleType = VehicleType.heli;
            }else if(vehicleIAmDriving.Model.IsPlane || vehicleIAmDriving.Model.IsBoat)
            {
                vehicleType = VehicleType.unsupported;
            }
            else
            {
                vehicleType = VehicleType.car;
            }
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
                if(memberInSeat != watchedPed)
                {
                    memberInSeat.BlockPermanentEvents = false;
                }
            }
        }

        public override void ResetUpdateInterval()
        {
            ticksBetweenUpdates = 50; //TODO modoption?
        }
    }
}
