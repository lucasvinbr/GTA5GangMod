using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA.GangAndTurfMod
{
    [System.Serializable]
    public class PotentialGangVehicle
    {
        public int modelHash;

        public static PotentialCarPool CarPool
        {
            get
            {
                if (carPool == null)
                {
                    carPool = PersistenceHandler.LoadFromFile<PotentialCarPool>("VehiclePool");

                    //if we still don't have a pool, create one!
                    if (carPool == null)
                    {
                        carPool = new PotentialCarPool();
                    }
                }

                return carPool;
            }

        }

        private static PotentialCarPool carPool;



        public PotentialGangVehicle(int modelHash)
        {
            this.modelHash = modelHash;
        }

        public PotentialGangVehicle()
        {
            this.modelHash = -1;
        }

        public static bool AddVehicleAndSavePool(PotentialGangVehicle newCar)
        {
            //check if there isn't an identical entry in the pool
            if (!CarPool.HasIdenticalEntry(newCar))
            {
                CarPool.carList.Add(newCar);
                PersistenceHandler.SaveToFile<PotentialCarPool>(CarPool, "VehiclePool");
                return true;
            }

            return false;
        }

        public static PotentialGangVehicle GetMemberFromPool()
        {
            PotentialGangVehicle returnedVehicle;

            if (CarPool.carList.Count <= 0)
            {
                UI.Notify("GTA5GangNTurfMod Warning: empty/bad carpool file! Enemy gangs won't have cars");
                return null;
            }

            returnedVehicle = CarPool.carList[RandomUtil.CachedRandom.Next(CarPool.carList.Count)];

            return returnedVehicle;
        }
    }

    [System.Serializable]
    public class PotentialCarPool
    {
        public List<PotentialGangVehicle> carList;

        public PotentialCarPool()
        {
            carList = new List<PotentialGangVehicle>();
        }

        public bool HasIdenticalEntry(PotentialGangVehicle potentialEntry)
        {

            for (int i = 0; i < carList.Count; i++)
            {
                if (carList[i].modelHash == potentialEntry.modelHash)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
