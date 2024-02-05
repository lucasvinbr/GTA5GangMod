using LemonUI;
using LemonUI.Menus;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting various mod options
    /// </summary>
    public class ModOptionsSubMenu : ModMenu
    {
        public ModOptionsSubMenu(ObjectPool menuPool) : base("modoptions_submenu_main", "Mod Options")
        {
            keyBindingsSubMenu = new KeyBindingsSubMenu();

            menuPool.Add(this);
            menuPool.Add(keyBindingsSubMenu);

            RecreateItems();
        }

        private readonly KeyBindingsSubMenu keyBindingsSubMenu;


        protected override void Setup()
        {
            Localization.OnLanguageChanged += OnLocalesChanged;
            Shown += RebuildItemsIfNeeded;
        }

        private void AddKeyBindingMenu()
        {
            var newButton = new NativeSubmenuItem(keyBindingsSubMenu, this);
            newButton.Title = Localization.GetTextByKey("menu_button_open_key_bindings", "Key Bindings...");
            newButton.Description = Localization.GetTextByKey("menu_button_open_key_bindings_desc", "Opens the Key Bindings Menu, which allows setting which keys are linked to this mod's commands.");
            Add(newButton);
        }

        private void AddMemberAggressivenessControl()
        {
            List<string> aggModes = new List<string>
            {
                Localization.GetTextByKey("member_aggressiveness_level_very_aggressive", "V. Aggressive"),
                Localization.GetTextByKey("member_aggressiveness_level_aggressive", "Aggressive"),
                Localization.GetTextByKey("member_aggressiveness_level_defensive", "Defensive")
            };

            var aggOption = new NativeListItem<string>(Localization.GetTextByKey("menu_listitem_member_aggressiveness", "Member Aggressiveness"),
                Localization.GetTextByKey("menu_listitem_member_aggressiveness_desc", "This controls how aggressive members from all gangs will be. Very aggressive members will shoot at cops and other gangs on sight, aggressive members will shoot only at other gangs on sight and defensive members will only shoot when one of them is attacked or aimed at."));
            aggOption.Items = aggModes;
            Add(aggOption);

            aggOption.SelectedIndex = (int)ModOptions.instance.gangMemberAggressiveness;

            aggOption.ItemChanged += (sender, args) =>
            {
                ModOptions.instance.SetMemberAggressiveness((ModOptions.GangMemberAggressivenessMode)args.Index);
            };
        }

        private void AddForceAIGangsTickButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_force_run_update_ai_gangs", "Run an Update on all AI Gangs"),
                Localization.GetTextByKey("menu_button_force_run_update_ai_gangs_desc", "Makes all AI Gangs try to upgrade themselves and/or invade other territories immediately. Their normal updates, which happen from time to time (configurable in the ModOptions file), will still happen normally after this."));
            Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                GangManager.instance.ForceTickAIGangs();
            };
        }

        private void AddForceAIAttackButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_force_ai_gang_attack_this_zone", "Force an AI Gang to Attack this zone"), 
                Localization.GetTextByKey("menu_button_force_ai_gang_attack_this_zone_desc", "If you control the current zone, makes a random AI Gang attack it, starting a war. The AI gang won't spend money to make this attack."));
            Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                GangAI enemyAttackerAI = RandoMath.RandomElement(GangManager.instance.enemyGangs);
                if (enemyAttackerAI != null)
                {
                    TurfZone curZone = ZoneManager.instance.GetCurrentTurfZone();
                    if (curZone != null)
                    {
                        if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
                        {
                            if (!GangWarManager.instance.TryStartWar(enemyAttackerAI.watchedGang, curZone, GangWarManager.AttackStrength.medium))
                            {
                                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_couldnt_start_forced_war", "Couldn't start a war. Is a war already in progress in this zone?"));
                            }
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_you_in_not_controlled_by_your_gang", "The zone you are in is not controlled by your gang."));
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_you_in_not_takeable", "The zone you are in has not been marked as takeable."));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_no_enemy_gangs_exist", "There aren't any enemy gangs in San Andreas!"));
                }
            };
        }

        private void AddReloadOptionsButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_reload_modoptions", "Reload Mod Options"), 
                Localization.GetTextByKey("menu_button_reload_modoptions_desc", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect."));
            Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                ModOptions.LoadOptionsInstance();
                ModOptions.OnModOptionsReloaded?.Invoke();
            };
        }

        private void AddResetWeaponOptionsButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_reset_weapon_list_prices", "Reset Weapon List and Prices to Defaults"),
                Localization.GetTextByKey("menu_button_reset_weapon_list_prices_desc", "Resets the weapon list in the ModOptions file back to the default values. The new options take effect immediately."));
            Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                ModOptions.instance.buyableWeapons.Clear();
                ModOptions.instance.SetWeaponListDefaultValues();
                ModOptions.instance.SaveOptions(false);

                MenuScript.instance.RefreshCostsTexts();
            };
        }

        private void AddResetOptionsButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_reset_modoptions", "Reset Mod Options to Defaults"),
                Localization.GetTextByKey("menu_button_reset_modoptions_desc", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately."));
            Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                ModOptions.instance.SetAllValuesToDefault();
            };
        }

        protected override void RecreateItems()
        {
            DetachEventsForModOptionEntries();
            modOptionCheckBoxes.Clear();

            Clear();

            AddModOptionToggle(nameof(ModOptions.instance.notificationsEnabled),
                Localization.GetTextByKey("menu_toggle_modoption_notificationsEnabled", "Notifications enabled?"),
                Localization.GetTextByKey("menu_toggle_modoption_notificationsEnabled_desc", "Enables/disables the displaying of messages whenever a gang takes over a zone."));

            AddMemberAggressivenessControl();

            AddModOptionToggle(nameof(ModOptions.instance.ambientSpawningEnabled),
                 Localization.GetTextByKey("menu_toggle_modoption_ambientSpawningEnabled", "Ambient member spawning?"),
                 Localization.GetTextByKey("menu_toggle_modoption_ambientSpawningEnabled_desc", "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars."));

            AddModOptionToggle(nameof(ModOptions.instance.ignoreTurfOwnershipWhenAmbientSpawning),
                 Localization.GetTextByKey("menu_toggle_modoption_ignoreTurfOwnershipWhenAmbientSpawning", "Ambient spawning: random gangs"),
                 Localization.GetTextByKey("menu_toggle_modoption_ignoreTurfOwnershipWhenAmbientSpawning_desc", "If enabled, ambient spawning will spawn members from any gang instead of only from the one which owns the zone you are in. This option does not affect member spawning via backup calls or gang wars."));

            AddModOptionToggle(nameof(ModOptions.instance.preventAIExpansion),
                Localization.GetTextByKey("menu_toggle_modoption_preventAIExpansion", "Prevent AI Gangs' Expansion?"),
                Localization.GetTextByKey("menu_toggle_modoption_preventAIExpansion_desc", "If checked, AI Gangs won't start wars or take neutral zones."));

            AddModOptionToggle(nameof(ModOptions.instance.showGangMemberBlips),
                Localization.GetTextByKey("menu_toggle_modoption_showGangMemberBlips", "Show Member and Car Blips?"),
                Localization.GetTextByKey("menu_toggle_modoption_showGangMemberBlips_desc", "If disabled, members and cars won't spawn with blips attached to them. (This option only affects those that spawn after the option is set)"));

            AddModOptionToggle(nameof(ModOptions.instance.membersSpawnWithMeleeOnly),
                 Localization.GetTextByKey("menu_toggle_modoption_membersSpawnWithMeleeOnly", "Gang members use melee weapons only?"),
                 Localization.GetTextByKey("menu_toggle_modoption_membersSpawnWithMeleeOnly_desc", "If checked, all gang members will spawn with melee weapons only, even if they purchase firearms or are set to start with pistols."));

            AddModOptionToggle(nameof(ModOptions.instance.warAgainstPlayerEnabled),
                 Localization.GetTextByKey("menu_toggle_modoption_warAgainstPlayerEnabled", "Enemy gangs can attack your turf?"),
                 Localization.GetTextByKey("menu_toggle_modoption_warAgainstPlayerEnabled_desc", "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them."));

            AddModOptionToggle(nameof(ModOptions.instance.forceSpawnCars),
                 Localization.GetTextByKey("menu_toggle_modoption_forceSpawnCars", "Backup cars can teleport to always arrive?"),
                 Localization.GetTextByKey("menu_toggle_modoption_forceSpawnCars_desc", "If enabled, backup cars, after taking too long to get to the player, will teleport close by. This will only affect friendly vehicles."));

            AddModOptionToggle(nameof(ModOptions.instance.gangsStartWithPistols),
                 Localization.GetTextByKey("menu_toggle_modoption_gangsStartWithPistols", "Gangs start with Pistols?"),
                 Localization.GetTextByKey("menu_toggle_modoption_gangsStartWithPistols_desc", "If checked, all gangs, except the player's, will start with pistols. Pistols will not be given to gangs already in town."));

            AddKeyBindingMenu();

            AddModOptionToggle(nameof(ModOptions.instance.joypadControls),
                 Localization.GetTextByKey("menu_toggle_modoption_joypadControls", "Use joypad controls?"),
                 Localization.GetTextByKey("menu_toggle_modoption_joypadControls_desc", "Enables/disables the use of joypad commands to recruit members (pad right), call backup (pad left) and output zone info (pad up). Commands are used while aiming. All credit goes to zixum."),
                 (nowChecked) =>
                 {
                     if (nowChecked)
                     {
                         UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_joypad_controls_activated", "Joypad controls activated. Remember to disable them when not using a joypad, as it is possible to use the commands with mouse/keyboard as well"));
                     }
                 });

            AddModOptionToggle(nameof(ModOptions.instance.protagonistsAreSpectators),
                 Localization.GetTextByKey("menu_toggle_modoption_protagonistsAreSpectators", "Player Is a Spectator"),
                 Localization.GetTextByKey("menu_toggle_modoption_protagonistsAreSpectators_desc", "If enabled, all gangs should ignore the player, even during wars."),
                 (newValue) => GangManager.instance.SetGangRelationsAccordingToAggrLevel());

            AddForceAIGangsTickButton();
            AddForceAIAttackButton();
            AddReloadOptionsButton();
            AddResetWeaponOptionsButton();
            AddResetOptionsButton();
        }
    }
}
