using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace GTA
{
    /// <summary>
    /// a zone that can be taken over by a gang.
    /// members from that gang will spawn if you are inside their zone
    /// </summary>
    [System.Serializable]
    public class TurfZone
    {
        public string zoneName, ownerGangName;

        public Math.Vector3 zoneBlipPosition;

        public int value = 0;

        [XmlIgnore]
        private Blip myBlip;

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

        [XmlIgnore]
        public Blip AttachedBlip
        {
            get
            {
                return myBlip;
            }

            set{
                myBlip = value;
            }
        }
    }
}
