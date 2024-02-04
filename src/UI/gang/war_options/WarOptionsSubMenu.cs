using LemonUI;
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting spawn points and skipping wars
    /// </summary>
    public class WarOptionsSubMenu : ModMenu
    {
        public WarOptionsSubMenu(ObjectPool menuPool) : base("war_options", "War Options")
        {
            warPotentialSpawnsSubMenu = new WarPotentialSpawnsSubMenu();

            menuPool.Add(this);
            menuPool.Add(warPotentialSpawnsSubMenu);
        }

        private readonly WarPotentialSpawnsSubMenu warPotentialSpawnsSubMenu;

        protected override void RecreateItems()
        {
            DetachEventsForModOptionEntries();
            modOptionCheckBoxes.Clear();

            Clear();

            NativeItem skipWarBtn = new NativeItem(Localization.GetTextByKey("menu_button_skip_current_war", "Skip current War"),
               Localization.GetTextByKey("menu_button_skip_current_war_desc", "If a war is currently occurring, it will instantly end, and its outcome will be defined by the strength and reinforcements of the involved gangs and a touch of randomness."));
            skipWarBtn.Activated += (sender, args) =>
            {
                if (GangWarManager.instance.focusedWar != null)
                {
                    while (GangWarManager.instance.focusedWar != null)
                    {
                        GangWarManager.instance.focusedWar.RunAutoResolveStep(1.1f);
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_no_war_in_progress_here", "There is no war in progress here."));
                }
            };
            Add(skipWarBtn);

            AddModOptionToggle(nameof(ModOptions.instance.showReinforcementCountsForAIWars),
                Localization.GetTextByKey("menu_toggle_modoption_showReinforcementCountsForAIWars", "Show reinforcement counts for AI Wars"),
                Localization.GetTextByKey("menu_toggle_modoption_showReinforcementCountsForAIWars_desc", "If enabled, reinforcement counts will also be shown when inside a war the player's gang is not involved in.")
                );

            AddModOptionToggle(nameof(ModOptions.instance.lockCurWarReinforcementCount),
                Localization.GetTextByKey("menu_toggle_modoption_lockCurWarReinforcementCount", "Lock current war reinforcement count"),
                Localization.GetTextByKey("menu_toggle_modoption_lockCurWarReinforcementCount_desc", "If enabled, reinforcement counts of the current war will never drop, making the war never end. This doesn't affect auto-resolution of distant wars.")
                );


            var warSpawnsMenuBtn = new NativeSubmenuItem(warPotentialSpawnsSubMenu, this);
            warSpawnsMenuBtn.Title = Localization.GetTextByKey("menu_button_submenu_war_potential_spawns", "War Potential Spawns...");
            warSpawnsMenuBtn.Description = Localization.GetTextByKey("menu_button_submenu_war_potential_spawns_desc", "Opens the War Potential Spawns Menu, which allows viewing, creating and deleting spawns to be used in wars.");
            Add(warSpawnsMenuBtn);
        }
    }
}
