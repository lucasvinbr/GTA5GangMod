using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to spawning members and vehicles,
	/// and handling/getting info from already spawned ones
    /// </summary>
    public class SpawnManager
    {
        public List<SpawnedGangMember> livingMembers;
        public List<SpawnedDrivingGangMember> livingDrivingMembers;
        public static SpawnManager instance;


		/// <summary>
		/// the number of currently alive members.
		/// (the number of entries in LivingMembers isn't the same as this)
		/// </summary>
		public int livingMembersCount = 0;

		public delegate void SuccessfulMemberSpawnDelegate();

        #region setup

        public SpawnManager()
        {
            instance = this;

            livingMembers = new List<SpawnedGangMember>();
            livingDrivingMembers = new List<SpawnedDrivingGangMember>();
        }


        #endregion

        /// <summary>
        /// marks all living members as no longer needed and removes their blips, 
        /// as if everyone had died or were too far from the player
        /// </summary>
        public void RemoveAllMembers()
        {
            for (int i = 0; i < livingMembers.Count; i++)
            {
                livingMembers[i].Die();
            }

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                livingDrivingMembers[i].ClearAllRefs();
            }

        }

		#region gang general control stuff


        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetSpawnedsUpdateInterval()
        {
            for (int i = 0; i < livingMembers.Count; i++)
            {
                livingMembers[i].ResetUpdateInterval();
            }
        }

        #endregion

        #region getters

        public List<Ped> GetSpawnedPedsOfGang(Gang desiredGang)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets all currently active spawned members of the desired gang.
        /// The onlyGetIfInsideVehicle will only add members who are inside vehicles to the returned list
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <param name="onlyGetIfInsideVehicle"></param>
        /// <returns></returns>
        public List<SpawnedGangMember> GetSpawnedMembersOfGang(Gang desiredGang, bool onlyGetIfInsideVehicle = false)
        {
            List<SpawnedGangMember> returnedList = new List<SpawnedGangMember>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
				if(livingMembers[i] != null) {
					if (livingMembers[i].myGang == desiredGang) {
						if (onlyGetIfInsideVehicle) {
							if (Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, livingMembers[i].watchedPed, false)) {
								returnedList.Add(livingMembers[i]);
							}
						}
						else {
							returnedList.Add(livingMembers[i]);
						}

					}
				}
                
            }

            return returnedList;
        }

        /// <summary>
        /// gets all (alive) driver members of the desired gang.
        /// </summary>
        /// <param name="desiredGang"></param>
        /// <returns></returns>
        public List<SpawnedDrivingGangMember> GetSpawnedDriversOfGang(Gang desiredGang)
        {
            List<SpawnedDrivingGangMember> returnedList = new List<SpawnedDrivingGangMember>();

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed != null)
                {
                    if(livingDrivingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(livingDrivingMembers[i]);
                    }
                }
            }

            return returnedList;
        }

        /// <summary>
        /// gets the SpawnedGangMember object that is handling the target ped's AI, optionally returning null instead if the ped is dead
        /// </summary>
        /// <param name="targetPed"></param>
        /// <param name="onlyGetIfIsAlive"></param>
        /// <returns></returns>
        public SpawnedGangMember GetTargetMemberAI(Ped targetPed, bool onlyGetIfIsAlive = false)
        {
            if (targetPed == null) return null;
            if (!targetPed.IsAlive && onlyGetIfIsAlive) return null;
            for(int i = 0; i < livingMembers.Count; i++)
            {
                if(livingMembers[i].watchedPed == targetPed)
                {
                    return livingMembers[i];
                }
            }

            return null;
        }

        public SpawnedDrivingGangMember GetTargetMemberDrivingAI(Ped targetMember)
        {
            if (targetMember == null) return null;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed == targetMember)
                {
                    return livingDrivingMembers[i];
                }
            }

            return null;
        }

        /// <summary>
        /// returns gang members who are not from the gang provided
        /// </summary>
        /// <param name="myGang"></param>
        /// <returns></returns>
        public List<Ped> GetMembersNotFromMyGang(Gang myGang, bool includePlayer = true)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < livingMembers.Count; i++)
            {
                if (livingMembers[i].watchedPed != null)
                {
                    if (livingMembers[i] != MindControl.instance.currentlyControlledMember &&
                        livingMembers[i].watchedPed.RelationshipGroup != myGang.relationGroupIndex)
                    {
                        returnedList.Add(livingMembers[i].watchedPed);
                    }
                }
            }

            if(includePlayer && myGang != GangManager.instance.PlayerGang)
            {
                returnedList.Add(MindControl.CurrentPlayerCharacter);
            }

            return returnedList;
        }

        public List<Ped> GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
			Logger.Log("GetHostilePedsAround: start");
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            foreach (Ped ped in detectedPeds)
            {
                if (referencePed.RelationshipGroup != ped.RelationshipGroup && ped.IsAlive)
                {
                    int pedRelation = (int) World.GetRelationshipBetweenGroups(ped.RelationshipGroup, referencePed.RelationshipGroup);
                    //if the relationship between them is hate or they were neutral and our reference ped has been hit by this ped...
                    if (pedRelation == 5 ||
                        (pedRelation >= 3 && referencePed.HasBeenDamagedBy(ped))) 
                    {
                        hostilePeds.Add(ped);
                    }
                }
               
            }
			Logger.Log("GetHostilePedsAround: end");
			return hostilePeds;
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player or referencePosition (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember(Vector3? referencePosition = null)
        {
            Vector3 chosenPos = Vector3.Zero;
            Vector3 referencePos = MindControl.CurrentPlayerCharacter.Position;

            if(referencePosition != null)
            {
                referencePos = referencePosition.Value;
            }

            chosenPos = World.GetSafeCoordForPed(referencePos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableMemberSpawnDistance(10));

            return chosenPos;
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor
        /// </summary>
        /// <returns></returns>
        public Vector3 FindCustomSpawnPoint(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            Vector3 chosenPos = Vector3.Zero;

            int attempts = 0;

            chosenPos = World.GetSafeCoordForPed(referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference, false);
            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 3 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {
                chosenPos = World.GetSafeCoordForPed(referencePoint + RandoMath.RandomDirection(true) *
                    averageDistanceFromReference, false);
                distFromRef = World.GetDistance(referencePoint, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

		/// <summary>
		/// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor.
		/// this version uses "GetNextPositionOnStreet"
		/// </summary>
		/// <returns></returns>
		public Vector3 FindCustomSpawnPointInStreet(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0) {
			Vector3 chosenPos = Vector3.Zero;
			Vector3 getNextPosTarget = Vector3.Zero;

			int attempts = 0;

			getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
						  averageDistanceFromReference;

			chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
				World.GetNextPositionOnStreet(getNextPosTarget);
			float distFromRef = World.GetDistance(referencePoint, chosenPos);
			while (((distFromRef > averageDistanceFromReference * 5 || (distFromRef < minDistanceFromReference)) ||
				(repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
				attempts <= maxAttempts) {

				getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
						  averageDistanceFromReference;
				chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
					World.GetNextPositionOnStreet(getNextPosTarget);

				distFromRef = World.GetDistance(referencePoint, chosenPos);
				attempts++;
			}

			return chosenPos;
		}

		public Vector3 FindGoodSpawnPointForCar()
        {
			Vector3 getNextPosTarget = Vector3.Zero;

			getNextPosTarget = MindControl.CurrentPlayerCharacter.Position + RandoMath.RandomDirection(true) *
						  ModOptions.instance.GetAcceptableCarSpawnDistance();

			return WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
					World.GetNextPositionOnStreet(getNextPosTarget);
        }

        /// <summary>
        /// makes one attempt to place the target vehicle on a street.
        /// if it fails, the vehicle is returned to its original position
        /// </summary>
        /// <param name="targetVehicle"></param>
        /// <param name="originalPos"></param>
        public void TryPlaceVehicleOnStreet(Vehicle targetVehicle, Vector3 originalPos)
        {
            targetVehicle.PlaceOnNextStreet();
            float distFromPlayer = World.GetDistance(MindControl.CurrentPlayerCharacter.Position, targetVehicle.Position);

            if(distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer)
            {
                targetVehicle.Position = originalPos;
            }
        }

        public SpawnedGangMember SpawnGangMember(Gang ownerGang, Vector3 spawnPos, SuccessfulMemberSpawnDelegate onSuccessfulMemberSpawn = null)
        {
            if(livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.memberVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }
            if (ownerGang.memberVariations.Count > 0)
            {
				Logger.Log("spawn member: begin");
                PotentialGangMember chosenMember =
                    RandoMath.GetRandomElementFromList(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if(newPed != null)
                {
                    chosenMember.SetPedAppearance(newPed);

                    newPed.Accuracy = ownerGang.memberAccuracyLevel;
                    newPed.MaxHealth = ownerGang.memberHealth;
                    newPed.Health = ownerGang.memberHealth;
                    newPed.Armor = ownerGang.memberArmor;

                    newPed.Money = RandoMath.CachedRandom.Next(60);

					//set the blip, if enabled
					if (ModOptions.instance.showGangMemberBlips) {
						newPed.AddBlip();
						newPed.CurrentBlip.IsShortRange = true;
						newPed.CurrentBlip.Scale = 0.65f;
						Function.Call(Hash.SET_BLIP_COLOUR, newPed.CurrentBlip, ownerGang.blipColor);

						//set blip name - got to use native, the c# blip.name returns error ingame
						Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
						Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, ownerGang.name + " member");
						Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);
					}
                    

                    bool hasDriveByGun = false; //used for when the member has to decide between staying inside a vehicle or not

                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        //get one weap from each type... if possible AND we're not forcing melee only
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.meleeWeapons), 1000, false, true);
                        if (!ModOptions.instance.membersSpawnWithMeleeOnly)
                        {
                            WeaponHash driveByGun = ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons);
                            hasDriveByGun = driveByGun != WeaponHash.Unarmed;
                            newPed.Weapons.Give(driveByGun, 1000, false, true);
                            newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.primaryWeapons), 1000, false, true);

                            //and one extra
                            newPed.Weapons.Give(RandoMath.GetRandomElementFromList(ownerGang.gangWeaponHashes), 1000, false, true);
                        }
                    }

                    //set the relationship group
                    newPed.RelationshipGroup = ownerGang.relationGroupIndex;

                    newPed.NeverLeavesGroup = true;

                    //newPed.BlockPermanentEvents = true;
                    //newPed.StaysInVehicleWhenJacked = true;

                    Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, newPed, false, false); //cannot attack friendlies
                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, newPed, 100); //average combat ability
                    //Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, newPed, 0, 0); //clears the flee attributes?

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // alwaysFight = true and canFightArmedWhenNotArmed. which one is which is unknown
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 5, true);
                    Function.Call(Hash.SET_PED_COMBAT_RANGE, newPed, 2); //combatRange = far
                   
                    newPed.CanSwitchWeapons = true;
                    newPed.CanWrithe = false; //no early dying

                    //enlist this new gang member in the spawned list!
                    SpawnedGangMember newMemberAI = null;

                    bool couldEnlistWithoutAdding = false;
                    for (int i = 0; i < livingMembers.Count; i++)
                    {
                        if (livingMembers[i].watchedPed == null)
                        {
                            livingMembers[i].AttachData(newPed, ownerGang, hasDriveByGun);
                            newMemberAI = livingMembers[i];
                            couldEnlistWithoutAdding = true;
                            break;
                        }
                    }
                    if (!couldEnlistWithoutAdding)
                    {
                        if (livingMembers.Count < ModOptions.instance.spawnedMemberLimit)
                        {
                            newMemberAI = new SpawnedGangMember(newPed, ownerGang, hasDriveByGun);
                            livingMembers.Add(newMemberAI);
                        }
                    }

                    livingMembersCount++;
					onSuccessfulMemberSpawn?.Invoke();
					Logger.Log("spawn member: end (success). livingMembers list size = " + livingMembers.Count);
					return newMemberAI;
                }
                else
                {
					Logger.Log("spawn member: end with fail (world createped returned null)");
					return null;
                }
            }
            return null;
        }

        public SpawnedDrivingGangMember SpawnGangVehicle(Gang ownerGang, Vector3 spawnPos, Vector3 destPos, bool playerIsDest = false, bool mustReachDest = false, SuccessfulMemberSpawnDelegate onSuccessfulPassengerSpawn = null)
        {
            if (livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.carVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }

            if (ownerGang.carVariations.Count > 0)
            {
				Logger.Log("spawn car: start");
				Vehicle newVehicle = World.CreateVehicle(RandoMath.GetRandomElementFromList(ownerGang.carVariations).modelHash, spawnPos);
                if(newVehicle != null)
                {
                    newVehicle.PrimaryColor = ownerGang.vehicleColor;


                    SpawnedGangMember driver = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn : onSuccessfulPassengerSpawn);
                    
                    if (driver != null)
                    {
                        driver.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                        driver.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Driver);

                        int passengerCount = newVehicle.PassengerSeats;
                        if (destPos == Vector3.Zero && passengerCount > 4) passengerCount = 4; //limit ambient passengers in order to have less impact in ambient spawning

                        for (int i = 0; i < passengerCount; i++)
                        {
                            SpawnedGangMember passenger = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn);
                            if (passenger != null)
                            {
                                passenger.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                                passenger.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                            }
                        }

                        SpawnedDrivingGangMember driverAI = EnlistDrivingMember(driver.watchedPed, newVehicle, destPos, ownerGang == GangManager.instance.PlayerGang, playerIsDest, mustReachDest);

						if (ModOptions.instance.showGangMemberBlips) {
							newVehicle.AddBlip();
							newVehicle.CurrentBlip.IsShortRange = true;

							Function.Call(Hash.SET_BLIP_COLOUR, newVehicle.CurrentBlip, ownerGang.blipColor);
						}
                        
						Logger.Log("spawn car: end (success)");
						return driverAI;
                    }
                    else
                    {
                        newVehicle.Delete();
						Logger.Log("spawn car: end (fail: couldnt spawn driver)");
						return null;
                    }
                }

				Logger.Log("spawn car: end (fail: car creation failed)");
			}

            return null;
        }

        public Ped SpawnParachutingMember(Gang ownerGang, Vector3 spawnPos, Vector3 destPos)
        {
            SpawnedGangMember spawnedPara = SpawnGangMember(ownerGang, spawnPos);
            if (spawnedPara != null)
            {
				spawnedPara.watchedPed.BlockPermanentEvents = true;
				spawnedPara.watchedPed.Task.ParachuteTo(destPos);
                return spawnedPara.watchedPed;
            }

            return null;
        }

        SpawnedDrivingGangMember EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, Vector3 destPos, bool friendlyToPlayer, bool playerIsDest = false, bool mustReachDest = false)
        {
            SpawnedDrivingGangMember newDriverAI = null;

            bool couldEnlistWithoutAdding = false;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed == null)
                {
                    newDriverAI = livingDrivingMembers[i];
                    livingDrivingMembers[i].AttachData(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest, mustReachDest);
                    couldEnlistWithoutAdding = true;
                    break;
                }
            }
            if (!couldEnlistWithoutAdding)
            {
                newDriverAI = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest, mustReachDest);
                livingDrivingMembers.Add(newDriverAI);
            }

            return newDriverAI;
        }
        #endregion
    }

}
