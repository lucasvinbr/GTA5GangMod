using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;

namespace GTA.GangAndTurfMod
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

        private int ticksSinceLastEnemyRetask = 0;

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
                warBlip.IsFlashing = true;
                warBlip.Sprite = BlipSprite.Deathmatch;
                warBlip.Color = BlipColor.Red;
                
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;
                ticksSinceLastEnemyRetask = 0;

                currentWave = 0;

                waveIsExhausted = false;

                isOccurring = true;

                int gangStrengthBonus = enemyGang.GetGangAIStrengthValue() / 10000;
                if(gangStrengthBonus > 4)
                {
                    gangStrengthBonus = 4;
                }

                if (theWarType == warType.attackingEnemy)
                {
                    waves = warZone.value + RandomUtil.CachedRandom.Next(gangStrengthBonus);
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");
                    Wait(4000);
                }
                else
                {
                    waves = 1 + RandomUtil.CachedRandom.Next(2);
                    Function.Call(Hash.PLAY_SOUND, -1, "Virus_Eradicated", "LESTER1A_SOUNDS", 0, 0, 1);
                    UI.Notify("The " + enemyGang.name + " are attacking " + warZone.zoneName + "!", true);
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
                            }
                            //spawn the wave
                            for (int i = 0; i < baseMembersPerWave + currentWave * RandomUtil.CachedRandom.Next(5); i++)
                            {
                                Ped spawnedMember = GangManager.instance.SpawnGangMember(enemyGang, World.GetNextPositionOnSidewalk
                                      (World.GetNextPositionOnStreet(Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 100)), true);

                                if (spawnedMember != null)
                                {
                                    spawnedMember.Task.FightAgainst(Game.Player.Character);
                                }
                                
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
                            enemyGang.TakeZone(warZone);
                            GangManager.instance.GiveTurfRewardToGang(enemyGang);
                            UI.ShowSubtitle("We've left our contested turf. It has been taken by the " + enemyGang.name + ".");
                        }

                        if(livingEnemies != null)
                        {
                            for (int i = 0; i < livingEnemies.Length; i++)
                            {
                                if(livingEnemies[i] != null)
                                {
                                    livingEnemies[i].MarkAsNoLongerNeeded();
                                }
                            }
                        }
                       
                        EndWar();
                    }
                }
                //if their leader is dead...
                if (!Game.Player.IsAlive && !GangManager.instance.hasChangedBody)
                {
                    //the war ends
                    if (curWarType == warType.attackingEnemy)
                    {
                        UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                        enemyGang.moneyAvailable += 5000;
                    }
                    else
                    {
                        enemyGang.TakeZone(warZone);
                        UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
                        GangManager.instance.GiveTurfRewardToGang(enemyGang);
                    }

                    if (livingEnemies != null)
                    {
                        for (int i = 0; i < livingEnemies.Length; i++)
                        {
                            if (livingEnemies[i] != null)
                            {
                                livingEnemies[i].MarkAsNoLongerNeeded();
                            }
                        }
                    }
                    EndWar();
                    return;
                }

                if (waveIsExhausted)
                {
                    ticksSinceLastEnemyRetask++;

                    if(ticksSinceLastEnemyRetask > 1000)
                    {
                        for (int i = 0; i < livingEnemies.Length; i++)
                        {
                            if (livingEnemies[i].IsAlive)
                            {
                                //sometimes we just spawn far away from the warzone and from the player!
                                //lets parachute close to the player then
                                if (World.GetDistance(livingEnemies[i].Position, Game.Player.Character.Position) > 250 &&
                                    World.GetDistance(livingEnemies[i].Position, World.GetNextPositionOnSidewalk
                                    (World.GetNextPositionOnStreet(warZone.zoneBlipPosition))) > 250)
                                {
                                    UI.Notify("relocated an enemy");
                                    livingEnemies[i].Position = World.GetNextPositionOnSidewalk
                                    (World.GetNextPositionOnStreet(Game.Player.Character.Position)) + RandomUtil.RandomDirection(true) * RandomUtil.CachedRandom.Next(5, 20) +
                                        Vector3.WorldUp * RandomUtil.CachedRandom.Next(50, 101);
                                    livingEnemies[i].Task.ParachuteTo(Game.Player.Character.Position);
                                }
                                if (!livingEnemies[i].IsInAir) livingEnemies[i].Task.FightAgainst(Game.Player.Character);
                            }
                        }
                        
                        ticksSinceLastEnemyRetask = 0;
                    }
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
                        ticksSinceLastEnemyRetask = 0;
                        waveIsExhausted = false;
                    }
                    else
                    {
                        if(curWarType == warType.attackingEnemy)
                        {
                            Gang playerGang = GangManager.instance.GetPlayerGang();
                            playerGang.TakeZone(warZone);
                            GangManager.instance.GiveTurfRewardToGang(playerGang);
                            UI.ShowSubtitle(warZone.zoneName + " is now ours!");
                        }
                        else
                        {
                            Game.Player.Money += 2500;
                            UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                        }
                       
                        EndWar();
                    }
                    

                    
                }

               
            }
        }
    }
}
