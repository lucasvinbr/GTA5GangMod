
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for buying/selling gang weapons
    /// </summary>
    public class GangWeaponsSubMenu : NativeMenu
    {
        public GangWeaponsSubMenu() : base("Gang and Turf Mod", "Gang Weapons")
        {
            Setup();
        }

        private readonly Dictionary<ModOptions.BuyableWeapon, NativeCheckboxItem> buyableWeaponCheckboxesDict =
    new Dictionary<ModOptions.BuyableWeapon, NativeCheckboxItem>();

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            //whenever this menu opens, updated options are removed and added again
            OnMenuOpen += GangWeaponsSubMenu_OnMenuOpen;

            OnCheckboxChange += (sender, item, checked_) =>
            {
                Gang playerGang = GangManager.instance.PlayerGang;

                foreach (KeyValuePair<ModOptions.BuyableWeapon, NativeCheckboxItem> kvp in buyableWeaponCheckboxesDict)
                {
                    if (kvp.Value == item)
                    {
                        if (playerGang.gangWeaponHashes.Contains(kvp.Key.wepHash))
                        {
                            playerGang.gangWeaponHashes.Remove(kvp.Key.wepHash);
                            MindControl.AddOrSubtractMoneyToProtagonist(kvp.Key.price);
                            GangManager.instance.SaveGangData();
                            UI.Screen.ShowSubtitle("Weapon Removed!");
                            item.Checked = false;
                        }
                        else
                        {
                            if (MindControl.AddOrSubtractMoneyToProtagonist(-kvp.Key.price))
                            {
                                playerGang.gangWeaponHashes.Add(kvp.Key.wepHash);
                                GangManager.instance.SaveGangData();
                                UI.Screen.ShowSubtitle("Weapon Bought!");
                                item.Checked = true;
                            }
                            else
                            {
                                UI.Screen.ShowSubtitle("You don't have enough money to buy that weapon for your gang.");
                                item.Checked = false;
                            }
                        }

                        break;
                    }
                }

            };
        }

        private void GangWeaponsSubMenu_OnMenuOpen(NativeMenu sender)
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


    }
}
