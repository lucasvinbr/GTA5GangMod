using LemonUI;
using LemonUI.Menus;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting gang cars' colors. Contains another submenu for the colors list
    /// </summary>
    public class GangCarColorsSubMenu : ModMenu
    {
        public GangCarColorsSubMenu(ObjectPool menuPool) : base("gang_car_colors", "Gang Car Colors")
        {
            colorsMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_car_colors_list", "Car Colors List"));
            menuPool.Add(colorsMenu);
            menuPool.Add(this);

            SetupColorsMenu();

            RecreateItems();
        }

        private readonly NativeMenu colorsMenu;
        private bool settingPrimaryColor = true;
        private readonly List<VehicleColor> vehicleColors = new List<VehicleColor>();
        private int curItemIndex = 0;

        protected override void Setup()
        {
            Localization.OnLanguageChanged += OnLocalesChanged;
            Shown += RebuildItemsIfNeeded;
        }

        private void SetupColorsMenu()
        {

            colorsMenu.SelectedIndexChanged += (sender, args) =>
            {
                curItemIndex = args.Index;
                Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (playerVehicle != null)
                {
                    if (settingPrimaryColor)
                    {
                        playerVehicle.Mods.PrimaryColor = vehicleColors[curItemIndex];
                    }
                    else
                    {
                        playerVehicle.Mods.SecondaryColor = vehicleColors[curItemIndex];
                    }
                }
            };

            colorsMenu.ItemActivated += (sender, args) =>
            {
                Gang editedGang = GangCustomizeSubMenu.GangBeingEdited;

                if (settingPrimaryColor)
                {
                    editedGang.vehicleColor = vehicleColors[curItemIndex];
                }
                else
                {
                    editedGang.secondaryVehicleColor = vehicleColors[curItemIndex];
                }

                GangManager.instance.SaveGangData(false);
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_vehicle_color_changed", "Gang vehicle color changed!"));
            };
        }

        private void FillCarColorEntries()
        {
            vehicleColors.Clear();

            foreach (ModOptions.GangColorTranslation colorList in ModOptions.instance.similarColors)
            {
                for (int i = 0; i < colorList.vehicleColors.Count; i++)
                {
                    colorsMenu.Add(new NativeItem(colorList.vehicleColors[i].ToString(), Localization.GetTextByKey("menu_button_pick_car_color_desc", "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change.")));
                    vehicleColors.Add(colorList.vehicleColors[i]);
                }

            }

            if (ModOptions.instance.extraPlayerExclusiveColors == null)
            {
                ModOptions.instance.SetColorTranslationDefaultValues();
            }

            //and the extra colors, only chooseable by the player!
            foreach (VehicleColor extraColor in ModOptions.instance.extraPlayerExclusiveColors)
            {
                colorsMenu.Add(new NativeItem(extraColor.ToString(), Localization.GetTextByKey("menu_button_pick_car_color_desc", "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change.")));
                vehicleColors.Add(extraColor);
            }
        }

        protected override void RecreateItems()
        {
            Clear();
            colorsMenu.Clear();

            NativeItem primaryBtn = new NativeSubmenuItem(colorsMenu, this)
            {
                Title = Localization.GetTextByKey("menu_button_customize_primary_car_color", "Customize Primary Car Color")
            };
            NativeItem secondaryBtn = new NativeSubmenuItem(colorsMenu, this)
            {
                Title = Localization.GetTextByKey("menu_button_customize_secondary_car_color", "Customize Secondary Car Color")
            };

            primaryBtn.Activated += (sender, args) =>
            {
                settingPrimaryColor = true;
            };

            secondaryBtn.Activated += (sender, args) =>
            {
                settingPrimaryColor = false;
            };

            Add(primaryBtn);
            Add(secondaryBtn);

            FillCarColorEntries();
        }
    }
}
