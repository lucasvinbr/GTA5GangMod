using LemonUI;
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for all of the player's gang editing options
    /// </summary>
    public class GangCustomizeSubMenu : ModMenu
    {
        public GangCustomizeSubMenu(ObjectPool menuPool) : base("gang_customize", "Gang Customization/Upgrades")
        {
            gangUpgradesSubMenu = new GangUpgradesSubMenu();
            gangWeaponsSubMenu = new GangWeaponsSubMenu();
            gangCarColorsSubMenu = new GangCarColorsSubMenu(menuPool);
            gangBlipColorSubMenu = new GangBlipColorSubMenu();

            menuPool.Add(gangUpgradesSubMenu);
            menuPool.Add(gangWeaponsSubMenu);
            menuPool.Add(gangBlipColorSubMenu);
            menuPool.Add(this);

            RecreateItems();
        }

        public void UpdateUpgradeCosts()
        {
            gangUpgradesSubMenu.UpdateUpgradeCosts();
        }

        private readonly GangUpgradesSubMenu gangUpgradesSubMenu;
        private readonly GangWeaponsSubMenu gangWeaponsSubMenu;
        private readonly GangCarColorsSubMenu gangCarColorsSubMenu;
        private readonly GangBlipColorSubMenu gangBlipColorSubMenu;

        public static Gang GangBeingEdited {
            get
            {
                if(_gangBeingEdited == null)
                {
                    _gangBeingEdited = GangManager.instance.PlayerGang;
                }

                return _gangBeingEdited;
            }
        }

        private static Gang _gangBeingEdited;

        private void AddRenameGangButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_rename_gang", "Rename Gang"),
                Localization.GetTextByKey("menu_button_rename_gang_desc", "Opens the input prompt for resetting your gang's name."));
            Add(newButton);

            newButton.Activated += (sender, args) =>
            {
                Visible = !Visible;
                MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterGangName, "FMMC_KEY_TIP12N", GangBeingEdited.name);
            };

            MenuScript.instance.OnInputFieldDone += (inputType, typedText) =>
            {
                if (inputType == MenuScript.DesiredInputType.enterGangName)
                {
                    if (typedText != "none" && GangManager.instance.GetGangByName(typedText) == null)
                    {
                        ZoneManager.instance.GiveGangZonesToAnother(GangBeingEdited.name, typedText);
                        GangBeingEdited.name = typedText;
                        GangManager.instance.SaveGangData();

                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_the_gang_now_known_as_the_", "The gang is now known as the ") + typedText);
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_name_not_allowed", "That name is not allowed, sorry! (It may be in use already)"));
                    }
                }
            };
        }

        private void AddSelectEditedGangButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_prefix_selected_gang", "Selected Gang: ") + GangBeingEdited.name,
                Localization.GetTextByKey("menu_button_select_gang_to_edit_desc", "Opens the prompt for selecting another gang to edit."));
            Add(newButton);

            newButton.Activated += (sender, args) =>
            {
                Visible = !Visible;
                MenuScript.instance.OpenPickAGangMenu(this,
                    Localization.GetTextByKey("menu_subtitle_select_gang_to_edit", "Select Gang to edit"),
                    GangManager.instance.gangData.gangs,
                    (pickedGang) =>
                    {
                        _gangBeingEdited = pickedGang;
                        RecreateItems();
                    });
            };
        }

        private void AddIsPlayerOwnedToggle()
        {
            NativeCheckboxItem newToggle = new NativeCheckboxItem(Localization.GetTextByKey("menu_toggle_is_gang_player_owned", "Is Player Owned?"),
                Localization.GetTextByKey("menu_toggle_is_gang_player_owned_desc", "If the checkbox is marked, this is your current gang. If not marked, you can set it as your gang, but you will leave the gang previously marked as yours."));
            Add(newToggle);

            if (GangBeingEdited.isPlayerOwned)
            {
                newToggle.Enabled = false;
            }

            newToggle.CheckboxChanged += (sender, args) =>
            {
                if (newToggle.Checked)
                {
                    GangManager.instance.SetGangAsPlayerOwned(GangBeingEdited);
                    RecreateItems();
                }
            };
        }

        protected override void Setup()
        {
            Localization.OnLanguageChanged += OnLocalesChanged;
            Shown += RebuildItemsIfNeeded;
        }

        protected override void RecreateItems()
        {
            Clear();

            AddSelectEditedGangButton();
            AddIsPlayerOwnedToggle();
            AddRenameGangButton();

            if (GangBeingEdited.isPlayerOwned)
            {
                var gangUpgradesBtn = new NativeSubmenuItem(gangUpgradesSubMenu, this);
                gangUpgradesBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_upgrades", "Gang Upgrades...");
                gangUpgradesBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_upgrades_desc", "Opens the Gang Upgrades menu, where it's possible to upgrade your members' attributes and general gang strength.");
                Add(gangUpgradesBtn);
            }

            var gangWeaponsBtn = new NativeSubmenuItem(gangWeaponsSubMenu, this);
            gangWeaponsBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_weapons", "Gang Weapons...");
            gangWeaponsBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_weapons_desc", "Opens the Gang Weapons menu, where it's possible to purchase and sell weapons used by the gang members.");
            Add(gangWeaponsBtn);

            var gangCarColorsBtn = new NativeSubmenuItem(gangCarColorsSubMenu, this);
            gangCarColorsBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_car_colors", "Gang Car Colors...");
            gangCarColorsBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_car_colors_desc", "Opens the Gang Car Colors menu, where it's possible to change the colors of the gang vehicles.");
            Add(gangCarColorsBtn);

            var gangBlipColorBtn = new NativeSubmenuItem(gangBlipColorSubMenu, this);
            gangBlipColorBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_blip_color", "Gang Blip Color...");
            gangBlipColorBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_blip_color_desc", "Opens the Gang Blip Color menu, where it's possible to change the color of the gang blips (members, vehicles and turf).");
            Add(gangBlipColorBtn);

            
        }
    }
}
