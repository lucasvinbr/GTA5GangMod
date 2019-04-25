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

namespace GTA.GangAndTurfMod {
	public class GangWarManager : Script {

		public int enemyReinforcements, alliedReinforcements;

		public bool isOccurring = false;

		public enum WarType {
			attackingEnemy,
			defendingFromEnemy
		}

		public enum AttackStrength {
			light,
			medium,
			large,
			massive
		}

		public WarType curWarType = WarType.attackingEnemy;

		private int curTicksAwayFromBattle = 0;

		/// <summary>
		/// numbers greater than 1 for player advantage, lesser for enemy advantage.
		/// this advantage affects the member respawns:
		/// whoever has the greater advantage tends to have priority when spawning
		/// </summary>
		private float reinforcementsAdvantage = 0.0f;

		private float spawnedMembersProportion;

		private const int MIN_TICKS_BETWEEN_CAR_SPAWNS = 20;

		private int ticksSinceLastCarSpawn = 0, ticksSinceLastEnemyRelocation = 0;

		//balance checks are what tries to ensure that reinforcement advantage is something meaningful in battle.
		//we try to reduce the amount of spawned members of one gang if they were meant to have less members defending/attacking than their enemy
		private const int TICKS_BETWEEN_BALANCE_CHECKS = 8;

		private int ticksSinceLastBalanceCheck = 0;

		private int timeLastWarAgainstPlayer = 0;

		private int initialEnemyReinforcements = 0, maxSpawnedAllies, maxSpawnedEnemies;

		private float spawnedAllies = 0, spawnedEnemies = 0;

		//this counter should help culling those enemy drivers that get stuck and count towards the enemy's numbers without being helpful
		public List<SpawnedGangMember> enemiesInsideCars;

		public TurfZone warZone;

		public bool playerNearWarzone = false;

		public Gang enemyGang;

		private Blip warBlip, warAreaBlip, enemySpawnBlip;

		private Blip[] alliedSpawnBlips;

		public Vector3[] enemySpawnPoints, alliedSpawnPoints;

		private bool spawnPointsSet = false;

		private AttackStrength curWarAtkStrength = AttackStrength.light;

		public UIResText alliedNumText, enemyNumText;

		public bool shouldDisplayReinforcementsTexts = false;

		public static GangWarManager instance;

		public GangWarManager() {
			instance = this;
			this.Tick += OnTick;
			this.Aborted += OnAbort;
			enemySpawnPoints = new Vector3[3];
			alliedSpawnPoints = new Vector3[3];
			alliedSpawnBlips = new Blip[3];


			alliedNumText = new UIResText("400", new Point(), 0.5f, Color.CadetBlue);
			enemyNumText = new UIResText("400", new Point(), 0.5f, Color.Red);

			alliedNumText.Outline = true;
			enemyNumText.Outline = true;

			alliedNumText.TextAlignment = UIResText.Alignment.Centered;
			enemyNumText.TextAlignment = UIResText.Alignment.Centered;
		}


		#region start/end/skip war
		public bool StartWar(Gang enemyGang, TurfZone warZone, WarType theWarType, AttackStrength attackStrength) {
			if (!isOccurring || enemyGang == GangManager.instance.PlayerGang) {
				this.enemyGang = enemyGang;
				this.warZone = warZone;
				this.curWarType = theWarType;
				curWarAtkStrength = attackStrength;
				playerNearWarzone = false;
				spawnPointsSet = false;

				warBlip = World.CreateBlip(warZone.zoneBlipPosition);
				warBlip.IsFlashing = true;
				warBlip.Sprite = BlipSprite.Deathmatch;
				warBlip.Color = BlipColor.Red;

				warAreaBlip = World.CreateBlip(warZone.zoneBlipPosition,
					ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
				warAreaBlip.Sprite = BlipSprite.BigCircle;
				warAreaBlip.Color = BlipColor.Red;
				warAreaBlip.Alpha = 175;


				Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
				Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "Gang War (versus " + enemyGang.name + ")");
				Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, warBlip);

				curTicksAwayFromBattle = 0;
				enemiesInsideCars = SpawnManager.instance.GetSpawnedMembersOfGang(enemyGang, true);

				if (theWarType == WarType.attackingEnemy) {
					alliedReinforcements = GangCalculations.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, attackStrength);
					enemyReinforcements = GangCalculations.CalculateDefenderReinforcements(enemyGang, warZone);
				}
				else {
					alliedReinforcements = GangCalculations.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone);
					enemyReinforcements = GangCalculations.CalculateAttackerReinforcements(enemyGang, attackStrength);
				}

				float screenRatio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;

				int proportionalScreenWidth = (int)(1080 * screenRatio); //nativeUI UIResText works with 1080p height

				alliedNumText.Position = new Point((proportionalScreenWidth / 2) - 120, 10);
				enemyNumText.Position = new Point((proportionalScreenWidth / 2) + 120, 10);

				alliedNumText.Caption = alliedReinforcements.ToString();
				enemyNumText.Caption = enemyReinforcements.ToString();

				initialEnemyReinforcements = enemyReinforcements;

				reinforcementsAdvantage = alliedReinforcements / (float)enemyReinforcements;

				spawnedAllies = SpawnManager.instance.GetSpawnedMembersOfGang(GangManager.instance.PlayerGang).Count;
				spawnedEnemies = SpawnManager.instance.GetSpawnedMembersOfGang(enemyGang).Count;

				maxSpawnedAllies = (int)(RandoMath.Max((ModOptions.instance.spawnedMemberLimit / 2) * reinforcementsAdvantage, 5));
				maxSpawnedEnemies = RandoMath.Max(ModOptions.instance.spawnedMemberLimit - maxSpawnedAllies, 5);

				isOccurring = true;

				//BANG-like sound
				Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS");

				if (theWarType == WarType.attackingEnemy) {
					UI.ShowSubtitle("The " + enemyGang.name + " are coming!");

					//if we are attacking, set spawns around the player!
					SetSpawnPoints(MindControl.SafePositionNearPlayer);
				}
				else {
					UI.Notify(string.Concat("The ", enemyGang.name, " are attacking ", warZone.zoneName, "! They are ",
						GangCalculations.CalculateAttackerReinforcements(enemyGang, attackStrength).ToString(),
						" against our ",
						GangCalculations.CalculateDefenderReinforcements(GangManager.instance.PlayerGang, warZone).ToString()));
					//spawns are set around the zone blip if we are defending
					if (World.GetDistance(MindControl.CurrentPlayerCharacter.Position, warZone.zoneBlipPosition) < 100) {
						SetSpawnPoints(warZone.zoneBlipPosition);
					}
				}

				SetHateRelationsBetweenGangs();

				return true;
			}
			else {
				return false;
			}


		}

		/// <summary>
		/// checks both gangs' situations and the amount of reinforcements left for each side.
		/// also considers their strength (with variations) in order to decide the likely outcome of this battle.
		/// returns true for a player victory and false for a defeat
		/// </summary>
		public bool SkipWar(float playerGangStrengthFactor = 1.0f) {
			//if the player was out of reinforcements, it's a defeat, no matter what
			if (alliedReinforcements <= 0) {
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
			if (itsAVictory) {
				battleReport = string.Concat(battleReport, " won the battle against the ", enemyGang.name, "! ");

				if (strengthProportion > 2f) {
					battleReport = string.Concat(battleReport, "They were crushed!");
				}
				else if (strengthProportion > 1.75f) {
					battleReport = string.Concat(battleReport, "We had the upper hand and they didn't have much of a chance!");
				}
				else if (strengthProportion > 1.5f) {
					battleReport = string.Concat(battleReport, "We fought well and took them down.");
				}
				else if (strengthProportion > 1.25f) {
					battleReport = string.Concat(battleReport, "They tried to resist, but we got them.");
				}
				else {
					battleReport = string.Concat(battleReport, "It was a tough battle, but we prevailed in the end.");
				}
			}
			else {
				battleReport = string.Concat(battleReport, " lost the battle against the ", enemyGang.name, ". ");

				if (strengthProportion < 0.5f) {
					battleReport = string.Concat(battleReport, "We were crushed!");
				}
				else if (strengthProportion < 0.625f) {
					battleReport = string.Concat(battleReport, "They had the upper hand and we had no chance!");
				}
				else if (strengthProportion < 0.75f) {
					battleReport = string.Concat(battleReport, "They fought well and we had to retreat.");
				}
				else if (strengthProportion < 0.875f) {
					battleReport = string.Concat(battleReport, "We did our best, but couldn't put them down.");
				}
				else {
					battleReport = string.Concat(battleReport, "We almost won, but in the end, we were defeated.");
				}
			}

			UI.Notify(battleReport);

			return itsAVictory;
		}

		public void EndWar(bool playerVictory) {
			bool weWereAttacking = curWarType == WarType.attackingEnemy;
			if (playerVictory) {
				int battleProfit = GangCalculations.CalculateBattleRewards(enemyGang, weWereAttacking ? warZone.value : (int)curWarAtkStrength, weWereAttacking);
				MindControl.instance.AddOrSubtractMoneyToProtagonist
					(battleProfit);

				UI.Notify("Victory rewards: $" + battleProfit.ToString());

				if (weWereAttacking) {
					GangManager.instance.PlayerGang.TakeZone(warZone);

					UI.ShowSubtitle(warZone.zoneName + " is now ours!");
				}
				else {
					UI.ShowSubtitle(warZone.zoneName + " remains ours!");

				}

				AmbientGangMemberSpawner.instance.postWarBackupsRemaining = ModOptions.instance.postWarBackupsAmount;
			}
			else {
				enemyGang.moneyAvailable += (int)
					(GangCalculations.CalculateBattleRewards(GangManager.instance.PlayerGang, !weWereAttacking ? warZone.value : (int)curWarAtkStrength, !weWereAttacking) *
					ModOptions.instance.extraProfitForAIGangsFactor);
				if (curWarType == WarType.attackingEnemy) {
					UI.ShowSubtitle("We've lost this battle. They keep the turf.");
				}
				else {
					enemyGang.TakeZone(warZone);
					UI.ShowSubtitle(warZone.zoneName + " has been taken by the " + enemyGang.name + "!");
				}
			}

			CheckIfBattleWasUnfair();
			Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "ScreenFlash", "WastedSounds");
			warBlip.Remove();
			warAreaBlip.Remove();
			shouldDisplayReinforcementsTexts = false;
			isOccurring = false;
			playerNearWarzone = false;
			AmbientGangMemberSpawner.instance.enabled = true;

			if (!weWereAttacking) {
				//prevent the player from being attacked again too early
				timeLastWarAgainstPlayer = ModCore.curGameTime;
			}

			if (enemySpawnBlip != null) {
				enemySpawnBlip.Remove();
			}

			foreach (Blip alliedBlip in alliedSpawnBlips) {
				if (alliedBlip != null) {
					alliedBlip.Remove();
				}
			}

			//reset relations to whatever is set in modoptions
			GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);

		}

		public bool CanStartWarAgainstPlayer
		{
			get
			{
				return (ModCore.curGameTime - timeLastWarAgainstPlayer > ModOptions.instance.minMsTimeBetweenAttacksOnPlayerTurf) &&
					MindControl.CurrentPlayerCharacter.IsAlive; //starting a war against the player when we're in the "wasted" screen would instantly end it
			}
		}

		#endregion

		#region spawn point setup

		public void ForceSetAlliedSpawnPoints(Vector3 targetBasePosition) {
			alliedSpawnPoints[0] = targetBasePosition;

			for (int i = 1; i < 3; i++) {
				alliedSpawnPoints[i] = SpawnManager.instance.FindCustomSpawnPoint(alliedSpawnPoints[0], 10, 3, 20);
			}

			for (int i = 0; i < alliedSpawnBlips.Length; i++) {
				if (alliedSpawnBlips[i] != null) {
					alliedSpawnBlips[i].Position = alliedSpawnPoints[i];
				}
			}

			if (!spawnPointsSet) {
				ReplaceEnemySpawnPoint(alliedSpawnPoints[0], 20);


				for (int i = 0; i < alliedSpawnBlips.Length; i++) {
					if (alliedSpawnBlips[i] == null) {
						CreatePlayerSpawnBlip(i);
					}
				}


				if (enemySpawnBlip == null) {
					CreateSpawnPointBlip(true);
				}

				if (alliedSpawnPoints[0] != Vector3.Zero &&
				enemySpawnPoints[0] != Vector3.Zero) {
					spawnPointsSet = true;
				}
				else {
					//we probably failed to place spawn points properly.
					//we will try placing the spawn points again in the next tick
					foreach (Blip alliedBlip in alliedSpawnBlips) {
						if (alliedBlip != null) {
							alliedBlip.Remove();
						}
					}
					enemySpawnBlip.Remove();
				}
			}
		}

		public void SetSpecificAlliedSpawnPoint(int spawnIndex, Vector3 targetPos) {
			alliedSpawnPoints[spawnIndex] = targetPos;
			if (alliedSpawnBlips[spawnIndex] != null) {
				alliedSpawnBlips[spawnIndex].Position = targetPos;
			}
		}

		/// <summary>
		/// tries to replace the enemy spawn point based on the allied spawn point, returning true if succeeds
		/// </summary>
		/// <returns></returns>
		public bool ReplaceEnemySpawnPoint() {
			return ReplaceEnemySpawnPoint(alliedSpawnPoints[0], 20);
		}

		/// <summary>
		/// tries to replace the enemy spawn point based on the provided ref point, returning true if succeeds
		/// </summary>
		/// <returns></returns>
		public bool ReplaceEnemySpawnPoint(Vector3 referencePoint, int minDistanceFromReference = 5) {
			Logger.Log("enemy spawn relocation: start", 3);
			Vector3 currentSpawnPoint = enemySpawnPoints[0];

			enemySpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPointInStreet(referencePoint,
				ModOptions.instance.GetAcceptableMemberSpawnDistance(40), minDistanceFromReference,
				1, alliedSpawnPoints[0], ModOptions.instance.minDistanceMemberSpawnFromPlayer);

			if (enemySpawnPoints[0] == Vector3.Zero) {
				//we failed to get a new point, lets keep the last one
				enemySpawnPoints[0] = currentSpawnPoint;
				ticksSinceLastEnemyRelocation = 0;
				Logger.Log("enemy spawn relocation: end (fail)", 3);
				return false;
			}
			else if (alliedSpawnPoints[0] != null &&
			   World.GetDistance(enemySpawnPoints[0], alliedSpawnPoints[0]) >= ModOptions.instance.maxDistanceMemberSpawnFromPlayer) {
				//the spawn is too far from the player's gang spawn!
				//relocate
				enemySpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPoint(referencePoint,
				ModOptions.instance.GetAcceptableMemberSpawnDistance(40), minDistanceFromReference,
				1, alliedSpawnPoints[0], ModOptions.instance.minDistanceMemberSpawnFromPlayer);

				//then check again if we failed...
				if (enemySpawnPoints[0] == Vector3.Zero) {
					enemySpawnPoints[0] = currentSpawnPoint;
					ticksSinceLastEnemyRelocation = 0;
					Logger.Log("enemy spawn relocation: end (fail after getting one too far)", 3);
					return false;
				}
			}

			for (int i = 1; i < 3; i++) {
				enemySpawnPoints[i] = SpawnManager.instance.FindCustomSpawnPoint(enemySpawnPoints[0], 10, 3, 1);
			}

			if (enemySpawnBlip != null) {
				enemySpawnBlip.Position = enemySpawnPoints[0];
			}

			ticksSinceLastEnemyRelocation = 0;
			Logger.Log("enemy spawn relocation: end (ok)", 3);
			return true;
		}

		void SetSpawnPoints(Vector3 initialReferencePoint) {
			Logger.Log("setSpawnPoints: begin", 3);
			//spawn points for both sides should be a bit far from each other, so that the war isn't just pure chaos
			//the defenders' spawn point should be closer to the reference point than the attacker
			if (curWarType == WarType.defendingFromEnemy) {
				alliedSpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPoint(initialReferencePoint,
				50, 5);
				enemySpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPoint(initialReferencePoint,
				ModOptions.instance.GetAcceptableMemberSpawnDistance(30), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
				30, alliedSpawnPoints[0], ModOptions.instance.minDistanceMemberSpawnFromPlayer);
			}
			else {
				enemySpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPoint(initialReferencePoint,
				50, 5, repulsor: MindControl.CurrentPlayerCharacter.Position, minDistanceFromRepulsor: 30);
				alliedSpawnPoints[0] = SpawnManager.instance.FindCustomSpawnPoint(initialReferencePoint,
				ModOptions.instance.GetAcceptableMemberSpawnDistance(30), ModOptions.instance.minDistanceMemberSpawnFromPlayer,
				30, enemySpawnPoints[0], ModOptions.instance.minDistanceMemberSpawnFromPlayer);
			}

			//set the other spawn points and our extra spawn point blips, so that we don't have to hunt where our troops will come from

			for (int i = 1; i < 3; i++) {
				alliedSpawnPoints[i] = SpawnManager.instance.FindCustomSpawnPoint(alliedSpawnPoints[0], 20, 10, 20);
				CreatePlayerSpawnBlip(i);
			}


			for (int i = 1; i < 3; i++) {
				enemySpawnPoints[i] = SpawnManager.instance.FindCustomSpawnPoint(enemySpawnPoints[0], 20, 10, 20);
			}

			//and the base blips for both sides
			CreateSpawnPointBlip(false);
			CreateSpawnPointBlip(true);

			for (int i = 0; i < 3; i++) {
				if (alliedSpawnPoints[i] == Vector3.Zero ||
					enemySpawnPoints[i] == Vector3.Zero) {
					spawnPointsSet = false; //failed to get spawn points!
											//we will try placing the spawn points again in the next tick
					foreach (Blip alliedBlip in alliedSpawnBlips) {
						if (alliedBlip != null) {
							alliedBlip.Remove();
						}
					}
					enemySpawnBlip.Remove();
					Logger.Log("setSpawnPoints: end (fail)", 3);
					return;
				}
			}

			spawnPointsSet = true;
			Logger.Log("setSpawnPoints: end (success)", 3);
		}

		void CreateSpawnPointBlip(bool enemySpawn) {
			if (enemySpawn) {
				enemySpawnBlip = World.CreateBlip(enemySpawnPoints[0]);

				enemySpawnBlip.Sprite = BlipSprite.PickupSpawn;
				enemySpawnBlip.Scale = 1.55f;
				Function.Call(Hash.SET_BLIP_COLOUR, enemySpawnBlip, enemyGang.blipColor);

				Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
				Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
					string.Concat("Gang War: ", enemyGang.name, " spawn point"));
				Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, enemySpawnBlip);
			}
			else {
				CreatePlayerSpawnBlip(0);
			}
		}

		void CreatePlayerSpawnBlip(int spawnIndex) {
			BlipSprite blipSprite = BlipSprite.Adversary10;

			switch (spawnIndex) {
				case 1:
					blipSprite = BlipSprite.Capture2;
					break;
				case 2:
					blipSprite = BlipSprite.Capture3;
					break;
				default:
					blipSprite = BlipSprite.Capture1;
					break;
			}

			Blip theNewBlip = World.CreateBlip(alliedSpawnPoints[spawnIndex]);

			theNewBlip.Sprite = blipSprite;
			theNewBlip.Scale = 0.9f;
			Function.Call(Hash.SET_BLIP_COLOUR, theNewBlip, GangManager.instance.PlayerGang.blipColor);

			Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
			Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
				string.Concat("Gang War: ", GangManager.instance.PlayerGang.name, " spawn point (", (spawnIndex + 1).ToString(), ")"));
			Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, theNewBlip);

			alliedSpawnBlips[spawnIndex] = theNewBlip;
		}

		/// <summary>
		/// if spawns are set, returns a random spawn that can be allied or hostile
		/// </summary>
		/// <returns></returns>
		public Vector3 GetRandomSpawnPoint() {
			if (spawnPointsSet) {
				if (RandoMath.RandomBool()) {
					return RandoMath.GetRandomElementFromArray(alliedSpawnPoints);
				}
				else {
					return RandoMath.GetRandomElementFromArray(enemySpawnPoints);
				}
			}
			else {
				return Vector3.Zero;
			}

		}

		#endregion


		/// <summary>
		///    the battle was unfair if the player's gang had guns and the enemy gang hadn't
		///    in this case, there is a possibility of the defeated gang instantly getting pistols
		///    in order to at least not get decimated all the time
		/// </summary>
		void CheckIfBattleWasUnfair() {


			if (enemyGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) == WeaponHash.Unarmed &&
				GangManager.instance.PlayerGang.GetListedGunFromOwnedGuns(ModOptions.instance.driveByWeapons) != WeaponHash.Unarmed) {
				if (RandoMath.RandomBool()) {
					enemyGang.gangWeaponHashes.Add(RandoMath.GetRandomElementFromList(ModOptions.instance.driveByWeapons));
					GangManager.instance.SaveGangData(false);
				}
			}
		}

		/// <summary>
		/// spawns a vehicle that has the player as destination
		/// </summary>
		public SpawnedDrivingGangMember SpawnAngryVehicle(bool isFriendly) {
			Math.Vector3 playerPos = MindControl.SafePositionNearPlayer,
				spawnPos = SpawnManager.instance.FindGoodSpawnPointForCar(playerPos);

			if (spawnPos == Vector3.Zero) return null;

			SpawnedDrivingGangMember spawnedVehicle = null;
			if (!isFriendly && spawnedEnemies - 4 < maxSpawnedEnemies) {
				spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(enemyGang,
					spawnPos, playerPos, false, false, IncrementEnemiesCount);
			}
			else if (spawnedAllies - 4 < maxSpawnedAllies) {
				spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
					spawnPos, playerPos, false, false, IncrementAlliesCount);
			}

			return spawnedVehicle;
		}

		public void SpawnMember(bool isFriendly) {
			Vector3 spawnPos = isFriendly ?
				RandoMath.GetRandomElementFromArray(alliedSpawnPoints) : RandoMath.GetRandomElementFromArray(enemySpawnPoints);

			if (spawnPos == default(Vector3)) return; //this means we don't have spawn points set yet

			SpawnedGangMember spawnedMember = null;

			if (isFriendly) {
				if (spawnedAllies < maxSpawnedAllies) {
					spawnedMember = SpawnManager.instance.SpawnGangMember(GangManager.instance.PlayerGang, spawnPos, onSuccessfulMemberSpawn: IncrementAlliesCount);
				}
				else return;

			}
			else {
				if (spawnedEnemies < maxSpawnedEnemies) {
					spawnedMember = SpawnManager.instance.SpawnGangMember(enemyGang, spawnPos, onSuccessfulMemberSpawn: IncrementEnemiesCount);
				}
				else return;
			}
		}

		void IncrementAlliesCount() { spawnedAllies++; }

		void IncrementEnemiesCount() { spawnedEnemies++; }

		public void OnEnemyDeath() {
			//check if the player was in or near the warzone when the death happened 
			if (playerNearWarzone) {
				enemyReinforcements--;

				//have we lost too many? its a victory for the player then
				if (enemyReinforcements <= 0) {
					EndWar(true);
				}
				else {
					enemyNumText.Caption = enemyReinforcements.ToString();
					//if we've lost too many people since the last time we changed spawn points,
					//change them again!
					if (initialEnemyReinforcements - enemyReinforcements > 0 &&
						ModOptions.instance.killsBetweenEnemySpawnReplacement > 0 &&
						enemyReinforcements % ModOptions.instance.killsBetweenEnemySpawnReplacement == 0) {
						if (spawnPointsSet) {
							ReplaceEnemySpawnPoint(alliedSpawnPoints[0], 20);
						}

					}
				}

			}
		}

		public void OnAllyDeath() {
			//check if the player was in or near the warzone when the death happened 
			if (playerNearWarzone) {
				alliedReinforcements--;

				if (alliedReinforcements <= 0) {
					EndWar(false);
				}
				else {
					alliedNumText.Caption = alliedReinforcements.ToString();
				}
			}
		}

		public void TryWarBalancing(bool cullFriendlies) {
			Logger.Log("war balancing: start", 3);
			List<SpawnedGangMember> spawnedMembers =
				SpawnManager.instance.GetSpawnedMembersOfGang(cullFriendlies ? GangManager.instance.PlayerGang : enemyGang);

			for (int i = 0; i < spawnedMembers.Count; i++) {
				if (spawnedMembers[i].watchedPed == null) continue;
				//don't attempt to cull a friendly driving member because they could be a backup car called by the player...
				//and the player can probably take more advantage of any stuck friendly vehicle than the AI can
				if ((!cullFriendlies || !Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, spawnedMembers[i].watchedPed, false)) &&
					MindControl.CurrentPlayerCharacter.Position.DistanceTo2D(spawnedMembers[i].watchedPed.Position) >
				ModOptions.instance.minDistanceMemberSpawnFromPlayer && !spawnedMembers[i].watchedPed.IsOnScreen) {
					spawnedMembers[i].Die(true);
					//make sure we don't exagerate!
					//stop if we're back inside the limits
					if ((cullFriendlies && spawnedAllies < maxSpawnedAllies) ||
						(!cullFriendlies && spawnedEnemies < maxSpawnedEnemies)) {
						break;
					}
				}

				Yield();
			}

			Logger.Log("war balancing: end", 3);
		}

		/// <summary>
		/// sometimes, too many enemy drivers get stuck with passengers, which causes quite a heavy impact on how many enemy foot members spawn.
		/// This is an attempt to circumvent that, hehe
		/// </summary>
		public void CullEnemyVehicles() {
			Logger.Log("cull enemy vehs: start", 3);
			for (int i = 0; i < enemiesInsideCars.Count; i++) {
				if (enemiesInsideCars[i].watchedPed != null &&
					MindControl.CurrentPlayerCharacter.Position.DistanceTo2D(enemiesInsideCars[i].watchedPed.Position) >
				ModOptions.instance.minDistanceMemberSpawnFromPlayer && !enemiesInsideCars[i].watchedPed.IsOnScreen) {
					enemiesInsideCars[i].Die(true);

					//make sure we don't exagerate!
					//stop if we're back inside a tolerable limit
					if (spawnedEnemies < ModOptions.instance.numSpawnsReservedForCarsDuringWars * 1.5f) {
						break;
					}
				}

				Yield();
			}
			Logger.Log("cull enemy vehs: end", 3);
		}

		public void DecrementSpawnedsNumber(bool memberWasFriendly) {
			if (memberWasFriendly) {
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
		public bool IsPlayerCloseToWar() {
			return (World.GetZoneName(MindControl.CurrentPlayerCharacter.Position) == warZone.zoneName ||
				warZone.zoneBlipPosition.DistanceTo2D(MindControl.CurrentPlayerCharacter.Position) <
				ModOptions.instance.maxDistToWarBlipBeforePlayerLeavesWar);
		}

		/// <summary>
		/// forces the hate relation level between the involved gangs (includes the player)
		/// </summary>
		public void SetHateRelationsBetweenGangs() {
			World.SetRelationshipBetweenGroups(Relationship.Hate, enemyGang.relationGroupIndex, GangManager.instance.PlayerGang.relationGroupIndex);
			World.SetRelationshipBetweenGroups(Relationship.Hate, GangManager.instance.PlayerGang.relationGroupIndex, enemyGang.relationGroupIndex);
			World.SetRelationshipBetweenGroups(Relationship.Hate, enemyGang.relationGroupIndex, Game.Player.Character.RelationshipGroup);
			World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, enemyGang.relationGroupIndex);
		}

		void OnTick(object sender, EventArgs e) {
			if (isOccurring) {
				if (IsPlayerCloseToWar()) {
					Logger.Log("warmanager inside war tick: begin. spAllies: " + spawnedAllies.ToString() + " spEnemies: " + spawnedEnemies.ToString(), 5);
					playerNearWarzone = true;
					shouldDisplayReinforcementsTexts = true;
					ticksSinceLastCarSpawn++;
					ticksSinceLastBalanceCheck++;
					ticksSinceLastEnemyRelocation++;
					curTicksAwayFromBattle = 0;

					if (ModOptions.instance.freezeWantedLevelDuringWars) {
						Game.WantedMultiplier = 0;
					}

					AmbientGangMemberSpawner.instance.enabled = false;


					if (ticksSinceLastCarSpawn > MIN_TICKS_BETWEEN_CAR_SPAWNS && RandoMath.RandomBool()) {
						SpawnAngryVehicle(RandoMath.RandomBool());

						ticksSinceLastCarSpawn = 0;
					}

					if (ticksSinceLastBalanceCheck > TICKS_BETWEEN_BALANCE_CHECKS) {
						ticksSinceLastBalanceCheck = 0;
						if (spawnedAllies > maxSpawnedAllies) {
							//try removing some members that can't currently be seen by the player or are far enough
							TryWarBalancing(true);
						}
						else if (spawnedEnemies > maxSpawnedEnemies) {
							TryWarBalancing(false);
						}

						//cull enemies inside cars if there are too many!
						enemiesInsideCars = SpawnManager.instance.GetSpawnedMembersOfGang(enemyGang, true);

						if (enemiesInsideCars.Count >
							RandoMath.Max(maxSpawnedEnemies / 3, ModOptions.instance.numSpawnsReservedForCarsDuringWars * 2)) {
							CullEnemyVehicles();
						}
					}

					if (!spawnPointsSet) SetSpawnPoints(warZone.zoneBlipPosition);
					else if (ModOptions.instance.ticksBetweenEnemySpawnReplacement > 0 &&
						ticksSinceLastEnemyRelocation > ModOptions.instance.ticksBetweenEnemySpawnReplacement)
						ReplaceEnemySpawnPoint(alliedSpawnPoints[0]);

					spawnedMembersProportion = spawnedAllies / RandoMath.Max(spawnedEnemies, 1.0f);

					//if the allied side is out of reinforcements, no more allies will be spawned by this system
					if (SpawnManager.instance.livingMembersCount < ModOptions.instance.spawnedMemberLimit - ModOptions.instance.numSpawnsReservedForCarsDuringWars) {
						SpawnMember(alliedReinforcements > 0 && spawnedMembersProportion < reinforcementsAdvantage && spawnedAllies < maxSpawnedAllies);
					}

					Logger.Log("warmanager inside war tick: end", 5);
					Wait(400);
				}
				else {
					playerNearWarzone = false;
					shouldDisplayReinforcementsTexts = false;
					curTicksAwayFromBattle++;
					AmbientGangMemberSpawner.instance.enabled = true;
					if (curTicksAwayFromBattle > ModOptions.instance.ticksBeforeWarEndWithPlayerAway) {
						EndWar(SkipWar(0.65f));
					}
				}
				//if the player's gang leader is dead...
				if (!Game.Player.IsAlive && !MindControl.instance.HasChangedBody) {
					//the war ends, but the outcome depends on how well the player's side was doing
					EndWar(SkipWar(0.9f));
					return;
				}
			}
		}

		void OnAbort(object sender, EventArgs e) {
			if (warBlip != null) {
				warBlip.Remove();
				warAreaBlip.Remove();
			}

			foreach (Blip alliedBlip in alliedSpawnBlips) {
				if (alliedBlip != null) {
					alliedBlip.Remove();
				}
			}

			if (enemySpawnBlip != null) {
				enemySpawnBlip.Remove();
			}
		}
	}
}
