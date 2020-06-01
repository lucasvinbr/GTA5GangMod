using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NativeUI.UIMenuDynamicListItem;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for buying/selling gang weapons
    /// </summary>
    public class GangWeaponsSubMenu : UIMenu
    {
        public GangWeaponsSubMenu(string title, string subtitle) : base(title, subtitle)
        {   
            Setup();
        }

		private readonly Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem> buyableWeaponCheckboxesDict =
	new Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem>();

		/// <summary>
		/// adds all buttons and events to the menu
		/// </summary>
		public void Setup()
        {
			//whenever this menu opens, updated options are removed and added again
            OnMenuOpen += GangWeaponsSubMenu_OnMenuOpen;

			OnCheckboxChange += (sender, item, checked_) => {
				Gang playerGang = GangManager.instance.PlayerGang;

				foreach (KeyValuePair<ModOptions.BuyableWeapon, UIMenuCheckboxItem> kvp in buyableWeaponCheckboxesDict)
				{
					if (kvp.Value == item)
					{
						if (playerGang.gangWeaponHashes.Contains(kvp.Key.wepHash))
						{
							playerGang.gangWeaponHashes.Remove(kvp.Key.wepHash);
							MindControl.instance.AddOrSubtractMoneyToProtagonist(kvp.Key.price);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Weapon Removed!");
							item.Checked = false;
						}
						else
						{
							if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-kvp.Key.price))
							{
								playerGang.gangWeaponHashes.Add(kvp.Key.wepHash);
								GangManager.instance.SaveGangData();
								UI.ShowSubtitle("Weapon Bought!");
								item.Checked = true;
							}
							else
							{
								UI.ShowSubtitle("You don't have enough money to buy that weapon for your gang.");
								item.Checked = false;
							}
						}

						break;
					}
				}

			};
		}

        private void GangWeaponsSubMenu_OnMenuOpen(UIMenu sender)
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
				UIMenuCheckboxItem weaponCheckBox = new UIMenuCheckboxItem
						(string.Concat(weaponsList[i].wepHash.ToString(), " - ", weaponsList[i].price.ToString()),
						playerGang.gangWeaponHashes.Contains(weaponsList[i].wepHash));
				buyableWeaponCheckboxesDict.Add(weaponsList[i], weaponCheckBox);
				AddItem(weaponCheckBox);
			}

			RefreshIndex();
		}


	}
}
