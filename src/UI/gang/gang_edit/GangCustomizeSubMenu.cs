using LemonUI;
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for all of the player's gang editing options
    /// </summary>
    public class GangCustomizeSubMenu : NativeMenu
    {
        public GangCustomizeSubMenu(ObjectPool menuPool) : base("Gang and Turf Mod", "Gang Customization/Upgrades")
        {
            gangUpgradesSubMenu = new GangUpgradesSubMenu();
            gangWeaponsSubMenu = new GangWeaponsSubMenu();
            gangCarColorsSubMenu = new GangCarColorsSubMenu(menuPool);
            gangBlipColorSubMenu = new GangBlipColorSubMenu();

            menuPool.Add(gangUpgradesSubMenu);
            menuPool.Add(gangWeaponsSubMenu);
            menuPool.Add(gangBlipColorSubMenu);
            menuPool.Add(this);


            Setup();
        }

        public void UpdateUpgradeCosts()
        {
            gangUpgradesSubMenu.UpdateUpgradeCosts();
        }

        private readonly GangUpgradesSubMenu gangUpgradesSubMenu;
        private readonly GangWeaponsSubMenu gangWeaponsSubMenu;
        private readonly GangCarColorsSubMenu gangCarColorsSubMenu;
        private readonly GangBlipColorSubMenu gangBlipColorSubMenu;

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            NativeItem gangUpgradesBtn = new NativeItem("Gang Upgrades...", "Opens the Gang Upgrades menu, where it's possible to upgrade your members' attributes and general gang strength.");
            Add(gangUpgradesBtn);
            BindMenuToItem(gangUpgradesSubMenu, gangUpgradesBtn);

            NativeItem gangWeaponsBtn = new NativeItem("Gang Weapons...", "Opens the Gang Weapons menu, where it's possible to purchase and sell weapons used by your gang members.");
            Add(gangWeaponsBtn);
            BindMenuToItem(gangWeaponsSubMenu, gangWeaponsBtn);

            NativeItem gangCarColorsBtn = new NativeItem("Gang Car Colors...", "Opens the Gang Car Colors menu, where it's possible to change the colors of your gang vehicles.");
            Add(gangCarColorsBtn);
            BindMenuToItem(gangCarColorsSubMenu, gangCarColorsBtn);

            NativeItem gangBlipColorBtn = new NativeItem("Gang Blip Color...", "Opens the Gang Blip Color menu, where it's possible to change the color of your gang blips (members, vehicles and turf).");
            Add(gangBlipColorBtn);
            BindMenuToItem(gangBlipColorSubMenu, gangBlipColorBtn);

            AddRenameGangButton();

            
        }

        private void AddRenameGangButton()
        {
            NativeItem newButton = new NativeItem("Rename Gang", "Opens the input prompt for resetting your gang's name.");
            Add(newButton);

            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Visible = !Visible;
                    MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterGangName, "FMMC_KEY_TIP12N", GangManager.instance.PlayerGang.name);
                }
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

                        UI.Screen.ShowSubtitle("Your gang is now known as the " + typedText);
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("That name is not allowed, sorry! (It may be in use already)");
                    }
                }
            };
        }
    }
}
