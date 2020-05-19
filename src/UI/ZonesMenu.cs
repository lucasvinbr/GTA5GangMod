using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for creating and editing custom zones
    /// </summary>
    public class ZonesMenu : UIMenu
    {
        public ZonesMenu(string title, string subtitle, MenuPool menuPool) : base(title, subtitle)
        {

			warAttackStrengthMenu = new UIMenu("Gang Mod", "Gang War Attack Options");
			customZonesSubMenu = new CustomZonesSubMenu("Gang Mod", "Custom Zones Menu");

			menuPool.Add(this);
			menuPool.Add(warAttackStrengthMenu);
			menuPool.Add(customZonesSubMenu);

			//add buttons to self and warMenu...
			AddGangWarAtkOptions();
			AddGangTakeoverButton();
			AddZoneUpgradeButton();
			AddAbandonZoneButton();
			AddSaveZoneButton();

			customZonesSubMenu.Setup();

			BindMenuToItem(customZonesSubMenu, new UIMenuItem("Edit/Create Custom Zones...", "Opens the Custom Zones Menu"));

			RefreshIndex();
			warAttackStrengthMenu.RefreshIndex();
		}

		public UIMenu warAttackStrengthMenu;

		private CustomZonesSubMenu customZonesSubMenu;

		private UIMenuItem takeZoneButton, upgradeZoneValueBtn, warLightAtkBtn, warMedAtkBtn, warLargeAtkBtn, warMassAtkBtn;

		private int curZoneValueUpgradeCost,
			warLightAtkCost, warMedAtkCost, warLargeAtkCost, warMassAtkCost;


		

		void AddSaveZoneButton()
		{
			UIMenuItem saveZoneBtn = new UIMenuItem("Add Current Zone to Takeables", "Makes the zone you are in become takeable by gangs and sets your position as the zone's reference position (if toggled, this zone's blip will show here).");
			AddItem(saveZoneBtn);
			OnItemSelect += (sender, item, index) => {
				if (item == saveZoneBtn)
				{
					string curZoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
					TurfZone curZone = ZoneManager.instance.GetZoneInLocation(curZoneName, MindControl.CurrentPlayerCharacter.Position);
					if (curZone == null)
					{
						//add a new zone then
						curZone = new TurfZone(curZoneName);
					}

					//update the zone's blip position even if it already existed
					curZone.zoneBlipPosition = MindControl.CurrentPlayerCharacter.Position;
					ZoneManager.instance.UpdateZoneData(curZone);
					UI.ShowSubtitle("Zone Data Updated!");
				}
			};

		}

		void AddGangTakeoverButton()
		{
			takeZoneButton = new UIMenuItem("Take current zone",
				"Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
				ModOptions.instance.baseCostToTakeTurf.ToString() + ". If it belongs to another gang, a battle will begin!");
			AddItem(takeZoneButton);
			OnItemSelect += (sender, item, index) => {
				if (item == takeZoneButton)
				{
					TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
					if (curZone == null)
					{
						UI.ShowSubtitle("this zone isn't marked as takeable.");
					}
					else
					{
						if (curZone.ownerGangName == "none")
						{
							if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.baseCostToTakeTurf))
							{
								GangManager.instance.PlayerGang.TakeZone(curZone);
								UI.ShowSubtitle("This zone is " + GangManager.instance.PlayerGang.name + " turf now!");
							}
							else
							{
								UI.ShowSubtitle("You don't have the resources to take over a neutral zone.");
							}
						}
						else
						{
							if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
							{
								UI.ShowSubtitle("Your gang already owns this zone.");
							}
							else
							{
								Visible = !Visible;
								UpdateGangWarAtkOptions(curZone);
								warAttackStrengthMenu.Visible = true;
							}
						}
					}
				}
			};
		}

		

		string GetReinforcementsComparisonMsg(GangWarManager.AttackStrength atkStrength, int defenderNumbers)
		{
			return string.Concat("We will have ",
				GangCalculations.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, atkStrength), " members against their ",
				defenderNumbers.ToString());
		}

		void AddGangWarAtkOptions()
		{

			Gang playerGang = GangManager.instance.PlayerGang;

			warLightAtkBtn = new UIMenuItem("Attack", "Attack. (Text set elsewhere)"); //those are updated when this menu is opened (UpdateGangWarAtkOptions)
			warMedAtkBtn = new UIMenuItem("Attack", "Attack.");
			warLargeAtkBtn = new UIMenuItem("Attack", "Attack.");
			warMassAtkBtn = new UIMenuItem("Attack", "Attack.");
			UIMenuItem cancelBtn = new UIMenuItem("Cancel", "Cancels the attack. No money is lost for canceling.");
			warAttackStrengthMenu.AddItem(warLightAtkBtn);
			warAttackStrengthMenu.AddItem(warMedAtkBtn);
			warAttackStrengthMenu.AddItem(warLargeAtkBtn);
			warAttackStrengthMenu.AddItem(warMassAtkBtn);
			warAttackStrengthMenu.AddItem(cancelBtn);

			warAttackStrengthMenu.OnItemSelect += (sender, item, index) => {
				TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
				if (item == warLightAtkBtn)
				{
					if (TryStartWar(warLightAtkCost, curZone, GangWarManager.AttackStrength.light)) warAttackStrengthMenu.Visible = false;
				}
				if (item == warMedAtkBtn)
				{
					if (TryStartWar(warMedAtkCost, curZone, GangWarManager.AttackStrength.medium)) warAttackStrengthMenu.Visible = false;
				}
				if (item == warLargeAtkBtn)
				{
					if (TryStartWar(warLargeAtkCost, curZone, GangWarManager.AttackStrength.large)) warAttackStrengthMenu.Visible = false;
				}
				if (item == warMassAtkBtn)
				{
					if (TryStartWar(warMassAtkCost, curZone, GangWarManager.AttackStrength.massive)) warAttackStrengthMenu.Visible = false;
				}
				else
				{
					warAttackStrengthMenu.Visible = false;
				}
			};
		}

		bool TryStartWar(int atkCost, TurfZone targetZone, GangWarManager.AttackStrength atkStrength)
		{
			if (targetZone.ownerGangName == GangManager.instance.PlayerGang.name)
			{
				UI.ShowSubtitle("You can't start a war against your own gang! (You probably have changed zones after opening this menu)");
				return false;
			}


			if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-atkCost, true))
			{
				if (!GangWarManager.instance.StartWar(GangManager.instance.GetGangByName(targetZone.ownerGangName), targetZone, GangWarManager.WarType.attackingEnemy, atkStrength))
				{
					UI.ShowSubtitle("A war is already in progress.");
					return false;
				}
				else
				{
					MindControl.instance.AddOrSubtractMoneyToProtagonist(-atkCost);
				}
				return true;
			}
			else
			{
				UI.ShowSubtitle("You don't have the resources to start a battle of this size.");
				return false;
			}
		}

		void AddZoneUpgradeButton()
		{
			upgradeZoneValueBtn = new UIMenuItem("Upgrade current zone",
				"Increases this zone's level. This level affects the income provided, the reinforcements available in a war and the presence of police in that zone. The zone's level is reset when it is taken by another gang. The level limit is configurable via the ModOptions file.");
			AddItem(upgradeZoneValueBtn);
			OnItemSelect += (sender, item, index) => {
				if (item == upgradeZoneValueBtn)
				{
					TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
					if (curZone == null)
					{
						UI.ShowSubtitle("this zone isn't marked as takeable.");
					}
					else
					{
						if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
						{
							if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost, true))
							{
								if (curZone.value >= ModOptions.instance.maxTurfValue)
								{
									UI.ShowSubtitle("This zone's level is already maxed!");
								}
								else
								{
									curZone.value++;
									ZoneManager.instance.SaveZoneData(false);
									UI.ShowSubtitle("Zone level increased!");
									MindControl.instance.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost);
									UpdateZoneUpgradeBtn();
								}
							}
							else
							{
								UI.ShowSubtitle("You don't have the resources to upgrade this zone.");
							}
						}
						else
						{
							UI.ShowSubtitle("You can only upgrade zones owned by your gang!");
						}

					}
				}
			};
		}

		void AddAbandonZoneButton()
		{
			UIMenuItem newButton = new UIMenuItem("Abandon Zone", "If the zone you are in is controlled by your gang, it instantly becomes neutral. You receive part of the money used for upgrading the zone.");
			AddItem(newButton);
			OnItemSelect += (sender, item, index) => {
				if (item == newButton)
				{
					TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
					if (curZone == null)
					{
						UI.ShowSubtitle("This zone hasn't been marked as takeable.");
					}
					else
					{
						if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
						{
							if (ModOptions.instance.notificationsEnabled)
							{
								UI.Notify(string.Concat("The ", curZone.ownerGangName, " have abandoned ",
									curZone.zoneName, ". It has become a neutral zone again."));
							}
							curZone.ownerGangName = "none";
							curZone.value = 0;

							int valueDifference = curZone.value - GangManager.instance.PlayerGang.baseTurfValue;
							if (valueDifference > 0)
							{
								MindControl.instance.AddOrSubtractMoneyToProtagonist
								(ModOptions.instance.baseCostToUpgradeSingleTurfValue * valueDifference);
							}

							UI.ShowSubtitle(curZone.zoneName + " is now neutral again.");
							ZoneManager.instance.UpdateZoneData(curZone);
						}
						else
						{
							UI.ShowSubtitle("Your gang does not own this zone.");
						}
					}
				}
			};
		}

		public void UpdateTakeOverBtnText()
		{
			takeZoneButton.Description = "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
				ModOptions.instance.baseCostToTakeTurf.ToString() + ". If it belongs to another gang, a battle will begin!";
		}

		public void UpdateZoneUpgradeBtn()
		{
			TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
			if (curZone == null)
			{
				upgradeZoneValueBtn.Text = "Upgrade current zone - (Not takeable)";
			}
			else
			{
				curZoneValueUpgradeCost = GangCalculations.CalculateTurfValueUpgradeCost(curZone.value);
				upgradeZoneValueBtn.Text = "Upgrade current zone - " + curZoneValueUpgradeCost.ToString();
			}

		}

		void UpdateGangWarAtkOptions(TurfZone targetZone)
		{
			Gang enemyGang = GangManager.instance.GetGangByName(targetZone.ownerGangName);
			Gang playerGang = GangManager.instance.PlayerGang;
			int defenderNumbers = GangCalculations.CalculateDefenderReinforcements(enemyGang, targetZone);
			warLightAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.light);
			warMedAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.medium);
			warLargeAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.large);
			warMassAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.massive);

			warLightAtkBtn.Text = "Light Attack - " + warLightAtkCost.ToString();
			warMedAtkBtn.Text = "Medium Attack - " + warMedAtkCost.ToString();
			warLargeAtkBtn.Text = "Large Attack - " + warLargeAtkCost.ToString();
			warMassAtkBtn.Text = "Massive Attack - " + warMassAtkCost.ToString();

			warLightAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.light, defenderNumbers);
			warMedAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.medium, defenderNumbers);
			warLargeAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.large, defenderNumbers);
			warMassAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.massive, defenderNumbers);

		}


	}
}
