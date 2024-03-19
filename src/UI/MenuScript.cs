using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the lemonUI-implementing class
    /// -------------thanks to the LemonUI developers!-------------
    /// https://github.com/LemonUIbyLemon/LemonUI
    /// </summary>
    public class MenuScript
    {
        private readonly ObjectPool menuPool;

        private readonly NativeMenu memberMenu, carMenu;

        private readonly NativeMenu specificGangMemberRegSubMenu, specificCarRegSubMenu;

        private readonly ZonesMenu zonesMenu;
        private readonly GangMenu gangMenu;
        private readonly PickAiGangMenu pickAiGangMenu;

        private Ped closestPed;

        private int memberStyle = 0, memberColor = 0;

        private bool savePotentialMembersAsExtended = false;

        public ChangeableKeyBinding targetKeyBindToChange = ChangeableKeyBinding.AddGroupBtn;

        /// <summary>
        /// action invoked when the input field closes
        /// </summary>
        public Action<DesiredInputType, string> OnInputFieldDone;

        public Action OnKeyBindingChanged;

        public enum DesiredInputType
        {
            none,
            enterGangName,
            changeKeyBinding,
            enterCustomZoneName,
        }

        public enum ChangeableKeyBinding
        {
            GangMenuBtn,
            ZoneMenuBtn,
            MindControlBtn,
            AddGroupBtn,
        }

        public DesiredInputType curInputType = DesiredInputType.none;

        public static MenuScript instance;

        public MenuScript()
        {
            instance = this;

            ModOptions.OnModOptionsReloaded += RefreshCostsTexts;

            menuPool = new ObjectPool();

            pickAiGangMenu = new PickAiGangMenu(menuPool);
            zonesMenu = new ZonesMenu(menuPool);
            memberMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_member_registration", "Gang Member Registration Controls"));
            carMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_vehicle_registration", "Gang Vehicle Registration Controls"));
            specificCarRegSubMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_specific_vehicle_registration", "Gang Vehicle Registration"));
            specificGangMemberRegSubMenu = new NativeMenu("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_specific_member_registration", "Gang Member Registration"));
            gangMenu = new GangMenu(menuPool);

            menuPool.Add(memberMenu);
            menuPool.Add(carMenu);
            menuPool.Add(specificCarRegSubMenu);
            menuPool.Add(specificGangMemberRegSubMenu);

            SetupSubMenus();

            RecreateItems();

            Localization.OnLanguageChanged += () => RecreateItems();

            foreach (var poolItem in menuPool)
            {
                var menu = (NativeMenu)poolItem;

                menu.UseMouse = false;
            }

        }

        #region menu opening methods
        public void OpenGangMenu()
        {
            if (!menuPool.AreAnyVisible && curInputType == DesiredInputType.none)
            {
                gangMenu.UpdateCosts();
                //UpdateBuyableWeapons();
                gangMenu.Visible = !gangMenu.Visible;
            }
        }

        public void OpenContextualRegistrationMenu()
        {
            if (!menuPool.AreAnyVisible && curInputType == DesiredInputType.none)
            {
                if (MindControl.CurrentPlayerCharacter.CurrentVehicle == null)
                {
                    closestPed = World.GetClosestPed(MindControl.CurrentPlayerCharacter.Position + MindControl.CurrentPlayerCharacter.ForwardVector * 6.0f, 5.5f);
                    if (closestPed != null)
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_selected", "ped selected!"));
                        World.AddExplosion(closestPed.Position, ExplosionType.Steam, 1.0f, 0.1f);
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_couldnt_find_ped_in_front_selected_self", "Couldn't find a ped in front of you! You have selected yourself."));
                        closestPed = MindControl.CurrentPlayerCharacter;
                        World.AddExplosion(closestPed.Position, ExplosionType.Extinguisher, 1.0f, 0.1f);
                    }

                    memberMenu.Visible = !memberMenu.Visible;
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_selected", "vehicle selected!"));
                    carMenu.Visible = !carMenu.Visible;
                }
                RefreshNewEnemyMenuContent();
            }
        }

        public void OpenZoneMenu()
        {
            if (!menuPool.AreAnyVisible && curInputType == DesiredInputType.none)
            {
                ZoneManager.instance.OutputCurrentZoneInfo();
                zonesMenu.UpdateZoneUpgradeBtn();
                zonesMenu.Visible = !zonesMenu.Visible;
            }
        }

        public void OpenPickAiGangMenu(NativeMenu callerMenu, string menuSubtitle, Action<Gang> onGangPicked)
        {
            callerMenu.Visible = false;
            pickAiGangMenu.Open(callerMenu, menuSubtitle, onGangPicked);
        }
        #endregion

        /// <summary>
        /// opens the input field using the provided data.
        /// remember to hide any open menus before calling!
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="menuCode"></param>
        /// <param name="initialText"></param>
        public void OpenInputField(DesiredInputType inputType, string menuCode, string initialText)
        {
            Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, menuCode, "", initialText, "", "", "", 30);
            curInputType = inputType;
        }

        public void Tick()
        {
            menuPool.Process();

            if (curInputType != DesiredInputType.changeKeyBinding && curInputType != DesiredInputType.none)
            {
                int inputFieldSituation = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);
                if (inputFieldSituation == 1)
                {
                    string typedText = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
                    OnInputFieldDone?.Invoke(curInputType, typedText);

                    curInputType = DesiredInputType.none;


                }
                else if (inputFieldSituation == 2 || inputFieldSituation == 3)
                {
                    curInputType = DesiredInputType.none;
                }
            }
        }

        /// <summary>
        /// refreshes costs displayed in most menus. Should be called if lots of mod options may have changed
        /// </summary>
        public void RefreshCostsTexts()
        {
            gangMenu.UpdateCosts();
            zonesMenu.UpdateTakeOverBtnText();
        }

        #region register Member/Vehicle Stuff

        private void AddMemberStyleChoices()
        {
            List<string> memberStyles = new List<string>
            {
                Localization.GetTextByKey("member_style_business", "Business"),
                Localization.GetTextByKey("member_style_street", "Street"),
                Localization.GetTextByKey("member_style_beach", "Beach"),
                Localization.GetTextByKey("member_style_special", "Special")
            };

            List<string> memberColors = new List<string>
            {
                Localization.GetTextByKey("member_color_white", "White"),
                Localization.GetTextByKey("member_color_black", "Black"),
                Localization.GetTextByKey("member_color_red", "Red"),
                Localization.GetTextByKey("member_color_green", "Green"),
                Localization.GetTextByKey("member_color_blue", "Blue"),
                Localization.GetTextByKey("member_color_yellow", "Yellow"),
                Localization.GetTextByKey("member_color_gray", "Gray"),
                Localization.GetTextByKey("member_color_pink", "Pink"),
                Localization.GetTextByKey("member_color_purple", "Purple")
            };

            var styleListItem = new NativeListItem<string>(Localization.GetTextByKey("menu_listitem_member_style", "Member Dressing Style"), 
                Localization.GetTextByKey("menu_listitem_member_style_desc", "The way the selected member is dressed. Used by the AI when picking members (if the AI gang's chosen style is the same as this member's, it may choose this member)."));
            var colorListItem = new NativeListItem<string>(Localization.GetTextByKey("menu_listitem_member_color", "Member Color"),
                Localization.GetTextByKey("menu_listitem_member_color_desc", "The color the member will be assigned to. Used by the AI when picking members (if the AI gang's color is the same as this member's, it may choose this member)."));
            NativeCheckboxItem extendedModeToggle = new NativeCheckboxItem(Localization.GetTextByKey("menu_toggle_extended_member_registration", "Extended Save Mode"), 
                Localization.GetTextByKey("menu_toggle_extended_member_registration_desc", "If enabled, saves all clothing indexes for non-freemode peds. Can help with some addon peds."), savePotentialMembersAsExtended);

            styleListItem.Items = memberStyles;
            colorListItem.Items = memberColors;

            memberMenu.Add(styleListItem);
            memberMenu.Add(colorListItem);
            memberMenu.Add(extendedModeToggle);

            styleListItem.ItemChanged += (sender, args) =>
            {
                memberStyle = args.Index;
            };

            colorListItem.ItemChanged += (sender, args) =>
            {
                memberColor = args.Index;
            };

            extendedModeToggle.CheckboxChanged += (sender, args) =>
            {
                savePotentialMembersAsExtended = extendedModeToggle.Checked;
            };

        }

        private void AddSaveMemberButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_save_pot_member_for_ai_gangs", "Save Potential Member for future AI gangs"), 
                Localization.GetTextByKey("menu_button_save_pot_member_for_ai_gangs_desc", "Saves the selected ped as a potential gang member with the specified data. AI gangs will be able to choose him\\her."));
            memberMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                {
                    if (PotentialGangMember.AddMemberAndSavePool(new FreemodePotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_freemode_member_added", "Potential freemode member added!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_similar_potential_member_exists", "A similar potential member already exists."));
                    }
                }
                else
                {
                    bool addAttempt = savePotentialMembersAsExtended ?
                        PotentialGangMember.AddMemberAndSavePool(new ExtendedPotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                        PotentialGangMember.AddMemberAndSavePool(new PotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));

                    if (addAttempt)
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_member_added", "Potential member added!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_similar_potential_member_exists", "A similar potential member already exists."));
                    }
                }
            };
        }

        private void AddNewPlayerGangMemberButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_save_member_to_your_gang", "Save ped type for your gang"),
                Localization.GetTextByKey("menu_button_save_member_to_your_gang_desc", "Saves the selected ped type as a member of your gang, with the specified data. The selected ped himself won't be a member, however."));
            memberMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                {
                    if (GangManager.instance.PlayerGang.AddMemberVariation(new FreemodePotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_freemode_member_added", "Freemode Member added successfully!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_your_gang_has_similar_member", "Your gang already has a similar member."));
                    }
                }
                else
                {
                    bool addAttempt = savePotentialMembersAsExtended ?
                        GangManager.instance.PlayerGang.AddMemberVariation(new ExtendedPotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                        GangManager.instance.PlayerGang.AddMemberVariation(new PotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));


                    if (addAttempt)
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_member_added", "Member added successfully!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_your_gang_has_similar_member", "Your gang already has a similar member."));
                    }
                }

            };

        }

        private void AddNewEnemyMemberSubMenu()
        {
            var subMenuItem = memberMenu.AddSubMenu(specificGangMemberRegSubMenu);
            subMenuItem.Title = Localization.GetTextByKey("menu_button_save_member_for_specific_enemy_gang", "Save ped type for a specific enemy gang...");

        }

        /// <summary>
        /// removes all options and then adds all gangs that are not controlled by the player as chooseable options in the "Save for a specific gang" submenus
        /// </summary>
        public void RefreshNewEnemyMenuContent()
        {
            specificGangMemberRegSubMenu.Clear();
            specificCarRegSubMenu.Clear();

            List<Gang> gangsList = GangManager.instance.gangData.gangs;

            for (int i = 0; i < gangsList.Count; i++)
            {
                if (!gangsList[i].isPlayerOwned)
                {
                    specificGangMemberRegSubMenu.Add(new NativeItem(gangsList[i].name));
                    specificCarRegSubMenu.Add(new NativeItem(gangsList[i].name));
                }
            }

        }

        private void AddRemoveGangMemberButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_remove_member_respective_gang", "Remove ped type from respective gang"),
                Localization.GetTextByKey("menu_button_remove_member_respective_gang_desc", "If the selected ped type was a member of a gang, it will no longer be. The selected ped himself will still be a member, however. This works for your own gang and for the enemies."));
            memberMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                Gang ownerGang = GangManager.instance.GetGangByRelGroup(closestPed.RelationshipGroup);
                if (ownerGang == null)
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_not_in_any_gang", "The ped doesn't seem to be in a gang."), 8000);
                    return;
                }
                if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                {
                    if (ownerGang.RemoveMemberVariation(new FreemodePotentialGangMember
                    (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_removed", "Member removed successfully!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_not_in_any_gang", "The ped doesn't seem to be in a gang."), 8000);
                    }
                }
                else
                {
                    if (ownerGang.RemoveMemberVariation(new PotentialGangMember
                    (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) ||
                    ownerGang.RemoveMemberVariation(new ExtendedPotentialGangMember
                    (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_member_removed", "Member removed successfully!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_not_in_any_gang", "The ped doesn't seem to be in a gang."), 8000);
                    }
                }
            };
        }

        private void AddRemoveFromAllGangsButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_remove_member_all_gangs_pool", "Remove ped type from all gangs and pool"), 
                Localization.GetTextByKey("menu_button_remove_member_all_gangs_pool_desc", "Removes the ped type from all gangs and from the member pool, which means future gangs also won't try to use this type. The selected ped himself will still be a gang member, however."));
            memberMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                {
                    FreemodePotentialGangMember memberToRemove = new FreemodePotentialGangMember
               (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                    if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_removed_from_pool", "Ped type removed from pool! (It might not be the only similar ped in the pool)"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_not_in_pool", "Ped type not found in pool."));
                    }

                    for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++)
                    {
                        GangManager.instance.gangData.gangs[i].RemoveMemberVariation(memberToRemove);
                    }
                }
                else
                {
                    PotentialGangMember memberToRemove = new PotentialGangMember
               (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);
                    ExtendedPotentialGangMember memberToRemoveEx = new ExtendedPotentialGangMember
               (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                    if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove) ||
                    PotentialGangMember.RemoveMemberAndSavePool(memberToRemoveEx))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_removed_from_pool", "Ped type removed from pool! (It might not be the only similar ped in the pool)"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_not_in_pool", "Ped type not found in pool."));
                    }

                    for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++)
                    {
                        GangManager.instance.gangData.gangs[i].RemoveMemberVariation(memberToRemove);
                    }
                }
            };
        }

        private void AddMakeFriendlyToPlayerGangButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_make_ped_friendly_your_gang", "Make Ped friendly to your gang"),
                Localization.GetTextByKey("menu_button_make_ped_friendly_your_gang_desc", "Makes the selected ped (and everyone from his group) and your gang become allies. Can't be used with cops or gangs from this mod! NOTE: this only lasts until scripts are loaded again"));
            memberMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                RelationshipGroup closestPedRelGroup = closestPed.RelationshipGroup;
                //check if we can really become allies with this guy
                if (closestPedRelGroup.Hash != Function.Call<int>(Hash.GET_HASH_KEY, "COP"))
                {
                    //he can still be from one of the gangs! we should check

                    if (GangManager.instance.GetGangByRelGroup(closestPedRelGroup) != null)
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_cannot_make_member_friendly", "That ped is a gang member! Gang members cannot be marked as allies"));
                        return;
                    }

                    //ok, we can be allies
                    Gang playerGang = GangManager.instance.PlayerGang;
                    playerGang.relGroup.SetRelationshipBetweenGroups(closestPedRelGroup, Relationship.Respect, true);
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_ped_group_is_now_friendly", "That ped's group is now an allied group!"));
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_cannot_make_cop_friendly", "That ped is a cop! Cops cannot be marked as allies"));
                }
            };
        }

        private void AddSaveVehicleButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_register_vehicle_ai_gangs", "Register Vehicle as usable by AI Gangs"),
                Localization.GetTextByKey("menu_button_register_vehicle_ai_gangs_desc", "Makes the vehicle type you are driving become chooseable as one of the types used by AI gangs."));
            carMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (curVehicle != null)
                {
                    if (PotentialGangVehicle.AddVehicleAndSavePool(new PotentialGangVehicle(curVehicle.Model.Hash)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_added_pool", "Vehicle added to pool!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_already_in_pool", "That vehicle has already been added to the pool."));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_vehicle", "You are not inside a vehicle."));
                }
            };
        }

        private void AddRegisterPlayerVehicleButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_register_vehicle_your_gang", "Register Vehicle for your Gang"),
                Localization.GetTextByKey("menu_button_register_vehicle_your_gang_desc", "Makes the vehicle type you are driving become one of the default types used by your gang."));
            carMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (curVehicle != null)
                {
                    // Capture vehicle mods here
                    List<VehicleModData> capturedMods = new List<VehicleModData>();
                    foreach (VehicleModType modType in Enum.GetValues(typeof(VehicleModType)))
                    {
                        int modIndex = curVehicle.Mods[modType].Index;
                        if (modIndex != -1) // If the mod is installed
                        {
                            capturedMods.Add(new VehicleModData { ModType = modType, ModValue = modIndex });
                        }
                    }

                    // Create a new PotentialGangVehicle and set its mods
                    PotentialGangVehicle newGangVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);
                    newGangVehicle.VehicleMods = capturedMods;

                    if (GangManager.instance.PlayerGang.AddGangCar(newGangVehicle))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_added_to_gang", "Gang vehicle added!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_already_in_your_gang", "That vehicle is already registered for your gang."));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_vehicle", "You are not inside a vehicle."));
                }

            };
        }

        private void AddRegisterEnemyVehicleButton()
        {
            var subMenuItem = carMenu.AddSubMenu(specificCarRegSubMenu);
            subMenuItem.Title = Localization.GetTextByKey("menu_button_register_vehicle_specific_enemy_gang", "Register vehicle for a specific enemy gang...");
            
            
        }

        private void AddRemovePlayerVehicleButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_remove_vehicle_from_your_gang", "Remove Vehicle Type from your Gang"),
                Localization.GetTextByKey("menu_button_remove_vehicle_from_your_gang_desc", "Removes the vehicle type you are driving from the possible vehicle types for your gang."));
            carMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (curVehicle != null)
                {
                    if (GangManager.instance.PlayerGang.RemoveGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_removed", "Gang vehicle removed!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_not_in_your_gang", "That vehicle is not registered for your gang."));
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_vehicle", "You are not inside a vehicle."));
                }
            };
        }

        private void AddRemoveVehicleEverywhereButton()
        {
            NativeItem newButton = new NativeItem(Localization.GetTextByKey("menu_button_remove_vehicle_all_gangs_pool", "Remove Vehicle Type from all gangs and pool"),
                Localization.GetTextByKey("menu_button_remove_vehicle_all_gangs_pool_desc", "Removes the vehicle type you are driving from the possible vehicle types for all gangs, including yours. Existing gangs will also stop using that car and get another one if needed."));
            carMenu.Add(newButton);
            newButton.Activated += (sender, args) =>
            {
                Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                if (curVehicle != null)
                {
                    PotentialGangVehicle removedVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);

                    if (PotentialGangVehicle.RemoveVehicleAndSavePool(removedVehicle))
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_removed_pool", "Vehicle type removed from pool!"));
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_not_in_pool", "Vehicle type not found in pool."));
                    }

                    for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++)
                    {
                        GangManager.instance.gangData.gangs[i].RemoveGangCar(removedVehicle);
                    }
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_vehicle", "You are not inside a vehicle."));
                }
            };
        }

        #endregion

        /// <summary>
        /// adds events which should only be added once
        /// </summary>
        private void SetupSubMenus()
        {
            specificGangMemberRegSubMenu.ItemActivated += (sender, args) =>
            {
                Gang pickedGang = GangManager.instance.GetGangByName(args.Item.Title);
                if (pickedGang != null)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (pickedGang.AddMemberVariation(new FreemodePotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_freemode_member_added", "Freemode Member added successfully!"));
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_that_gang_has_similar_member", "That gang already has a similar member."));
                        }
                    }
                    else
                    {
                        bool addAttempt = savePotentialMembersAsExtended ?
                            pickedGang.AddMemberVariation(new ExtendedPotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                            pickedGang.AddMemberVariation(new PotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));


                        if (addAttempt)
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_potential_member_added", "Member added successfully!"));
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_that_gang_has_similar_member", "That gang already has a similar member."));
                        }
                    }
                }
            };

            specificCarRegSubMenu.ItemActivated += (sender, args) =>
            {
                Gang pickedGang = GangManager.instance.GetGangByName(args.Item.Title);
                if (pickedGang != null)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        // Capture vehicle mods
                        List<VehicleModData> capturedMods = new List<VehicleModData>();
                        foreach (VehicleModType modType in Enum.GetValues(typeof(VehicleModType)))
                        {
                            int modIndex = curVehicle.Mods[modType].Index;
                            if (modIndex != -1) // If the mod is installed
                            {
                                capturedMods.Add(new VehicleModData { ModType = modType, ModValue = modIndex });
                            }
                        }

                        // Create a new PotentialGangVehicle and set its mods
                        PotentialGangVehicle newGangVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);
                        newGangVehicle.VehicleMods = capturedMods;

                        if (pickedGang.AddGangCar(newGangVehicle))
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_added_to_gang", "Gang vehicle added!"));
                        }
                        else
                        {
                            UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_vehicle_already_in_that_gang", "That vehicle is already registered for that gang."));
                        }
                    }
                    else
                    {
                        UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_vehicle", "You are not inside a vehicle."));
                    }
                }
            };
        }

        private void RecreateItems()
        {
            memberMenu.Clear();
            carMenu.Clear();

            AddMemberStyleChoices();
            AddSaveMemberButton();
            AddNewPlayerGangMemberButton();
            AddNewEnemyMemberSubMenu();
            AddRemoveGangMemberButton();
            AddRemoveFromAllGangsButton();
            AddMakeFriendlyToPlayerGangButton();


            AddSaveVehicleButton();
            AddRegisterPlayerVehicleButton();
            AddRegisterEnemyVehicleButton();
            AddRemovePlayerVehicleButton();
            AddRemoveVehicleEverywhereButton();
        }
    }
}
