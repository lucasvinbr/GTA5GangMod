using NativeUI;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting potential spawn points and skipping wars
    /// </summary>
    public class WarPotentialSpawnsSubMenu : UIMenu
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
            UIMenuCheckboxItem showSpawnBlipsToggle = new UIMenuCheckboxItem("Show spawns on map", false,
               "If enabled, all spawns will be marked with blips on the map.");

            UIMenuItem addNewSpawnBtn = new UIMenuItem("Add New Potential Spawn Here",
               "Adds a new position to be used as a spawn/control point in Gang Wars. You must be on the ground for the spawn to work correctly!");

            UIMenuItem removeSpawnBtn = new UIMenuItem("Remove Nearby Spawn",
               "The first spawn found nearby will be deleted from the potential spawns.");


            OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == showSpawnBlipsToggle)
                {
                    PotentialSpawnsForWars.ToggleBlips(checked_);
                }
            };

            AddItem(showSpawnBlipsToggle);
            AddItem(addNewSpawnBtn);
            AddItem(removeSpawnBtn);

            OnItemSelect += (sender, item, index) =>
            {
                if (item == addNewSpawnBtn)
                {
                    if (MindControl.CurrentPlayerCharacter.IsInAir)
                    {
                        UI.ShowSubtitle("You must be on the ground to add a potential spawn!");
                        return;
                    }

                    if(PotentialSpawnsForWars.AddPositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                    {
                        UI.ShowSubtitle("Potential spawn added!");
                    }
                    else
                    {
                        UI.ShowSubtitle("There's another spawn too close to this one!");
                    }
                    
                }
                else if (item == removeSpawnBtn)
                {
                    if (PotentialSpawnsForWars.RemovePositionAndSave(MindControl.CurrentPlayerCharacter.Position))
                    {
                        UI.ShowSubtitle("Potential spawn removed!");
                    }
                    else
                    {
                        UI.ShowSubtitle("Couldn't find a potential spawn nearby! Try getting closer.");
                    }
                }
            };

            RefreshIndex();
        }

    }
}
