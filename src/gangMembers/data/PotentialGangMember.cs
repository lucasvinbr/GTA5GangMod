using GTA.Native;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// a potential gang member is a ped with preset model variations and a color to which he is linked.
    /// that way, when an AI gang is picking a color, it will pick members with a similar color
    /// </summary>
    [XmlInclude(typeof(FreemodePotentialGangMember))]
    [XmlInclude(typeof(ExtendedPotentialGangMember))]
    public class PotentialGangMember
    {
        public enum DressStyle
        {
            business,
            street,
            beach,
            special
        }

        public enum MemberColor
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

        //we save head, hair, torso and leg data (we don't save hair texture here, we do only in the extended save mode).
        //head id is believed to be 0, hair is 2, torso 3 and legs 4
        //if data is saved with a value of -1, it wasn't considered important (by the person who saved) to the linked color

        public int modelHash;
        public int hairDrawableIndex;
        public int headDrawableIndex, headTextureIndex;
        public int torsoDrawableIndex, torsoTextureIndex;
        public int legsDrawableIndex, legsTextureIndex;
        /// <summary>
        /// a "version number" of the registration.
        /// this exists to make sure we don't break old registrations and adapt some checks to fit them.
        /// this shouldn't affect registrations' "is equal" checks
        /// </summary>
        public int regVersion;
        public DressStyle myStyle;
        public MemberColor linkedColor;

        [XmlIgnore]
        public static PotentialMemberPool MemberPool
        {
            get
            {
                if (memberPool == null)
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

        public PotentialGangMember(int modelHash, DressStyle myStyle, MemberColor linkedColor,
             int headDrawableIndex = -1, int headTextureIndex = -1, int hairDrawableIndex = -1,
            int torsoDrawableIndex = -1, int torsoTextureIndex = -1, int legsDrawableIndex = -1, int legsTextureIndex = -1)
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
            this.regVersion = 1;
        }

        /// <summary>
        /// almost an equality check in all fields, but if one of them is -1 in our data, it's also counted as similar
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSimilarTo(PotentialGangMember other)
        {

            if (modelHash == other.modelHash &&
                        (hairDrawableIndex == -1 || hairDrawableIndex == other.hairDrawableIndex) &&
                        (headDrawableIndex == -1 || headDrawableIndex == other.headDrawableIndex) &&
                        (headTextureIndex == -1 || headTextureIndex == other.headTextureIndex) &&
                       (legsDrawableIndex == -1 || legsDrawableIndex == other.legsDrawableIndex) &&
                        (legsTextureIndex == -1 || legsTextureIndex == other.legsTextureIndex) &&
                       (torsoDrawableIndex == -1 || torsoDrawableIndex == other.torsoDrawableIndex) &&
                        (torsoTextureIndex == -1 || torsoTextureIndex == other.torsoTextureIndex))
            {
                return true;
            }

            return false;
        }

        public PotentialGangMember(Ped sourcePed, DressStyle myStyle, MemberColor linkedColor)
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
            this.myStyle = DressStyle.special;
            this.linkedColor = MemberColor.white;
            this.torsoDrawableIndex = -1;
            this.torsoTextureIndex = -1;
            this.legsDrawableIndex = -1;
            this.legsTextureIndex = -1;
            this.hairDrawableIndex = -1;
            this.headDrawableIndex = -1;
            this.headTextureIndex = -1;
            this.regVersion = 0;
        }

        /// <summary>
        /// uses the data stored in this potential gang member to alter the appearance of the target ped
        /// </summary>
        /// <param name="targetPed"></param>
        public virtual void SetPedAppearance(Ped targetPed)
        {

            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 1);

            // old version (no regVersion, regVersion 0):
            // check if hairDrawableIndex is set, to make sure this entry is recent enough
            // and/or intentionally edits head/hair data
            if(regVersion < 1)
            {
                if (hairDrawableIndex != -1)
                {
                    int randomHairTex = RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                        targetPed, 2, hairDrawableIndex));
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 0, headDrawableIndex, headTextureIndex, pedPalette);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 2, hairDrawableIndex, randomHairTex, pedPalette);
                }
            }
            else
            {
                if(headDrawableIndex != -1)
                {
                    int headTexIndex = headTextureIndex != -1 ?
                        headTextureIndex :
                        RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                            targetPed, 0, headTextureIndex));
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 0, headDrawableIndex, headTexIndex, pedPalette);
                }

                if(hairDrawableIndex != -1)
                {
                    int randomHairTex = RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                        targetPed, 2, hairDrawableIndex));
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 2, hairDrawableIndex, randomHairTex, pedPalette);
                }
            }
            

            if (torsoDrawableIndex != -1)
            {
                int torsoTexIndex = torsoTextureIndex != -1 ?
                    torsoTextureIndex :
                    RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                        targetPed, 3, torsoDrawableIndex));
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 3, torsoDrawableIndex, torsoTexIndex, pedPalette);
            }

            if (legsDrawableIndex != -1)
            {
                int legsTexIndex = legsTextureIndex != -1 ?
                    legsTextureIndex :
                    RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                        targetPed, 4, legsDrawableIndex));
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 4, legsDrawableIndex, legsTexIndex, pedPalette);
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

        public static PotentialGangMember GetMemberFromPool(DressStyle style, MemberColor color)
        {
            PotentialGangMember returnedMember;

            if (MemberPool.memberList.Count <= 0)
            {
                UI.Notification.Show(Localization.GetTextByKey("notify_warn_bad_memberpool_file", "GTA5GangNTurfMod Warning: empty/bad memberpool file! Enemy gangs won't spawn"));
                return null;
            }

            int attempts = 0;
            do
            {
                returnedMember = MemberPool.memberList[RandoMath.CachedRandom.Next(MemberPool.memberList.Count)];
                attempts++;
            } while ((returnedMember.linkedColor != color || returnedMember.myStyle != style) && attempts < 1000);

            if (returnedMember.linkedColor != color || returnedMember.myStyle != style)
            {
                //we couldnt find one randomly.
                //lets try to find one the straightforward way then
                for (int i = 0; i < MemberPool.memberList.Count; i++)
                {
                    returnedMember = MemberPool.memberList[i];
                    if (returnedMember.linkedColor == color && returnedMember.myStyle == style)
                    {
                        return returnedMember;
                    }
                }
                
                UI.Notification.Show(string.Format(Localization.GetTextByKey("notify_failed_find_pot_member_style_x_color_y", "failed to find a potential member of style {0} and color {1}"),
                    style.ToString(),
                    color.ToString()));
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
                if (potentialEntry.GetType() == typeof(FreemodePotentialGangMember))
                {
                    return FreemodePotentialGangMember.FreemodeSimilarEntryCheck(potentialEntry as FreemodePotentialGangMember) != null;
                }
                else if (potentialEntry.GetType() == typeof(ExtendedPotentialGangMember))
                {
                    return ExtendedPotentialGangMember.ExtendedSimilarEntryCheck(potentialEntry as ExtendedPotentialGangMember) != null;
                }


                for (int i = 0; i < memberList.Count; i++)
                {
                    if (potentialEntry.IsSimilarTo(memberList[i]))
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
                else if (potentialEntry.GetType() == typeof(ExtendedPotentialGangMember))
                {
                    return ExtendedPotentialGangMember.ExtendedSimilarEntryCheck(potentialEntry as ExtendedPotentialGangMember);
                }

                for (int i = 0; i < memberList.Count; i++)
                {
                    if (potentialEntry.IsSimilarTo(memberList[i]))
                    {
                        return memberList[i];
                    }
                }
                return null;
            }

        }
    }
}

