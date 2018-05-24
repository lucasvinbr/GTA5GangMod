using System;
using System.Collections.Generic;
using GTA.Native;
using System.Xml.Serialization;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the Gang!
    /// gangs have names, colors, turf, money and stats about cars and gang peds
    /// </summary>
    public class Gang
    {
        public string name;
        public int blipColor;
        public VehicleColor vehicleColor;
        public int moneyAvailable;
        public bool isPlayerOwned = false;

        //the gang's relationshipgroup
		[XmlIgnore]
        public int relationGroupIndex;

        public int memberAccuracyLevel = 1;
        public int memberHealth = 10;
        public int memberArmor = 0;

        public int baseTurfValue = 0;

        //car stats - the models
        public List<PotentialGangVehicle> carVariations = new List<PotentialGangVehicle>();

        //acceptable ped model/texture/component combinations
        public List<PotentialGangMember> memberVariations = new List<PotentialGangMember>();

        public List<WeaponHash> gangWeaponHashes = new List<WeaponHash>();
        //the weapons the AI Gang will probably buy if they have enough cash
        public List<WeaponHash> preferredWeaponHashes = new List<WeaponHash>();

        /// <summary>
        /// what does this Gang give priority to when spending money?
        /// (all gangs should try to expand a little, but the expanders should go beyond that)
        /// </summary>
        public enum AIUpgradeTendency
        {
            toughMembers,
            bigGuns,
            toughTurf,
            moreExpansion
        }

        public AIUpgradeTendency upgradeTendency = AIUpgradeTendency.moreExpansion;

        public Gang(string name, VehicleColor color, bool isPlayerOwned, int moneyAvailable = -1)
        {
            this.name = name;
            this.vehicleColor = color;

            this.isPlayerOwned = isPlayerOwned;

            this.memberHealth = ModOptions.instance.startingGangMemberHealth;

            if(!isPlayerOwned)
            {
                upgradeTendency = (AIUpgradeTendency) RandoMath.CachedRandom.Next(4);
            }

            if(moneyAvailable <= 0)
            {
                this.moneyAvailable = RandoMath.CachedRandom.Next(5, 15) * ModOptions.instance.baseCostToTakeTurf; //this isnt used if this is the player's gang - he'll use his own money instead
            }
            else
            {
                this.moneyAvailable = moneyAvailable;
            }
            

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


        public void TakeZone(TurfZone takenZone, bool doNotify = true)
        {
            if(doNotify && ModOptions.instance.notificationsEnabled)
            {
                string notificationMsg = string.Concat("The ", name, " have taken ", takenZone.zoneName);
                if(takenZone.ownerGangName != "none")
                {
                    notificationMsg = string.Concat(notificationMsg, " from the ", takenZone.ownerGangName);
                }
                notificationMsg = string.Concat(notificationMsg, "!");
                UI.Notify(notificationMsg);
            }
            takenZone.value = baseTurfValue;
            takenZone.ownerGangName = name;
            ZoneManager.instance.UpdateZoneData(takenZone);
        }

		/// <summary>
		///this checks if the gangs member, blip and car colors are consistent, like black, black and black.
		///if left unassigned, the blip color is 0 and the car color is metallic black:
		///a sign that somethings wrong, because 0 is white blip color
		/// </summary>
		public void EnforceGangColorConsistency() {
			ModOptions.GangColorTranslation ourColor = ModOptions.instance.GetGangColorTranslation(memberVariations[0].linkedColor);
			if ((blipColor == 0 && ourColor.baseColor != PotentialGangMember.memberColor.white) ||
				(vehicleColor == VehicleColor.MetallicBlack && ourColor.baseColor != PotentialGangMember.memberColor.black)) {
				blipColor = RandoMath.GetRandomElementFromArray(ourColor.blipColors);
				vehicleColor = RandoMath.GetRandomElementFromList(ourColor.vehicleColors);
				GangManager.instance.SaveGangData(false);
			}
		}

		/// <summary>
		/// checks and adjusts (if necessary) this gang's levels in order to make it conform to the current modOptions
		/// </summary>
		public void AdjustStatsToModOptions() {
			memberHealth = RandoMath.TrimValue(memberHealth, ModOptions.instance.startingGangMemberHealth, ModOptions.instance.maxGangMemberHealth);
			memberArmor = RandoMath.TrimValue(memberArmor, 0, ModOptions.instance.maxGangMemberArmor);
			memberAccuracyLevel = RandoMath.TrimValue(memberAccuracyLevel, 0, ModOptions.instance.maxGangMemberAccuracy);
			baseTurfValue = RandoMath.TrimValue(baseTurfValue, 0, ModOptions.instance.maxTurfValue);
			GangManager.instance.SaveGangData(false);
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

        /// <summary>
        /// when a gang fights against another, this value is used to influence the outcome.
        /// it varies a little, to give that extra chance to weaker gangs
        /// </summary>
        /// <returns></returns>
        public int GetGangVariedStrengthValue()
        {
            int weaponValue = 200;
            if(gangWeaponHashes.Count > 0)
            {
                ModOptions.BuyableWeapon randomWeap = ModOptions.instance.GetBuyableWeaponByHash(RandoMath.GetRandomElementFromList(gangWeaponHashes));
                if(randomWeap != null)
                {
                    weaponValue = randomWeap.price;
                }
            }
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                weaponValue / 200 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

        /// <summary>
        /// this value doesn't have random variations. we use the gang's number of territories
        ///  and upgrades to define this. a high value is around 2000, 3000
        /// </summary>
        /// <returns></returns>
        public int GetFixedStrengthValue()
        {
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

        /// <summary>
        /// uses the number of territories and the gang's strength
        /// </summary>
        /// <returns></returns>
        public int GetReinforcementsValue()
        {
            return ZoneManager.instance.GetZonesControlledByGang(name).Count * 50 +
                baseTurfValue * 500;
        }

        public int CompareGunsByPrice(WeaponHash x, WeaponHash y)
        {
            ModOptions.BuyableWeapon buyableX = ModOptions.instance.GetBuyableWeaponByHash(x),
                buyableY = buyableX = ModOptions.instance.GetBuyableWeaponByHash(y);
            if (buyableX == null)
            {
                if (buyableY == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (buyableY == null)
                {
                    return 1;
                }
                else
                {

                    return buyableY.price.CompareTo(buyableX.price);
                }
            }
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
