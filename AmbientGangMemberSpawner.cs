﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA.GangAndTurfMod
{
    class AmbientGangMemberSpawner : Script
    {
        public string gangName{
            get{
                return "luquinhas boladao";
            }
        }
        public bool enabled = true;
        public static AmbientGangMemberSpawner instance;


        void OnTick(object sender, EventArgs e)
        {
            Wait(4000 + RandomUtil.CachedRandom.Next(2000));
            ZoneManager.instance.RefreshZoneBlips(); //since this runs once in a while, let's also refresh the zone blips

            //lets try to spawn the current zone's corresponding gang members!
            if (enabled)
            {
                TurfZone curTurfZone = ZoneManager.instance.GetCurrentTurfZone();
                if (curTurfZone != null)
                {
                    if (curTurfZone.ownerGangName != "none" && GangManager.instance.GetGangByName(curTurfZone.ownerGangName) != null) //only spawn if there really is a gang in control here
                    {
                        // also reduce police influence
                        Game.WantedMultiplier = ModOptions.instance.wantedFactorWhenInGangTurf;
                        Game.MaxWantedLevel = ModOptions.instance.maxWantedLevelInGangTurf;
                        GangManager.instance.SpawnGangMember
                       (GangManager.instance.GetGangByName
                       (curTurfZone.ownerGangName), World.GetNextPositionOnSidewalk
                               (World.GetNextPositionOnStreet((Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 60))));

                        Wait(1000 + RandomUtil.CachedRandom.Next(3000000) / GangManager.instance.GetGangByName
                       (curTurfZone.ownerGangName).GetGangAIStrengthValue());
                    }
                    else
                    {
                        Game.WantedMultiplier = 1;
                        Game.MaxWantedLevel = 6;
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
