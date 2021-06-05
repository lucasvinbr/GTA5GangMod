using NativeUI;
using System.Collections.Generic;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting various mod options
    /// </summary>
    public class ModOptionsSubMenu : ModMenu
    {
        public ModOptionsSubMenu(MenuPool menuPool) : base("Mod Options")
        {
            keyBindingsSubMenu = new KeyBindingsSubMenu();


            menuPool.Add(this);
            menuPool.Add(keyBindingsSubMenu);

            Setup();
        }

        public UIMenuListItem aggOption;
        private readonly KeyBindingsSubMenu keyBindingsSubMenu;


        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            AddModOptionToggle(nameof(ModOptions.instance.notificationsEnabled),
                "Notifications enabled?",
                "Enables/disables the displaying of messages whenever a gang takes over a zone.");
            
            AddMemberAggressivenessControl();
            
            AddModOptionToggle(nameof(ModOptions.instance.ambientSpawningEnabled),
                 "Ambient member spawning?",
                 "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars.");
            
            AddModOptionToggle(nameof(ModOptions.instance.ignoreTurfOwnershipWhenAmbientSpawning),
                 "Ambient spawning: random gangs",
                 "If enabled, ambient spawning will spawn members from any gang instead of only from the one which owns the zone you are in. This option does not affect member spawning via backup calls or gang wars.");
            
            AddModOptionToggle(nameof(ModOptions.instance.preventAIExpansion),
                "Prevent AI Gangs' Expansion?",
                "If checked, AI Gangs won't start wars or take neutral zones.");
            
            AddModOptionToggle(nameof(ModOptions.instance.showGangMemberBlips),
                "Show Member and Car Blips?",
                "If disabled, members and cars won't spawn with blips attached to them. (This option only affects those that spawn after the option is set)");
            
            AddModOptionToggle(nameof(ModOptions.instance.membersSpawnWithMeleeOnly),
                 "Gang members use melee weapons only?",
                 "If checked, all gang members will spawn with melee weapons only, even if they purchase firearms or are set to start with pistols.");

            AddModOptionToggle(nameof(ModOptions.instance.warAgainstPlayerEnabled),
                 "Enemy gangs can attack your turf?",
                 "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them.");

            AddModOptionToggle(nameof(ModOptions.instance.forceSpawnCars),
                 "Backup cars can teleport to always arrive?",
                 "If enabled, backup cars, after taking too long to get to the player, will teleport close by. This will only affect friendly vehicles.");

            AddModOptionToggle(nameof(ModOptions.instance.gangsStartWithPistols),
                 "Gangs start with Pistols?",
                 "If checked, all gangs, except the player's, will start with pistols. Pistols will not be given to gangs already in town.");

            AddKeyBindingMenu();
            
            AddModOptionToggle(nameof(ModOptions.instance.joypadControls),
                 "Use joypad controls?",
                 "Enables/disables the use of joypad commands to recruit members (pad right), call backup (pad left) and output zone info (pad up). Commands are used while aiming. All credit goes to zixum.",
                 (nowChecked) =>
                 {
                     if (nowChecked)
                     {
                         UI.ShowSubtitle("Joypad controls activated. Remember to disable them when not using a joypad, as it is possible to use the commands with mouse/keyboard as well");
                     }
                 });
            
            AddModOptionToggle(nameof(ModOptions.instance.playerIsASpectator),
                 "Player Is a Spectator",
                 "If enabled, all gangs should ignore the player, even during wars.",
                 (newValue) => GangManager.instance.SetGangRelationsAccordingToAggrLevel());

            AddForceAIGangsTickButton();
            AddForceAIAttackButton();
            AddReloadOptionsButton();
            AddResetWeaponOptionsButton();
            AddResetOptionsButton();

            RefreshIndex();
        }

        private void AddKeyBindingMenu()
        {
            UIMenuItem newButton = new UIMenuItem("Key Bindings...", "Opens the Key Bindings Menu, which allows setting which keys are linked to this mod's commands.");
            AddItem(newButton);
            BindMenuToItem(keyBindingsSubMenu, newButton);
        }

        private void AddMemberAggressivenessControl()
        {
            List<dynamic> aggModes = new List<dynamic>
            {
                "V. Aggressive",
                "Aggressive",
                "Defensive"
            };

            aggOption = new UIMenuListItem("Member Aggressiveness", aggModes, (int)ModOptions.instance.gangMemberAggressiveness, "This controls how aggressive members from all gangs will be. Very aggressive members will shoot at cops and other gangs on sight, aggressive members will shoot only at other gangs on sight and defensive members will only shoot when one of them is attacked or aimed at.");
            AddItem(aggOption);

            aggOption.Index = (int)ModOptions.instance.gangMemberAggressiveness;

            OnListChange += (sender, item, index) =>
            {
                if (item == aggOption)
                {
                    ModOptions.instance.SetMemberAggressiveness((ModOptions.GangMemberAggressivenessMode)index);
                }
            };
        }

        private void AddForceAIGangsTickButton()
        {
            UIMenuItem newButton = new UIMenuItem("Run an Update on all AI Gangs", "Makes all AI Gangs try to upgrade themselves and/or invade other territories immediately. Their normal updates, which happen from time to time (configurable in the ModOptions file), will still happen normally after this.");
            AddItem(newButton);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    GangManager.instance.ForceTickAIGangs();
                }
            };
        }

        private void AddForceAIAttackButton()
        {
            UIMenuItem newButton = new UIMenuItem("Force an AI Gang to Attack this zone", "If you control the current zone, makes a random AI Gang attack it, starting a war. The AI gang won't spend money to make this attack.");
            AddItem(newButton);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
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
                                    UI.ShowSubtitle("Couldn't start a war. Is a war already in progress in this zone?");
                                }
                            }
                            else
                            {
                                UI.ShowSubtitle("The zone you are in is not controlled by your gang.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("The zone you are in has not been marked as takeable.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("There aren't any enemy gangs in San Andreas!");
                    }
                }
            };
        }

        private void AddReloadOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
            AddItem(newButton);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.LoadOptionsInstance();
                    GangManager.instance.ResetGangUpdateIntervals();
                    GangManager.instance.AdjustGangsToModOptions();
                    GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);

                    MenuScript.instance.RefreshCostsTexts();
                }
            };
        }

        private void AddResetWeaponOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Weapon List and Prices to Defaults", "Resets the weapon list in the ModOptions file back to the default values. The new options take effect immediately.");
            AddItem(newButton);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.instance.buyableWeapons.Clear();
                    ModOptions.instance.SetWeaponListDefaultValues();
                    ModOptions.instance.SaveOptions(false);

                    MenuScript.instance.RefreshCostsTexts();
                }
            };
        }

        private void AddResetOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
            AddItem(newButton);
            OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.instance.SetAllValuesToDefault();

                    MenuScript.instance.RefreshCostsTexts();
                }
            };
        }
    }
}
