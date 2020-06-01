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
    /// submenu for setting gang cars' colors
    /// </summary>
    public class GangCarColorsSubMenu : UIMenu
    {
        public GangCarColorsSubMenu(string title, string subtitle) : base(title, subtitle)
        {   
            Setup();
        }


		private readonly Dictionary<VehicleColor, UIMenuItem> carColorEntries =
			new Dictionary<VehicleColor, UIMenuItem>();

		/// <summary>
		/// adds all buttons and events to the menu
		/// </summary>
		public void Setup()
        {
			FillCarColorEntries();

			VehicleColor[] carColorsArray = carColorEntries.Keys.ToArray();
			UIMenuItem[] colorButtonsArray = carColorEntries.Values.ToArray();

			for (int i = 0; i < colorButtonsArray.Length; i++)
			{
				AddItem(colorButtonsArray[i]);
			}

			RefreshIndex();

			OnIndexChange += (sender, index) => {
				Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
				if (playerVehicle != null)
				{
					playerVehicle.PrimaryColor = carColorsArray[index];
					playerVehicle.SecondaryColor = carColorsArray[index];
				}
			};

			OnItemSelect += (sender, item, checked_) => {
				for (int i = 0; i < carColorsArray.Length; i++)
				{
					if (item == carColorEntries[carColorsArray[i]])
					{
						Gang playerGang = GangManager.instance.PlayerGang;
						playerGang.vehicleColor = carColorsArray[i];
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
