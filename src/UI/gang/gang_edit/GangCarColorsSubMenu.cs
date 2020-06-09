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
    /// submenu for setting gang cars' colors. Contains another submenu for the colors list
    /// </summary>
    public class GangCarColorsSubMenu : UIMenu
    {
        public GangCarColorsSubMenu(string title, string subtitle, MenuPool menuPool) : base(title, subtitle)
        {
			colorsMenu = new UIMenu("Gand and Turf Mod", "Car Colors List");
			menuPool.Add(colorsMenu);
			menuPool.Add(this);

			Setup();
        }

		private readonly UIMenu colorsMenu;
		private bool settingPrimaryColor = true;
		private readonly Dictionary<VehicleColor, UIMenuItem> carColorEntries =
			new Dictionary<VehicleColor, UIMenuItem>();

		/// <summary>
		/// adds all buttons and events to the menu
		/// </summary>
		public void Setup()
        {
			UIMenuItem primaryBtn = new UIMenuItem("Customize Primary Car Color");
			UIMenuItem secondaryBtn = new UIMenuItem("Customize Secondary Car Color");

			OnItemSelect += (sender, selectedItem, index) =>
			{
				settingPrimaryColor = selectedItem == primaryBtn;
			};

			//it's the same menu for both options
			BindMenuToItem(colorsMenu, primaryBtn);
			BindMenuToItem(colorsMenu, secondaryBtn);

			RefreshIndex();

			SetupColorsMenu();
		}

        private void SetupColorsMenu()
        {
			FillCarColorEntries();

			VehicleColor[] carColorsArray = carColorEntries.Keys.ToArray();
			UIMenuItem[] colorButtonsArray = carColorEntries.Values.ToArray();

			for (int i = 0; i < colorButtonsArray.Length; i++)
			{
				colorsMenu.AddItem(colorButtonsArray[i]);
			}

			colorsMenu.RefreshIndex();

			colorsMenu.OnIndexChange += (sender, index) => {
				Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
				if (playerVehicle != null)
				{
					if (settingPrimaryColor)
					{
						playerVehicle.PrimaryColor = carColorsArray[index];
					}
					else
					{
						playerVehicle.SecondaryColor = carColorsArray[index];
					}
				}
			};

			colorsMenu.OnItemSelect += (sender, item, checked_) => {
				for (int i = 0; i < carColorsArray.Length; i++)
				{
					if (item == carColorEntries[carColorsArray[i]])
					{
						Gang playerGang = GangManager.instance.PlayerGang;

						if (settingPrimaryColor)
						{
							playerGang.vehicleColor = carColorsArray[i];
						}
						else
						{
							playerGang.secondaryVehicleColor = carColorsArray[i];
						}

						GangManager.instance.SaveGangData(false);
						UI.ShowSubtitle("Gang vehicle color changed!");
						break;
					}
				}
			};
		}

		void FillCarColorEntries()
		{
			foreach (ModOptions.GangColorTranslation colorList in ModOptions.instance.similarColors)
			{
				for (int i = 0; i < colorList.vehicleColors.Count; i++)
				{
					carColorEntries.Add(colorList.vehicleColors[i], new UIMenuItem(colorList.vehicleColors[i].ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
				}

			}

			if (ModOptions.instance.extraPlayerExclusiveColors == null)
			{
				ModOptions.instance.SetColorTranslationDefaultValues();
			}

			//and the extra colors, only chooseable by the player!
			foreach (VehicleColor extraColor in ModOptions.instance.extraPlayerExclusiveColors)
			{
				carColorEntries.Add(extraColor, new UIMenuItem(extraColor.ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
			}
		}
	}
}
