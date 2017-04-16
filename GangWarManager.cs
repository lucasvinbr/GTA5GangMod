using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;
using NativeUI;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    public class GangWarManager : Script
    {

        public int enemyReinforcements, alliedReinforcements;

        public bool isOccurring = false;

        public enum warType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public enum attackStrength
        {
            light,
            medium,
            large,
            massive
        }

        public warType curWarType = warType.attackingEnemy;

        private int curTicksAwayFromBattle = 0;

        /// <summary>
        /// numbers greater than 1 for player advantage, lesser for enemy advantage.
        /// this advantage affects the member respawns:
        /// whoever has the greater advantage tends to have priority when spawning
        /// </summary>
        private float reinforcementsAdvantage = 0.0f;

        private float spawnedMembersProportion;

        private int ticksBeforeAutoResolution = 30000, ticksSinceLastCarSpawn = 0, minTicksBetweenCarSpawns = 20;

        //balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
        //we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
        private int ticksSinceLastBalanceCheck = 0, ticksBetweenBalanceChecks = 8;

        private int initialEnemyReinforcements = 0, maxSpawnedAllies, maxSpawnedEnemies;

        private float spawnedAllies = 0, spawnedEnemies = 0;

        public TurfZone warZone;

        public Gang enemyGang;

        public static GangWarManager instance;

        private Blip warBlip, alliedSpawnBlip, enemySpawnBlip;

        private Vector3[] enemySpawnPoints, alliedSpawnPoints;

        private bool spawnPointsSet = false;

        public UIResText alliedNumText, enemyNumText;

        public bool shouldDisplayReinforcementsTexts = false;

        public GangWarManager()
        {
            instance = this;
            this.Tick += OnTick;
            enemySpawnPoints = new Vector3[3];
            alliedSpawnPoints = new Vector3[3];
            alliedNumText = new UIResText("400", new Point(576, 10), 0.5f, Color.CadetBlue);
            enemyNumText = new UIResText("400", new Point(800, 10), 0.5f, Color.Red);

            alliedNumText.Outline = true;
            enemyNumText.Outline = true;
        }

        

        public bool StartWar(Gang enemyGang, TurfZone warZone, warType theWarType, attackStrength attackStrength)
        {
            //TODO disable wars during missions
            if (!isOccurring || enemyGang == GangManager.instance.PlayerGang)
            {
                this.enemyGang = enemyGang;
                this.warZone = warZone;
                this.curWarType = theWarType;

                spawnPointsSet = false;

                warBlip = World.CreateBlip(warZone.zoneBlipPosition);
                warBlip.IsFlashing = true;
                warBlip.Sprite = BlipSprite.Deathmatch;
                warBlip.Color = BlipColor.Red;
                
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;

                if(theWarType == warType.attackingEnemy)
                {
                    alliedReinforcements = GangManager.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, attackStrength);
                    enemyReinforcements = GangManager.CalculateDefenderReinforcements(enemyGang, warZone);
                }
                else
                {
                    alliedReinforcements = GangManager.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone);
                    enemyReinforcements = GangManager.CalculateAttackerReinforcements(enemyGang, attackStrength);
                }

                alliedNumText.Caption = alliedReinforcements.ToString();
                enemyNumText.Caption = enemyReinforcements.ToString();

                initialEnemyReinforcements = enemyReinforcements;

                reinforcementsAdvantage = alliedReinforcements / (float) enemyReinforcements;

                spawnedAllies = GangManager.instance.GetSpawnedMembersOfGang(GangManager.instance.PlayerGang).Count;
                spawnedEnemies = GangManager.instance.GetSpawnedMembersOfGang(enemyGang).Count;

                maxSpawnedAllies = (int) (RandoMath.Max((ModOptions.instance.spawnedMemberLimit / 2) * reinforcementsAdvantage, 5));
                maxSpawnedEnemies = RandoMath.Max(ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies, 5);

                isOccurring = true;

                if(World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 100)
                {
                    SetSpawnPoints(warZone.zoneBlipPosition);
                }

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (theWarType == warType.attackingEnemy)
                {
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");
                }
                else
                {
                    UI.Notify(string.Concat("The " , enemyGang.name , " are attacking " , warZone.zoneName , "! They are ",
                        GangManager.CalculateAttackerReinforcements(enemyGang, attackStrength).ToString(),
                        " against our ",
                        GangManager.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone).ToString()));
                }

                return true;
            }
            else
            {
                return false;
            }
            

        }

        public void ForceSetAlliedSpawnPoint(Vector3 targetBasePosition)
        {
            alliedSpawnPoints[0] = targetBasePosition;

            for (int i = 1; i < 3; i++)
            {
                alliedSpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(alliedSpawnPoints[0], 10, 3, 20);
            }

            if (alliedSpawnBlip != null)
            {
                alliedSpawnBlip.Position = alliedSpawnPoints[0];
            }
        }

        public void ReplaceEnemySpawnPoint()
        {
            Vector3 currentSpawnPoint = enemySpawnPoints[0];

            enemySpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(warZone.zoneBlipPosition,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), 5,
                30, alliedSpawnPoints[0], ModOptions.instance.minDistanceMemberSpawnFromPlayer);

            if(enemySpawnPoints[0] == Vector3.Zero)
            {
                //we failed to get a new point, lets keep the last one
                enemySpawnPoints[0] = currentSpawnPoint;
            }

            for (int i = 1; i < 3; i++)
            {
                enemySpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(enemySpawnPoints[0], 10, 3, 20);
            }

            if(enemySpawnBlip != null)
            {
                enemySpawnBlip.Position = enemySpawnPoints[0];
            }
        }

        void SetSpawnPoints(Vector3 initialReferencePoint)
        {
            //spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos
            //the defenders' spawn point should be closer to the war blip than the attacker
            if(curWarType == warType.defendingFromEnemy)
            {
                alliedSpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                50, 5);
                enemySpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
                30, alliedSpawnPoints[0], ModOptions.instance.maxDistanceMemberSpawnFromPlayer);
            }
            else
            {
                enemySpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                50, 5, repulsor: Game.Player.Character.Position, minDistanceFromRepulsor: 30);
                alliedSpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(initialReferencePoint,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
                30, enemySpawnPoints[0], ModOptions.instance.maxDistanceMemberSpawnFromPlayer);
            }
            
            for (int i = 1; i < 3; i++)
            {
                alliedSpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(alliedSpawnPoints[0], 20, 10, 20);
            }

            
            for (int i = 1; i < 3; i++)
            {
                enemySpawnPoints[i] = GangManager.instance.FindCustomSpawnPoint(enemySpawnPoints[0], 20, 10, 20);
            }

            //and the spawn point blips, so that we don't have to hunt where our troops will come from
            alliedSpawnBlip = World.CreateBlip(alliedSpawnPoints[0]);
            alliedSpawnBlip.Sprite = BlipSprite.PickupSpawn;
            alliedSpawnBlip.Scale = 1.55f;
            Function.Call(Hash.SET_BLIP_COLOUR, alliedSpawnBlip, GangManager.instance.PlayerGang.blipColor);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War: ", GangManager.instance.PlayerGang.name, " spawn point"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, alliedSpawnBlip);

            enemySpawnBlip = World.CreateBlip(enemySpawnPoints[0]);
            enemySpawnBlip.Sprite = BlipSprite.PickupSpawn;
            enemySpawnBlip.Scale = 1.55f;
            Function.Call(Hash.SET_BLIP_COLOUR, enemySpawnBlip, enemyGang.blipColor);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("Gang War: ", enemyGang.name, " spawn point"));
            Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, enemySpawnBlip);

            

            if (alliedSpawnPoints[0] != Vector3.Zero &&
                enemySpawnPoints[0] != Vector3.Zero)
            {
                spawnPointsSet = true;
            }
            else
            {
                //we probably failed to place spawn points properly.
                //we will try placing the spawn points again in the next tick
                alliedSpawnBlip.Remove();
                enemySpawnBlip.Remove();
            }
            
        }

        /// <summary>
        /// checks both gangs' situations and the amount of reinforcements left for each side.
        /// also considers their strength (with variations) in order to decide the likely outcome of this battle.
        /// returns true for a player victory and false for a defeat
        /// </summary>
        public bool SkipWar(float playerGangStrengthFactor = 1.0f)
        {
            //if the player was out of reinforcements, it's a defeat, no matter what
            if (alliedReinforcements <= 0)
            {
                return false;
            }

            int alliedBaseStr = GangManager.instance.PlayerGang.GetGangVariedStrengthValue(),
                enemyBaseStr = enemyGang.GetGangVariedStrengthValue();
            //the amount of reinforcements counts here
            float totalAlliedStrength = alliedBaseStr * playerGangStrengthFactor +
                RandoMath.Max(4, alliedBaseStr / 100) * alliedReinforcements,
                totalEnemyStrength = enemyBaseStr +
                RandoMath.Max(4, enemyBaseStr / 100) * enemyReinforcements;

            bool itsAVictory = totalAlliedStrength > totalEnemyStrength;

            float strengthProportion = totalAlliedStrength / totalEnemyStrength;

            string battleReport = "Battle report: We";

            //we attempt to provide a little report on what happened
            if (itsAVictory)
            {
                battleReport = string.Concat(battleReport, " won the battle against the ", enemyGang.name, "! ");

                if (strengthProportion > 2f)
                {
                    battleReport = string.Concat(battleReport, "They were crushed!");
                }
                else if (strengthProportion > 1.75f)
                {
                    battleReport = string.Concat(battleReport, "We had the upper hand and they didn't have much of a chance!");
                }
                else if (strengthProportion > 1.5f)
                {
                    battleReport = string.Concat(battleReport, "We fought well and took them down.");
                }
                else if (strengthProportion > 1.25f)
                {
                    battleReport = string.Concat(battleReport, "They tried to resist, but we got them.");
                }
                else
                {
                    battleReport = string.Concat(battleReport, "It was a tough battle, but we prevailed in the end.");
                }
            }
            else
            {
                battleReport = string.Concat(battleReport, " lost the battle against the ", enemyGang.name, ". ");

                if (strengthProportion < 0.5f)
                {
                    battleReport = string.Concat(battleReport, "We were crushed!");
                }
                else if (strengthProportion < 0.625f)
                {
                    battleReport = string.Concat(battleReport, "They had the upper hand and we had no chance!");
                }
                else if (strengthProportion < 0.75f)
                {
                    battleReport = string.Concat(battleReport, "They fought well and we had to retreat.");
                }
                else if (strengthProportion < 0.875f)
                {
                    battleReport = string.Concat(battleReport, "We did our best, but couldn't put them down.");
                }
                else
                {
                    battleReport = string.Concat(battleReport, "We almost won, but in the end, we were defeated.");
                }
            }

            UI.Notify(battleReport);

            return itsAVictory;
        }

        public void EndWar(bool playerVictory)
        {
            if (playerVictory)
            {
                int battleProfit = GangManager.CalculateBattleRewards(enemyGang, curWarType == warType.attackingEnemy);
                GangManager.instance.AddOrSubtractMoneyToProtagonist
                    (battleProfit);

                UI.Notify("Victory rewards: $" + battleProfit.ToString());

                if (curWarType == warType.attackingEnemy)
                {
                    GangManager.instance.PlayerGang.TakeZone(warZone);
                    
                    UI.ShowSubtitle(warZone.zoneName + " is now ours!");
                }
                else
                {
                    UI.ShowSubtitle(warZone.zoneName + " remains ours!");
                    
                }
            }
            else
            {
                enemyGang.moneyAvailable += (int)
                    (GangManager.CalculateBattleRewards(GangManager.instance.PlayerGang, curWarType != warType.attackingEnemy) *
                    ModOptions.instance.extraProfitForAIGangsFactor);
                if (curWarType == warType.attackingEnemy)
                {
                    UI.ShowSubtitle("We've lost this battle. They keep the turf.");
                }
                else
                {
                    enemyGang.TakeZone(warZone);
                    UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
                }
            }

            CheckIfBattleWasUnfair();
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ScreenFlash", "WastedSounds");
            warBlip.Remove();
            shouldDisplayReinforcementsTexts = false;
            isOccurring = false;
            AmbientGangMemberSpawner.instance.enabled = true;
            GangManager.instance.GetGangAI(enemyGang).ticksSinceLastFightWithPlayer = 0;

            if (alliedSpawnBlip != null)
            {
                alliedSpawnBlip.Remove();
                enemySpawnBlip.Remove();
            }
            
        }

        void CheckIfBattleWasUnfair()
        {
            //the battle was unfair if the player's gang had guns and the enemy gang hadn't
            //in this case, there is a possibility of the defeated gang instantly getting pistols
            //in order to at least not get decimated all the time

            if(enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
                GangManager.instance.PlayerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) != WeaponHash.Unarmed)
            {
                if (RandoMath.RandomBool())
                {
                    enemyGang.gangWeaponHashes.Add(RandoMath.GetRandomElementFromList(ModOptions.instance.driveByWeapons));
                    enemyGang.gangWeaponHashes.Sort(enemyGang.CompareGunsByPrice);
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
            if (!isFriendly && spawnedEnemies - 4 < maxSpawnedEnemies)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(enemyGang,
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true, IncrementEnemiesCount);
            }
            else if(spawnedAllies - 4 < maxSpawnedAllies)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                    spawnPos, GangManager.instance.FindGoodSpawnPointForCar(), true, false, true, IncrementAlliesCount);
            }
            
            if (spawnedVehicle != null)
            {
                spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver).Task.DriveTo(spawnedVehicle, Game.Player.Character.Position, 25, 100);
                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver), 4457020); //ignores roads, avoids obstacles
            }
        }

        public void SpawnMember(bool isFriendly)
        {
            Vector3 spawnPos = isFriendly ? 
                RandoMath.GetRandomElementFromArray(alliedSpawnPoints) : RandoMath.GetRandomElementFromArray(enemySpawnPoints);
            Ped spawnedMember = null;

            if (isFriendly)
            {
                GangManager.instance.SpawnGangMember(GangManager.instance.PlayerGang, spawnPos, onSuccessfulMemberSpawn: IncrementAlliesCount);
            }
            else
            {
                if (spawnedEnemies < maxSpawnedEnemies)
                    GangManager.instance.SpawnGangMember(enemyGang, spawnPos, onSuccessfulMemberSpawn: IncrementEnemiesCount);
                else return;
            }
                

            if (spawnedMember != null)
            {
                if (isFriendly)
                {
                    spawnedMember.Task.RunTo(Game.Player.Character.Position);
                }
                else
                {
                    spawnedMember.Task.FightAgainst(Game.Player.Character);
                }
                
            }
        }

        void IncrementAlliesCount() { spawnedAllies++; }

        void IncrementEnemiesCount() { spawnedEnemies++; }

        public void OnEnemyDeath()
        {
            //check if the player was in or near the warzone when the death happened 
            if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 600){
                enemyReinforcements--;

                //have we lost too many? its a victory for the player then
                if(enemyReinforcements <= 0)
                {
                    EndWar(true);
                }
                else
                {
                    //UI.ShowSubtitle(enemyReinforcements.ToString() + " kills remaining!", 900);
                    enemyNumText.Caption = enemyReinforcements.ToString();
                    //if we've lost too many people since the last time we changed spawn points,
                    //change them again!
                    if(initialEnemyReinforcements - enemyReinforcements > 0 &&
                        ModOptions.instance.killsBetweenEnemySpawnReplacement > 0 &&
                        enemyReinforcements % ModOptions.instance.killsBetweenEnemySpawnReplacement == 0)
                    {
                        ReplaceEnemySpawnPoint();
                    }
                }

            }
        }

        public void OnAllyDeath(bool itWasThePlayer = false)
        {
            //check if the player was in or near the warzone when the death happened 
            if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 600)
            {
                alliedReinforcements = RandoMath.Max(alliedReinforcements - 1, 0);

                alliedNumText.Caption = alliedReinforcements.ToString();
                //we can't lose by running out of reinforcements only.
                //the player must fall or the war be skipped for it to end as a defeat

                if (alliedReinforcements >= 0)
                {
                    //UI.ShowSubtitle(alliedReinforcements.ToString() + " of us remain!", 900);
                }
                else
                {
                    if (itWasThePlayer)
                    {
                        //then it's a defeat
                        EndWar(false);
                    }
                }
            }
        }

        public void TryWarBalancing(bool cullFriendlies)
        {
            List<SpawnedGangMember> spawnedMembers = 
                GangManager.instance.GetSpawnedMembersOfGang(cullFriendlies ? GangManager.instance.PlayerGang : enemyGang);

            for(int i = 0; i < spawnedMembers.Count; i++)
            {
                if(!Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, spawnedMembers[i].watchedPed, false) &&
                    World.GetDistance(Game.Player.Character.Position, spawnedMembers[i].watchedPed.Position) >
                ModOptions.instance.minDistanceMemberSpawnFromPlayer && !spawnedMembers[i].watchedPed.IsOnScreen)
                {
                    spawnedMembers[i].Die();
                    //make sure we don't exagerate!
                    //stop if we're back inside the limits
                    if ((cullFriendlies && spawnedAllies < maxSpawnedAllies) ||
                        (!cullFriendlies && spawnedEnemies < maxSpawnedEnemies)){
                        break;
                    }
                }
            }
        }

        public void DecrementSpawnedsNumber(bool memberWasFriendly)
        {
            if (memberWasFriendly)
            {
                spawnedAllies--;
                if (spawnedAllies < 0) spawnedAllies = 0;
            }
            else {
                spawnedEnemies--;
                if (spawnedEnemies < 0) spawnedEnemies = 0;
            }
        }

        void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                if (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 300)
                {

                    shouldDisplayReinforcementsTexts = true;
                    ticksSinceLastCarSpawn++;
                    ticksSinceLastBalanceCheck++;
                    curTicksAwayFromBattle = 0;
                    Game.WantedMultiplier = 0;

                    AmbientGangMemberSpawner.instance.enabled = false;

                    if (ModOptions.instance.emptyZoneDuringWar)
                    {
                        Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                        Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                    }

                    if (RandoMath.RandomBool() && ticksSinceLastCarSpawn > minTicksBetweenCarSpawns)
                    {
                        SpawnAngryVehicle(false);

                        if(curWarType == warType.defendingFromEnemy && RandoMath.RandomBool() && alliedReinforcements > 0)
                        {
                            SpawnAngryVehicle(true); //automatic backup for us
                        }

                        ticksSinceLastCarSpawn = 0;
                    }

                    if(ticksSinceLastBalanceCheck > ticksBetweenBalanceChecks)
                    {
                        ticksSinceLastBalanceCheck = 0;
                        if(spawnedAllies > maxSpawnedAllies)
                        {
                            //try removing some members that can't currently be seen by the player or are far enough
                            TryWarBalancing(true);
                        }else if(spawnedEnemies > maxSpawnedEnemies)
                        {
                            TryWarBalancing(false);
                        }
                    }

                    if (!spawnPointsSet) SetSpawnPoints(warZone.zoneBlipPosition);

                    spawnedMembersProportion = spawnedAllies / RandoMath.Max(spawnedEnemies, 1.0f);

                    //if the allied side is out of reinforcements, no more allies will be spawned by this system.
                    //it won't be a defeat, however, until the player dies
                    if(GangManager.instance.livingMembersCount < ModOptions.instance.spawnedMemberLimit - ModOptions.instance.numSpawnsReservedForCarsDuringWars)
                    {
                        SpawnMember(alliedReinforcements > 0 && spawnedMembersProportion < reinforcementsAdvantage && spawnedAllies < maxSpawnedAllies);
                    }

                    Wait(400);
                }
                else
                {
                    shouldDisplayReinforcementsTexts = false;
                    curTicksAwayFromBattle++;
                    AmbientGangMemberSpawner.instance.enabled = true;
                    if (curTicksAwayFromBattle > ticksBeforeAutoResolution)
                    {
                        EndWar(SkipWar(0.75f));
                    }
                }
                //if the player's gang leader is dead...
                if (!Game.Player.IsAlive && !GangManager.instance.hasChangedBody)
                {
                    //the war ends, but the outcome depends on how well the player's side was doing
                    EndWar(SkipWar());
                    return;
                }
            }
        }
    }
}
