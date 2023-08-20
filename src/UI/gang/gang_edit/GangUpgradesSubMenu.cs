

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for creating and editing custom zones
    /// </summary>
    public class GangUpgradesSubMenu : NativeMenu
    {
        public GangUpgradesSubMenu() : base("Gang and Turf Mod", "Gang Upgrades")
        {
            AddGangUpgradesBtns();
            
        }

        private int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost, gangValueUpgradeCost;

        private NativeItem healthButton, armorButton, accuracyButton, upgradeGangValueBtn;


        public void UpdateUpgradeCosts()
        {
            Gang playerGang = GangManager.instance.PlayerGang;
            healthUpgradeCost = GangCalculations.CalculateHealthUpgradeCost(playerGang.memberHealth);
            armorUpgradeCost = GangCalculations.CalculateArmorUpgradeCost(playerGang.memberArmor);
            accuracyUpgradeCost = GangCalculations.CalculateAccuracyUpgradeCost(playerGang.memberAccuracyLevel);
            gangValueUpgradeCost = GangCalculations.CalculateGangValueUpgradeCost(playerGang.baseTurfValue);

            healthButton.Text = "Upgrade Member Health - " + healthUpgradeCost.ToString();
            armorButton.Text = "Upgrade Member Armor - " + armorUpgradeCost.ToString();
            accuracyButton.Text = "Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString();
            upgradeGangValueBtn.Text = "Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString();
        }

        private void AddGangUpgradesBtns()
        {
            //upgrade buttons
            healthButton = new NativeItem("Upgrade Member Health - " + healthUpgradeCost.ToString(), "Increases gang member starting and maximum health. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            armorButton = new NativeItem("Upgrade Member Armor - " + armorUpgradeCost.ToString(), "Increases gang member starting body armor. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            accuracyButton = new NativeItem("Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString(), "Increases gang member firing accuracy. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            upgradeGangValueBtn = new NativeItem("Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString(), "Increases the level territories have after you take them. This level affects the income provided, the reinforcements available in a war and reduces general police presence. The limit is configurable via the ModOptions file.");
            Add(healthButton);
            Add(armorButton);
            Add(accuracyButton);
            Add(upgradeGangValueBtn);

            OnItemSelect += (sender, item, index) =>
            {
                Gang playerGang = GangManager.instance.PlayerGang;

                if (item == healthButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost, true))
                    {
                        if (playerGang.memberHealth < ModOptions.instance.maxGangMemberHealth)
                        {
                            playerGang.memberHealth += ModOptions.instance.GetHealthUpgradeIncrement();
                            if (playerGang.memberHealth > ModOptions.instance.maxGangMemberHealth)
                            {
                                playerGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle("Member health upgraded!");
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle("Your members' health is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == armorButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost, true))
                    {
                        if (playerGang.memberArmor < ModOptions.instance.maxGangMemberArmor)
                        {
                            playerGang.memberArmor += ModOptions.instance.GetArmorUpgradeIncrement();
                            if (playerGang.memberArmor > ModOptions.instance.maxGangMemberArmor)
                            {
                                playerGang.memberArmor = ModOptions.instance.maxGangMemberArmor;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle("Member armor upgraded!");
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle("Your members' armor is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == accuracyButton)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost, true))
                    {
                        if (playerGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy)
                        {
                            playerGang.memberAccuracyLevel += ModOptions.instance.GetAccuracyUpgradeIncrement();
                            if (playerGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy)
                            {
                                playerGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle("Member accuracy upgraded!");
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle("Your members' accuracy is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == upgradeGangValueBtn)
                {
                    if (MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost, true))
                    {
                        if (playerGang.baseTurfValue < ModOptions.instance.maxTurfValue)
                        {
                            playerGang.baseTurfValue++;
                            if (playerGang.baseTurfValue > ModOptions.instance.maxTurfValue)
                            {
                                playerGang.baseTurfValue = ModOptions.instance.maxTurfValue;
                            }
                            MindControl.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle("Gang Base Strength upgraded!");
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle("Your Gang Base Strength is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                UpdateUpgradeCosts();

            };

        }

    }
}
