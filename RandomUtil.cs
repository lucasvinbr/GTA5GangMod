using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA
{
    /// <summary>
    /// just a few useful methods for getting random stuff
    /// </summary>
    class RandomUtil
    {

        public static Random CachedRandom
        {
            get
            {
                if(random == null)
                {
                    random = new Random();
                }
                return random;
            }
        }

        static Random random;

        /// <summary>
        /// returns a random direction with z = 0 or not
        /// </summary>
        /// <returns></returns>
        public static Math.Vector3 RandomDirection(bool zeroZ)
        {
            Math.Vector3 theDirection = Math.Vector3.Zero;
            if (zeroZ)
            {
                theDirection = Math.Vector3.RandomXY();
            }
            else
            {
                theDirection = Math.Vector3.RandomXYZ();
            }

            theDirection.Normalize();
            return theDirection;
        }

        /// <summary>
        /// just a little function for a 50% chance for true or false
        /// </summary>
        /// <returns></returns>
        public static bool RandomBool()
        {
            return CachedRandom.Next(0, 2) == 0;
        }

        public static T GetRandomElementFromList<T>(List<T> theList)
        {
            return theList[CachedRandom.Next(theList.Count)];
        }
    }
}
