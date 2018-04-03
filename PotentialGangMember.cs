using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// a potential gang member is a ped with preset model variations and a color to which he is linked.
    /// that way, when an AI gang is picking a color, it will pick members with a similar color
    /// </summary>
    [XmlInclude(typeof(FreemodePotentialGangMember))]
    public class PotentialGangMember
    {
       public enum dressStyle
        {
            business,
            street,
            beach,
            special
        }

        public enum memberColor
        {
            white,
            black,
            red,
            green,
            blue,
            yellow,
            gray,
            pink,
            purple
        }

        //we just save the torso and leg data.
        //torso component id is believed to be 3 and legs 4
        //if data is saved with a value of -1, it wasn't considered important (by the person who saved) to the linked color

        public int modelHash;
        public int hairDrawableIndex;
        public int headDrawableIndex, headTextureIndex;
        public int torsoDrawableIndex, torsoTextureIndex;
        public int legsDrawableIndex, legsTextureIndex;
        public dressStyle myStyle;
        public memberColor linkedColor;

        [XmlIgnore]
        public static PotentialMemberPool MemberPool {
            get
            {
                if(memberPool == null)
                {
                    memberPool = PersistenceHandler.LoadFromFile<PotentialMemberPool>("MemberPool");

                    //if we still don't have a pool, create one!
                    if (memberPool == null)
                    {
                        memberPool = new PotentialMemberPool();
                    }
                }
               
                return memberPool;
            }

        }

        private static PotentialMemberPool memberPool;

        public PotentialGangMember(int modelHash, dressStyle myStyle, memberColor linkedColor,
             int headDrawableIndex = -1, int headTextureIndex = -1, int hairDrawableIndex = -1,
            int torsoDrawableIndex = -1,int torsoTextureIndex = -1, int legsDrawableIndex = -1, int legsTextureIndex = -1)
        {
            this.modelHash = modelHash;
            this.myStyle = myStyle;
            this.linkedColor = linkedColor;
            this.hairDrawableIndex = hairDrawableIndex;
            this.headDrawableIndex = headDrawableIndex;
            this.headTextureIndex = headTextureIndex;
            this.torsoDrawableIndex = torsoDrawableIndex;
            this.torsoTextureIndex = torsoTextureIndex;
            this.legsDrawableIndex = legsDrawableIndex;
            this.legsTextureIndex = legsTextureIndex;
        }

        public PotentialGangMember(Ped sourcePed, dressStyle myStyle, memberColor linkedColor)
        {
            this.myStyle = myStyle;
            this.linkedColor = linkedColor;

            modelHash = sourcePed.Model.Hash;

            headDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, sourcePed, 0);
            headTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, sourcePed, 0);

            hairDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, sourcePed, 2);

            torsoDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, sourcePed, 3);
            torsoTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, sourcePed, 3);

            legsDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, sourcePed, 4);
            legsTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, sourcePed, 4);
        }

        public PotentialGangMember()
        {
            this.modelHash = -1;
            this.myStyle = dressStyle.special;
            this.linkedColor = memberColor.white;
            this.torsoDrawableIndex = -1;
            this.torsoTextureIndex = -1;
            this.legsDrawableIndex = -1;
            this.legsTextureIndex = -1;
            this.hairDrawableIndex = -1;
            this.headDrawableIndex = -1;
            this.headTextureIndex = -1;
        }

        /// <summary>
        /// uses the data stored in this potential gang member to alter the appearance of the target ped
        /// </summary>
        /// <param name="targetPed"></param>
        public virtual void SetPedAppearance(Ped targetPed)
        {
            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 1);
            //if we're not a legacy registration, set the head and hair data too
            if (hairDrawableIndex != -1)
            {
                int randomHairTex = RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                    targetPed, 2, hairDrawableIndex));
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 0, headDrawableIndex, headTextureIndex, pedPalette);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 2, hairDrawableIndex, randomHairTex, pedPalette);
            }

            if(torsoDrawableIndex != -1 && torsoTextureIndex != -1)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 3, torsoDrawableIndex, torsoTextureIndex, pedPalette);
            }
            
            if(legsDrawableIndex != -1 && legsTextureIndex != -1)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 4, legsDrawableIndex, legsTextureIndex, pedPalette);
            }
            
        }

        public static bool AddMemberAndSavePool(PotentialGangMember newMember)
        {
            //check if there isn't an identical entry in the pool
            if (!MemberPool.HasIdenticalEntry(newMember))
            {
                MemberPool.memberList.Add(newMember);
                PersistenceHandler.SaveToFile(MemberPool, "MemberPool");
                return true;
            }

            return false;
        }

        public static bool RemoveMemberAndSavePool(PotentialGangMember newMember)
        {
            //find an identical or similar entry and remove it
            PotentialGangMember similarEntry = MemberPool.GetSimilarEntry(newMember);
            if (similarEntry != null)
            {
                MemberPool.memberList.Remove(similarEntry);
                PersistenceHandler.SaveToFile(MemberPool, "MemberPool");
                return true;
            }

            return false;
        }

        public static PotentialGangMember GetMemberFromPool(dressStyle style, memberColor color)
        {
            PotentialGangMember returnedMember;

            if(MemberPool.memberList.Count <= 0)
            {
                UI.Notify("GTA5GangNTurfMod Warning: empty/bad memberpool file! Enemy gangs won't spawn");
                return null;
            }

            int attempts = 0;
            do
            {
                returnedMember = MemberPool.memberList[RandoMath.CachedRandom.Next(MemberPool.memberList.Count)];
                attempts++;
            } while ((returnedMember.linkedColor != color || returnedMember.myStyle != style) && attempts < 1000);

            if(returnedMember.linkedColor != color || returnedMember.myStyle != style)
            {
                //we couldnt find one randomly.
                //lets try to find one the straightforward way then
                for(int i = 0; i < MemberPool.memberList.Count; i++)
                {
                    returnedMember = MemberPool.memberList[i];
                    if (returnedMember.linkedColor == color && returnedMember.myStyle == style)
                    {
                        return returnedMember;
                    }
                }

                UI.Notify("failed to find a potential member of style " + style.ToString() + " and color " + color.ToString());
            }

            return returnedMember;
        }

        public class PotentialMemberPool
        {
            public List<PotentialGangMember> memberList;

            public PotentialMemberPool()
            {
                memberList = new List<PotentialGangMember>();
            }

            public bool HasIdenticalEntry(PotentialGangMember potentialEntry)
            {
                if(potentialEntry.GetType() == typeof(FreemodePotentialGangMember))
                {
                    return FreemodePotentialGangMember.FreemodeSimilarEntryCheck(potentialEntry as FreemodePotentialGangMember) != null;
                }
                

                for(int i = 0; i < memberList.Count; i++)
                {
                    if (memberList[i].modelHash == potentialEntry.modelHash &&
                        memberList[i].hairDrawableIndex == potentialEntry.hairDrawableIndex &&
                        memberList[i].headDrawableIndex == potentialEntry.headDrawableIndex &&
                        memberList[i].headTextureIndex == potentialEntry.headTextureIndex &&
                        memberList[i].legsDrawableIndex == potentialEntry.legsDrawableIndex &&
                        memberList[i].legsTextureIndex == potentialEntry.legsTextureIndex &&
                        memberList[i].torsoDrawableIndex == potentialEntry.torsoDrawableIndex &&
                        memberList[i].torsoTextureIndex == potentialEntry.torsoTextureIndex)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// gets a similar entry to the member provided.
            /// it may not be the only similar one, however
            /// </summary>
            /// <param name="potentialEntry"></param>
            /// <returns></returns>
            public PotentialGangMember GetSimilarEntry(PotentialGangMember potentialEntry)
            {
                if (potentialEntry.GetType() == typeof(FreemodePotentialGangMember))
                {
                    return FreemodePotentialGangMember.FreemodeSimilarEntryCheck(potentialEntry as FreemodePotentialGangMember);
                }

                for (int i = 0; i < memberList.Count; i++)
                {
                    if (memberList[i].modelHash == potentialEntry.modelHash &&
                        (memberList[i].hairDrawableIndex == -1 || memberList[i].hairDrawableIndex == potentialEntry.hairDrawableIndex) &&
                        (memberList[i].headDrawableIndex == -1 || memberList[i].headDrawableIndex == potentialEntry.headDrawableIndex) &&
                        (memberList[i].headTextureIndex == -1 || memberList[i].headTextureIndex == potentialEntry.headTextureIndex) &&
                       (memberList[i].legsDrawableIndex == -1 || memberList[i].legsDrawableIndex == potentialEntry.legsDrawableIndex) &&
                        (memberList[i].legsTextureIndex == -1 || memberList[i].legsTextureIndex == potentialEntry.legsTextureIndex) &&
                       (memberList[i].torsoDrawableIndex == -1 || memberList[i].torsoDrawableIndex == potentialEntry.torsoDrawableIndex) &&
                        (memberList[i].torsoTextureIndex == -1 || memberList[i].torsoTextureIndex == potentialEntry.torsoTextureIndex))
                    {
                        return memberList[i];
                    }
                }
                return null;
            }
        }
    }
}

