using System.Security.Policy;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script contains methods used to define and calculate values used in many aspects of the mod.
    /// </summary>
    public class GangCalculations
    {
        public static int CalculateHealthUpgradeCost(int currentMemberHealth)
        {
            return ModOptions.instance.baseCostToUpgradeHealth + (currentMemberHealth + 20) * (20 * (currentMemberHealth / 20) + 1);
        }

        public static int CalculateArmorUpgradeCost(int currentMemberArmor)
        {
            return ModOptions.instance.baseCostToUpgradeArmor + (currentMemberArmor + 20) * (50 * (currentMemberArmor / 25));
        }

        public static int CalculateAccuracyUpgradeCost(int currentMemberAcc)
        {
            return ((currentMemberAcc / 5) + 1) * ModOptions.instance.baseCostToUpgradeAccuracy;
        }

        public static int CalculateGangValueUpgradeCost(int currentGangValue)
        {
            return (currentGangValue + 1) * ModOptions.instance.baseCostToUpgradeGeneralGangTurfValue;
        }

        public static int CalculateTurfValueUpgradeCost(int currentTurfValue)
        {
            return (currentTurfValue + 1) * ModOptions.instance.baseCostToUpgradeSingleTurfValue;
        }

        public static int CalculateAttackCost(Gang attackerGang, GangWarManager.AttackStrength attackType)
        {
            if (attackType == GangWarManager.AttackStrength.light) return ModOptions.instance.baseCostToTakeTurf;

            int attackTypeInt = (int)attackType;
            float attackScale = attackTypeInt / 3.0f;

            return ModOptions.instance.baseCostToTakeTurf + (int) (ModOptions.instance.maxAdditionalCostToTakeTurf * attackScale * attackScale);
        }

        /// <summary>
        /// assuming a maxed attack equals max turf value, returns the turf value equivalent to the provided attack type
        /// </summary>
        /// <param name="attackType"></param>
        /// <returns></returns>
        public static int CalculateTurfValueEquivalentToGangAttack(GangWarManager.AttackStrength attackType)
        {
            if (attackType == GangWarManager.AttackStrength.light) return 0;

            int maxAttackIntensity = (int)GangWarManager.AttackStrength.massive;
            // attack intensity doesn't scale up linearly
            maxAttackIntensity *= maxAttackIntensity;

            int attackTypeInt = (int)attackType;
            attackTypeInt *= attackTypeInt;

            return RandoMath.CeilToInt(ModOptions.instance.maxTurfValue * (attackTypeInt / (float) maxAttackIntensity));
        }

        public static GangWarManager.AttackStrength CalculateRequiredAttackStrength(Gang attackerGang, int defenderStrength)
        {
            GangWarManager.AttackStrength requiredAtk = GangWarManager.AttackStrength.light;

            for (int i = 0; i < 3; i++)
            {
                if (CalculateAttackerStrength(attackerGang, requiredAtk) >= defenderStrength)
                {
                    break;
                }
                else
                {
                    requiredAtk++;
                }
            }

            return requiredAtk;
        }

        public static int CalculateAttackerReinforcements(Gang attackerGang, GangWarManager.AttackStrength attackType)
        {
            // maxed attack should have almost as many reinforcements as a maxed zone
            int attackTypeAsTurfValue = CalculateTurfValueEquivalentToGangAttack(attackType);
            return (int) ((ModOptions.instance.extraKillsPerTurfValue * ModOptions.instance.maxTurfValue * (attackTypeAsTurfValue / (float) ModOptions.instance.maxTurfValue) +
                ModOptions.instance.baseNumKillsBeforeWarVictory +
                attackerGang.GetBonusReinforcementsCount()) * 0.95f * attackerGang.memberAmountInWarsMultiplier);
        }

        public static int CalculateDefenderReinforcements(Gang defenderGang, TurfZone targetZone)
        {
            return (int) (ModOptions.instance.extraKillsPerTurfValue * targetZone.value + ModOptions.instance.baseNumKillsBeforeWarVictory +
                defenderGang.GetBonusReinforcementsCount() * defenderGang.memberAmountInWarsMultiplier);
        }

        /// <summary>
        /// gets the reinforcement count for the defenders and estimates a total power based on that number and
        /// the gang's fixed strength value
        /// </summary>
        /// <param name="defenderGang"></param>
        /// <param name="contestedZone"></param>
        /// <returns></returns>
        public static int CalculateDefenderStrength(Gang defenderGang, TurfZone contestedZone)
        {
            return (int) (defenderGang.GetFixedStrengthValue() *
                CalculateDefenderReinforcements(defenderGang, contestedZone));
        }

        /// <summary>
        /// gets the reinforcement count for the attackers and estimates a total power based on that number and
        /// the gang's fixed strength value
        /// </summary>
        /// <param name="defenderGang"></param>
        /// <param name="contestedZone"></param>
        /// <returns></returns>
        public static int CalculateAttackerStrength(Gang attackerGang, GangWarManager.AttackStrength attackType)
        {
            return (int) (attackerGang.GetFixedStrengthValue() *
                CalculateAttackerReinforcements(attackerGang, attackType));
        }

        /// <summary>
        /// uses the base reward for taking enemy turf (half if it was just a battle for defending)
        /// and the enemy strength (with variation) to define the "loot"
        /// </summary>
        /// <returns></returns>
        public static int CalculateBattleRewards(Gang ourEnemy, int battleScale, bool weWereAttacking)
        {
            int baseReward = ModOptions.instance.rewardForTakingEnemyTurf;
            if (weWereAttacking)
            {
                baseReward /= 2;
            }
            return (int)(baseReward * ourEnemy.GetGangVariedStrengthValue()) * (battleScale + 1);
        }

        public static int CalculateRewardForZone(TurfZone zone, int ownerGangTurfsCount)
        {

            float singleZoneReward = (((float)zone.value / RandoMath.Max(ModOptions.instance.maxTurfValue, 1)) *
                (ModOptions.instance.maxRewardPerZoneOwned - ModOptions.instance.baseRewardPerZoneOwned)) +
                ModOptions.instance.baseRewardPerZoneOwned;

            return (int)((1 + ModOptions.instance.rewardMultiplierPerZone * ownerGangTurfsCount) * singleZoneReward);

        }
    }

}
