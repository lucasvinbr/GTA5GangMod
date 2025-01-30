using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting potential spawn points and skipping wars
    /// </summary>
    public class WarPotentialSpawnsSubMenu : ModMenu
    {
        public WarPotentialSpawnsSubMenu() : base("war_potential_spawns", "War Potential Spawns Menu")
        {
        }

        protected override void RecreateItems()
        {
            Clear();

            NativeCheckboxItem showSpawnBlipsToggle = new NativeCheckboxItem(Localization.GetTextByKey("menu_button_show_pot_spawns_on_map", "Show spawns on map"),
               Localization.GetTextByKey("menu_button_show_pot_spawns_on_map_desc", "For debugging purposes. If enabled, the nearest spawns will be marked with blips on the map."),
               false);

            NativeItem addNewSpawnBtn = new NativeItem(Localization.GetTextByKey("menu_button_add_pot_spawn_here", "Add New Potential Spawn Here"),
               Localization.GetTextByKey("menu_button_add_pot_spawn_here_desc", "Adds a new position to be used as a spawn/control point in Gang Wars. You must be on the ground for the spawn to work correctly!"));

            NativeItem removeSpawnBtn = new NativeItem(Localization.GetTextByKey("menu_button_remove_nearby_pot_spawn", "Remove Nearby Spawn"),
               Localization.GetTextByKey("menu_button_remove_nearby_pot_spawn_desc", "The first spawn found nearby will be deleted from the potential spawns."));

            showSpawnBlipsToggle.CheckboxChanged += (sender, args) =>
            {
                PotentialSpawnsForWars.ToggleBlips(showSpawnBlipsToggle.Checked);
                if (showSpawnBlipsToggle.Checked) PotentialSpawnsForWars.UpdateBlipDisplay(MindControl.CurrentPlayerCharacter.Position);
            };

            addNewSpawnBtn.Activated += (sender, args) =>
            {
                if (MindControl.CurrentPlayerCharacter.IsInAir)
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_must_be_on_ground_add_pot_spawn", "You must be on the ground to add a potential spawn!"));
                    return;
                }

                if (PotentialSpawnsForWars.AddPositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_pot_spawn_added", "Potential spawn added!"));
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_another_pot_spawn_too_close", "There's another spawn too close to this one!"));
                }
            };

            removeSpawnBtn.Activated += (sender, args) =>
            {
                if (PotentialSpawnsForWars.RemovePositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_pot_spawn_removed", "Potential spawn removed!"));
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_no_pot_spawn_nearby", "Couldn't find a potential spawn nearby! Try getting closer."));
                }
            };

            Add(showSpawnBlipsToggle);
            Add(addNewSpawnBtn);
            Add(removeSpawnBtn);
        }
    }
}
