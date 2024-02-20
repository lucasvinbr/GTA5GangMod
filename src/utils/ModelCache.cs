

using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script contains methods used to load and unload models used by the mod
    /// </summary>
    public static class ModelCache
    {
        public static readonly List<Model> cachedVehicleModels = new List<Model>();
        public static readonly List<Model> cachedPedModels = new List<Model>();

        public static Model GetPedModel(int modelHash)
        {
            Model foundModel = cachedPedModels.Find(pm => pm.Hash == modelHash);
            if(foundModel.Hash == modelHash)
            {
                return foundModel;
            }
            else
            {
                foundModel = new Model(modelHash);
                foundModel.Request();
                if (foundModel.IsLoaded)
                {
                    cachedPedModels.Add(foundModel);
                    return foundModel;
                }
            }

            return null;
        }

        /// <summary>
        /// if model was in the cache, mark it as no longer needed and remove it from the cache list
        /// </summary>
        /// <param name="modelHash"></param>
        public static void RemovePedModelFromCache(int modelHash)
        {
            Model foundModel = cachedPedModels.Find(pm => pm.Hash == modelHash);

            if(foundModel.Hash == modelHash)
            {
                foundModel.MarkAsNoLongerNeeded();
                cachedPedModels.Remove(foundModel);
            }
        }

        /// <summary>
        /// get model from cache, or load it, and then add it to cache if it was successfully loaded
        /// </summary>
        /// <param name="modelHash"></param>
        /// <returns></returns>
        public static Model GetVehicleModel(int modelHash)
        {
            Model foundModel = cachedVehicleModels.Find(vm => vm.Hash == modelHash);
            if (foundModel.Hash == modelHash)
            {
                return foundModel;
            }
            else
            {
                foundModel = new Model(modelHash);
                foundModel.Request();
                if (foundModel.IsLoaded)
                {
                    cachedVehicleModels.Add(foundModel);
                    return foundModel;
                }
            }

            return null;
        }

        /// <summary>
        /// if model was in the cache, mark it as no longer needed and remove it from the cache list
        /// </summary>
        /// <param name="modelHash"></param>
        public static void RemoveVehicleModelFromCache(int modelHash)
        {
            Model foundModel = cachedVehicleModels.Find(vm => vm.Hash == modelHash);

            if (foundModel.Hash == modelHash)
            {
                foundModel.MarkAsNoLongerNeeded();
                cachedVehicleModels.Remove(foundModel);
            }
        }

        public static void UnloadAllModels()
        {
            foreach(var vm in cachedVehicleModels)
            {
                vm.MarkAsNoLongerNeeded();
            }
            cachedVehicleModels.Clear();

            foreach(var pm in cachedPedModels)
            {
                pm.MarkAsNoLongerNeeded();
            }
            cachedPedModels.Clear();

        }
    }

}
