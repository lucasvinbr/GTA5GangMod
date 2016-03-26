using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA.GangAndTurfMod
{
    public class SpawnedGangMember : UpdatedClass
    {
        
        public Ped watchedPed;

        public override void Update()
        {
            if (watchedPed.IsInAir || watchedPed.IsPlayer)
            {
                return;
            }
            if ((RandomUtil.CachedRandom.Next(10) <= 7 || Game.Player.IsTargetting(watchedPed)) &&
                !watchedPed.IsInCombat)
            {
                watchedPed.Task.FightAgainstHatedTargets(100);
            }

            if (watchedPed.IsInCombat)
            {
                Gang pedGang = GangManager.instance.GetGangByRelGroup(watchedPed.RelationshipGroup);
                if (pedGang != null)
                foreach (Ped member in GangManager.instance.GetSpawnedMembersOfGang
                    (pedGang))
                {
                    if (!member.IsInCombat && !member.IsInAir && !member.IsPlayer)
                    {
                        member.Task.FightAgainstHatedTargets(200);
                    }
                    
                }
            }
            else
            {
                if (watchedPed.RelationshipGroup == GangManager.instance.GetPlayerGang().relationGroupIndex)
                {

                    if (!watchedPed.IsInCombat)
                    {
                        //help the player if he's in trouble and we're not
                        foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character.Position, 100))
                        {
                            if (ped.IsInCombat && (ped.RelationshipGroup != GangManager.instance.GetPlayerGang().relationGroupIndex &&
                                ped.RelationshipGroup != Game.Player.Character.RelationshipGroup))
                            {
                                watchedPed.Task.FightAgainst(ped);
                                break;
                            }
                        }
                    }

                }
            }

            if (!watchedPed.IsAlive)
            {
                watchedPed.CurrentBlip.Remove();
                this.watchedPed = null;
            }
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangMemberAIUpdates;
        }

        public SpawnedGangMember(Ped watchedPed)
        {
            this.watchedPed = watchedPed;
            ResetUpdateInterval();
        }

    }
}
