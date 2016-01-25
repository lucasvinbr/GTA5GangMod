using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA
{
    class AmbientGangMemberSpawner : Script
    {

        public bool enabled = true;
        public static AmbientGangMemberSpawner instance;


        void OnTick(object sender, EventArgs e)
        {
            //every now and then, lets try to spawn the current zone's corresponding gang members!
            if (enabled)
            {
                Wait(5000 + RandomUtil.CachedRandom.Next(15000));
                TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
                if (curTurfZone != null)
                {
                    if (curTurfZone.ownerGangName != "none" && GangManager.instance.GetGangByName(curTurfZone.ownerGangName) != null) //only spawn if there really is a gang in control here
                    {
                        GangManager.instance.SpawnGangMember
                       (GangManager.instance.GetGangByName
                       (curTurfZone.ownerGangName), World.GetNextPositionOnSidewalk
                               (World.GetNextPositionOnStreet((Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 60))));
                    }

                }
            }
        }

        public AmbientGangMemberSpawner()
        {
            this.Tick += OnTick;
            instance = this;
        }
    }
}
