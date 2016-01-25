using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA
{
    class GangWarManager : Script
    {

        public int waves, currentWave, baseMembersPerWave = 7, maxCarsPerWave = 3;

        public bool isOccurring = false;

        public enum warType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public warType curWarType = warType.attackingEnemy;

        private int curTicksAwayFromBattle = 0;

        private int ticksBeforeAutoLose = 30000;

        public TurfZone warZone;

        public Gang enemyGang;

        public static GangWarManager instance;

        private bool waveIsExhausted = false;

        private Ped[] livingEnemies;

        private Blip warBlip;

        public GangWarManager()
        {
            instance = this;
            this.Tick += OnTick;
        }

        public bool StartWar(Gang enemyGang, TurfZone warZone, warType theWarType)
        {
            //TODO disable wars during missions
            if (!isOccurring)
            {
                this.enemyGang = enemyGang;
                this.warZone = warZone;
                this.curWarType = theWarType;

                warBlip = World.CreateBlip(warZone.zoneBlipPosition);
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);
                warBlip.IsFlashing = true;
                warBlip.Color = BlipColor.Red;
                warBlip.Sprite = BlipSprite.Deathmatch;

                curTicksAwayFromBattle = 0;

                currentWave = 0;

                waveIsExhausted = false;

                isOccurring = true;
                if (theWarType == warType.attackingEnemy)
                {
                    waves = warZone.value + RandomUtil.CachedRandom.Next(2);
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");
                    Wait(4000);
                }
                else
                {
                    waves = 2 + RandomUtil.CachedRandom.Next(2);
                    UI.ShowSubtitle("The " + enemyGang.name + " are attacking " + warZone.zoneName + "!");
                }

                return true;
            }
            else
            {
                return false;
            }
            

        }

        public void EndWar()
        {
            warBlip.Remove();
            isOccurring = false;
            Game.WantedMultiplier = 1;
            AmbientGangMemberSpawner.instance.enabled = true;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName)
                {
                    curTicksAwayFromBattle = 0;
                    if (currentWave <= waves && !waveIsExhausted)
                    {
                            if (currentWave == 0)
                            {
                                Game.WantedMultiplier = 0;
                                AmbientGangMemberSpawner.instance.enabled = false;
                            }
                            //spawn the wave
                            for (int i = 0; i < baseMembersPerWave + currentWave * RandomUtil.CachedRandom.Next(5); i++)
                            {
                                Ped spawnedMember = GangManager.instance.SpawnGangMember(enemyGang, World.GetNextPositionOnSidewalk
                                      (World.GetNextPositionOnStreet(Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 100)), true);
                                spawnedMember.Task.FightAgainst(Game.Player.Character);
                                Wait(200);
                            }

                            livingEnemies = GangManager.instance.GetSpawnedMembersOfGang(enemyGang);

                            waveIsExhausted = true;
                      }
                }
                else
                {
                    curTicksAwayFromBattle++;
                    if (curTicksAwayFromBattle > ticksBeforeAutoLose)
                    {
                        //we lose by not being there
                        if (curWarType == warType.attackingEnemy)
                        {
                            UI.ShowSubtitle("We've fled from the battle! The zone remains theirs.");

                        }
                        else
                        {
                            UI.ShowSubtitle("We've left our contested turf. It has been taken by the " + enemyGang.name + ".");

                        }

                        for (int i = 0; i < livingEnemies.Length; i++)
                        {

                            livingEnemies[i].MarkAsNoLongerNeeded();

                        }
                        EndWar();
                    }
                }


                if (!Game.Player.IsAlive)
                {
                    //the war ends
                    if (curWarType == warType.attackingEnemy)
                    {
                        UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                    }
                    else
                    {
                        UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
                    }
                    
                    for (int i = 0; i < livingEnemies.Length; i++)
                    {
                       
                        livingEnemies[i].MarkAsNoLongerNeeded();
                       
                    }
                    EndWar();
                    return;
                }

                if (waveIsExhausted)
                {
                    for(int i = 0; i < livingEnemies.Length; i++)
                    {
                        if (livingEnemies[i].IsDead)
                        {
                            livingEnemies[i].MarkAsNoLongerNeeded();                            
                        }
                        else
                        {
                            return;
                        }
                    }

                    //this wave's been cleared
                    if(currentWave < waves)
                    {
                        UI.ShowSubtitle("You've survived wave " + (currentWave + 1).ToString() + "!");
                        Wait(5000);
                        currentWave++;
                        waveIsExhausted = false;
                    }
                    else
                    {
                        if(curWarType == warType.attackingEnemy)
                        {
                            GangManager.instance.GetPlayerGang().TakeZone(warZone);
                           
                        }
                        else
                        {
                            Game.Player.Money += 1500;
                            UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                        }
                       
                        EndWar();
                    }
                    

                    
                }

               
            }
        }
    }
}
