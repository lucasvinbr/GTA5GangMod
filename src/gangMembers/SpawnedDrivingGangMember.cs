using GTA.Math;
using GTA.Native;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    public enum VehicleType
    {
        car,
        heli,
        plane,
        unsupported
    }
    public class SpawnedDrivingGangMember : UpdatedClass
    {
        public Ped watchedPed;
        public Gang myGang;
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
                    float despawnDist = ModOptions.instance.carWithDestinationDespawnDistanceFromPlayer;
                    if(vehicleType == VehicleType.plane)
                    {
                        despawnDist *= 10.0f;
                    }
                    if (vehicleIAmDriving.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) > despawnDist)
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

            if(vehicleType != VehicleType.heli && vehicleType != VehicleType.plane)
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
                    if (deliveringCar)
                    {
                        if(vehicleType == VehicleType.car)
                        {
                            DriverLeaveVehicle();
                        } 
                        else if(vehicleType == VehicleType.heli)
                        {
                            // land, then leave heli

                            // pilot ped, piloted heli, target veh, target ped, targed destination X, Y, Z, mission code (9 - circle around dest, 4 - go to dest),
                            // move speed, landing radius, target heading, ?, ?, ?, landing flags (32 - land on dest, 0 - hover over dest)
                            watchedPed.Task.StartHeliMission(vehicleIAmDriving, destination, VehicleMissionType.LandAndWait, 5.0f, 100.0f, 0, 0, -1, 300, HeliMissionFlags.LandOnArrival);

                            if (vehicleIAmDriving.IsOnAllWheels)
                            {
                                DriverLeaveVehicle();
                            }
                        }
                        else if(vehicleType == VehicleType.plane)
                        {
                            // don't try to land planes
                            watchedPed.Task.StartPlaneMission(vehicleIAmDriving, MindControl.CurrentPlayerCharacter, VehicleMissionType.Escort, MAX_SPEED, 200.0f, 80, 80);
                        }
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

                            if (vehicleType == VehicleType.heli)
                            {
                                // land heli if unarmed, keep attacking if armed
                                // if our heli doesn't have guns, land on destination
                                if (vehicleHasGuns)
                                {
                                    var randomEnemy = SpawnManager.instance.GetFirstMemberNotFromMyGang(myGang, true);
                                    if(randomEnemy != null)
                                    {
                                        watchedPed.Task.StartHeliMission(vehicleIAmDriving, randomEnemy, VehicleMissionType.Attack, MAX_SPEED, 100.0f, 30, 20);
                                    }
                                }
                                else
                                {
                                    watchedPed.Task.StartHeliMission(vehicleIAmDriving, destination, VehicleMissionType.LandAndWait, 5.0f, 100.0f, 0, 0, -1, 300, HeliMissionFlags.LandOnArrival);
                                }
                            }
                            else if(vehicleType == VehicleType.plane)
                            {
                                var randomEnemy = SpawnManager.instance.GetFirstMemberNotFromMyGang(myGang, true);
                                if (randomEnemy != null)
                                {
                                    watchedPed.Task.StartPlaneMission(vehicleIAmDriving, randomEnemy, VehicleMissionType.Attack, MAX_SPEED, 200.0f, 60, 60);
                                }
                            }

                            if (updatesWhileDroppingPassengers > ModOptions.instance.driverUpdateLimitWhileDroppingOffPassengers)
                            {
                                ClearAllRefs(true);
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
                            watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relGroup)
                        {
                            Vector3 teleportDest = World.GetNextPositionOnStreet(MindControl.CurrentPlayerCharacter.Position, true);
                            if (vehicleType == VehicleType.heli || vehicleType == VehicleType.plane)
                            {
                                vehicleIAmDriving.Position = teleportDest + Vector3.WorldUp * 120f;
                            }
                            else
                            {
                                vehicleIAmDriving.Position = teleportDest;
                            }
                        }

                    }
                    //wherever we were going, if we intended to leave the car there, let's just leave it here
                    if (deliveringCar && vehicleType != VehicleType.heli && vehicleType != VehicleType.plane)
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
                    // we're still headed for the destination
                    if (!watchedPed.IsPlayer)
                    {
                        if (playerAsDest && playerInVehicle)
                        {

                            //teleport if we're failing to escort due to staying too far
                            //(should only happen with friendly vehicles and if forceSpawnCars is true)
                            if (vehicleType != VehicleType.heli && vehicleType != VehicleType.plane && ModOptions.instance.forceSpawnCars &&
                                watchedPed.RelationshipGroup == GangManager.instance.PlayerGang.relGroup &&
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
                                if (vehicleType == VehicleType.heli)
                                {
                                    // hover over destination (hopefully this means "hover over player's heli")
                                    watchedPed.Task.StartHeliMission(vehicleIAmDriving, destination, VehicleMissionType.Escort, MAX_SPEED / 2, 20.0f, 20, 20);
                                }
                                else if(vehicleType == VehicleType.plane)
                                {
                                    watchedPed.Task.StartPlaneMission(vehicleIAmDriving, MindControl.CurrentPlayerCharacter, VehicleMissionType.Escort, MAX_SPEED, 200.0f, 80, 80);
                                }
                                else
                                {
                                    //just keep following on the ground in this case;
                                    //both allies and enemies should do it
                                    watchedPed.Task.DriveTo
                                        (vehicleIAmDriving, destination, ModOptions.instance.driverDistanceToDestForArrival, MAX_SPEED,
                                        (DrivingStyle)GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest));
                                }
                            }
                            else
                            {
                                if (isFriendlyToPlayer)
                                {
                                    watchedPed.Task.StartVehicleMission(vehicleIAmDriving, MindControl.CurrentPlayerCharacter.CurrentVehicle, VehicleMissionType.Escort, MAX_SPEED / 2.0f, GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest), -1, -1);
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
                                // head towards location.
                                // if our heli doesn't have guns, land on destination
                                if (vehicleHasGuns)
                                {
                                    var randomEnemy = SpawnManager.instance.GetFirstMemberNotFromMyGang(myGang, true);
                                    if (randomEnemy != null)
                                    {
                                        watchedPed.Task.StartHeliMission(vehicleIAmDriving, randomEnemy, VehicleMissionType.Attack, MAX_SPEED, 100.0f, 30, 20);
                                    }
                                }
                                else
                                {
                                    watchedPed.Task.StartHeliMission(vehicleIAmDriving, destination, VehicleMissionType.LandAndWait, 5.0f, 100.0f, 0, 0, -1, 300, HeliMissionFlags.LandOnArrival);
                                }
                            }
                            else if(vehicleType == VehicleType.plane)
                            {
                                var randomEnemy = SpawnManager.instance.GetFirstMemberNotFromMyGang(myGang, true);
                                if (randomEnemy != null)
                                {
                                    watchedPed.Task.StartPlaneMission(vehicleIAmDriving, randomEnemy, VehicleMissionType.Attack, MAX_SPEED, 200.0f, 60, 60);
                                }
                            }
                            else
                            {
                                watchedPed.Task.DriveTo(vehicleIAmDriving, destination, ModOptions.instance.driverDistanceToDestForArrival / 2, targetSpeed,
                                    (DrivingStyle) GetAppropriateDrivingStyle(attemptingUnstuckVehicle, distToDest));
                            }
                        }
                    }
                }
            }
        }

        private VehicleDrivingFlags GetAppropriateDrivingStyle(bool unstucking, float distToDest)
        {
            return (VehicleDrivingFlags)GetAppropriateDrivingStyle_uint(unstucking, distToDest);
        }

        private uint GetAppropriateDrivingStyle_uint(bool unstucking, float distToDest)
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
                //UI.Screen.ShowSubtitle(vehicleIAmDriving.FriendlyName + "'s driver is leaving vehicle", 800);
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
            bool shouldParachute = ((vehicleType == VehicleType.heli || vehicleType == VehicleType.plane) && !vehicleIAmDriving.IsOnAllWheels) ||
                vehicleIAmDriving.HeightAboveGround > 15.0f;
            int numParachuting = 0;
            for (int i = myPassengers.Count - 1; i >= 0; i--)
            {
                var passenger = myPassengers[i];
                if (passenger != watchedPed && MindControl.CurrentPlayerCharacter != passenger)
                {

                    if (passenger.IsUsingAnyVehicleWeapon()) continue;

                    //UI.Screen.ShowSubtitle(vehicleIAmDriving.FriendlyName + " is dropping off passenger " + passenger.SeatIndex + ". veh has guns? " + vehicleHasGuns, 800);
                    if (shouldParachute)
                    {
                        SpawnManager.instance.GetTargetMemberAI(passenger, true)?.StartParachuting(destination, 0 + numParachuting * 1200);
                        numParachuting++;
                        myPassengers.RemoveAt(i);
                    }
                    else
                    {
                        passenger.Task.LeaveVehicle();
                        myPassengers.RemoveAt(i);
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
                if (vehicleIAmDriving.AttachedBlip != null)
                {
                    vehicleIAmDriving.AttachedBlip.Delete();
                }

                vehicleIAmDriving.MarkAsNoLongerNeeded();
                vehicleIAmDriving = null;
            }

            if (makeDriverRoamPostClear && watchedPed.IsInVehicle() && watchedPed.IsAlive && MindControl.CurrentPlayerCharacter != watchedPed)
            {

                if (vehicleType == VehicleType.heli)
                {
                    // flee from player character!
                    watchedPed.Task.StartHeliMission(watchedPed.CurrentVehicle, MindControl.CurrentPlayerCharacter, VehicleMissionType.Flee, 15.0f, 99.0f, 20, 20);
                }
                else if (vehicleType == VehicleType.plane)
                {
                    watchedPed.Task.StartPlaneMission(watchedPed.CurrentVehicle, MindControl.CurrentPlayerCharacter, VehicleMissionType.Flee, MAX_SPEED, 99.0f, 80, 80);
                }
                else
                {
                    watchedPed.VehicleDrivingFlags = (VehicleDrivingFlags) ModOptions.instance.wanderingDriverDrivingStyle;
                    watchedPed.Task.CruiseWithVehicle(watchedPed.CurrentVehicle, 15,
                        (DrivingStyle)ModOptions.instance.wanderingDriverDrivingStyle);
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

        public SpawnedDrivingGangMember(Ped watchedPed, Gang ownerGang, Vehicle vehicleIAmDriving, Vector3 destination, bool isFriendlyToPlayer, bool playerAsDest = false, bool deliveringCar = false)
        {
            ResetUpdateInterval();
            AttachData(watchedPed, ownerGang, vehicleIAmDriving, destination, isFriendlyToPlayer, playerAsDest, deliveringCar);
        }

        public void AttachData(Ped targetPed, Gang ownerGang, Vehicle targetVehicle, Vector3 theDest, bool isFriendlyToPlayer, bool playerIsDest, bool deliveringCar)
        {
            watchedPed = targetPed;
            vehicleIAmDriving = targetVehicle;
            destination = theDest;
            myGang = ownerGang;
            playerAsDest = playerIsDest;
            this.deliveringCar = deliveringCar;
            this.isFriendlyToPlayer = isFriendlyToPlayer;
            targetPed.DrivingStyle = (DrivingStyle) GetAppropriateDrivingStyle(false, 100.0f);
            targetPed.VehicleDrivingFlags = GetAppropriateDrivingStyle(false, 100.0f);
            updatesWhileGoingToDest = 0;
            updatesWhileDroppingPassengers = 0;
            attemptingUnstuckVehicle = false;
            stuckCounter = 0;

            vehicleHasGuns = Function.Call<bool>(Hash.DOES_VEHICLE_HAVE_WEAPONS, targetVehicle);

            SetWatchedPassengers();

            //UI.Screen.ShowSubtitle(vehicleIAmDriving.FriendlyName + " has " + myPassengers.Count + " passengers", 800);

            if (vehicleIAmDriving.IsHelicopter)
            {
                vehicleType = VehicleType.heli;
            }else if(vehicleIAmDriving.IsPlane)
            {
                vehicleType = VehicleType.plane;
            }
            else if (vehicleIAmDriving.IsBoat)
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
            for (int i = 0; i < vehicleIAmDriving.PassengerCapacity; i++)
            {
                Ped memberInSeat = Function.Call<Ped>(Hash.GET_PED_IN_VEHICLE_SEAT, vehicleIAmDriving, i);
                if (!memberInSeat.Exists()) continue;

                myPassengers.Add(memberInSeat);

                if(memberInSeat != watchedPed)
                {
                    memberInSeat.BlockPermanentEvents = false;

                }

                if (vehicleHasGuns)
                {
                    if (memberInSeat.IsUsingAnyVehicleWeapon())
                    {
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, memberInSeat, 3, false); // BF_CanLeaveVehicle  
                    }
                }
            }
        }

        public override void ResetUpdateInterval()
        {
            ticksBetweenUpdates = 50; //TODO modoption?
        }
    }
}
