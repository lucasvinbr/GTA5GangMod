

using LemonUI;
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// menu for most zone-related actions
    /// </summary>
    public class ZonesMenu : ModMenu
    {
        public ZonesMenu(ObjectPool menuPool) : base("zone_controls", "Zone Controls")
        {
            warAttackStrengthMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("zone_attack_submenu_main", "Gang War Attack Options"));
            customZonesSubMenu = new CustomZonesSubMenu();

            menuPool.Add(this);
            menuPool.Add(warAttackStrengthMenu);
            menuPool.Add(customZonesSubMenu);

            customZonesSubMenu.Setup();

            RecreateItems();
        }
    

        public NativeMenu warAttackStrengthMenu;

        private readonly CustomZonesSubMenu customZonesSubMenu;

        private NativeItem takeZoneButton, upgradeZoneValueBtn, warLightAtkBtn, warMedAtkBtn, warLargeAtkBtn, warMassAtkBtn;

        private int curZoneValueUpgradeCost,
            warLightAtkCost, warMedAtkCost, warLargeAtkCost, warMassAtkCost;

        private void AddSaveZoneButton()
        {
            NativeItem saveZoneBtn = new NativeItem(Localization.GetTextByKey("menu_button_add_zone_to_takeables_set_blip_pos", "Add Current Zone to Takeables/Set Blip Position"),
                Localization.GetTextByKey("menu_button_desc_add_zone_to_takeables_set_blip_pos", "Makes the zone you are in become takeable by gangs and/or sets your position as the zone's reference position (if toggled, this zone's blip will show here)."));
            Add(saveZoneBtn);

            saveZoneBtn.Activated += (s, e) =>
            {
                string curZoneName = ZoneManager.LegacyGetZoneName(World.GetZoneDisplayName(MindControl.CurrentPlayerCharacter.Position));
                TurfZone curZone = ZoneManager.instance.GetZoneInLocation(curZoneName, MindControl.CurrentPlayerCharacter.Position);
                if (curZone == null)
                {
                    //add a new zone then
                    curZone = new TurfZone(curZoneName);
                }

                //update the zone's blip position even if it already existed
                curZone.zoneBlipPosition = MindControl.CurrentPlayerCharacter.Position;
                ZoneManager.instance.UpdateZoneData(curZone);
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_data_updated", "Zone Data Updated!"));
            };

        }

        private void AddGangTakeoverButton()
        {
            takeZoneButton = new NativeItem(Localization.GetTextByKey("menu_button_take_current_zone", "Take current zone"),
                string.Format(
                    Localization.GetTextByKey("menu_button_desc_take_current_zone_cost_x",
                    "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of ${0}. If it belongs to another gang, a battle will begin!"),
                    ModOptions.instance.baseCostToTakeTurf.ToString())
                );
            Add(takeZoneButton);

            takeZoneButton.Activated += (s, e) =>
            {
                TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
                if (curZone == null)
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_not_marked_as_takeable", "this zone isn't marked as takeable."));
                }
                else
                {
                    Gang ownerGang = curZone.ownerGangName == "none" ? null : GangManager.instance.GetGangByName(curZone.ownerGangName);
                    if (ownerGang == null)
                    {
                        if (MindControl.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.baseCostToTakeTurf))
                        {
                            GangManager.instance.PlayerGang.TakeZone(curZone);
                            UI.Screen.ShowSubtitle(string.Format(
                                Localization.GetTextByKey("subtitle_zone_is_now_of_gang_x", "This zone is {0} turf now!"),
                                GangManager.instance.PlayerGang.name
                                ));
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_not_enough_resources_to_take_neutral_zone", "You don't have the resources to take over a neutral zone."));
                        }
                    }
                    else
                    {
                        if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_already_owned_by_player_gang", "Your gang already owns this zone."));
                        }
                        else
                        {
                            Visible = !Visible;
                            UpdateGangWarAtkOptions(curZone);
                            warAttackStrengthMenu.Visible = true;
                        }
                    }
                }
            };
        }

        private string GetReinforcementsComparisonMsg(GangWarManager.AttackStrength atkStrength, int defenderNumbers)
        {
            return string.Format(Localization.GetTextByKey("menu_button_desc_start_war_our_x_against_their_y", "We will have {0} members against their {1}"), 
                GangCalculations.CalculateAttackerReinforcements(GangManager.instance.PlayerGang, atkStrength),
                defenderNumbers.ToString());
        }

        private void AddGangWarAtkOptions()
        {

            Gang playerGang = GangManager.instance.PlayerGang;

            warLightAtkBtn = new NativeItem("Attack", "Attack. (Text set elsewhere)"); //those are updated when this menu is opened (UpdateGangWarAtkOptions)
            warMedAtkBtn = new NativeItem("Attack", "Attack.");
            warLargeAtkBtn = new NativeItem("Attack", "Attack.");
            warMassAtkBtn = new NativeItem("Attack", "Attack.");
            NativeItem cancelBtn = new NativeItem(Localization.GetTextByKey("menu_button_cancel_zone_attack", "Cancel"), Localization.GetTextByKey("menu_button_desc_cancel_zone_attack", "Cancels the attack. No money is lost for canceling."));
            warAttackStrengthMenu.Add(warLightAtkBtn);
            warAttackStrengthMenu.Add(warMedAtkBtn);
            warAttackStrengthMenu.Add(warLargeAtkBtn);
            warAttackStrengthMenu.Add(warMassAtkBtn);
            warAttackStrengthMenu.Add(cancelBtn);

            TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);

            warLightAtkBtn.Activated += (e, s) =>
            {
                if (TryStartWar(warLightAtkCost, curZone, GangWarManager.AttackStrength.light)) warAttackStrengthMenu.Visible = false;
            };
            warMedAtkBtn.Activated += (e, s) =>
            {
                if (TryStartWar(warMedAtkCost, curZone, GangWarManager.AttackStrength.medium)) warAttackStrengthMenu.Visible = false;
            };
            warLargeAtkBtn.Activated += (e, s) =>
            {
                if (TryStartWar(warLargeAtkCost, curZone, GangWarManager.AttackStrength.large)) warAttackStrengthMenu.Visible = false;
            };
            warMassAtkBtn.Activated += (e, s) =>
            {
                if (TryStartWar(warMassAtkCost, curZone, GangWarManager.AttackStrength.massive)) warAttackStrengthMenu.Visible = false;
            };
            cancelBtn.Activated += (e, s) =>
            {
                warAttackStrengthMenu.Visible = false;
            };

        }

        private bool TryStartWar(int atkCost, TurfZone targetZone, GangWarManager.AttackStrength atkStrength)
        {
            if (targetZone.ownerGangName == GangManager.instance.PlayerGang.name)
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_cannot_start_war_against_own_gang", "You can't start a war against your own gang! (You probably have changed zones after opening this menu)"));
                return false;
            }


            if (MindControl.AddOrSubtractMoneyToProtagonist(-atkCost, true))
            {
                if (!GangWarManager.instance.TryStartWar(GangManager.instance.PlayerGang, targetZone, atkStrength))
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_war_already_in_progress_here", "A war is already in progress here! Skip the war to start a new one, or wait for it to end."));
                    return false;
                }
                else
                {
                    MindControl.AddOrSubtractMoneyToProtagonist(-atkCost);
                }
                return true;
            }
            else
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_resources_for_battle_of_this_size", "You don't have the resources to start a battle of this size."));
                return false;
            }
        }

        private void AddZoneUpgradeButton()
        {
            upgradeZoneValueBtn = new NativeItem(Localization.GetTextByKey("menu_button_upgrade_cur_zone", "Upgrade current zone"),
                Localization.GetTextByKey("menu_button_desc_upgrade_cur_zone", "Increases this zone's level. This level affects the income provided, the reinforcements available in a war and the presence of police in that zone. The zone's level is reset when it is taken by another gang. The level limit is configurable via the ModOptions file."));
            Add(upgradeZoneValueBtn);

            upgradeZoneValueBtn.Activated += (e, s) =>
            {
                TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
                if (curZone == null)
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_not_marked_as_takeable", "this zone isn't marked as takeable."));
                }
                else
                {
                    if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
                    {
                        if (MindControl.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost, true))
                        {
                            if (curZone.value >= ModOptions.instance.maxTurfValue)
                            {
                                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_level_already_maxed", "This zone's level is already maxed!"));
                            }
                            else
                            {
                                curZone.ChangeValue(curZone.value + 1);
                                ZoneManager.instance.SaveZoneData(false);
                                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_level_increased", "Zone level increased!"));
                                MindControl.AddOrSubtractMoneyToProtagonist(-curZoneValueUpgradeCost);
                                UpdateZoneUpgradeBtn();
                            }
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_enough_resources_to_upgrade_zone", "You don't have the resources to upgrade this zone."));
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_can_only_upgrade_your_own_zones", "You can only upgrade zones owned by your gang!"));
                    }

                }
            };
            
        }

        private void AddAbandonZoneButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_abandon_zone", "Abandon Zone"), Localization.GetTextByKey("menu_button_desc_abandon_zone", "If the zone you are in is controlled by your gang, it instantly becomes neutral. You receive part of the money used for upgrading the zone."));
            Add(newButton);

            newButton.Activated += (e, s) =>
            {
                TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
                if (curZone == null)
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_not_marked_as_takeable", "This zone hasn't been marked as takeable."));
                }
                else
                {
                    if (curZone.ownerGangName == GangManager.instance.PlayerGang.name)
                    {

                        if (ModOptions.instance.notificationsEnabled)
                        {
                            UI.Notification.Show(string.Format(Localization.GetTextByKey("notify_gang_x_has_abandoned_zone_y", 
                                "The {0} have abandoned {1}. It has become a neutral zone again."),
                                curZone.ownerGangName, curZone.zoneName));
                        }
                        curZone.ownerGangName = "none";
                        curZone.ChangeValue(0);

                        int valueDifference = curZone.value - GangManager.instance.PlayerGang.baseTurfValue;
                        if (valueDifference > 0)
                        {
                            MindControl.AddOrSubtractMoneyToProtagonist
                            (ModOptions.instance.baseCostToUpgradeSingleTurfValue * valueDifference);
                        }

                        UI.Screen.ShowSubtitle(string.Format(Localization.GetTextByKey("subtitle_zone_x_is_now_neutral", "{0} is now neutral again."),
                            curZone.zoneName));

                        if (curZone.IsBeingContested())
                        {
                            //end the war being fought here, since we're leaving
                            GangWarManager.instance.GetWarOccurringOnZone(curZone).EndWar(false);
                        }

                        ZoneManager.instance.UpdateZoneData(curZone);
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_your_gang_doesnt_own_this_zone", "Your gang does not own this zone."));
                    }
                }
            };
        }

        public void UpdateTakeOverBtnText()
        {
            takeZoneButton.Description = string.Format(
                Localization.GetTextByKey("menu_button_take_zone_cost_x_desc",
                "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of ${0}. If it belongs to another gang, a battle will begin!"), ModOptions.instance.baseCostToTakeTurf.ToString());
        }

        public void UpdateZoneUpgradeBtn()
        {
            TurfZone curZone = ZoneManager.instance.GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
            if (curZone == null)
            {
                upgradeZoneValueBtn.Title = Localization.GetTextByKey("menu_button_upgrade_zone", "Upgrade current zone") + " - " + Localization.GetTextByKey("menu_button_upgrade_zone_not_takeable_suffix", "(Not takeable)");
            }
            else
            {
                curZoneValueUpgradeCost = GangCalculations.CalculateTurfValueUpgradeCost(curZone.value);
                upgradeZoneValueBtn.Title = Localization.GetTextByKey("menu_button_upgrade_zone", "Upgrade current zone") + " - " + curZoneValueUpgradeCost.ToString();
            }

        }

        private void UpdateGangWarAtkOptions(TurfZone targetZone)
        {
            Gang enemyGang = GangManager.instance.GetGangByName(targetZone.ownerGangName);
            Gang playerGang = GangManager.instance.PlayerGang;
            int defenderNumbers = GangCalculations.CalculateDefenderReinforcements(enemyGang, targetZone);
            warLightAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.light);
            warMedAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.medium);
            warLargeAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.large);
            warMassAtkCost = GangCalculations.CalculateAttackCost(playerGang, GangWarManager.AttackStrength.massive);

            warLightAtkBtn.Title = Localization.GetTextByKey("menu_button_start_war_light_attack", "Light Attack") + " - " + warLightAtkCost.ToString();
            warMedAtkBtn.Title = Localization.GetTextByKey("menu_button_start_war_medium_attack", "Medium Attack") + " - " + warMedAtkCost.ToString();
            warLargeAtkBtn.Title = Localization.GetTextByKey("menu_button_start_war_large_attack", "Large Attack") + " - " + warLargeAtkCost.ToString();
            warMassAtkBtn.Title = Localization.GetTextByKey("menu_button_start_war_massive_attack", "Massive Attack") + " - " + warMassAtkCost.ToString();

            warLightAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.light, defenderNumbers);
            warMedAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.medium, defenderNumbers);
            warLargeAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.large, defenderNumbers);
            warMassAtkBtn.Description = GetReinforcementsComparisonMsg(GangWarManager.AttackStrength.massive, defenderNumbers);

        }

        /// <summary>
        /// create events. Should only be run once, usually
        /// </summary>
        protected override void Setup()
        {
            Localization.OnLanguageChanged += OnLocalesChanged;
            Shown += RebuildItemsIfNeeded;
        }

        protected override void RecreateItems()
        {
            Clear();

            warAttackStrengthMenu.Name = Localization.GetTextByKey("zone_attack_submenu_main", "Gang War Attack Options");
            //add buttons to self and warMenu...
            AddGangWarAtkOptions();
            AddGangTakeoverButton();
            AddZoneUpgradeButton();
            AddAbandonZoneButton();
            AddSaveZoneButton();
            Add(new NativeSubmenuItem(customZonesSubMenu, this)
            {
                Title = Localization.GetTextByKey("menu_button_custom_zones_menu", "Edit/Create Custom Zones..."),
                Description = Localization.GetTextByKey("menu_button_desc_custom_zones_menu", "Opens the Custom Zones Menu")
            });
        }
    }
}
