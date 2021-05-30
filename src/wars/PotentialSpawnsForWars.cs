using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GTA.GangAndTurfMod
{
    public static class PotentialSpawnsForWars
    {
        public const float MIN_DIST_BETWEEN_POINTS = 10.0f;

        private static readonly List<Blip> spawnBlips = new List<Blip>();
        private static List<Vector3> _positionsList;

        public static bool showingBlips = false;

        public const int MAX_DISPLAYED_BLIPS = 100;

        public static List<Vector3> PositionsList
        {
            get
            {
                if (_positionsList == null)
                {
                    _positionsList = PersistenceHandler.LoadFromFile<List<Vector3>>("PotentialSpawnsForWars");

                    //if we still don't have a pool, create one!
                    if (_positionsList == null)
                    {
                        _positionsList = new List<Vector3>();
                    }
                }

                return _positionsList;
            }

        }

        public static bool HasNearbyEntry(Vector3 potentialNewEntry)
        {
            return GetFirstPotentialSpawnInRadiusFromPos(potentialNewEntry, MIN_DIST_BETWEEN_POINTS) != Vector3.Zero;
        }


        public static Vector3 GetFirstPotentialSpawnInRadiusFromPos(Vector3 refPos, float radius)
        {
            for (int i = 0; i < PositionsList.Count; i++)
            {
                if (refPos.DistanceTo(PositionsList[i]) < radius)
                {
                    return PositionsList[i];
                }
            }

            return Vector3.Zero;
        }

        public static List<Vector3> GetAllPotentialSpawnsInRadiusFromPos(Vector3 pos, float radius)
        {
            List<Vector3> returnedSpawns = new List<Vector3>();
            for (int i = 0; i < PositionsList.Count; i++)
            {
                if (pos.DistanceTo2D(PositionsList[i]) < radius)
                {
                    returnedSpawns.Add(PositionsList[i]);
                }
            }

            return returnedSpawns;
        }

        public static void ToggleBlips(bool active)
        {
            if (!active)
            {
                foreach (Blip blip in spawnBlips)
                {
                    blip.Remove();
                }

                spawnBlips.Clear();
            }

            showingBlips = active;
        }

        public static void UpdateBlipDisplay(Vector3 playerPos)
        {
            //closest zones should be the first ones in the list
            PositionsList.Sort((posX, posY) => playerPos.DistanceTo2D(posX).CompareTo(playerPos.DistanceTo(posY)));

            foreach (Blip blip in spawnBlips)
            {
                blip.Remove();
            }

            spawnBlips.Clear();


            for (int i = 0; i < RandoMath.Min(MAX_DISPLAYED_BLIPS, PositionsList.Count); i++)
            {
                AddBlipForPosition(PositionsList[i]);
            }
        }

        private static void AddBlipForPosition(Vector3 position)
        {
            Blip newBlip = World.CreateBlip(position);
            newBlip.Name = "Gang war potential spawn point";
            newBlip.IsShortRange = true;
            spawnBlips.Add(newBlip);
        }

        public static bool AddPositionAndSave(Vector3 position)
        {
            //check if there isn't an almost identical entry in the pool
            if (!HasNearbyEntry(position))
            {
                PositionsList.Add(position);
                if (showingBlips)
                {
                    AddBlipForPosition(position);
                }
                PersistenceHandler.SaveToFile(PositionsList, "PotentialSpawnsForWars");
                return true;
            }

            return false;
        }

        /// <summary>
        /// removes the first position found less than MIN_DIST_BETWEEN_POINTS from the provided position. Returns true if a position was removed.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool RemovePositionAndSave(Vector3 position)
        {
            //find an identical or similar entry and remove it
            Vector3 nearestPos = GetFirstPotentialSpawnInRadiusFromPos(position, MIN_DIST_BETWEEN_POINTS);
            if (nearestPos != Vector3.Zero)
            {
                PositionsList.Remove(nearestPos);

                for(int i = 0; i < spawnBlips.Count; i++)
                {
                    if(spawnBlips[i].Position == nearestPos)
                    {
                        spawnBlips[i].Remove();
                        spawnBlips.RemoveAt(i);
                        break;
                    }
                }

                PersistenceHandler.SaveToFile(PositionsList, "PotentialSpawnsForWars");
                return true;
            }

            return false;
        }

    }
}

