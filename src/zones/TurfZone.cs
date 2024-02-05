using GTA.Math;
using GTA.Native;
using System.Xml.Serialization;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// a zone that can be taken over by a gang.
    /// members from that gang will spawn if you are inside their zone
    /// </summary>
    [XmlInclude(typeof(CustomTurfZone))]
    public class TurfZone
    {
        public string zoneName, ownerGangName;

        public Math.Vector3 zoneBlipPosition;

        public int value = 0;

        [XmlIgnore]
        protected Blip myBlip;

        public TurfZone(string zoneName)
        {
            this.zoneName = zoneName;
            ownerGangName = "none";
        }

        public TurfZone()
        {
            this.zoneName = "zone";
            this.ownerGangName = "none";
        }

        /// <summary>
        /// returns the zone's localized name
        /// </summary>
        /// <returns></returns>
        public virtual string GetDisplayName()
        {
            return World.GetZoneLocalizedName(zoneBlipPosition);
        }

        /// <summary>
        /// true if the provided ingame zone and/or location are considered to be "inside" this turf zone
        /// </summary>
        /// <param name="gameZoneName"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public virtual bool IsLocationInside(string gameZoneName, Vector3 location)
        {
            //we don't care about location, just about the ingame zone name
            if (gameZoneName == zoneName)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// if the blip is being displayed, refreshes its size and color
        /// </summary>
        public virtual void UpdateBlip()
        {
            if (myBlip != null)
            {
                Gang ownerGang = GangManager.instance.GetGangByName(ownerGangName);
                if (ownerGang == null)
                {
                    myBlip.Sprite = BlipSprite.GTAOPlayerSafehouseDead;
                    myBlip.Color = BlipColor.White;
                    myBlip.RemoveNumberLabel();
                }
                else
                {
                    myBlip.Sprite = BlipSprite.GTAOPlayerSafehouse;
                    Function.Call(Hash.SET_BLIP_COLOUR, myBlip, ownerGang.blipColor);

                    if (ownerGang.isPlayerOwned)
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, myBlip, 0f, 255, 0f);
                    }
                    else
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, myBlip, 255, 0f, 0f);
                    }

                    myBlip.NumberLabel = value;
                }

                if (ownerGang != null)
                {
                    myBlip.Name = string.Concat(zoneName, " (", ownerGangName, " turf, level ", value.ToString(), ")");
                }
                else
                {
                    myBlip.Name = string.Concat(zoneName, " (neutral territory)");
                }

            }

        }

        public virtual void UpdateBlipPosition()
        {
            myBlip.Position = zoneBlipPosition;
        }

        /// <summary>
        /// extras are additions to the zone's look in the map, like the area blips for custom zones
        /// </summary>
        /// <param name="withExtras"></param>
        public virtual void CreateAttachedBlip(bool withExtras = false)
        {
            if (myBlip == null)
            {
                myBlip = World.CreateBlip(zoneBlipPosition);
            }
        }

        public virtual void RemoveBlip()
        {
            if (myBlip != null)
            {
                myBlip.Delete();
                myBlip = null;
            }
        }

        /// <summary>
        /// true if a war is occurring in this zone
        /// </summary>
        /// <returns></returns>
        public virtual bool IsBeingContested()
        {
            return GangWarManager.instance.IsZoneContested(this);
        }

        /// <summary>
        /// level 5 zone and maxTurfValue modOption is 10 -> returns 0.5 (50%)
        /// </summary>
        /// <returns></returns>
        public float GetUpgradePercentage()
        {
            return value / (float) RandoMath.Max(ModOptions.instance.maxTurfValue, 1);
        }

    }
}
