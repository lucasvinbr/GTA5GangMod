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

        [System.Serializable]
        public class TurfZoneData
        {
            public List<TurfZone> zoneList;

            public TurfZoneData()
            {
                zoneList = new List<TurfZone>();
            }
        }

        [System.Serializable]
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
            PersistenceHandler.SaveToFile<TurfZoneData>(zoneData, "TurfZoneData", notifySuccess);
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
                        zoneInfoMsg += " It is owned by the " + currentZone.ownerGangName;
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

        public int CompareZonesByDistToPlayer(TurfZone x, TurfZone y)
        {
            if(x == null)
            {
                if(y == null)
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
                if(y == null)
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

        #region blip related methods

        public void CreateAttachedBlip(TurfZone targetZone)
        {
            if(targetZone.AttachedBlip == null)
            {
                targetZone.AttachedBlip = World.CreateBlip(targetZone.zoneBlipPosition);
                targetZone.AttachedBlip.Sprite = BlipSprite.ArmWrestling;
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
                        //zoneData.zoneList[i].AttachedBlip.Scale = 1;
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
                            //zoneData.zoneList[i].AttachedBlip.Scale = 1;
                            CreateAttachedBlip(zoneData.zoneList[i]);
                            UpdateZoneBlip(zoneData.zoneList[i]);
                        }
                        else
                        {
                            //zoneData.zoneList[i].AttachedBlip.Scale = 0;
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
                    targetZone.AttachedBlip.Color = BlipColor.White;
                    Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, targetZone.AttachedBlip, 0f, 0f, 0f);
                }
                else {

                    Function.Call(Hash.SET_BLIP_COLOUR, targetZone.AttachedBlip, ownerGang.blipColor);

                    if (ownerGang.isPlayerOwned)
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, targetZone.AttachedBlip, 0f, 0.5f, 0f);
                    }
                    else
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, targetZone.AttachedBlip, 0.5f, 0f, 0f);
                    }
                }

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                if (ownerGang != null)
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat(targetZone.zoneName, " (", targetZone.ownerGangName, " turf)"));
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
            TurfZone[] fromGangZones = GetZonesControlledByGang(FromGang);
            for(int i = 0; i < fromGangZones.Length; i++)
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

        public TurfZone[] GetZonesControlledByGang(string desiredGangName)
        {
            List<TurfZone> ownedZones = new List<TurfZone>();

            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if (zoneData.zoneList[i].ownerGangName == desiredGangName)
                {
                    ownedZones.Add(zoneData.zoneList[i]);
                }
            }

            return ownedZones.ToArray();
        }

        public TurfZone GetClosestZoneToTargetZone(TurfZone targetZone, bool hostileOrNeutralZonesOnly = false)
        {
            float smallestDistance = 0;
            TurfZone closestZone = targetZone;
            for (int i = 0; i < zoneData.zoneList.Count; i++)
            {
                float distanceToThisZone = World.GetDistance(targetZone.zoneBlipPosition, zoneData.zoneList[i].zoneBlipPosition);
                if(distanceToThisZone != 0 && 
                    (!hostileOrNeutralZonesOnly || targetZone.ownerGangName != zoneData.zoneList[i].ownerGangName))
                {
                    if(smallestDistance == 0 || smallestDistance > distanceToThisZone)
                    {
                        closestZone = zoneData.zoneList[i];
                        smallestDistance = distanceToThisZone;
                    }
                }
            }

            return closestZone;
        }

        public TurfZone GetRandomZone()
        {
            if(zoneData.zoneList.Count > 0)
            {
                return zoneData.zoneList[RandomUtil.CachedRandom.Next(0, zoneData.zoneList.Count)];
            }
            else
            {
                return null;
            }
            
        }

       
        #endregion




    }
}
