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

        public int waves, currentEnemyCasualties, casualtiesForEnemyDefeat;

        public bool isOccurring = false;

        public enum warType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public warType curWarType = warType.attackingEnemy;

        private int curTicksAwayFromBattle = 0, ticksSinceLastCarSpawn = 0;

        private int ticksBeforeAutoLose = 30000, minTicksBetweenCarSpawns = 1000;

        public TurfZone warZone;

        public Gang enemyGang;

        public static GangWarManager instance;

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
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;

                currentEnemyCasualties = 0;
                casualtiesForEnemyDefeat = waves + ModOptions.instance.baseNumKillsBeforeWarVictory + 
                    RandomUtil.CachedRandom.Next(ModOptions.instance.baseNumKillsBeforeWarVictory / 6);

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
            currentEnemyCasualties = 0;
            isOccurring = false;
            Game.WantedMultiplier = 1;
        }

        void CheckIfBattleWasUnfair()
        {
            //the battle was unfair if the player's gang had guns and the enemy gang hadn't
            //in this case, there is a possibility of the defeated gang instantly getting pistols
            //in order to at least not get decimated all the time

            if(enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
                GangManager.instance.GetPlayerGang().GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) != WeaponHash.Unarmed)
            {
                if (RandomUtil.RandomBool())
                {
                    enemyGang.gangWeaponHashes.Add(RandomUtil.GetRandomElementFromList(ModOptions.instance.driveByWeapons));
                    GangManager.instance.SaveGangData(false);
                }
            }
        }

        /// <summary>
        /// spawns a vehicle that has the player as destination
        /// </summary>
        public void SpawnAngryVehicle(bool isFriendly)
        {
            Math.Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForCar();
            Vehicle spawnedVehicle = null;
            if (!isFriendly)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(enemyGang,
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true);
            }
            else
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.GetPlayerGang(),
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true);
            }
            
            if (spawnedVehicle != null)
            {
                spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver).Task.DriveTo(spawnedVehicle, Game.Player.Character.Position, 25, 100);
                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver), 4457020); //ignores roads, avoids obstacles

                ticksSinceLastCarSpawn = RandomUtil.CachedRandom.Next(-minTicksBetweenCarSpawns, 0);
            }
        }

        public void OnEnemyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 300){
                currentEnemyCasualties++;

                //have we lost too many? its a victory for the player then
                if(currentEnemyCasualties >= casualtiesForEnemyDefeat)
                {
                    if (curWarType == warType.attackingEnemy)
                    {
                        Gang playerGang = GangManager.instance.GetPlayerGang();
                        playerGang.TakeZone(warZone);
                        GangManager.instance.AddOrSubtractMoneyToProtagonist(ModOptions.instance.rewardForTakingEnemyTurf);
                        UI.ShowSubtitle(warZone.zoneName + " is now ours!");
                        CheckIfBattleWasUnfair();
                        Function.Call(Hash.PLAY_SOUND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0, 0, 1);
                    }
                    else
                    {
                        GangManager.instance.AddOrSubtractMoneyToProtagonist(ModOptions.instance.rewardForTakingEnemyTurf / 2);
                        UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                        CheckIfBattleWasUnfair();
                        Function.Call(Hash.PLAY_SOUND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0, 0, 1);
                    }

                    EndWar();
                }
                else
                {
                    UI.ShowSubtitle((casualtiesForEnemyDefeat - currentEnemyCasualties).ToString() + " kills remaining!", 900);
                }

            }
        }

        void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                ticksSinceLastCarSpawn++;
                if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName)
                {
                    curTicksAwayFromBattle = 0;
                    Game.WantedMultiplier = 0;

                    if (ModOptions.instance.emptyZoneDuringWar)
                    {
                        Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                        Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                    }

                    if (ticksSinceLastCarSpawn >= minTicksBetweenCarSpawns)
                    {
                        SpawnAngryVehicle(false);

                        if(curWarType == warType.defendingFromEnemy && RandomUtil.RandomBool())
                        {
                            SpawnAngryVehicle(true); //automatic backup for us
                        }
                    }

                    if (GangManager.instance.GetSpawnedMembersOfGang(enemyGang).Length < ModOptions.instance.spawnedMemberLimit / 2)
                    {
                        Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForMember();
                        Ped spawnedMember = GangManager.instance.SpawnGangMember(enemyGang, spawnPos);

                        if (spawnedMember != null)
                        {
                            spawnedMember.Task.FightAgainst(Game.Player.Character);
                        }

                        Wait(200);
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
                        enemyGang.moneyAvailable += ModOptions.instance.costToTakeNeutralTurf;
                    }
                    else
                    {
                        enemyGang.TakeZone(warZone);
                        UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
                        GangManager.instance.GiveTurfRewardToGang(enemyGang);
                    }

                    EndWar();
                    return;
                }
            }
        }
    }
}
