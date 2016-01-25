using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using System.Windows.Forms;

namespace GTA
{
    /// <summary>
    /// this script deals with gangs taking over zones. it also allows adding new zones to be taken
    /// </summary>
    public class ZoneManager : Script
    {
        [System.Serializable]
        public class TurfZoneData
        {
            public List<TurfZone> zoneList;

            public TurfZoneData()
            {
                zoneList = new List<TurfZone>();
            }
        }

        public static ZoneManager instance;
        TurfZoneData zoneData;


        public ZoneManager()
        {
            instance = this;
            this.KeyUp += onKeyUp;

            zoneData = PersistenceHandler.LoadFromFile<TurfZoneData>("TurfZoneData");
            if (zoneData == null)
            {
                zoneData = new TurfZoneData();
            }
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.N)
            {
                if (e.Modifiers == Keys.None)
                {
                    OutputCurrentZoneInfo();
                }
            }
        }

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
                        currentZone.ownerGangName = "none";
                        SaveZoneData();
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

        public TurfZone GetZoneByName(string zoneName)
        {
            for(int i = 0; i < zoneData.zoneList.Count; i++)
            {
                if(zoneData.zoneList[i].zoneName == zoneName)
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

        public void SaveZoneData()
        {
            PersistenceHandler.SaveToFile<TurfZoneData>(zoneData, "TurfZoneData");
        }

        public void UpdateZoneData(TurfZone newTurfZone)
        {
            if (!zoneData.zoneList.Contains(newTurfZone))
            {
                zoneData.zoneList.Add(newTurfZone);
            }
            
            SaveZoneData();
        }
    }
}
