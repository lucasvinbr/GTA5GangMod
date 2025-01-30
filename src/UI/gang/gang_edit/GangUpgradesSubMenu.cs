using LemonUI;
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for creating and editing custom zones
    /// </summary>
    public class GangUpgradesSubMenu : ModMenu
    {
        public GangUpgradesSubMenu() : base("Gang and Turf Mod", "Gang Upgrades")
        {            
        }

        private int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost, gangValueUpgradeCost;

        private NativeItem healthButton, armorButton, accuracyButton, upgradeGangValueBtn;


        public void UpdateUpgradeCosts()
        {
            Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;
            healthUpgradeCost = GangCalculations.CalculateHealthUpgradeCost(editedGang.memberHealth);
            armorUpgradeCost = GangCalculations.CalculateArmorUpgradeCost(editedGang.memberArmor);
            accuracyUpgradeCost = GangCalculations.CalculateAccuracyUpgradeCost(editedGang.memberAccuracyLevel);
            gangValueUpgradeCost = GangCalculations.CalculateGangValueUpgradeCost(editedGang.baseTurfValue);

            healthButton.Title = Localization.GetTextByKey("menu_button_upgrade_member_health", "Upgrade Member Health") + " - " + healthUpgradeCost.ToString();
            armorButton.Title = Localization.GetTextByKey("menu_button_upgrade_member_armor", "Upgrade Member Armor") + " - " + armorUpgradeCost.ToString();
            accuracyButton.Title = Localization.GetTextByKey("menu_button_upgrade_member_accuracy", "Upgrade Member Accuracy") + " - " + accuracyUpgradeCost.ToString();
            upgradeGangValueBtn.Title = Localization.GetTextByKey("menu_button_upgrade_gang_base_strength", "Upgrade Gang Base Strength") + " - " + gangValueUpgradeCost.ToString();
        }

        private void AddGangUpgradesBtns()
        {
            //upgrade buttons
            healthButton = new NativeItem(Localization.GetTextByKey("menu_button_upgrade_member_health", "Upgrade Member Health") + " - " + healthUpgradeCost.ToString(),
                Localization.GetTextByKey("menu_button_upgrade_member_health_desc", "Increases gang member starting and maximum health. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file."));
            armorButton = new NativeItem(Localization.GetTextByKey("menu_button_upgrade_member_armor", "Upgrade Member Armor") + " - " + armorUpgradeCost.ToString(),
                Localization.GetTextByKey("menu_button_upgrade_member_armor_desc", "Increases gang member starting body armor. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file."));
            accuracyButton = new NativeItem(Localization.GetTextByKey("menu_button_upgrade_member_accuracy", "Upgrade Member Accuracy") + " - " + accuracyUpgradeCost.ToString(),
                Localization.GetTextByKey("menu_button_upgrade_member_accuracy_desc", "Increases gang member firing accuracy. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file."));
            upgradeGangValueBtn = new NativeItem(Localization.GetTextByKey("menu_button_upgrade_gang_base_strength", "Upgrade Gang Base Strength") + " - " + gangValueUpgradeCost.ToString(),
                Localization.GetTextByKey("menu_button_upgrade_gang_base_strength_desc", "Increases the level territories have after you take them. This level affects the income provided, the reinforcements available in a war and reduces general police presence. The limit is configurable via the ModOptions file."));
            Add(healthButton);
            Add(armorButton);
            Add(accuracyButton);
            Add(upgradeGangValueBtn);

            healthButton.Activated += (sender, args) =>
            {
                Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;
                if (MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost, true))
                {
                    if (editedGang.memberHealth < ModOptions.instance.maxGangMemberHealth)
                    {
                        editedGang.memberHealth += ModOptions.instance.GetHealthUpgradeIncrement();
                        if (editedGang.memberHealth > ModOptions.instance.maxGangMemberHealth)
                        {
                            editedGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
                        }
                        MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost);
                        GangManager.instance.SaveGangData();
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_health_upgraded", "Member health upgraded!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_health_maxed_configurable", "Your members' health is at its maximum limit (it can be configured in the ModOptions file)"));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_money_for_upgrade", "You don't have enough money to buy that upgrade."));
                }
                UpdateUpgradeCosts();
            };

            armorButton.Activated += (sender, args) =>
            {
                Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;
                if (MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost, true))
                {
                    if (editedGang.memberArmor < ModOptions.instance.maxGangMemberArmor)
                    {
                        editedGang.memberArmor += ModOptions.instance.GetArmorUpgradeIncrement();
                        if (editedGang.memberArmor > ModOptions.instance.maxGangMemberArmor)
                        {
                            editedGang.memberArmor = ModOptions.instance.maxGangMemberArmor;
                        }
                        MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost);
                        GangManager.instance.SaveGangData();
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_armor_upgraded", "Member armor upgraded!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_health_maxed_configurable", "Your members' armor is at its maximum limit (it can be configured in the ModOptions file)"));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_money_for_upgrade", "You don't have enough money to buy that upgrade."));
                }
                UpdateUpgradeCosts();
            };

            accuracyButton.Activated += (sender, args) =>
            {
                Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;
                if (MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost, true))
                {
                    if (editedGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy)
                    {
                        editedGang.memberAccuracyLevel += ModOptions.instance.GetAccuracyUpgradeIncrement();
                        if (editedGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy)
                        {
                            editedGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
                        }
                        MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost);
                        GangManager.instance.SaveGangData();
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_accuracy_upgraded", "Member accuracy upgraded!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_accuracy_maxed_configurable", "Your members' accuracy is at its maximum limit (it can be configured in the ModOptions file)"));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_money_for_upgrade", "You don't have enough money to buy that upgrade."));
                }
                UpdateUpgradeCosts();
            };

            upgradeGangValueBtn.Activated += (sender, args) =>
            {
                Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;
                if (MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost, true))
                {
                    if (editedGang.baseTurfValue < ModOptions.instance.maxTurfValue)
                    {
                        editedGang.baseTurfValue++;
                        if (editedGang.baseTurfValue > ModOptions.instance.maxTurfValue)
                        {
                            editedGang.baseTurfValue = ModOptions.instance.maxTurfValue;
                        }
                        MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost);
                        GangManager.instance.SaveGangData();
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_strength_upgraded", "Gang Base Strength upgraded!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_strength_maxed_configurable", "Your Gang Base Strength is at its maximum limit (it can be configured in the ModOptions file)"));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_money_for_upgrade", "You don't have enough money to buy that upgrade."));
                }
                UpdateUpgradeCosts();
            };

        }

        protected override void RecreateItems()
        {
            Clear();

            AddGangUpgradesBtns();
        }
    }
}
