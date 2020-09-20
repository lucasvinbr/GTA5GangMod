using NativeUI;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// menu for most gang-related stuff... but also takes to mod options
    /// </summary>
    public class GangMenu : UIMenu
    {
        public GangMenu(MenuPool menuPool) : base("Gang and Turf Mod", "Main Menu")
        {
            gangCustomizeSubMenu = new GangCustomizeSubMenu(menuPool);
            warOptionsSubMenu = new WarOptionsSubMenu(menuPool);
            modOptionsSubMenu = new ModOptionsSubMenu(menuPool);

            menuPool.Add(this);

            Setup();

        }



        private UIMenuItem carBackupBtn, paraBackupBtn;

        private readonly GangCustomizeSubMenu gangCustomizeSubMenu;
        private readonly WarOptionsSubMenu warOptionsSubMenu;
        private readonly ModOptionsSubMenu modOptionsSubMenu;

        public void UpdateCosts()
        {
            gangCustomizeSubMenu.UpdateUpgradeCosts();
            carBackupBtn.Text = "Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")";
            this.paraBackupBtn.Text = "Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")";
        }

        private void Setup()
        {
            AddCallBackupBtns();

            UIMenuItem warOptionsBtn = new UIMenuItem("War Options...", "Opens the War Menu, containing options useful in a Gang War.");
            AddItem(warOptionsBtn);
            BindMenuToItem(warOptionsSubMenu, warOptionsBtn);

            UIMenuItem gangCustomizeBtn = new UIMenuItem("Gang Customization/Upgrades...", "Opens the Gang Customization and Upgrades menu.");
            AddItem(gangCustomizeBtn);
            BindMenuToItem(gangCustomizeSubMenu, gangCustomizeBtn);

            UIMenuItem modOptionsBtn = new UIMenuItem("Mod Options...", "Opens the Mod Options Menu, which allows various configurations of the mod to be tweaked. Options not found in this menu can only be tweaked directly in the ModOptions.xml file.");
            AddItem(modOptionsBtn);
            BindMenuToItem(modOptionsSubMenu, modOptionsBtn);

            RefreshIndex();
        }

        #region backup-related stuff



        private void AddCallBackupBtns()
        {
            carBackupBtn = new UIMenuItem("Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")", "Calls one of your gang's vehicles to your position. All passengers leave the vehicle once it arrives.");
            paraBackupBtn = new UIMenuItem("Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")", "Calls a gang member who parachutes to your position (member survival not guaranteed!).");

            AddItem(carBackupBtn);
            AddItem(paraBackupBtn);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == carBackupBtn)
                {
                    if (GangManager.instance.CallCarBackup() != null)
                    {
                        Visible = false;
                    }

                }

                if (item == paraBackupBtn)
                {
                    if (GangManager.instance.CallParachutingBackup() != null)
                    {
                        Visible = false;
                    }
                }
            };


        }

        #endregion
    }
}
