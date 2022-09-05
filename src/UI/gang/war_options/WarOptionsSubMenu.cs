using NativeUI;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting spawn points and skipping wars
    /// </summary>
    public class WarOptionsSubMenu : UIMenu
    {
        public WarOptionsSubMenu(MenuPool menuPool) : base("Gang and Turf Mod", "War Options")
        {
            warPotentialSpawnsSubMenu = new WarPotentialSpawnsSubMenu();

            menuPool.Add(this);
            menuPool.Add(warPotentialSpawnsSubMenu);

            Setup();
        }

        private readonly WarPotentialSpawnsSubMenu warPotentialSpawnsSubMenu;

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            UIMenuItem skipWarBtn = new UIMenuItem("Skip current War",
               "If a war is currently occurring, it will instantly end, and its outcome will be defined by the strength and reinforcements of the involved gangs and a touch of randomness.");

            AddItem(skipWarBtn);

            UIMenuCheckboxItem showReinforcementsAIWarsToggle = new UIMenuCheckboxItem("Show reinforcement counts for AI Wars", 
                ModOptions.instance.showReinforcementCountsForAIWars,
               "If enabled, reinforcement counts will also be shown when inside a war the player's gang is not involved in.");

            UIMenuCheckboxItem lockReinforcementsToggle = new UIMenuCheckboxItem("Lock current war reinforcement count",
                ModOptions.instance.lockCurWarReinforcementCount,
               "If enabled, reinforcement counts of the current war will never drop, making the war never end. This doesn't affect auto-resolution of distant wars.");

            AddItem(showReinforcementsAIWarsToggle);
            AddItem(lockReinforcementsToggle);


            UIMenuItem warSpawnsMenuBtn = new UIMenuItem("War Potential Spawns...", "Opens the War Potential Spawns Menu, which allows viewing, creating and deleting spawns to be used in wars.");
            AddItem(warSpawnsMenuBtn);
            BindMenuToItem(warPotentialSpawnsSubMenu, warSpawnsMenuBtn);

            OnItemSelect += (sender, item, index) =>
            {

                if (item == skipWarBtn)
                {
                    if (GangWarManager.instance.focusedWar != null)
                    {
                        while(GangWarManager.instance.focusedWar != null)
                        {
                            GangWarManager.instance.focusedWar.RunAutoResolveStep(1.1f);
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("There is no war in progress here.");
                    }
                }
            };

            OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == showReinforcementsAIWarsToggle)
                {
                    ModOptions.instance.showReinforcementCountsForAIWars = checked_;
                    ModOptions.instance.SaveOptions(false);
                }

                if (item == lockReinforcementsToggle)
                {
                    ModOptions.instance.lockCurWarReinforcementCount = checked_;
                    ModOptions.instance.SaveOptions(false);
                }
            };

            RefreshIndex();
        }

    }
}
