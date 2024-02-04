using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to spawning members and vehicles,
    /// and handling/getting info from already spawned ones
    /// </summary>
    public class SpawnManager
    {
        /// <summary>
        /// all memberAIs initialized since the mod started. Not all are assured to be being used at any time
        /// </summary>
        public List<SpawnedGangMember> memberAIs;
        public List<SpawnedDrivingGangMember> livingDrivingMembers;

        /// <summary>
        /// a "preserved" member is a dead one that is kept around in an effort to keep the body from disappearing
        /// </summary>
        public List<Ped> preservedDeadBodies;


        public static SpawnManager instance;


        /// <summary>
        /// the number of currently alive members.
        /// (the number of entries in memberAIs isn't the same as this)
        /// </summary>
        public int livingMembersCount = 0;

        /// <summary>
        /// the number of "thinking" driving members, those that we still want to control
        /// </summary>
        public int thinkingDrivingMembersCount = 0;

        public delegate void SuccessfulMemberSpawnDelegate();

        #region setup

        public SpawnManager()
        {
            instance = this;

            memberAIs = new List<SpawnedGangMember>();
            livingDrivingMembers = new List<SpawnedDrivingGangMember>();
            preservedDeadBodies = new List<Ped>();
        }


        #endregion

        #region cleanup
        /// <summary>
        /// marks all living members as no longer needed and removes their blips, 
        /// as if everyone had died or were too far from the player
        /// </summary>
        public void RemoveAllMembers()
        {
            for (int i = 0; i < memberAIs.Count; i++)
            {
                memberAIs[i].Die();
            }

            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                livingDrivingMembers[i].ClearAllRefs();
            }

        }

        public void RemoveAllDeadBodies()
        {
            foreach(Ped deadPed in preservedDeadBodies)
            {
                deadPed.MarkAsNoLongerNeeded();
            }

            preservedDeadBodies.Clear();
        }

        #endregion

        #region reset handling


        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetSpawnedsUpdateInterval()
        {
            for (int i = 0; i < memberAIs.Count; i++)
            {
                memberAIs[i].ResetUpdateInterval();
            }
        }

        #endregion

        #region Eddlm's spawnpos generator

        //from: https://gtaforums.com/topic/843561-pathfind-node-types
        //with some personal preference edits

        public enum Nodetype
        {
            Road,
            AnyRoad,
            Offroad,
            Water
        }

        /// <summary>
        /// gets the closest vehicle node of the desired type, 
        /// optionally returning the next pos on sidewalk instead of the node pos.
        /// All credit goes to Eddlm!
        /// </summary>
        /// <param name="desiredPos"></param>
        /// <param name="roadtype"></param>
        /// <param name="sidewalk"></param>
        /// <returns></returns>
        public static Vector3 GenerateSpawnPos(Vector3 desiredPos, Nodetype roadtype, bool sidewalk)
        {
            Vector3 finalpos;
            bool forceOffroad = false;
            OutputArgument outArgA = new OutputArgument();
            int nodeNumber = 1;
            int roadTypeAsInt;
            switch (roadtype)
            {
                case Nodetype.Offroad:
                    roadTypeAsInt = 1;
                    forceOffroad = true;
                    break;
                default:
                    roadTypeAsInt = (int)roadtype;
                    break;
            }

            int nodeID = Function.Call<int>(
                Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID, desiredPos.X, desiredPos.Y, desiredPos.Z,
                nodeNumber, roadTypeAsInt, 300f, 300f);
            if (forceOffroad)
            {
                while (!Function.Call<bool>(Hash.GET_VEHICLE_NODE_IS_SWITCHED_OFF, nodeID) && nodeNumber < 500)
                {
                    nodeNumber++;
                    nodeID = Function.Call<int>(Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID,
                        desiredPos.X, desiredPos.Y, desiredPos.Z, nodeNumber, roadTypeAsInt, 300f, 300f);
                }
            }

            Function.Call(Hash.GET_VEHICLE_NODE_POSITION, nodeID, outArgA);
            finalpos = outArgA.GetResult<Vector3>();
            if (sidewalk) finalpos = World.GetNextPositionOnSidewalk(finalpos);

            return finalpos;
        }

        /// <summary>
        /// gets the closest vehicle node and its heading
        /// </summary>
        /// <param name="desiredPos"></param>
        /// <returns></returns>
        public static Vector3 GenerateSpawnPosWithHeading(Vector3 desiredPos, out float heading)
        {
            Vector3 finalpos;
            int roadTypeAsInt = (int)Nodetype.AnyRoad;

            OutputArgument outCoords = new OutputArgument();
            OutputArgument outRoadheading = new OutputArgument();

            Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,
                desiredPos.X, desiredPos.Y, desiredPos.Z, outCoords, outRoadheading, roadTypeAsInt, 3, 0);

            finalpos = outCoords.GetResult<Vector3>();
            heading = outRoadheading.GetResult<float>();

            return finalpos;
        }

        #endregion

        #region getters

        public List<Ped> GetSpawnedPedsOfGang(Gang desiredGang)
        {
            List<Ped> returnedList = new List<Ped>();

            for (int i = 0; i < memberAIs.Count; i++)
            {
                if (memberAIs[i].watchedPed != null)
                {
                    if (memberAIs[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
                    {
                        returnedList.Add(memberAIs[i].watchedPed);
                    }
                }
            }

            return returnedList;
        }

        public List<SpawnedGangMember> GetAllLivingMembers()
        {
            List<SpawnedGangMember> returnedList = new List<SpawnedGangMember>();

            for (int i = 0; i < memberAIs.Count; i++)
            {
                if (memberAIs[i] != null)
                {
                    if (memberAIs[i].watchedPed != null)
                    {
                        returnedList.Add(memberAIs[i]);
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

            for (int i = 0; i < memberAIs.Count; i++)
            {
                if (memberAIs[i] != null)
                {
                    if (memberAIs[i].myGang == desiredGang)
                    {
                        if (onlyGetIfInsideVehicle)
                        {
                            if (Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, memberAIs[i].watchedPed, false))
                            {
                                returnedList.Add(memberAIs[i]);
                            }
                        }
                        else
                        {
                            returnedList.Add(memberAIs[i]);
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
                    if (livingDrivingMembers[i].watchedPed.RelationshipGroup == desiredGang.relationGroupIndex)
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
            for (int i = 0; i < memberAIs.Count; i++)
            {
                if (memberAIs[i].watchedPed == targetPed)
                {
                    return memberAIs[i];
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

            for (int i = 0; i < memberAIs.Count; i++)
            {
                if (memberAIs[i].watchedPed != null)
                {
                    if (memberAIs[i] != MindControl.currentlyControlledMember &&
                        memberAIs[i].watchedPed.RelationshipGroup != myGang.relationGroupIndex)
                    {
                        returnedList.Add(memberAIs[i].watchedPed);
                    }
                }
            }

            if (includePlayer && myGang != GangManager.instance.PlayerGang)
            {
                returnedList.Add(MindControl.CurrentPlayerCharacter);
            }

            return returnedList;
        }

        public List<Ped> GetHostilePedsAround(Vector3 targetPos, Ped referencePed, float radius)
        {
            Logger.Log("GetHostilePedsAround: start", 3);
            Ped[] detectedPeds = World.GetNearbyPeds(targetPos, radius);

            List<Ped> hostilePeds = new List<Ped>();

            var refPedRelGroup = referencePed.RelationshipGroup;

            foreach (Ped ped in detectedPeds)
            {
                if (refPedRelGroup != ped.RelationshipGroup && ped.IsAlive)
                {
                    int pedRelation = (int)refPedRelGroup.GetRelationshipBetweenGroups(ped.RelationshipGroup);
                    //if the relationship between them is hate or they were neutral and our reference ped has been hit by this ped...
                    if (pedRelation == 5 ||
                        (pedRelation >= 3 && referencePed.HasBeenDamagedBy(ped)))
                    {
                        hostilePeds.Add(ped);
                    }
                }

            }
            Logger.Log("GetHostilePedsAround: end", 3);
            return hostilePeds;
        }


        /// <summary>
        /// true if there are too many drivers with driver AI attached
        /// </summary>
        /// <returns></returns>
        public bool HasThinkingDriversLimitBeenReached()
        {
            return thinkingDrivingMembersCount >= ModOptions.instance.thinkingCarLimit;
        }

        #endregion

        #region spawner methods

        /// <summary>
        /// a good spawn point is one that is not too close and not too far from the player or referencePosition (according to the Mod Options)
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForMember(Vector3? referencePosition = null)
        {
            Vector3 chosenPos;
            Vector3 referencePos = referencePosition ?? MindControl.SafePositionNearPlayer;


            chosenPos = World.GetNextPositionOnSidewalk(referencePos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableMemberSpawnDistance(10));

            return chosenPos;
        }

        /// <summary>
        /// finds a spawn point that is close to the specified reference point and, optionally, far from the specified repulsor
        /// </summary>
        /// <returns></returns>
        public Vector3 FindCustomSpawnPoint(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            Vector3 chosenPos;

            int attempts = 0;

            chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference);
            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 3 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {
                chosenPos = World.GetNextPositionOnSidewalk(referencePoint + RandoMath.RandomDirection(true) *
                    averageDistanceFromReference);
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
        public Vector3 FindCustomSpawnPointInStreet(Vector3 referencePoint, float averageDistanceFromReference, float minDistanceFromReference, int maxAttempts = 10, Vector3? repulsor = null, float minDistanceFromRepulsor = 0)
        {
            Vector3 chosenPos;
            Vector3 getNextPosTarget;

            int attempts = 0;

            getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference;

            chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                World.GetNextPositionOnStreet(getNextPosTarget);
            float distFromRef = World.GetDistance(referencePoint, chosenPos);
            while (((distFromRef > averageDistanceFromReference * 5 || (distFromRef < minDistanceFromReference)) ||
                (repulsor != null && World.GetDistance(repulsor.Value, chosenPos) < minDistanceFromRepulsor)) &&
                attempts <= maxAttempts)
            {

                getNextPosTarget = referencePoint + RandoMath.RandomDirection(true) *
                          averageDistanceFromReference;
                chosenPos = WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                    World.GetNextPositionOnStreet(getNextPosTarget);

                distFromRef = World.GetDistance(referencePoint, chosenPos);
                attempts++;
            }

            return chosenPos;
        }

        /// <summary>
        /// finds a nice spot neither too close or far from the reference pos, or the player safe pos if not provided.
        /// use the safe pos as parameter if you've already got it before calling this func!
        /// </summary>
        /// <param name="referencePos"></param>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForCar(Vector3? referencePos = null)
        {
            Vector3 refPos = referencePos ?? MindControl.SafePositionNearPlayer;
            Vector3 getNextPosTarget;


            getNextPosTarget = refPos + RandoMath.RandomDirection(true) *
                          ModOptions.instance.GetAcceptableCarSpawnDistance();

            return WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                    GenerateSpawnPos(getNextPosTarget, Nodetype.AnyRoad, false);
        }

        /// <summary>
        /// finds a nice spot in the general direction neither too close or far from the reference pos
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointForCar(Vector3 referencePos, Vector3 targetDirectionFromReference)
        {
            Vector3 getNextPosTarget;


            getNextPosTarget = referencePos + targetDirectionFromReference *
                          ModOptions.instance.GetAcceptableCarSpawnDistance();

            return WorldLocChecker.PlayerIsAwayFromRoads ? World.GetNextPositionOnSidewalk(getNextPosTarget) :
                    GenerateSpawnPos(getNextPosTarget, Nodetype.AnyRoad, false); ;
        }

        /// <summary>
        /// finds a nice spot and heading in the general direction neither too close or far from the reference pos
        /// </summary>
        /// <returns></returns>
        public Vector3 FindGoodSpawnPointWithHeadingForCar(Vector3 referencePos, Vector3 targetDirectionFromReference, out float heading)
        {
            Vector3 getNextPosTarget;


            getNextPosTarget = referencePos + targetDirectionFromReference *
                          ModOptions.instance.GetAcceptableCarSpawnDistance();

            if (WorldLocChecker.PlayerIsAwayFromRoads)
            {
                heading = RandoMath.RandomHeading();
                return World.GetNextPositionOnSidewalk(getNextPosTarget);
            }
            else
            {
                return GenerateSpawnPosWithHeading(getNextPosTarget, out heading);
            }
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
            float distFromPlayer = targetVehicle.Position.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position);

            if (distFromPlayer > ModOptions.instance.maxDistanceCarSpawnFromPlayer ||
                distFromPlayer < ModOptions.instance.minDistanceCarSpawnFromPlayer)
            {
                targetVehicle.Position = originalPos;
            }
        }

        public SpawnedGangMember SpawnGangMember(Gang ownerGang, Vector3 spawnPos, SuccessfulMemberSpawnDelegate onSuccessfulMemberSpawn = null, bool spawnWithWeaponEquipped = false)
        {
            if (livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.memberVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }
            if (ownerGang.memberVariations.Count > 0)
            {
                Logger.Log(string.Concat("spawn member: begin (gang: ", ownerGang.name, ")"), 4);
                PotentialGangMember chosenMember =
                    RandoMath.RandomElement(ownerGang.memberVariations);
                Ped newPed = World.CreatePed(chosenMember.modelHash, spawnPos);
                if (newPed != null)
                {
                    chosenMember.SetPedAppearance(newPed);

                    newPed.Accuracy = (int) (ownerGang.memberAccuracyLevel * ownerGang.memberAccuracyMultiplier);

                    newPed.CanWrithe = ModOptions.instance.gangMembersCanWrithe; //no early dying?
                    int memberHealth = 100 + RandoMath.Max(1, (int)(ownerGang.memberHealth * ownerGang.memberHealthMultiplier));
                    newPed.MaxHealth = memberHealth;
                    newPed.Health = memberHealth;
                    newPed.Armor = (int) (ownerGang.memberArmor * ownerGang.memberArmorMultiplier);

                    newPed.IsFireProof = ModOptions.instance.gangMembersAreFireproof;

                    newPed.FiringPattern = ownerGang.membersFiringPattern;

                    if (ModOptions.instance.membersCanDropMoneyOnDeath)
                    {
                        newPed.Money = RandoMath.CachedRandom.Next(60);
                    }
                    else
                    {
                        newPed.Money = 0;
                    }

                    //set the blip, if enabled
                    if (ModOptions.instance.showGangMemberBlips)
                    {
                        newPed.AddBlip();
                        newPed.CurrentBlip.IsShortRange = true;
                        newPed.CurrentBlip.Scale = 0.65f;
                        Function.Call(Hash.SET_BLIP_COLOUR, newPed.CurrentBlip, ownerGang.blipColor);

                        //set blip name - got to use native, the c# blip.name returns error ingame
                        Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                        Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Format(Localization.GetTextByKey("blip_member_of_gang_x", "{0} member"), ownerGang.name));
                        Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, newPed.CurrentBlip);
                    }


                    bool hasDriveByGun = false; //used for when the member has to decide between staying inside a vehicle or not

                    //give a weapon
                    if (ownerGang.gangWeaponHashes.Count > 0)
                    {
                        //get one weap from each type... if possible AND we're not forcing melee only
                        newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.meleeWeapons), 1000, spawnWithWeaponEquipped, true);
                        if (!ModOptions.instance.membersSpawnWithMeleeOnly)
                        {
                            WeaponHash driveByGun = ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons);
                            hasDriveByGun = driveByGun != WeaponHash.Unarmed;
                            newPed.Weapons.Give(driveByGun, 1000, false, true);
                            newPed.Weapons.Give(ownerGang.GetListedGunFromOwnedGuns(ModOptions.instance.primaryWeapons), 1000, spawnWithWeaponEquipped, true);

                            //and one extra
                            newPed.Weapons.Give(RandoMath.RandomElement(ownerGang.gangWeaponHashes), 1000, false, true);
                        }
                    }

                    //set the relationship group
                    newPed.RelationshipGroup = ownerGang.relationGroupIndex;

                    newPed.CanSwitchWeapons = true;

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 0, ModOptions.instance.gangMembersCanUseCover);

                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 46, true); // BF_CanFightArmedPedsWhenNotArmed 
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 5, true); // BF_AlwaysFight 
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, newPed, 68, !ModOptions.instance.gangMembersReactToFriendliesBeingShot); // BF_DisableReactToBuddyShot

                    newPed.SetConfigFlag(107, !ModOptions.instance.gangMembersRagdollWhenShot); //CPED_CONFIG_FLAG_DontActivateRagdollFromBulletImpact 
                    newPed.SetConfigFlag(227, true); //CPED_CONFIG_FLAG_ForceRagdollUponDeath 
                    newPed.SetConfigFlag(237, false); //CPED_CONFIG_FLAG_BlocksPathingWhenDead  

                    newPed.DrownsInWater = false;

                    //enlist this new gang member in the spawned list!
                    SpawnedGangMember newMemberAI = null;

                    bool couldEnlistWithoutAdding = false;
                    for (int i = 0; i < memberAIs.Count; i++)
                    {
                        if (memberAIs[i].watchedPed == null)
                        {
                            memberAIs[i].AttachData(newPed, ownerGang, hasDriveByGun);
                            newMemberAI = memberAIs[i];
                            couldEnlistWithoutAdding = true;
                            break;
                        }
                    }
                    if (!couldEnlistWithoutAdding)
                    {
                        newMemberAI = new SpawnedGangMember(newPed, ownerGang, hasDriveByGun);
                        memberAIs.Add(newMemberAI);
                    }

                    livingMembersCount++;
                    onSuccessfulMemberSpawn?.Invoke();
                    Logger.Log("spawn member: end (success). memberAI list size = " + memberAIs.Count, 4);
                    return newMemberAI;
                }
                else
                {
                    Logger.Log("spawn member: end with fail (world createped returned null)", 4);
                    return null;
                }
            }
            return null;
        }

        public SpawnedDrivingGangMember SpawnGangVehicle(Gang ownerGang, Vector3 spawnPos, Vector3 destPos, bool playerIsDest = false, bool isDeliveringCar = false, SuccessfulMemberSpawnDelegate onSuccessfulPassengerSpawn = null, int maxMembersToSpawnInVehicle = -1)
        {
            if (livingMembersCount >= ModOptions.instance.spawnedMemberLimit || spawnPos == Vector3.Zero || ownerGang.carVariations == null)
            {
                //don't start spawning, we're on the limit already or we failed to find a good spawn point or we haven't started up our data properly yet
                return null;
            }

            if (ownerGang.carVariations.Count > 0)
            {
                Logger.Log("spawn car: start", 4);
                PotentialGangVehicle potentialGangVehicle = RandoMath.RandomElement(ownerGang.carVariations);

                Vehicle newVehicle = World.CreateVehicle(potentialGangVehicle.modelHash, spawnPos);

                if(!ModOptions.instance.gangHelicoptersEnabled && newVehicle.Model.IsHelicopter && (!playerIsDest && !isDeliveringCar))
                {
                    newVehicle.Delete();
                    return null;
                }

                if (newVehicle != null)
                {
                    newVehicle.PrimaryColor = ownerGang.vehicleColor;
                    newVehicle.SecondaryColor = ownerGang.secondaryVehicleColor;

                    SpawnedGangMember driver = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn, true);

                    if (driver != null)
                    {
                        driver.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                        driver.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Driver);

                        int passengerCount = newVehicle.PassengerSeats;

                        if(destPos == Vector3.Zero)
                        {
                            passengerCount = RandoMath.Min(passengerCount, 4);//limit ambient passengers in order to have less impact in ambient spawning
                        }
                        else
                        {
                            if(maxMembersToSpawnInVehicle != -1)
                            {
                                passengerCount = RandoMath.Min(passengerCount, maxMembersToSpawnInVehicle - 1);
                            }
                        }



                        for (int i = 0; i < passengerCount; i++)
                        {
                            SpawnedGangMember passenger = SpawnGangMember(ownerGang, spawnPos, onSuccessfulMemberSpawn: onSuccessfulPassengerSpawn, true);
                            if (passenger != null)
                            {
                                passenger.curStatus = SpawnedGangMember.MemberStatus.inVehicle;
                                passenger.watchedPed.SetIntoVehicle(newVehicle, VehicleSeat.Any);
                            }
                        }

                        SpawnedDrivingGangMember driverAI = EnlistDrivingMember(driver.watchedPed, newVehicle, destPos, ownerGang == GangManager.instance.PlayerGang, playerIsDest, isDeliveringCar);

                        if (ModOptions.instance.showGangMemberBlips)
                        {
                            newVehicle.AddBlip();
                            newVehicle.CurrentBlip.IsShortRange = true;

                            Function.Call(Hash.SET_BLIP_COLOUR, newVehicle.CurrentBlip, ownerGang.blipColor);
                        }

                        newVehicle.IsRadioEnabled = false;

                        // extra handling to spawn flying helicopters
                        if (newVehicle.Model.IsHelicopter)
                        {
                            newVehicle.Position += Vector3.WorldUp * (100 + RandoMath.CachedRandom.Next(50));
                            Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, newVehicle);
                        }

                        // Apply the stored mods to the newVehicle
                        if (potentialGangVehicle.VehicleMods != null)
                        {
                            // Ensure the vehicle has a valid modkit ID
                            Function.Call(Hash.SET_VEHICLE_MOD_KIT, newVehicle, 0);

                            foreach (var modData in potentialGangVehicle.VehicleMods)
                            {
                                if (modData.ModValue != -1)
                                {
                                    newVehicle.SetMod(modData.ModType, modData.ModValue, false);
                                }
                            }
                        }

                        thinkingDrivingMembersCount++;
                        Logger.Log("spawn car: end (success)", 4);
                        return driverAI;
                    }
                    else
                    {
                        newVehicle.Delete();
                        Logger.Log("spawn car: end (fail: couldn't spawn driver)", 4);
                        return null;
                    }
                }

                Logger.Log("spawn car: end (fail: car creation failed)", 4);
            }

            return null;
        }

        public Ped SpawnParachutingMember(Gang ownerGang, Vector3 spawnPos, Vector3 destPos)
        {
            SpawnedGangMember spawnedPara = SpawnGangMember(ownerGang, spawnPos);
            if (spawnedPara != null)
            {
                //spawnedPara.watchedPed.BlockPermanentEvents = true;
                //spawnedPara.watchedPed.Task.ParachuteTo(destPos);
                spawnedPara.StartParachuting(destPos, 100);
                return spawnedPara.watchedPed;
            }

            return null;
        }

        private SpawnedDrivingGangMember EnlistDrivingMember(Ped pedToEnlist, Vehicle vehicleDriven, Vector3 destPos, bool friendlyToPlayer, bool playerIsDest = false, bool deliveringCar = false)
        {
            SpawnedDrivingGangMember newDriverAI = null;

            bool couldEnlistWithoutAdding = false;
            for (int i = 0; i < livingDrivingMembers.Count; i++)
            {
                if (livingDrivingMembers[i].watchedPed == null)
                {
                    newDriverAI = livingDrivingMembers[i];
                    livingDrivingMembers[i].AttachData(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest, deliveringCar);
                    couldEnlistWithoutAdding = true;
                    break;
                }
            }
            if (!couldEnlistWithoutAdding)
            {
                newDriverAI = new SpawnedDrivingGangMember(pedToEnlist, vehicleDriven, destPos, friendlyToPlayer, playerIsDest, deliveringCar);
                livingDrivingMembers.Add(newDriverAI);
            }

            return newDriverAI;
        }
        #endregion

        #region dead bodies related

        /// <summary>
        /// stores the body in a list;
        /// we keep this list's size under the deadBodyLimit set in the ModOptions
        /// </summary>
        /// <param name="deadPed"></param>
        public void PreserveDeadBody(Ped deadPed)
        {
            if (ModOptions.instance.preservedDeadBodyLimit <= 0)
            {
                //if the limit's at 0, this feature is disabled
                deadPed.MarkAsNoLongerNeeded();
                return; 
            }

            //remove "old" bodies before adding this one
            while(preservedDeadBodies.Count > ModOptions.instance.preservedDeadBodyLimit)
            {
                preservedDeadBodies[0].MarkAsNoLongerNeeded();
                preservedDeadBodies.RemoveAt(0);
            }

            preservedDeadBodies.Add(deadPed);
        }

        #endregion
    }

}
