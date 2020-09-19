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

                    myBlip.Scale = 1.0f + 0.65f / ((ModOptions.instance.maxTurfValue + 1) / (value + 1));
                }

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                if (ownerGang != null)
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat(zoneName, " (", ownerGangName, " turf, level ", value.ToString(), ")"));
                }
                else
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat(zoneName, " (neutral territory)"));
                }

                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, myBlip);
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
                myBlip.Remove();
                myBlip = null;
            }
        }

        /// <summary>
        /// true if a war is occurring in this zone
        /// </summary>
        /// <returns></returns>
        public virtual bool IsBeingContested()
        {
            return GangWarManager.instance.isOccurring && GangWarManager.instance.warZone == this;
        }

    }
}
