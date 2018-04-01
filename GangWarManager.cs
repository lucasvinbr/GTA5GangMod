using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;
using NativeUI;
using System.Drawing;
using GTA;

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

        private int ticksBeforeAutoResolution = 30000, ticksSinceLastCarSpawn = 0, minTicksBetweenCarSpawns = 20, ticksSinceLastEnemyRelocation = 0;

        //balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
        //we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
        private int ticksSinceLastBalanceCheck = 0, ticksBetweenBalanceChecks = 8;

        private int initialEnemyReinforcements = 0, maxSpawnedAllies, maxSpawnedEnemies;

        private float spawnedAllies = 0, spawnedEnemies = 0;

        //this counter should help culling those enemy drivers that get stuck and count towards the enemy's numbers without being helpful
        public List<SpawnedGangMember> enemiesInsideCars;

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
            this.Aborted += OnAbort;
            enemySpawnPoints = new Vector3[3];
            alliedSpawnPoints = new Vector3[3];


            alliedNumText = new UIResText("400", new Point(), 0.5f, Color.CadetBlue);
            enemyNumText = new UIResText("400", new Point(), 0.5f, Color.Red);

            alliedNumText.Outline = true;
            enemyNumText.Outline = true;

            alliedNumText.TextAlignment = UIResText.Alignment.Centered;
            enemyNumText.TextAlignment = UIResText.Alignment.Centered;
        }


        #region start/end/skip war
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
                warBlip.Position += Vector3.WorldUp * 5; //an attempt to make the war blip be drawn over the zone blip
                
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

                curTicksAwayFromBattle = 0;
                enemiesInsideCars = GangManager.instance.GetSpawnedMembersOfGang(enemyGang, true);

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

                float screenRatio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;

                int proportionalScreenWidth = (int)(1080 * screenRatio); //nativeUI UIResText works with 1080p height

                alliedNumText.Position = new Point((proportionalScreenWidth / 2) - 120, 10);
                enemyNumText.Position = new Point((proportionalScreenWidth / 2) + 120, 10);

                alliedNumText.Caption = alliedReinforcements.ToString();
                enemyNumText.Caption = enemyReinforcements.ToString();

                initialEnemyReinforcements = enemyReinforcements;

                reinforcementsAdvantage = alliedReinforcements / (float) enemyReinforcements;

                spawnedAllies = GangManager.instance.GetSpawnedMembersOfGang(GangManager.instance.PlayerGang).Count;
                spawnedEnemies = GangManager.instance.GetSpawnedMembersOfGang(enemyGang).Count;

                maxSpawnedAllies = (int) (RandoMath.Max((ModOptions.instance.spawnedMemberLimit / 2) * reinforcementsAdvantage, 5));
                maxSpawnedEnemies = RandoMath.Max(ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies, 5);

                isOccurring = true;

                //if the enemy is the attacker, try placing spawn points around the zone's blip position, which should be a good reference point
                if(theWarType == warType.defendingFromEnemy &&
                    (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 100))
                {
                    SetSpawnPoints(warZone.zoneBlipPosition);
                }

                

                //BANG-like sound
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

                if (theWarType == warType.attackingEnemy)
                {
                    UI.ShowSubtitle("The " + enemyGang.name + " are coming!");

                    //if we are attacking, set spawns around the player!
                    if (!spawnPointsSet)
                    {
                        SetSpawnPoints(Game.Player.Character.Position);
                    }
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

                AmbientGangMemberSpawner.instance.postWarBackupsRemaining = ModOptions.instance.postWarBackupsAmount;
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

        #endregion

        #region spawn point setup

        public void ForceSetAlliedSpawnPoints(Vector3 targetBasePosition)
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

            if (!spawnPointsSet)
            {
                ReplaceEnemySpawnPoint(alliedSpawnPoints[0], 20);

                if(alliedSpawnBlip == null)
                {
                    CreateSpawnPointBlip(false);
                }

                if (enemySpawnBlip == null)
                {
                    CreateSpawnPointBlip(true);
                }

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
        }

        public void SetSpecificAlliedSpawnPoint(int spawnIndex, Vector3 targetPos)
        {
            alliedSpawnPoints[spawnIndex] = targetPos;
        }

        public void ReplaceEnemySpawnPoint(Vector3 referencePoint, int minDistanceFromReference = 5)
        {
            Vector3 currentSpawnPoint = enemySpawnPoints[0];

            enemySpawnPoints[0] = GangManager.instance.FindCustomSpawnPoint(referencePoint,
                ModOptions.instance.GetAcceptableMemberSpawnDistance(), minDistanceFromReference,
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

            ticksSinceLastEnemyRelocation = 0;
        }

        void SetSpawnPoints(Vector3 initialReferencePoint)
        {
            //spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos
            //the defenders' spawn point should be closer to the reference point than the attacker
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
            CreateSpawnPointBlip(false);

            CreateSpawnPointBlip(true);

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

        void CreateSpawnPointBlip(bool enemySpawn)
        {
            if (enemySpawn)
            {
                enemySpawnBlip = World.CreateBlip(enemySpawnPoints[0]);

                enemySpawnBlip.Sprite = BlipSprite.PickupSpawn;
                enemySpawnBlip.Scale = 1.55f;
                Function.Call(Hash.SET_BLIP_COLOUR, enemySpawnBlip, enemyGang.blipColor);

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
                    string.Concat("Gang War: ", enemyGang.name, " spawn point"));
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, enemySpawnBlip);
            }
            else
            {
                alliedSpawnBlip = World.CreateBlip(alliedSpawnPoints[0]);

                alliedSpawnBlip.Sprite = BlipSprite.PickupSpawn;
                alliedSpawnBlip.Scale = 1.55f;
                Function.Call(Hash.SET_BLIP_COLOUR, alliedSpawnBlip, GangManager.instance.PlayerGang.blipColor);

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
                    string.Concat("Gang War: ", GangManager.instance.PlayerGang.name, " spawn point"));
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, alliedSpawnBlip);
            }
        }

        #endregion

       
        /// <summary>
        ///    the battle was unfair if the player's gang had guns and the enemy gang hadn't
        ///    in this case, there is a possibility of the defeated gang instantly getting pistols
        ///    in order to at least not get decimated all the time
        /// </summary>
        void CheckIfBattleWasUnfair()
        {


            if (enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
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
        public SpawnedDrivingGangMember SpawnAngryVehicle(bool isFriendly)
        {
            Math.Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForCar(),
                playerPos = Game.Player.Character.Position;

            SpawnedDrivingGangMember spawnedVehicle = null;
            if (!isFriendly && spawnedEnemies - 4 < maxSpawnedEnemies)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(enemyGang,
                    spawnPos, playerPos, true, IncrementEnemiesCount);
            }
            else if(spawnedAllies - 4 < maxSpawnedAllies)
            {
                spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
                    spawnPos, playerPos, true, IncrementAlliesCount);
            }

            return spawnedVehicle;
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
            if (IsPlayerCloseToWar())
            {
                enemyReinforcements--;

                //have we lost too many? its a victory for the player then
                if(enemyReinforcements <= 0)
                {
                    EndWar(true);
                }
                else
                {
                    enemyNumText.Caption = enemyReinforcements.ToString();
                    //if we've lost too many people since the last time we changed spawn points,
                    //change them again!
                    if(initialEnemyReinforcements - enemyReinforcements > 0 &&
                        ModOptions.instance.killsBetweenEnemySpawnReplacement > 0 &&
                        enemyReinforcements % ModOptions.instance.killsBetweenEnemySpawnReplacement == 0)
                    {
                        if(RandoMath.RandomBool() && spawnPointsSet)
                        {
                            ReplaceEnemySpawnPoint(alliedSpawnPoints[0], 20);
                        }
                        else
                        {
                            ReplaceEnemySpawnPoint(warZone.zoneBlipPosition);
                        }
                        
                    }
                }

            }
        }

        public void OnAllyDeath(bool itWasThePlayer = false)
        {
            //check if the player was in or near the warzone when the death happened 
            if (IsPlayerCloseToWar())
            {
                alliedReinforcements = RandoMath.Max(alliedReinforcements - 1, 0);

                alliedNumText.Caption = alliedReinforcements.ToString();
                //we can't lose by running out of reinforcements only.
                //the player must fall or the war be skipped for it to end as a defeat

                if (alliedReinforcements > 0)
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
                if (spawnedMembers[i].watchedPed == null) continue;
                //don't attempt to cull a friendly driving member because they could be a backup car called by the player...
                //and the player can probably take more advantage of any stuck friendly vehicle than the AI can
                if((!cullFriendlies || !Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, spawnedMembers[i].watchedPed, false)) &&
                    World.GetDistance(Game.Player.Character.Position, spawnedMembers[i].watchedPed.Position) >
                ModOptions.instance.minDistanceMemberSpawnFromPlayer && !spawnedMembers[i].watchedPed.IsOnScreen)
                {
                    spawnedMembers[i].Die(true);
                    //make sure we don't exagerate!
                    //stop if we're back inside the limits
                    if ((cullFriendlies && spawnedAllies < maxSpawnedAllies) ||
                        (!cullFriendlies && spawnedEnemies < maxSpawnedEnemies)){
                        break;
                    }
                }

                Yield();
            }
        }

        /// <summary>
        /// sometimes, too many enemy drivers get stuck with passengers, which causes quite a heavy impact on how many enemy foot members spawn.
        /// This is an attempt to circumvent that, hehe
        /// </summary>
        public void CullEnemyVehicles()
        {
            for (int i = 0; i < enemiesInsideCars.Count; i++)
            {
                if (enemiesInsideCars[i].watchedPed != null &&
                    World.GetDistance(Game.Player.Character.Position, enemiesInsideCars[i].watchedPed.Position) >
                ModOptions.instance.minDistanceMemberSpawnFromPlayer && !enemiesInsideCars[i].watchedPed.IsOnScreen)
                {
                    enemiesInsideCars[i].Die(true);
                    
                    //make sure we don't exagerate!
                    //stop if we're back inside a tolerable limit
                    if (spawnedEnemies < ModOptions.instance.numSpawnsReservedForCarsDuringWars * 1.5f)
                    {
                        break;
                    }
                }

                Yield();
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

        /// <summary>
        /// true if the player is in the war zone or close enough to the zone blip
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerCloseToWar()
        {
            return (World.GetZoneName(Game.Player.Character.Position) == warZone.zoneName ||
                World.GetDistance(Game.Player.Character.Position, warZone.zoneBlipPosition) < 
                ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
        }

        void OnTick(object sender, EventArgs e)
        {
            if (isOccurring)
            {
                if (IsPlayerCloseToWar())
                {

                    shouldDisplayReinforcementsTexts = true;
                    ticksSinceLastCarSpawn++;
                    ticksSinceLastBalanceCheck++;
                    ticksSinceLastEnemyRelocation++;
                    curTicksAwayFromBattle = 0;
                    Game.WantedMultiplier = 0;

                    AmbientGangMemberSpawner.instance.enabled = false;

                    
                    if (ticksSinceLastCarSpawn > minTicksBetweenCarSpawns && RandoMath.RandomBool())
                    {
                        SpawnAngryVehicle(false);

                        if (curWarType == warType.defendingFromEnemy && RandoMath.RandomBool() && alliedReinforcements > 0)
                        {
                            Yield();
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
                        }
                        else if(spawnedEnemies > maxSpawnedEnemies)
                        {
                            TryWarBalancing(false);
                        }

                        //cull enemies inside cars if there are too many!
                        enemiesInsideCars = GangManager.instance.GetSpawnedMembersOfGang(enemyGang, true);

                        if (enemiesInsideCars.Count > 
                            RandoMath.Max(maxSpawnedEnemies / 3, ModOptions.instance.numSpawnsReservedForCarsDuringWars * 2))
                        {
                            CullEnemyVehicles();
                        }
                    }

                    if (!spawnPointsSet) SetSpawnPoints(warZone.zoneBlipPosition);
                    else if (ticksSinceLastEnemyRelocation > ModOptions.instance.ticksBetweenEnemySpawnReplacement) ReplaceEnemySpawnPoint(alliedSpawnPoints[0]);

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
                        EndWar(SkipWar(0.65f));
                    }
                }
                //if the player's gang leader is dead...
                if (!Game.Player.IsAlive && !GangManager.instance.HasChangedBody)
                {
                    //the war ends, but the outcome depends on how well the player's side was doing
                    EndWar(SkipWar(0.9f));
                    return;
                }
            }
        }

        void OnAbort(object sender, EventArgs e)
        {
            if(warBlip != null)
            {
                warBlip.Remove();
            }
            
            if (alliedSpawnBlip != null)
            {
                alliedSpawnBlip.Remove();
            }

            if (enemySpawnBlip != null)
            {
                enemySpawnBlip.Remove();
            }
        }
    }
}
