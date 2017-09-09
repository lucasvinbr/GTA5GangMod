using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// just a few useful methods for getting random stuff
    /// and some math stuff as well
    /// </summary>
    class RandoMath
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
        /// returns a random float between 0 and 360
        /// </summary>
        /// <returns></returns>
        public static float RandomHeading()
        {
            return ((float) CachedRandom.NextDouble()) * 360.0f;
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

        public static T GetRandomElementFromArray<T>(T[] theArray)
        {
            return theArray[CachedRandom.Next(theArray.Length)];
        }

        /// <summary>
        /// returns the absolute value (without sign)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Abs(int value)
        {
            if (value >= 0) return value;
            else
            {
                return value * -1;
            }
        }

        /// <summary>
        /// returns the lesser of two values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float Min(float x, float y)
        {
            if (x <= y) return x;
            else return y;
        }

        /// <summary>
        /// returns the lesser of two values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int Min(int x, int y)
        {
            if (x <= y) return x;
            else return y;
        }

        /// <summary>
        /// returns the greater of two values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int Max(int x, int y)
        {
            if (x >= y) return x;
            else return y;
        }

        /// <summary>
        /// returns the greater of two values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float Max(float x, float y)
        {
            if (x >= y) return x;
            else return y;
        }
    }
}
