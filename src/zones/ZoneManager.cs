using GTA.Math;
using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script deals with gangs taking over zones. it also allows adding new zones to be taken
    /// </summary>
    public class ZoneManager
    {

        public enum ZoneBlipDisplay
        {
            none,
            fiveClosest,
            allZones,
        }

        public ZoneBlipDisplay curBlipDisplay = ZoneBlipDisplay.none;

        #region setup/save stuff

        public class TurfZoneData
        {
            public List<TurfZone> zoneList;

            public TurfZoneData()
            {
                zoneList = new List<TurfZone>();
            }
        }


        public static ZoneManager instance;
        public TurfZoneData zoneData;


        public ZoneManager()
        {
            instance = this;

            zoneData = PersistenceHandler.LoadFromFile<TurfZoneData>("TurfZoneData");
            if (zoneData == null)
            {
                zoneData = new TurfZoneData();
            }

        }

        public void SaveZoneData(bool notifySuccess = true)
        {
            AutoSaver.instance.zoneDataDirty = true;
            if (notifySuccess)
            {
                AutoSaver.instance.zoneDataNotifySave = true;
            }
        }

        public void UpdateZoneData(TurfZone newTurfZone)
        {
            if (!zoneData.zoneList.Contains(newTurfZone))
            {
                zoneData.zoneList.Add(newTurfZone);
            }

            newTurfZone.CreateAttachedBlip();
            newTurfZone.UpdateBlipPosition();

            RefreshZoneBlips();

            SaveZoneData(false);
        }

        #endregion

        #region legacy zone name fetching
        /// <summary>
        /// Zone naming became properly localized in shvdn3. In order to not have to rebuild turfZoneData, we're keeping this legacy fetching
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static string LegacyGetZoneName(string zoneId)
        {
            switch (zoneId.ToUpper())
            {
                case "AIRP":
                    return "Los Santos International Airport";
                case "ALAMO":
                    return "Alamo Sea";
                case "ALTA":
                    return "Alta";
                case "ARMYB":
                    return "Fort Zancudo";
                case "BANHAMC":
                    return "Banham Canyon";
                case "BANNING":
                    return "Banning";
                case "BAYTRE":
                    return "Baytree Canyon";
                case "BEACH":
                    return "Vespucci Beach";
                case "BHAMCA":
                    return "Banham Canyon";
                case "BRADP":
                    return "Braddock Pass";
                case "BRADT":
                    return "Braddock Tunnel";
                case "BURTON":
                    return "Burton";
                case "CALAFB":
                    return "Calafia Bridge";
                case "CANNY":
                    return "Raton Canyon";
                case "CCREAK":
                    return "Cassidy Creek";
                case "CHAMH":
                    return "Chamberlain Hills";
                case "CHIL":
                    return "Vinewood Hills";
                case "CHU":
                    return "Chumash";
                case "CMSW":
                    return "Chiliad Mountain State Wilderness";
                case "CYPRE":
                    return "Cypress Flats";
                case "DAVIS":
                    return "Davis";
                case "DELBE":
                    return "Del Perro Beach";
                case "DELPE":
                    return "Del Perro";
                case "DELSOL":
                    return "Puerto Del Sol";
                case "DESRT":
                    return "Grand Senora Desert";
                case "DOWNT":
                    return "Downtown";
                case "DTVINE":
                    return "Downtown Vinewood";
                case "EAST_V":
                    return "East Vinewood";
                case "EBURO":
                    return "El Burro Heights";
                case "ELGORL":
                    return "El Gordo Lighthouse";
                case "ELYSIAN":
                    return "Elysian Island";
                case "GALFISH":
                    return "Galilee";
                case "GALLI":
                    return "Galileo Park";
                case "GOLF":
                    return "GWC and Golfing Society";
                case "GRAPES":
                    return "Grapeseed";
                case "GREATC":
                    return "Great Chaparral";
                case "HARMO":
                    return "Harmony";
                case "HAWICK":
                    return "Hawick";
                case "HORS":
                    return "Vinewood Racetrack";
                case "HUMLAB":
                    return "Humane Labs and Research";
                case "ISHEISTZONE":
                    return "Island";
                case "JAIL":
                    return "Bolingbroke Penitentiary";
                case "KOREAT":
                    return "Little Seoul";
                case "LACT":
                    return "Land Act Reservoir";
                case "LAGO":
                    return "Lago Zancudo";
                case "LDAM":
                    return "Land Act Dam";
                case "LEGSQU":
                    return "Legion Square";
                case "LMESA":
                    return "La Mesa";
                case "LOSPUER":
                    return "La Puerta";
                case "MIRR":
                    return "Mirror Park";
                case "MORN":
                    return "Morningwood";
                case "MOVIE":
                    return "Richards Majestic";
                case "MTCHIL":
                    return "Mount Chiliad";
                case "MTGORDO":
                    return "Mount Gordo";
                case "MTJOSE":
                    return "Mount Josiah";
                case "MURRI":
                    return "Murrieta Heights";
                case "NCHU":
                    return "North Chumash";
                case "NOOSE":
                    return "N.O.O.S.E.";
                case "OBSERV":
                    return "Galileo Observatory";
                case "OCEANA":
                    return "Pacific Ocean";
                case "PALCOV":
                    return "Paleto Cove";
                case "PALETO":
                    return "Paleto Bay";
                case "PALFOR":
                    return "Paleto Forest";
                case "PALHIGH":
                    return "Palomino Highlands";
                case "PALMPOW":
                    return "Palmer-Taylor Power Station";
                case "PBLUFF":
                    return "Pacific Bluffs";
                case "PBOX":
                    return "Pillbox Hill";
                case "PROCOB":
                    return "Procopio Beach";
                case "PROL":
                    return "North Yankton";
                case "RANCHO":
                    return "Rancho";
                case "RGLEN":
                    return "Richman Glen";
                case "RICHM":
                    return "Richman";
                case "ROCKF":
                    return "Rockford Hills";
                case "RTRAK":
                    return "Redwood Lights Track";
                case "SANAND":
                    return "San Andreas";
                case "SANCHIA":
                    return "San Chianski Mountain Range";
                case "SANDY":
                    return "Sandy Shores";
                case "SKID":
                    return "Mission Row";
                case "SLAB":
                    return "Stab City";
                case "STAD":
                    return "Maze Bank Arena";
                case "STRAW":
                    return "Strawberry";
                case "TATAMO":
                    return "Tataviam Mountains";
                case "TERMINA":
                    return "Terminal";
                case "TEXTI":
                    return "Textile City";
                case "TONGVAH":
                    return "Tongva Hills";
                case "TONGVAV":
                    return "Tongva Valley";
                case "VCANA":
                    return "Vespucci Canals";
                case "VESP":
                    return "Vespucci";
                case "VINE":
                    return "Vinewood";
                case "WINDF":
                    return "RON Alternates Wind Farm";
                case "WVINE":
                    return "West Vinewood";
                case "ZANCUDO":
                    return "Zancudo River";
                case "ZP_ORT":
                    return "Port of South Los Santos";
                case "ZQ_UAR":
                    return "Davis Quartz";
                default:
                    return string.Empty;
            }
        }
        #endregion
        public TurfZone GetZoneInLocation(Vector3 location)
        {
            //prioritize custom zones
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].GetType() != typeof(TurfZone) &&
                    zoneData.zoneList[i].IsLocationInside(string.Empty, location))
                {
                    return zoneData.zoneList[i];
                }
            }

            //fall back to getting by zone name
            return GetZoneByName(ZoneManager.LegacyGetZoneName(World.GetZoneDisplayName(location)));
        }

        public TurfZone GetZoneInLocation(string zoneName, Vector3 location)
        {
            //prioritize custom zones
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].GetType() != typeof(TurfZone) &&
                    zoneData.zoneList[i].IsLocationInside(zoneName, location))
                {
                    return zoneData.zoneList[i];
                }
            }

            //fall back to getting by zone name
            return GetZoneByName(zoneName);
        }

        /// <summary>
        /// returns zone in location and a flag telling whether it's a custom zone or not
        /// </summary>
        /// <param name="zoneName"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public TurfZone GetZoneInLocation(string zoneName, Vector3 location, out bool isCustomZone)
        {
            //prioritize custom zones
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].GetType() != typeof(TurfZone) &&
                    zoneData.zoneList[i].IsLocationInside(zoneName, location))
                {
                    isCustomZone = true;
                    return zoneData.zoneList[i];
                }
            }

            //fall back to getting by zone name
            isCustomZone = false;
            return GetZoneByName(zoneName);
        }

        public void OutputCurrentZoneInfo()
        {
            string legacyName = LegacyGetZoneName(World.GetZoneDisplayName(MindControl.CurrentPlayerCharacter.Position));
            string zoneInfoMsg;
            TurfZone currentZone = GetZoneInLocation(legacyName, MindControl.CurrentPlayerCharacter.Position);

            if (currentZone != null)
            {
                zoneInfoMsg = "Current zone is " + currentZone.GetDisplayName() + ".";
                if (currentZone.ownerGangName != "none")
                {
                    if (GangManager.instance.GetGangByName(currentZone.ownerGangName) == null)
                    {
                        GiveGangZonesToAnother(currentZone.ownerGangName, "none");
                        currentZone.ownerGangName = "none";
                        SaveZoneData(false);
                        zoneInfoMsg += " It isn't owned by any gang.";
                    }
                    else
                    {
                        zoneInfoMsg += " It is owned by the " + currentZone.ownerGangName + ".";

                        zoneInfoMsg += " Its current level is " + currentZone.value.ToString();
                    }
                }
                else
                {
                    zoneInfoMsg += " It isn't owned by any gang.";
                }
            }
            else
            {
                zoneInfoMsg = "Current zone is " + World.GetZoneLocalizedName(MindControl.CurrentPlayerCharacter.Position) + ".  It hasn't been marked as takeable yet.";
            }

            UI.Screen.ShowSubtitle(zoneInfoMsg);
        }

        public static int CompareZonesByDistToPlayer(TurfZone x, TurfZone y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    Vector3 playerPos = MindControl.CurrentPlayerCharacter.Position;
                    return playerPos.DistanceTo2D(x.zoneBlipPosition).
                        CompareTo(playerPos.DistanceTo2D(y.zoneBlipPosition));
                }
            }
        }


        public static int CompareZonesByValue(TurfZone x, TurfZone y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                else
                {
                    return y.value.CompareTo(x.value);
                }
            }
        }

        #region blip related methods


        public void ChangeBlipDisplay()
        {
            curBlipDisplay++;
            if (curBlipDisplay > ZoneBlipDisplay.allZones)
            {
                curBlipDisplay = ZoneBlipDisplay.none;
            }

            RefreshZoneBlips();
        }

        public void ChangeBlipDisplay(ZoneBlipDisplay desiredDisplayType)
        {
            curBlipDisplay = desiredDisplayType;
            if (curBlipDisplay > ZoneBlipDisplay.allZones)
            {
                curBlipDisplay = ZoneBlipDisplay.none;
            }

            RefreshZoneBlips();
        }

        public void RefreshZoneBlips()
        {
            switch (curBlipDisplay)
            {
                case ZoneBlipDisplay.none:
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        zoneData.zoneList[i].RemoveBlip();
                    }
                    break;
                case ZoneBlipDisplay.allZones:
                    //refresh the closest since we only show area blips for the closest
                    zoneData.zoneList.Sort(CompareZonesByDistToPlayer);
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        zoneData.zoneList[i].CreateAttachedBlip(i < 5);
                        zoneData.zoneList[i].UpdateBlip();
                    }
                    break;
                case ZoneBlipDisplay.fiveClosest:
                    zoneData.zoneList.Sort(CompareZonesByDistToPlayer);
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        if (i < 5)
                        {
                            zoneData.zoneList[i].CreateAttachedBlip(true);
                            zoneData.zoneList[i].UpdateBlip();
                        }
                        else
                        {
                            zoneData.zoneList[i].RemoveBlip();
                        }
                    }
                    break;
                default:
                    UI.Notification.Show("Invalid blip display type");
                    break;
            }
        }

        #endregion

        public void GiveGangZonesToAnother(string FromGang, string ToGang)
        {
            List<TurfZone> fromGangZones = GetZonesControlledByGang(FromGang);
            for (int i = 0; i < fromGangZones.Count; i++)
            {
                fromGangZones[i].ownerGangName = ToGang;
            }

            SaveZoneData(false);
        }

        #region getters


        public bool DoesZoneWithNameExist(string zoneName)
        {
            return GetZoneByName(zoneName) != null;
        }

        /// <summary>
        /// not exposed in favor of other zone retrieval options that better handle custom zones
        /// </summary>
        /// <param name="zoneName"></param>
        /// <returns></returns>
        private TurfZone GetZoneByName(string zoneName)
        {
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].zoneName == zoneName)
                {
                    return zoneData.zoneList[i];
                }
            }

            return null;
        }

        /// <summary>
        /// gets the turfzone of where the player is
        /// </summary>
        /// <returns></returns>
        public TurfZone GetCurrentTurfZone()
        {
            return GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
        }

        public List<TurfZone> GetZonesControlledByGang(string desiredGangName)
        {
            List<TurfZone> ownedZones = new List<TurfZone>();

            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].ownerGangName == desiredGangName)
                {
                    ownedZones.Add(zoneData.zoneList[i]);
                }
            }

            return ownedZones;
        }

        /// <summary>
        /// returns how much, in 0 to 1 percentage, of the "takeable world" the target gang owns
        /// </summary>
        /// <param name="desiredGangName"></param>
        /// <returns></returns>
        public float GetPercentOfZonesOwnedByGang(string desiredGangName)
        {
            int numOwnedZones = 0;
            foreach(TurfZone z in zoneData.zoneList)
            {
                if(z.ownerGangName == desiredGangName)
                {
                    numOwnedZones++;
                }
            }

            return (float)numOwnedZones / zoneData.zoneList.Count;
        }

        public TurfZone GetClosestZoneToTargetZone(TurfZone targetZone, bool hostileOrNeutralZonesOnly = false, bool randomBetween3Closest = true)
        {
            float smallestDistance = 0;
            //we start our top 3 closest zones list with only the zone we want to get the closest from and start replacing as we find better ones
            //the result may not be the 3 closest zones, but thats okay
            List<TurfZone> top3ClosestZones = new List<TurfZone> { targetZone, targetZone, targetZone };
            int timesFoundBetterZone = 0;
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                float distanceToThisZone = World.GetDistance(targetZone.zoneBlipPosition, zoneData.zoneList[i].zoneBlipPosition);
                if (distanceToThisZone != 0 &&
                    (!hostileOrNeutralZonesOnly || targetZone.ownerGangName != zoneData.zoneList[i].ownerGangName))
                {
                    if (smallestDistance == 0 || smallestDistance > distanceToThisZone)
                    {
                        timesFoundBetterZone++;
                        top3ClosestZones.Insert(0, zoneData.zoneList[i]);
                        top3ClosestZones.RemoveAt(3);
                        smallestDistance = distanceToThisZone;
                    }
                }
            }

            if (randomBetween3Closest && timesFoundBetterZone >= 3) //only get a random from top 3 if we found 3 different zones
            {
                return RandoMath.RandomElement(top3ClosestZones);
            }
            else
            {
                return top3ClosestZones[0];
            }

        }

        public TurfZone GetRandomZone(bool preferablyNeutralZone = false)
        {
            if (!preferablyNeutralZone)
            {
                return RandoMath.RandomElement(zoneData.zoneList);
            }
            else
            {
                if (zoneData.zoneList.Count > 0)
                {
                    List<TurfZone> possibleTurfChoices = new List<TurfZone>();

                    possibleTurfChoices.AddRange(zoneData.zoneList);

                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        if (possibleTurfChoices.Count == 0)
                        {
                            //we've run out of options! abort
                            break;
                        }
                        TurfZone chosenZone = RandoMath.RandomElement(possibleTurfChoices);
                        if (!preferablyNeutralZone || chosenZone.ownerGangName == "none")
                        {
                            return chosenZone;
                        }
                        else
                        {
                            possibleTurfChoices.Remove(chosenZone);
                        }
                    }

                    //if we couldn't find a neutral zone, just get any zone
                    return GetRandomZone(false);
                }

            }

            return null;
        }


        #endregion




    }
}
