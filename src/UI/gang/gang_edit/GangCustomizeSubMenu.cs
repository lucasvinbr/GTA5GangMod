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


        private void AddRenameGangButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_rename_gang", "Rename Gang"),
                Localization.GetTextByKey("menu_button_rename_gang_desc", "Opens the input prompt for resetting your gang's name."));
            Add(newButton);

            newButton.Activated += (sender, args) =>
            {
                Visible = !Visible;
                MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterGangName, "FMMC_KEY_TIP12N", GangManager.instance.PlayerGang.name);
            };

            MenuScript.instance.OnInputFieldDone += (inputType, typedText) =>
            {
                if (inputType == MenuScript.DesiredInputType.enterGangName)
                {
                    if (typedText != "none" && GangManager.instance.GetGangByName(typedText) == null)
                    {
                        ZoneManager.instance.GiveGangZonesToAnother(GangManager.instance.PlayerGang.name, typedText);
                        GangManager.instance.PlayerGang.name = typedText;
                        GangManager.instance.SaveGangData();

                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_your_gang_now_known_as_the_", "Your gang is now known as the ") + typedText);
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_name_not_allowed", "That name is not allowed, sorry! (It may be in use already)"));
                    }
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

            var gangUpgradesBtn = new NativeSubmenuItem(gangUpgradesSubMenu, this);
            gangUpgradesBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_upgrades", "Gang Upgrades...");
            gangUpgradesBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_upgrades_desc", "Opens the Gang Upgrades menu, where it's possible to upgrade your members' attributes and general gang strength.");
            Add(gangUpgradesBtn);

            var gangWeaponsBtn = new NativeSubmenuItem(gangWeaponsSubMenu, this);
            gangWeaponsBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_weapons", "Gang Weapons...");
            gangWeaponsBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_weapons_desc", "Opens the Gang Weapons menu, where it's possible to purchase and sell weapons used by your gang members.");
            Add(gangWeaponsBtn);

            var gangCarColorsBtn = new NativeSubmenuItem(gangCarColorsSubMenu, this);
            gangCarColorsBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_car_colors", "Gang Car Colors...");
            gangCarColorsBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_car_colors_desc", "Opens the Gang Car Colors menu, where it's possible to change the colors of your gang vehicles.");
            Add(gangCarColorsBtn);

            var gangBlipColorBtn = new NativeSubmenuItem(gangBlipColorSubMenu, this);
            gangBlipColorBtn.Title = Localization.GetTextByKey("menu_button_submenu_gang_blip_color", "Gang Blip Color...");
            gangBlipColorBtn.Description = Localization.GetTextByKey("menu_button_submenu_gang_blip_color_desc", "Opens the Gang Blip Color menu, where it's possible to change the color of your gang blips (members, vehicles and turf).");
            Add(gangBlipColorBtn);

            AddRenameGangButton();
        }
    }
}
