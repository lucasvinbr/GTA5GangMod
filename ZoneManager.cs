using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script deals with gangs taking over zones. it also allows adding new zones to be taken
    /// </summary>
    public class ZoneManager
    {
        
        public enum zoneBlipDisplay
        {
            none,
            fiveClosest,
            allZones,
        }

        public zoneBlipDisplay curBlipDisplay = zoneBlipDisplay.none;

        #region setup/save stuff

        public class TurfZoneData
        {
            public List<TurfZone> zoneList;

            public TurfZoneData()
            {
                zoneList = new List<TurfZone>();
            }
        }

        public class AreaBlip
        {
            public Math.Vector3 position;
            public float radius;

            public AreaBlip()
            {
                radius = 100;
                position = Math.Vector3.Zero;
            }

            public AreaBlip(Math.Vector3 position, float radius)
            {
                this.position = position;
                this.radius = radius;
            }

        }

        public static ZoneManager instance;
        TurfZoneData zoneData;


        public ZoneManager()
        {
            instance = this;

            zoneData = PersistenceHandler.LoadFromFile<TurfZoneData>("TurfZoneData");
            if (zoneData == null)
            {
                zoneData = new TurfZoneData();
            }

            for(int i = 0; i < zoneData.zoneList.Count; i++)
            {
                List<AreaBlip> zoneCircleList = zoneData.zoneList[i].zoneCircles;
                for (int j = 0; j < zoneCircleList.Count; j++)
                {
                    Blip circleBlip = World.CreateBlip(zoneCircleList[j].position, zoneCircleList[j].radius);
                    circleBlip.Alpha = 50; //TODO add mod option to control alpha!
                    zoneData.zoneList[i].myCircleBlips.Add(circleBlip);
                }
            }
        }

        public void SaveZoneData(bool notifySuccess = true)
        {
            PersistenceHandler.SaveToFile(zoneData, "TurfZoneData", notifySuccess);
        }

        public void UpdateZoneData(TurfZone newTurfZone)
        {
            if (!zoneData.zoneList.Contains(newTurfZone))
            {
                zoneData.zoneList.Add(newTurfZone);
            }

            CreateAttachedBlip(newTurfZone);

            newTurfZone.AttachedBlip.Position = newTurfZone.zoneBlipPosition;

            RefreshZoneBlips();

            SaveZoneData(false);
        }

        #endregion

        public void OutputCurrentZoneInfo()
        {
            string zoneName = World.GetZoneName(Game.Player.Character.Position);
            string zoneInfoMsg = "Current zone is " + zoneName + ".";
            TurfZone currentZone = GetZoneByName(zoneName);

            if (currentZone != null)
            {
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
                zoneInfoMsg += " It hasn't been marked as takeable yet.";
            }

            UI.ShowSubtitle(zoneInfoMsg);
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
                    Vector3 playerPos = Game.Player.Character.Position;
                    return World.GetDistance(x.zoneBlipPosition, playerPos).
                        CompareTo(World.GetDistance(y.zoneBlipPosition, playerPos));
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

        public void CreateAttachedBlip(TurfZone targetZone)
        {
            if(targetZone.AttachedBlip == null)
            {
                targetZone.AttachedBlip = World.CreateBlip(targetZone.zoneBlipPosition);
            }
           
        }

        public void AddNewCircleBlip(Vector3 position, TurfZone targetZone)
        {
            if(targetZone != null)
            {
                Blip newCircleBlip = World.CreateBlip(position, 100);
                newCircleBlip.Alpha = 50;
                targetZone.myCircleBlips.Add(newCircleBlip);
                if (targetZone.zoneCircles == null) targetZone.zoneCircles = new List<AreaBlip>();
                targetZone.zoneCircles.Add(new AreaBlip(position, 100));
            }
        }

        public void ChangeBlipDisplay()
        {
            curBlipDisplay++;
            if(curBlipDisplay > zoneBlipDisplay.allZones)
            {
                curBlipDisplay = zoneBlipDisplay.none;
            }

            RefreshZoneBlips();
        }

        public void ChangeBlipDisplay(zoneBlipDisplay desiredDisplayType)
        {
            curBlipDisplay = desiredDisplayType;
            if (curBlipDisplay > zoneBlipDisplay.allZones)
            {
                curBlipDisplay = zoneBlipDisplay.none;
            }

            RefreshZoneBlips();
        }

        public void RefreshZoneBlips()
        {
            switch (curBlipDisplay)
            {
                case zoneBlipDisplay.none:
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        //zoneData.zoneList[i].AttachedBlip.Scale = 0;
                        if(zoneData.zoneList[i].AttachedBlip != null)
                        {
                            zoneData.zoneList[i].AttachedBlip.Remove();
                            zoneData.zoneList[i].AttachedBlip = null;
                        }
                        
                    }
                    break;
                case zoneBlipDisplay.allZones:
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        CreateAttachedBlip(zoneData.zoneList[i]);
                        UpdateZoneBlip(zoneData.zoneList[i]);
                    }
                    break;
                case zoneBlipDisplay.fiveClosest:
                    zoneData.zoneList.Sort(CompareZonesByDistToPlayer);
                    for (int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        if (i < 5)
                        {
                            CreateAttachedBlip(zoneData.zoneList[i]);
                            UpdateZoneBlip(zoneData.zoneList[i]);
                        }
                        else
                        {
                            if (zoneData.zoneList[i].AttachedBlip != null)
                            {
                                zoneData.zoneList[i].AttachedBlip.Remove();
                                zoneData.zoneList[i].AttachedBlip = null;
                            }
                        }
                    }
                    break;
                default:
                    UI.Notify("Invalid blip display type");
                    break;
            }
        }

        public void UpdateZoneBlip(TurfZone targetZone)
        {
            if (targetZone.AttachedBlip != null)
            {
                Gang ownerGang = GangManager.instance.GetGangByName(targetZone.ownerGangName);
                if (ownerGang == null)
                {
                    targetZone.AttachedBlip.Sprite = BlipSprite.GTAOPlayerSafehouseDead;
                    targetZone.AttachedBlip.Color = BlipColor.White;
                }
                else {
                    targetZone.AttachedBlip.Sprite = BlipSprite.GTAOPlayerSafehouse;
                    Function.Call(Hash.SET_BLIP_COLOUR, targetZone.AttachedBlip, ownerGang.blipColor);

                    if (ownerGang.isPlayerOwned)
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, targetZone.AttachedBlip, 0f, 255, 0f);
                    }
                    else
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, targetZone.AttachedBlip, 255, 0f, 0f);
                    }

                    targetZone.AttachedBlip.Scale = 1.0f + 0.65f / ((ModOptions.instance.maxTurfValue + 1) / (targetZone.value + 1));
                }

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                if (ownerGang != null)
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat(targetZone.zoneName, " (", targetZone.ownerGangName, " turf, level ", targetZone.value.ToString(), ")"));
                }
                else
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat(targetZone.zoneName, " (neutral territory)"));
                }

                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, targetZone.AttachedBlip);
            }
        }

        #endregion

        public void GiveGangZonesToAnother(string FromGang, string ToGang)
        {
            List<TurfZone> fromGangZones = GetZonesControlledByGang(FromGang);
            for(int i = 0; i < fromGangZones.Count; i++)
            {
                fromGangZones[i].ownerGangName = ToGang;
            }

            SaveZoneData(false);
        }

        #region getters

        public TurfZone GetZoneByName(string zoneName)
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
        /// gets the turfzone of where the player is.
        /// basically a call to getZoneByName, but it's called so often I'm too lazy to write this all over
        /// </summary>
        /// <returns></returns>
        public TurfZone GetCurrentTurfZone()
        {
            return GetZoneByName(World.GetZoneName(Game.Player.Character.Position));
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
                if(distanceToThisZone != 0 && 
                    (!hostileOrNeutralZonesOnly || targetZone.ownerGangName != zoneData.zoneList[i].ownerGangName))
                {
                    if(smallestDistance == 0 || smallestDistance > distanceToThisZone)
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
                return RandoMath.GetRandomElementFromList(top3ClosestZones);
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
                return RandoMath.GetRandomElementFromList(zoneData.zoneList);
            }
            else
            {
                if(zoneData.zoneList.Count > 0)
                {
                    List<TurfZone> possibleTurfChoices = new List<TurfZone>();

                    possibleTurfChoices.AddRange(zoneData.zoneList);

                    for(int i = 0; i < zoneData.zoneList.Count; i++)
                    {
                        if(possibleTurfChoices.Count == 0)
                        {
                            //we've run out of options! abort
                            break;
                        }
                        TurfZone chosenZone = RandoMath.GetRandomElementFromList(possibleTurfChoices);
                        if(!preferablyNeutralZone || chosenZone.ownerGangName == "none")
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
