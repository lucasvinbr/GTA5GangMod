using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to gang behavior and relations.
    /// </summary>
    public class GangCalculations
    {
		public static int CalculateHealthUpgradeCost(int currentMemberHealth) {
			return ModOptions.instance.baseCostToUpgradeHealth + (currentMemberHealth + 20) * (20 * (currentMemberHealth / 20) + 1);
		}

		public static int CalculateArmorUpgradeCost(int currentMemberArmor) {
			return ModOptions.instance.baseCostToUpgradeArmor + (currentMemberArmor + 20) * (50 * (currentMemberArmor / 25));
		}

		public static int CalculateAccuracyUpgradeCost(int currentMemberAcc) {
			return ((currentMemberAcc / 5) + 1) * ModOptions.instance.baseCostToUpgradeAccuracy;
		}

		public static int CalculateGangValueUpgradeCost(int currentGangValue) {
			return (currentGangValue + 1) * ModOptions.instance.baseCostToUpgradeGeneralGangTurfValue;
		}

		public static int CalculateTurfValueUpgradeCost(int currentTurfValue) {
			return (currentTurfValue + 1) * ModOptions.instance.baseCostToUpgradeSingleTurfValue;
		}

		public static int CalculateAttackCost(Gang attackerGang, GangWarManager.AttackStrength attackType) {
			int attackTypeInt = (int)attackType;
			int pow2NonZeroAttackType = (attackTypeInt * attackTypeInt + 1);
			return ModOptions.instance.baseCostToTakeTurf + ModOptions.instance.baseCostToTakeTurf * attackTypeInt * attackTypeInt +
				attackerGang.GetFixedStrengthValue() * pow2NonZeroAttackType;
		}

		public static GangWarManager.AttackStrength CalculateRequiredAttackStrength(Gang attackerGang, int defenderStrength) {
			GangWarManager.AttackStrength requiredAtk = GangWarManager.AttackStrength.light;

			int attackerGangStrength = attackerGang.GetFixedStrengthValue();

			for (int i = 0; i < 3; i++) {
				if (attackerGangStrength * (i * i + 1) > defenderStrength) {
					break;
				}
				else {
					requiredAtk++;
				}
			}

			return requiredAtk;
		}

		public static int CalculateAttackerReinforcements(Gang attackerGang, GangWarManager.AttackStrength attackType) {
			return ModOptions.instance.extraKillsPerTurfValue * ((int)(attackType + 1) * (int)(attackType + 1)) + ModOptions.instance.baseNumKillsBeforeWarVictory / 2 +
				attackerGang.GetReinforcementsValue() / 100;
		}

		public static int CalculateDefenderStrength(Gang defenderGang, TurfZone contestedZone) {
			return defenderGang.GetFixedStrengthValue() * (contestedZone.value + 1);
		}

		public static int CalculateDefenderReinforcements(Gang defenderGang, TurfZone targetZone) {
			return ModOptions.instance.extraKillsPerTurfValue * targetZone.value + ModOptions.instance.baseNumKillsBeforeWarVictory +
				defenderGang.GetReinforcementsValue() / 100;
		}

		/// <summary>
		/// uses the base reward for taking enemy turf (half if it was just a battle for defending)
		/// and the enemy strength (with variation) to define the "loot"
		/// </summary>
		/// <returns></returns>
		public static int CalculateBattleRewards(Gang ourEnemy, int battleScale, bool weWereAttacking) {
			int baseReward = ModOptions.instance.rewardForTakingEnemyTurf;
			if (weWereAttacking) {
				baseReward /= 2;
			}
			return (baseReward + ourEnemy.GetGangVariedStrengthValue()) * (battleScale + 1);
		}
	}

}
