using GTA.Math;
using GTA.Native;
using System.Xml.Serialization;


namespace GTA.GangAndTurfMod
{

    /// <summary>
    /// zone not attached to any game zone, created by the player.
    /// has an area blip to define its influence
    /// </summary>
    public class CustomTurfZone : TurfZone
    {

        [XmlIgnore]
        public Blip areaBlip;

        public float areaRadius;

        public const float MIN_ZONE_RADIUS = 20.0f, DEFAULT_ZONE_RADIUS = 50.0f, MAX_ZONE_RADIUS = 500.0f;

        public CustomTurfZone()
        {
            areaRadius = DEFAULT_ZONE_RADIUS;
            zoneName = "zone";
            ownerGangName = "none";
        }

        public CustomTurfZone(string zoneName)
        {
            this.zoneName = zoneName;
            ownerGangName = "none";
            areaRadius = DEFAULT_ZONE_RADIUS;
        }

        public override string GetDisplayName()
        {
            return zoneName;
        }

        public override bool IsLocationInside(string gameZoneName, Vector3 location)
        {
            return Vector3.Distance2D(location, zoneBlipPosition) <= areaRadius;
        }

        public override void UpdateBlip()
        {
            if (myBlip != null)
            {
                Gang ownerGang = GangManager.instance.GetGangByName(ownerGangName);
                if (ownerGang == null)
                {
                    myBlip.Sprite = BlipSprite.GTAOPlayerSafehouseDead;
                    myBlip.Color = BlipColor.White;
                    myBlip.RemoveNumberLabel();

                    if (areaBlip != null)
                    {
                        areaBlip.Color = BlipColor.White;
                        areaBlip.Alpha = 60;
                    }
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

                    if (areaBlip != null)
                    {
                        Function.Call(Hash.SET_BLIP_COLOUR, areaBlip, ownerGang.blipColor);
                        areaBlip.Alpha = 60 + (int)(75 / ((ModOptions.instance.maxTurfValue + 1) / ((float)value + 1)));
                    }
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

        public override void UpdateBlipPosition()
        {
            base.UpdateBlipPosition();

            if (areaBlip != null) areaBlip.Position = zoneBlipPosition;
        }

        public override void CreateAttachedBlip(bool withExtras = false)
        {
            base.CreateAttachedBlip(withExtras);

            if (withExtras && areaBlip == null)
            {
                areaBlip = World.CreateBlip(zoneBlipPosition, areaRadius);
            }
        }

        public override void RemoveBlip()
        {
            base.RemoveBlip();

            if (areaBlip != null)
            {
                areaBlip.Delete();
                areaBlip = null;
            }
        }
    }
}
