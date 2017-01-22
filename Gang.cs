﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the Gang!
    /// gangs have names, colors, turf, money and stats about cars and gang peds
    /// </summary>
    [System.Serializable]
    public class Gang
    {
        public string name;
        public int blipColor;
        public VehicleColor vehicleColor;
        public int moneyAvailable;
        public bool isPlayerOwned = false;

        //the gang's relationshipgroup
        //it's not really saved, so it is set up differently on every run (and reloading of scripts)
        public int relationGroupIndex;

        public int memberAccuracyLevel = 1;
        public int memberHealth = 50;
        public int memberArmor = 0;

        //car stats - the models
        public List<PotentialGangVehicle> carVariations = new List<PotentialGangVehicle>();

        //acceptable ped model/texture/component combinations
        public List<PotentialGangMember> memberVariations = new List<PotentialGangMember>();

        public List<WeaponHash> gangWeaponHashes = new List<WeaponHash>();
        //the weapons the AI Gang will probably buy if they have enough cash
        public List<WeaponHash> preferredWeaponHashes = new List<WeaponHash>();

        public Gang(string name, VehicleColor color, bool isPlayerOwned)
        {
            this.name = name;
            this.vehicleColor = color;

            this.isPlayerOwned = isPlayerOwned;

            moneyAvailable = RandoMath.CachedRandom.Next(10000, 50000); //this isnt used if this is the player's gang - he'll use his own money instead

        }

        public Gang()
        {
            
        }

        public void SetPreferredWeapons()
        {
            if (ModOptions.instance.buyableWeapons.Count == 0) return; //don't even start looking if there aren't any buyable weapons

            //add at least one of each type - melee, primary and drive-by
            preferredWeaponHashes.Add(ModOptions.instance.GetWeaponFromListIfBuyable(ModOptions.instance.meleeWeapons));
            preferredWeaponHashes.Add(ModOptions.instance.GetWeaponFromListIfBuyable(ModOptions.instance.driveByWeapons));
            preferredWeaponHashes.Add(ModOptions.instance.GetWeaponFromListIfBuyable(ModOptions.instance.primaryWeapons));

            //and some more for that extra variation
            for (int i = 0; i < RandoMath.CachedRandom.Next(2, 5); i++)
            {
                preferredWeaponHashes.Add(RandoMath.GetRandomElementFromList(ModOptions.instance.buyableWeapons).wepHash);
            }

            GangManager.instance.SaveGangData(false);
        }


        public void TakeZone(TurfZone takenZone)
        {
            takenZone.value = 0;
            takenZone.ownerGangName = name;
            ZoneManager.instance.UpdateZoneData(takenZone);
        }

        public bool AddMemberVariation(PotentialGangMember newMember)
        {
            for(int i = 0; i < memberVariations.Count; i++)
            {
                if (memberVariations[i].modelHash == newMember.modelHash &&
                        memberVariations[i].hairDrawableIndex == newMember.hairDrawableIndex &&
                        memberVariations[i].headDrawableIndex == newMember.headDrawableIndex &&
                        memberVariations[i].headTextureIndex == newMember.headTextureIndex &&
                        memberVariations[i].legsDrawableIndex == newMember.legsDrawableIndex &&
                        memberVariations[i].legsTextureIndex == newMember.legsTextureIndex &&
                        memberVariations[i].torsoDrawableIndex == newMember.torsoDrawableIndex &&
                        memberVariations[i].torsoTextureIndex == newMember.torsoTextureIndex)
                {
                    return false;
                }
            }

            memberVariations.Add(newMember);
            GangManager.instance.SaveGangData(isPlayerOwned);
            return true;
        }

        public bool RemoveMemberVariation(PotentialGangMember sadMember) // :(
        {
            for (int i = 0; i < memberVariations.Count; i++)
            {
                if(memberVariations[i].headDrawableIndex == -1)
                {
                    if (memberVariations[i].modelHash == sadMember.modelHash &&
                       (memberVariations[i].legsDrawableIndex == -1 || memberVariations[i].legsDrawableIndex == sadMember.legsDrawableIndex) &&
                       (memberVariations[i].legsTextureIndex == -1 || memberVariations[i].legsTextureIndex == sadMember.legsTextureIndex) &&
                       (memberVariations[i].torsoDrawableIndex == -1 || memberVariations[i].torsoDrawableIndex == sadMember.torsoDrawableIndex) &&
                       (memberVariations[i].torsoTextureIndex == -1 || memberVariations[i].torsoTextureIndex == sadMember.torsoTextureIndex))
                    {
                        memberVariations.Remove(memberVariations[i]);

                        //get new members if we have none now and we're AI-controlled
                        if(memberVariations.Count == 0 && !isPlayerOwned)
                        {
                            GangManager.instance.GetMembersForGang(this);
                        }

                        GangManager.instance.SaveGangData();
                        return true;
                    }
                }
                else
                {
                    if (memberVariations[i].modelHash == sadMember.modelHash &&
                        memberVariations[i].hairDrawableIndex == sadMember.hairDrawableIndex &&
                        memberVariations[i].headDrawableIndex == sadMember.headDrawableIndex &&
                        memberVariations[i].headTextureIndex == sadMember.headTextureIndex &&
                       memberVariations[i].legsDrawableIndex == sadMember.legsDrawableIndex &&
                       memberVariations[i].legsTextureIndex == sadMember.legsTextureIndex &&
                       memberVariations[i].torsoDrawableIndex == sadMember.torsoDrawableIndex &&
                       memberVariations[i].torsoTextureIndex == sadMember.torsoTextureIndex)
                    {
                        memberVariations.Remove(memberVariations[i]);

                        //get new members if we have none now and we're AI-controlled
                        if (memberVariations.Count == 0 && !isPlayerOwned)
                        {
                            GangManager.instance.GetMembersForGang(this);
                        }

                        GangManager.instance.SaveGangData();
                        return true;
                    }
                }
                
            }

            return false;
        }

        public bool AddGangCar(PotentialGangVehicle newVehicleType)
        {
            for(int i = 0; i < carVariations.Count; i++)
            {
                if (newVehicleType.modelHash != carVariations[i].modelHash)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            carVariations.Add(newVehicleType);
            GangManager.instance.SaveGangData();
            return true;
        }

        public bool RemoveGangCar(PotentialGangVehicle sadVehicle)
        {
            for (int i = 0; i < carVariations.Count; i++)
            {
                if (sadVehicle.modelHash != carVariations[i].modelHash)
                {
                    continue;
                }
                else
                {
                    carVariations.Remove(carVariations[i]);

                    //if we're AI and we're out of cars, get a replacement for this one
                    if(carVariations.Count == 0 && !isPlayerOwned)
                    {
                        carVariations.Add(PotentialGangVehicle.GetCarFromPool());
                    }

                    GangManager.instance.SaveGangData();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// gives pistols to this gang if the gangsStartWithPistols mod option is toggled on
        /// </summary>
        public void GetPistolIfOptionsRequire()
        {
            if (ModOptions.instance.gangsStartWithPistols)
            {
                if (!gangWeaponHashes.Contains(WeaponHash.Pistol))
                {
                    gangWeaponHashes.Add(WeaponHash.Pistol);
                }
            }
        }

        public void EnforceGangColorConsistency()
        {
            //this checks if the gangs member, blip and car colors are consistent, like black, black and black
            //if left unassigned, the blip color is 0 and the car color is metallic black
            //a sign that somethings wrong, because 0 is white blip color
            ModOptions.GangColorTranslation ourColor = ModOptions.instance.GetGangColorTranslation(memberVariations[0].linkedColor);
            if ((blipColor == 0 && ourColor.baseColor != PotentialGangMember.memberColor.white) ||
                (vehicleColor == VehicleColor.MetallicBlack && ourColor.baseColor != PotentialGangMember.memberColor.black))
            {                
                blipColor = ourColor.blipColor;
                vehicleColor = RandoMath.GetRandomElementFromList(ourColor.vehicleColors);
                GangManager.instance.SaveGangData(false);
            }
        }

        /// <summary>
        /// when an AI gang fights against another, this value is used to influence the outcome
        /// </summary>
        /// <returns></returns>
        public int GetGangAIStrengthValue()
        {
            int weaponValue = 200;
            if(gangWeaponHashes.Count > 0)
            {
                weaponValue = ModOptions.instance.GetBuyableWeaponByHash(RandoMath.GetRandomElementFromList(gangWeaponHashes)).price;
            }
            return moneyAvailable / 10000 +
                ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                weaponValue / 20 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

        public WeaponHash GetListedGunFromOwnedGuns(List<WeaponHash> targetList)
        {
            List<WeaponHash> possibleGuns = new List<WeaponHash>();
            for(int i = 0; i < gangWeaponHashes.Count; i++)
            {
                if (targetList.Contains(gangWeaponHashes[i]))
                {
                    possibleGuns.Add(gangWeaponHashes[i]);
                }
            }

            if (possibleGuns.Count > 0)
            {
                return RandoMath.GetRandomElementFromList(possibleGuns);
            }
            return WeaponHash.Unarmed;
        }

    }
}
