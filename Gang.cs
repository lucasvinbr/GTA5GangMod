using System;
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
        public VehicleColor color;
        public int moneyAvailable;
        public bool isPlayerOwned = false;

        //the gang's relationshipgroup
        //it's not really saved, so it is set up differently on every run (and reloading of scripts)
        public int relationGroupIndex;

        //car stats - the model
        public int gangVehicleHash = -1;
        
        public int memberAccuracyLevel = 20;
        public int memberHealth = 120;
        public int memberArmor = 0;

        //acceptable ped model/texture/component combinations
        public List<PotentialGangMember> memberVariations = new List<PotentialGangMember>();

        public List<WeaponHash> gangWeaponHashes = new List<WeaponHash>();
        //the weapons the AI Gang will probably buy if they have enough cash
        public List<WeaponHash> preferredWeaponHashes = new List<WeaponHash>();

        public Gang(string name, VehicleColor color, bool isPlayerOwned)
        {
            this.name = name;
            this.color = color;
            this.isPlayerOwned = isPlayerOwned;

            moneyAvailable = RandomUtil.CachedRandom.Next(60000, 100000); //this isnt used if this is the player's gang - he'll use his own money instead

            gangWeaponHashes.Add(WeaponHash.SNSPistol);

        }

        public Gang()
        {
            
        }

        public void SetPreferredWeapons()
        {
            for(int i = 0; i < RandomUtil.CachedRandom.Next(2, 5); i++)
            {
                preferredWeaponHashes.Add(RandomUtil.GetRandomElementFromList(ModOptions.instance.buyableWeapons).wepHash);
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
                if (memberVariations[i].modelHash == sadMember.modelHash &&
                       memberVariations[i].legsDrawableIndex == sadMember.legsDrawableIndex &&
                       memberVariations[i].legsTextureIndex == sadMember.legsTextureIndex &&
                       memberVariations[i].torsoDrawableIndex == sadMember.torsoDrawableIndex &&
                       memberVariations[i].torsoTextureIndex == sadMember.torsoTextureIndex)
                {
                    memberVariations.Remove(memberVariations[i]);
                    GangManager.instance.SaveGangData();
                    return true;
                }
            }

            return false;
        }

        public bool SetGangCar(Vehicle newVehicleType)
        {
            if (newVehicleType.Model.Hash != gangVehicleHash)
            {
                gangVehicleHash = newVehicleType.Model.Hash;
                GangManager.instance.SaveGangData();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// when an AI gang fights against another, this value is used to influence the outcome
        /// </summary>
        /// <returns></returns>
        public int GetGangAIStrengthValue()
        {
            return moneyAvailable / 10000 +
                ZoneManager.instance.GetZonesControlledByGang(name).Length * 50 +
                ModOptions.instance.GetBuyableWeaponByHash(RandomUtil.GetRandomElementFromList(gangWeaponHashes)).price / 20 +
                memberAccuracyLevel * 10 +
                memberArmor +
                memberHealth;
        }

    }
}
