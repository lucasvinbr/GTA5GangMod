using GTA.Math;
using NativeUI;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the B menu, for most gang-related stuff... but also takes to mod options
    /// </summary>
    public class GangMenu : UIMenu
    {
        
        public GangMenu(MenuPool menuPool) : base(
            Localization.GetTextByKey("menu_title_mod_name", "Gang and Turf Mod"), Localization.GetTextByKey("menu_subtitle_mod_name", "Main Menu"))
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
            carBackupBtn.Text = string.Format(
                Localization.GetTextByKey("menu_button_call_backup_vehicle_cost_x", "Call Backup Vehicle (${0})"), 
                ModOptions.instance.costToCallBackupCar.ToString());
            
            paraBackupBtn.Text = string.Format(
                Localization.GetTextByKey("menu_button_call_parachuting_member_cost_x", "Call Parachuting Member (${0})"),
                ModOptions.instance.costToCallParachutingMember.ToString());
        }

        private void Setup()
        {
            AddCallBackupBtns();

                
            UIMenuItem warOptionsBtn = new UIMenuItem(
                Localization.GetTextByKey("menu_button_war_options", "War Options..."),
                Localization.GetTextByKey("menu_button_desc_war_options", "Opens the War Menu, containing options useful in a Gang War."));
            AddItem(warOptionsBtn);
            BindMenuToItem(warOptionsSubMenu, warOptionsBtn);

            
            UIMenuItem gangCustomizeBtn = new UIMenuItem(
                Localization.GetTextByKey("menu_button_gang_customization_upgrades", "Gang Customization/Upgrades..."),
                Localization.GetTextByKey("menu_button_desc_gang_customization_upgrades", "Opens the Gang Customization and Upgrades menu."));
            AddItem(gangCustomizeBtn);
            BindMenuToItem(gangCustomizeSubMenu, gangCustomizeBtn);

            
            UIMenuItem modOptionsBtn = new UIMenuItem(
                Localization.GetTextByKey("menu_button_mod_options", "Mod Options..."),
                Localization.GetTextByKey("menu_button_desc_mod_options", "Opens the Mod Options Menu, which allows various configurations of the mod to be tweaked. Options not found in this menu can only be tweaked directly in the ModOptions.xml file."));
            AddItem(modOptionsBtn);
            BindMenuToItem(modOptionsSubMenu, modOptionsBtn);

            RefreshIndex();
        }

        #region backup-related stuff



        private void AddCallBackupBtns()
        {
            
            carBackupBtn = new UIMenuItem(
                string.Format(
                    Localization.GetTextByKey("menu_button_call_backup_vehicle_cost_x", "Call Backup Vehicle (${0})"),
                    ModOptions.instance.costToCallBackupCar.ToString()),
                Localization.GetTextByKey("menu_button_desc_call_backup_vehicle", "Calls one of your gang's vehicles to your position. The driver will leave the vehicle once it arrives."));

            paraBackupBtn = new UIMenuItem(
                string.Format(
                    Localization.GetTextByKey("menu_button_call_parachuting_member_cost_x", "Call Parachuting Member (${0})"),
                    ModOptions.instance.costToCallParachutingMember.ToString()),
                Localization.GetTextByKey("menu_button_desc_call_parachuting_member", "Calls a gang member who parachutes to your position (member survival not guaranteed!).")
                );

            

            UIMenuItem enemyBackupBtn = new UIMenuItem(
                Localization.GetTextByKey("menu_button_spawn_enemy_backup_vehicle", "Spawn Enemy Backup Vehicle"),
                Localization.GetTextByKey("menu_button_desc_spawn_enemy_backup_vehicle", "Spawns a vehicle of the target AI gang, attempting to reach your location.")
                );

            AddItem(carBackupBtn);
            AddItem(paraBackupBtn);
            AddItem(enemyBackupBtn);

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

                if(item == enemyBackupBtn)
                {
                    
                    MenuScript.instance.OpenPickAiGangMenu(
                        this,
                        Localization.GetTextByKey("menu_subtitle_select_gang_spawn_vehicle", "Select gang from which to spawn a vehicle"),
                        (pickedGang) =>
                    {
                        Vector3 playerPos = MindControl.SafePositionNearPlayer;

                        SpawnedDrivingGangMember spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(pickedGang,
                            SpawnManager.instance.FindGoodSpawnPointForCar(playerPos), playerPos, true, true);
                        if (spawnedVehicle != null)
                        {
                            UI.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_spawned", "Vehicle spawned!"), 1000);
                        }
                        else
                        {
                            UI.ShowSubtitle(Localization.GetTextByKey("subtitle_too_many_members_around_or_picked_gang_has_no_vehicles_members", "There are too many gang members around or the picked gang has no vehicles/members registered."));
                        }
                    });
                }
            };


        }

        #endregion
    }
}
