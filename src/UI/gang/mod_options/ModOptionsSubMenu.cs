using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NativeUI.UIMenuDynamicListItem;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting various mod options
    /// </summary>
    public class ModOptionsSubMenu : UIMenu
    {
        public ModOptionsSubMenu(string title, string subtitle, MenuPool menuPool) : base(title, subtitle)
        {
			keyBindingsSubMenu = new KeyBindingsSubMenu("Gang and Turf Mod", "Key Bindings");


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
			AddNotificationsToggle();
			AddMemberAggressivenessControl();
			AddEnableAmbientSpawnToggle();
			AddAiExpansionToggle();
			AddShowMemberBlipsToggle();
			AddMeleeOnlyToggle();
			AddEnableWarVersusPlayerToggle();
			AddEnableCarTeleportToggle();
			AddGangsStartWithPistolToggle();
			AddKeyBindingMenu();
			AddGamepadControlsToggle();
			AddPlayerSpectatorToggle();
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

		void AddNotificationsToggle()
		{
			UIMenuCheckboxItem notifyToggle = new UIMenuCheckboxItem("Notifications enabled?", ModOptions.instance.notificationsEnabled, "Enables/disables the displaying of messages whenever a gang takes over a zone.");

			AddItem(notifyToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == notifyToggle)
				{
					ModOptions.instance.notificationsEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddMemberAggressivenessControl()
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

			OnListChange += (sender, item, index) => {
				if (item == aggOption)
				{
					ModOptions.instance.SetMemberAggressiveness((ModOptions.GangMemberAggressivenessMode)index);
				}
			};
		}

		void AddAiExpansionToggle()
		{
			UIMenuCheckboxItem aiToggle = new UIMenuCheckboxItem("Prevent AI Gangs' Expansion?", ModOptions.instance.preventAIExpansion, "If checked, AI Gangs won't start wars or take neutral zones.");

			AddItem(aiToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == aiToggle)
				{
					ModOptions.instance.preventAIExpansion = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddMeleeOnlyToggle()
		{
			UIMenuCheckboxItem meleeToggle = new UIMenuCheckboxItem("Gang members use melee weapons only?", ModOptions.instance.membersSpawnWithMeleeOnly, "If checked, all gang members will spawn with melee weapons only, even if they purchase firearms or are set to start with pistols.");

			AddItem(meleeToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == meleeToggle)
				{
					ModOptions.instance.membersSpawnWithMeleeOnly = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableWarVersusPlayerToggle()
		{
			UIMenuCheckboxItem warToggle = new UIMenuCheckboxItem("Enemy gangs can attack your turf?", ModOptions.instance.warAgainstPlayerEnabled, "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them.");

			AddItem(warToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == warToggle)
				{
					ModOptions.instance.warAgainstPlayerEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableAmbientSpawnToggle()
		{
			UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Ambient member spawning?", ModOptions.instance.ambientSpawningEnabled, "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars.");

			AddItem(spawnToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == spawnToggle)
				{
					ModOptions.instance.ambientSpawningEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddShowMemberBlipsToggle()
		{
			UIMenuCheckboxItem blipToggle = new UIMenuCheckboxItem("Show Member and Car Blips?", ModOptions.instance.showGangMemberBlips, "If disabled, members and cars won't spawn with blips attached to them. (This option only affects those that spawn after the option is set)");

			AddItem(blipToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == blipToggle)
				{
					ModOptions.instance.showGangMemberBlips = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableCarTeleportToggle()
		{
			UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Backup cars can teleport to always arrive?", ModOptions.instance.forceSpawnCars, "If enabled, backup cars, after taking too long to get to the player, will teleport close by. This will only affect friendly vehicles.");

			AddItem(spawnToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == spawnToggle)
				{
					ModOptions.instance.forceSpawnCars = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddGangsStartWithPistolToggle()
		{
			UIMenuCheckboxItem pistolToggle = new UIMenuCheckboxItem("Gangs start with Pistols?", ModOptions.instance.gangsStartWithPistols, "If checked, all gangs, except the player's, will start with pistols. Pistols will not be given to gangs already in town.");

			AddItem(pistolToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == pistolToggle)
				{
					ModOptions.instance.gangsStartWithPistols = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddPlayerSpectatorToggle()
		{
			UIMenuCheckboxItem spectatorToggle = new UIMenuCheckboxItem("Player Is a Spectator", ModOptions.instance.playerIsASpectator, "If enabled, all gangs should ignore the player, even during wars.");

			AddItem(spectatorToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == spectatorToggle)
				{
					ModOptions.instance.playerIsASpectator = checked_;
					ModOptions.instance.SaveOptions(false);
					GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);
				}

			};
		}

		void AddGamepadControlsToggle()
		{
			UIMenuCheckboxItem padToggle = new UIMenuCheckboxItem("Use joypad controls?", ModOptions.instance.joypadControls, "Enables/disables the use of joypad commands to recruit members (pad right), call backup (pad left) and output zone info (pad up). Commands are used while aiming. All credit goes to zixum.");

			AddItem(padToggle);
			OnCheckboxChange += (sender, item, checked_) => {
				if (item == padToggle)
				{
					ModOptions.instance.joypadControls = checked_;
					if (checked_)
					{
						UI.ShowSubtitle("Joypad controls activated. Remember to disable them when not using a joypad, as it is possible to use the commands with mouse/keyboard as well");
					}
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddForceAIGangsTickButton()
		{
			UIMenuItem newButton = new UIMenuItem("Run an Update on all AI Gangs", "Makes all AI Gangs try to upgrade themselves and/or invade other territories immediately. Their normal updates, which happen from time to time (configurable in the ModOptions file), will still happen normally after this.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					GangManager.instance.ForceTickAIGangs();
				}
			};
		}

		void AddForceAIAttackButton()
		{
			UIMenuItem newButton = new UIMenuItem("Force an AI Gang to Attack this zone", "If you control the current zone, makes a random AI Gang attack it, starting a war. The AI gang won't spend money to make this attack.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					GangAI enemyAttackerAI = RandoMath.GetRandomElementFromList(GangManager.instance.enemyGangs);
					if (enemyAttackerAI != null)
					{
						TurfZone curZone = ZoneManager.instance.GetCurrentTurfZone();
						if (curZone != null)
						{
							if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
							{
								if (!GangWarManager.instance.StartWar(enemyAttackerAI.watchedGang, curZone,
									GangWarManager.WarType.defendingFromEnemy, GangWarManager.AttackStrength.medium))
								{
									UI.ShowSubtitle("Couldn't start a war. Is a war already in progress?");
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

		void AddReloadOptionsButton()
		{
			UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					ModOptions.LoadOptionsInstance();
					GangManager.instance.ResetGangUpdateIntervals();
					GangManager.instance.AdjustGangsToModOptions();

					MenuScript.instance.RefreshCostsTexts();
				}
			};
		}

		void AddResetWeaponOptionsButton()
		{
			UIMenuItem newButton = new UIMenuItem("Reset Weapon List and Prices to Defaults", "Resets the weapon list in the ModOptions file back to the default values. The new options take effect immediately.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					ModOptions.instance.buyableWeapons.Clear();
					ModOptions.instance.SetWeaponListDefaultValues();
					ModOptions.instance.SaveOptions(false);

					MenuScript.instance.RefreshCostsTexts();
				}
			};
		}

		void AddResetOptionsButton()
		{
			UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					ModOptions.instance.SetAllValuesToDefault();

					MenuScript.instance.RefreshCostsTexts();
				}
			};
		}
	}
}
