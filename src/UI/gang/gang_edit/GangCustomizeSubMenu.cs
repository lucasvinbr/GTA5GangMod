using NativeUI;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for all of the player's gang editing options
    /// </summary>
    public class GangCustomizeSubMenu : UIMenu
    {
        public GangCustomizeSubMenu(string title, string subtitle, MenuPool menuPool) : base(title, subtitle)
        {
            gangUpgradesSubMenu = new GangUpgradesSubMenu("Gang and Turf Mod", "Gang Upgrades");
            gangWeaponsSubMenu = new GangWeaponsSubMenu("Gang and Turf Mod", "Gang Weapons");
            gangCarColorsSubMenu = new GangCarColorsSubMenu("Gang and Turf Mod", "Gang Car Colors", menuPool);
            gangBlipColorSubMenu = new GangBlipColorSubMenu("Gang and Turf Mod", "Gang Blip Color");

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
            UIMenuItem gangUpgradesBtn = new UIMenuItem("Gang Upgrades...", "Opens the Gang Upgrades menu, where it's possible to upgrade your members' attributes and general gang strength.");
            AddItem(gangUpgradesBtn);
            BindMenuToItem(gangUpgradesSubMenu, gangUpgradesBtn);

            UIMenuItem gangWeaponsBtn = new UIMenuItem("Gang Weapons...", "Opens the Gang Weapons menu, where it's possible to purchase and sell weapons used by your gang members.");
            AddItem(gangWeaponsBtn);
            BindMenuToItem(gangWeaponsSubMenu, gangWeaponsBtn);

            UIMenuItem gangCarColorsBtn = new UIMenuItem("Gang Car Colors...", "Opens the Gang Car Colors menu, where it's possible to change the colors of your gang vehicles.");
            AddItem(gangCarColorsBtn);
            BindMenuToItem(gangCarColorsSubMenu, gangCarColorsBtn);

            UIMenuItem gangBlipColorBtn = new UIMenuItem("Gang Blip Color...", "Opens the Gang Blip Color menu, where it's possible to change the color of your gang blips (members, vehicles and turf).");
            AddItem(gangBlipColorBtn);
            BindMenuToItem(gangBlipColorSubMenu, gangBlipColorBtn);

            AddRenameGangButton();

            RefreshIndex();
        }

        private void AddRenameGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Rename Gang", "Opens the input prompt for resetting your gang's name.");
            AddItem(newButton);

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

                        UI.ShowSubtitle("Your gang is now known as the " + typedText);
                    }
                    else
                    {
                        UI.ShowSubtitle("That name is not allowed, sorry! (It may be in use already)");
                    }
                }
            };
        }
    }
}
