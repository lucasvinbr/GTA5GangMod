using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting potential spawn points and skipping wars
    /// </summary>
    public class WarPotentialSpawnsSubMenu : NativeMenu
    {
        public WarPotentialSpawnsSubMenu() : base("Gang and Turf Mod", "War Potential Spawns Menu")
        {
            Setup();
        }

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            NativeCheckboxItem showSpawnBlipsToggle = new NativeCheckboxItem("Show spawns on map", false,
               "For debugging purposes. If enabled, the nearest spawns will be marked with blips on the map.");

            NativeItem addNewSpawnBtn = new NativeItem("Add New Potential Spawn Here",
               "Adds a new position to be used as a spawn/control point in Gang Wars. You must be on the ground for the spawn to work correctly!");

            NativeItem removeSpawnBtn = new NativeItem("Remove Nearby Spawn",
               "The first spawn found nearby will be deleted from the potential spawns.");


            OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == showSpawnBlipsToggle)
                {
                    PotentialSpawnsForWars.ToggleBlips(checked_);
                    if (checked_) PotentialSpawnsForWars.UpdateBlipDisplay(MindControl.CurrentPlayerCharacter.Position);
                }
            };

            Add(showSpawnBlipsToggle);
            Add(addNewSpawnBtn);
            Add(removeSpawnBtn);

            OnItemSelect += (sender, item, index) =>
            {
                if (item == addNewSpawnBtn)
                {
                    if (MindControl.CurrentPlayerCharacter.IsInAir)
                    {
                        UI.Screen.ShowSubtitle("You must be on the ground to add a potential spawn!");
                        return;
                    }

                    if(PotentialSpawnsForWars.AddPositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                    {
                        UI.Screen.ShowSubtitle("Potential spawn added!");
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("There's another spawn too close to this one!");
                    }
                    
                }
                else if (item == removeSpawnBtn)
                {
                    if (PotentialSpawnsForWars.RemovePositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                    {
                        UI.Screen.ShowSubtitle("Potential spawn removed!");
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle("Couldn't find a potential spawn nearby! Try getting closer.");
                    }
                }
            };

            
        }

    }
}
