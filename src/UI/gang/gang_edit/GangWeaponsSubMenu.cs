using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for buying/selling gang weapons
    /// </summary>
    public class GangWeaponsSubMenu : ModMenu
    {
        public GangWeaponsSubMenu() : base("gang_weapons", "Gang Weapons")
        {
        }

        private readonly Dictionary<ModOptions.BuyableWeapon, NativeCheckboxItem> buyableWeaponCheckboxesDict =
    new Dictionary<ModOptions.BuyableWeapon, NativeCheckboxItem>();

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        protected override void Setup()
        {
            //whenever this menu opens, updated options are removed and added again
            Shown += GangWeaponsSubMenu_OnMenuOpen;

            ItemActivated += (sender, itemActivatedArgs) =>
            {
                Gang playerGang = GangManager.instance.PlayerGang;
                NativeCheckboxItem pickedItem = itemActivatedArgs.Item as NativeCheckboxItem;
                
                foreach (KeyValuePair<ModOptions.BuyableWeapon, NativeCheckboxItem> kvp in buyableWeaponCheckboxesDict)
                {
                    if (kvp.Value == pickedItem)
                    {
                        if (playerGang.gangWeaponHashes.Contains(kvp.Key.wepHash))
                        {
                            playerGang.gangWeaponHashes.Remove(kvp.Key.wepHash);
                            MindControl.AddOrSubtractMoneyToProtagonist(kvp.Key.price);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_weapon_removed", "Weapon Removed!"));
                            pickedItem.Checked = false;
                        }
                        else
                        {
                            if (MindControl.AddOrSubtractMoneyToProtagonist(-kvp.Key.price))
                            {
                                playerGang.gangWeaponHashes.Add(kvp.Key.wepHash);
                                GangManager.instance.SaveGangData();
                                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_weapon_bought", "Weapon Bought!"));
                                pickedItem.Checked = true;
                            }
                            else
                            {
                                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_money_to_buy_gang_weapon", "You don't have enough money to buy that weapon for your gang."));
                                pickedItem.Checked = false;
                            }
                        }

                        break;
                    }
                }

            };
        }

        private void GangWeaponsSubMenu_OnMenuOpen(object sender, EventArgs _)
        {
            RefreshBuyableWeaponsMenuContent();
        }

        /// <summary>
        /// removes all options and then adds all weapons (from ModOptions's buyableWeapons) again
        /// </summary>
        public void RefreshBuyableWeaponsMenuContent()
        {
            Clear();

            buyableWeaponCheckboxesDict.Clear();

            List<ModOptions.BuyableWeapon> weaponsList = ModOptions.instance.buyableWeapons;

            Gang playerGang = GangManager.instance.PlayerGang;

            for (int i = 0; i < weaponsList.Count; i++)
            {
                NativeCheckboxItem weaponCheckBox = new NativeCheckboxItem
                        (string.Concat(weaponsList[i].wepHash.ToString(), " - ", weaponsList[i].price.ToString()),
                        playerGang.gangWeaponHashes.Contains(weaponsList[i].wepHash));
                buyableWeaponCheckboxesDict.Add(weaponsList[i], weaponCheckBox);
                Add(weaponCheckBox);
            }

        }

        protected override void RecreateItems()
        {
            RefreshBuyableWeaponsMenuContent();
        }
    }
}
