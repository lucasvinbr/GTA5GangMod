using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA
{
    /// <summary>
    /// a potential gang member is a ped with preset model variations and a color to which he is linked.
    /// that way, when an AI gang is picking a color, it will pick members with a similar color
    /// </summary>
    [System.Serializable]
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
        public int torsoDrawableIndex, torsoTextureIndex;
        public int legsDrawableIndex, legsTextureIndex;
        public dressStyle myStyle;
        public memberColor linkedColor;
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
            int torsoDrawableIndex = -1,int torsoTextureIndex = -1, int legsDrawableIndex = -1, int legsTextureIndex = -1)
        {
            this.modelHash = modelHash;
            this.myStyle = myStyle;
            this.linkedColor = linkedColor;
            this.torsoDrawableIndex = torsoDrawableIndex;
            this.torsoTextureIndex = torsoTextureIndex;
            this.legsDrawableIndex = legsDrawableIndex;
            this.legsTextureIndex = legsTextureIndex;
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
        }

        public static bool AddMemberAndSavePool(PotentialGangMember newMember)
        {
            //check if there isn't an identical entry in the pool
            if (!MemberPool.HasIdenticalEntry(newMember))
            {
                MemberPool.memberList.Add(newMember);
                PersistenceHandler.SaveToFile<PotentialMemberPool>(MemberPool, "MemberPool");
                return true;
            }

            return false;
        }

        [System.Serializable]
        public class PotentialMemberPool
        {
            public List<PotentialGangMember> memberList;

            public PotentialMemberPool()
            {
                memberList = new List<PotentialGangMember>();
            }

            public bool HasIdenticalEntry(PotentialGangMember potentialEntry)
            {
                
                for(int i = 0; i < memberList.Count; i++)
                {
                    if (memberList[i].modelHash == potentialEntry.modelHash &&
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
        }
    }
}

