using GTA.Native;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// basically a potential gang member with more data to be saved, but not as much as the freemode member
    /// </summary>
    public class ExtendedPotentialGangMember : PotentialGangMember
    {

        /// <summary>
        /// more drawable data, unused by potential gang members, but should probably be used here
        /// (indexes used are 1 and 5-11)
        /// </summary>
        public int[] extraDrawableIndexes;

        public int[] extraTextureIndexes;

        public int hairTextureIndex;

        public ExtendedPotentialGangMember()
        {
            extraDrawableIndexes = new int[8];
            extraTextureIndexes = new int[8];
            modelHash = -1;
            myStyle = DressStyle.special;
            linkedColor = MemberColor.white;
            torsoDrawableIndex = -1;
            torsoTextureIndex = -1;
            legsDrawableIndex = -1;
            legsTextureIndex = -1;
            hairDrawableIndex = -1;
            hairTextureIndex = -1;
            headDrawableIndex = -1;
            headTextureIndex = -1;
        }

        public ExtendedPotentialGangMember(Ped targetPed, DressStyle myStyle, MemberColor linkedColor) : base(targetPed, myStyle, linkedColor)
        {
            extraDrawableIndexes = new int[8];
            extraTextureIndexes = new int[8];
            
            hairTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, 2);

            //we've already got the model hash, torso indexes and stuff.
            //time to get the new data
            for (int i = 0; i < 11; i++)
            {
                //extra drawable indexes
                if (i == 1)
                {
                    extraDrawableIndexes[0] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    extraTextureIndexes[0] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }

                //indexes from 5 to 11
                if (i > 4)
                {
                    extraDrawableIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, targetPed, i);
                    extraTextureIndexes[i - 4] = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, targetPed, i);
                }
            }

        }

        public override void SetPedAppearance(Ped targetPed)
        {
            int pedPalette = Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, targetPed, 1);
            
            if(headDrawableIndex != -1)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 0, headDrawableIndex, headTextureIndex, pedPalette);
            }
            
            if(hairDrawableIndex != -1)
            {
                int hairTexIndex = hairTextureIndex != -1 ?
                    hairTextureIndex :
                    RandoMath.CachedRandom.Next(Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,
                        targetPed, 2, hairDrawableIndex));

                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, 2, hairDrawableIndex, hairTexIndex, pedPalette);
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

            //new data time!
            for (int i = 0; i < 11; i++)
            {

                //extra drawable indexes
                if (i == 1)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, extraDrawableIndexes[0], extraTextureIndexes[0], pedPalette);
                }

                //indexes from 5 to 11
                if (i > 4 && i < 12)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, targetPed, i, extraDrawableIndexes[i - 4], extraTextureIndexes[i - 4], pedPalette);
                }
            }

        }

        public static ExtendedPotentialGangMember ExtendedSimilarEntryCheck(ExtendedPotentialGangMember potentialEntry)
        {
            for (int i = 0; i < MemberPool.memberList.Count; i++)
            {
                if (MemberPool.memberList[i].GetType() == typeof(ExtendedPotentialGangMember))
                {
                    ExtendedPotentialGangMember extendedEntry = MemberPool.memberList[i] as ExtendedPotentialGangMember;

                    if (extendedEntry.modelHash == potentialEntry.modelHash &&
                    extendedEntry.hairDrawableIndex == potentialEntry.hairDrawableIndex &&
                    extendedEntry.headDrawableIndex == potentialEntry.headDrawableIndex &&
                    extendedEntry.headTextureIndex == potentialEntry.headTextureIndex &&
                    extendedEntry.legsDrawableIndex == potentialEntry.legsDrawableIndex &&
                    extendedEntry.legsTextureIndex == potentialEntry.legsTextureIndex &&
                    extendedEntry.torsoDrawableIndex == potentialEntry.torsoDrawableIndex &&
                    extendedEntry.torsoTextureIndex == potentialEntry.torsoTextureIndex &&
                    extendedEntry.hairTextureIndex == potentialEntry.hairTextureIndex &&
                    RandoMath.AreIntArrayContentsTheSame(extendedEntry.extraDrawableIndexes, potentialEntry.extraDrawableIndexes) &&
                    RandoMath.AreIntArrayContentsTheSame(extendedEntry.extraTextureIndexes, potentialEntry.extraTextureIndexes))
                    {
                        return extendedEntry;
                    }
                }
                else continue;

            }
            return null;
        }

    }
}
