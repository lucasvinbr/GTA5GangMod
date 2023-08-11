using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the nativeUI-implementing class
    /// -------------thanks to the NativeUI developers!-------------
    /// </summary>
    public class MenuScript
    {
        private readonly MenuPool menuPool;

        private readonly UIMenu memberMenu, carMenu;

        private UIMenu specificGangMemberRegSubMenu, specificCarRegSubMenu;

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

            menuPool = new MenuPool();

            pickAiGangMenu = new PickAiGangMenu(menuPool);
            zonesMenu = new ZonesMenu(menuPool);
            memberMenu = new UIMenu("Gang and Turf Mod", "Gang Member Registration Controls");
            carMenu = new UIMenu("Gang and Turf Mod", "Gang Vehicle Registration Controls");
            gangMenu = new GangMenu(menuPool);

            menuPool.Add(memberMenu);
            menuPool.Add(carMenu);

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

            memberMenu.RefreshIndex();

            //add mouse click as another "select" button
            menuPool.SetKey(UIMenu.MenuControls.Select, Control.PhoneSelect);
            InstructionalButton clickButton = new InstructionalButton(Control.PhoneSelect, "Select");
            zonesMenu.AddInstructionalButton(clickButton);
            gangMenu.AddInstructionalButton(clickButton);
            memberMenu.AddInstructionalButton(clickButton);
            zonesMenu.warAttackStrengthMenu.AddInstructionalButton(clickButton);

        }

        #region menu opening methods
        public void OpenGangMenu()
        {
            if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none)
            {
                gangMenu.UpdateCosts();
                //UpdateBuyableWeapons();
                gangMenu.Visible = !gangMenu.Visible;
            }
        }

        public void OpenContextualRegistrationMenu()
        {
            if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none)
            {
                if (MindControl.CurrentPlayerCharacter.CurrentVehicle == null)
                {
                    closestPed = World.GetClosestPed(MindControl.CurrentPlayerCharacter.Position + MindControl.CurrentPlayerCharacter.ForwardVector * 6.0f, 5.5f);
                    if (closestPed != null)
                    {
                        UI.ShowSubtitle("ped selected!");
                        World.AddExplosion(closestPed.Position, ExplosionType.Steam, 1.0f, 0.1f);
                    }
                    else
                    {
                        UI.ShowSubtitle("Couldn't find a ped in front of you! You have selected yourself.");
                        closestPed = MindControl.CurrentPlayerCharacter;
                        World.AddExplosion(closestPed.Position, ExplosionType.Extinguisher, 1.0f, 0.1f);
                    }

                    memberMenu.Visible = !memberMenu.Visible;
                }
                else
                {
                    UI.ShowSubtitle("vehicle selected!");
                    carMenu.Visible = !carMenu.Visible;
                }
                RefreshNewEnemyMenuContent();
            }
        }

        public void OpenZoneMenu()
        {
            if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none)
            {
                ZoneManager.instance.OutputCurrentZoneInfo();
                zonesMenu.UpdateZoneUpgradeBtn();
                zonesMenu.Visible = !zonesMenu.Visible;
            }
        }

        public void OpenPickAiGangMenu(UIMenu callerMenu, string menuSubtitle, Action<Gang> onGangPicked)
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
            menuPool.ProcessMenus();

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
            List<dynamic> memberStyles = new List<dynamic>
            {
                "Business",
                "Street",
                "Beach",
                "Special"
            };

            List<dynamic> memberColors = new List<dynamic>
            {
                "White",
                "Black",
                "Red",
                "Green",
                "Blue",
                "Yellow",
                "Gray",
                "Pink",
                "Purple"
            };

            UIMenuListItem styleList = new UIMenuListItem("Member Dressing Style", memberStyles, 0, "The way the selected member is dressed. Used by the AI when picking members (if the AI gang's chosen style is the same as this member's, it may choose this member).");
            UIMenuListItem colorList = new UIMenuListItem("Member Color", memberColors, 0, "The color the member will be assigned to. Used by the AI when picking members (if the AI gang's color is the same as this member's, it may choose this member).");
            UIMenuCheckboxItem extendedModeToggle = new UIMenuCheckboxItem("Extended Save Mode", savePotentialMembersAsExtended, "If enabled, saves all clothing indexes for non-freemode peds. Can help with some addon peds.");

            memberMenu.AddItem(styleList);
            memberMenu.AddItem(colorList);
            memberMenu.AddItem(extendedModeToggle);
            memberMenu.OnListChange += (sender, item, index) =>
            {
                if (item == styleList)
                {
                    memberStyle = item.Index;
                }
                else if (item == colorList)
                {
                    memberColor = item.Index;
                }

            };

            memberMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == extendedModeToggle)
                {
                    savePotentialMembersAsExtended = checked_;
                }
            };

        }

        private void AddSaveMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save Potential Member for future AI gangs", "Saves the selected ped as a potential gang member with the specified data. AI gangs will be able to choose him\\her.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (PotentialGangMember.AddMemberAndSavePool(new FreemodePotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Potential freemode member added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("A similar potential member already exists.");
                        }
                    }
                    else
                    {
                        bool addAttempt = savePotentialMembersAsExtended ?
                            PotentialGangMember.AddMemberAndSavePool(new ExtendedPotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                            PotentialGangMember.AddMemberAndSavePool(new PotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));

                        if (addAttempt)
                        {
                            UI.ShowSubtitle("Potential member added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("A similar potential member already exists.");
                        }
                    }

                }
            };
        }

        private void AddNewPlayerGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save ped type for your gang", "Saves the selected ped type as a member of your gang, with the specified data. The selected ped himself won't be a member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (GangManager.instance.PlayerGang.AddMemberVariation(new FreemodePotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Freemode Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your gang already has a similar member.");
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
                            UI.ShowSubtitle("Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your gang already has a similar member.");
                        }
                    }

                }
            };
        }

        private void AddNewEnemyMemberSubMenu()
        {
            specificGangMemberRegSubMenu = menuPool.AddSubMenu(memberMenu, "Save ped type for a specific enemy gang...");

            specificGangMemberRegSubMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang pickedGang = GangManager.instance.GetGangByName(item.Text);
                if (pickedGang != null)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (pickedGang.AddMemberVariation(new FreemodePotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Freemode Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That gang already has a similar member.");
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
                            UI.ShowSubtitle("Member added successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That gang already has a similar member.");
                        }
                    }
                }
            };

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
                    specificGangMemberRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
                    specificCarRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
                }
            }

            specificGangMemberRegSubMenu.RefreshIndex();
            specificCarRegSubMenu.RefreshIndex();

        }

        private void AddRemoveGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove ped type from respective gang", "If the selected ped type was a member of a gang, it will no longer be. The selected ped himself will still be a member, however. This works for your own gang and for the enemies.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Gang ownerGang = GangManager.instance.GetGangByRelGroup(closestPed.RelationshipGroup);
                    if (ownerGang == null)
                    {
                        UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        return;
                    }
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        if (ownerGang.RemoveMemberVariation(new FreemodePotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member removed successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        }
                    }
                    else
                    {
                        if (ownerGang.RemoveMemberVariation(new PotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) ||
                        ownerGang.RemoveMemberVariation(new ExtendedPotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)))
                        {
                            UI.ShowSubtitle("Member removed successfully!");
                        }
                        else
                        {
                            UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
                        }
                    }
                }
            };
        }

        private void AddRemoveFromAllGangsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove ped type from all gangs and pool", "Removes the ped type from all gangs and from the member pool, which means future gangs also won't try to use this type. The selected ped himself will still be a gang member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01)
                    {
                        FreemodePotentialGangMember memberToRemove = new FreemodePotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                        if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove))
                        {
                            UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
                        }
                        else
                        {
                            UI.ShowSubtitle("Ped type not found in pool.");
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
                            UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
                        }
                        else
                        {
                            UI.ShowSubtitle("Ped type not found in pool.");
                        }

                        for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++)
                        {
                            GangManager.instance.gangData.gangs[i].RemoveMemberVariation(memberToRemove);
                        }
                    }
                }
            };
        }

        private void AddMakeFriendlyToPlayerGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Make Ped friendly to your gang", "Makes the selected ped (and everyone from his group) and your gang become allies. Can't be used with cops or gangs from this mod! NOTE: this only lasts until scripts are loaded again");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    int closestPedRelGroup = closestPed.RelationshipGroup;
                    //check if we can really become allies with this guy
                    if (closestPedRelGroup != Function.Call<int>(Hash.GET_HASH_KEY, "COP"))
                    {
                        //he can still be from one of the gangs! we should check

                        if (GangManager.instance.GetGangByRelGroup(closestPedRelGroup) != null)
                        {
                            UI.ShowSubtitle("That ped is a gang member! Gang members cannot be marked as allies");
                            return;
                        }

                        //ok, we can be allies
                        Gang playerGang = GangManager.instance.PlayerGang;
                        World.SetRelationshipBetweenGroups(Relationship.Respect, playerGang.relationGroupIndex, closestPedRelGroup);
                        World.SetRelationshipBetweenGroups(Relationship.Respect, closestPedRelGroup, playerGang.relationGroupIndex);
                        UI.ShowSubtitle("That ped's group is now an allied group!");
                    }
                    else
                    {
                        UI.ShowSubtitle("That ped is a cop! Cops cannot be marked as allies");
                    }
                }
            };
        }

        private void AddSaveVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register Vehicle as usable by AI Gangs", "Makes the vehicle type you are driving become chooseable as one of the types used by AI gangs.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (PotentialGangVehicle.AddVehicleAndSavePool(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Vehicle added to pool!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle has already been added to the pool.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRegisterPlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register Vehicle for your Gang", "Makes the vehicle type you are driving become one of the default types used by your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        // Capture vehicle mods
                        List<VehicleModData> capturedMods = new List<VehicleModData>();
                        foreach (VehicleMod modType in Enum.GetValues(typeof(VehicleMod)))
                        {
                            int modIndex = curVehicle.GetMod(modType);
                            if (modIndex != -1) // If the mod is installed
                            {
                                capturedMods.Add(new VehicleModData { ModType = modType, ModValue = modIndex });
                            }
                        }

                        // Create a new PotentialGangVehicle and set its mods
                        PotentialGangVehicle newGangVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);
                        newGangVehicle.VehicleMods = capturedMods;

                        if (GangManager.instance.PlayerGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is already registered for your gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRegisterEnemyVehicleButton()
        {
            specificCarRegSubMenu = menuPool.AddSubMenu(carMenu, "Register vehicle for a specific enemy gang...");

            specificCarRegSubMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang pickedGang = GangManager.instance.GetGangByName(item.Text);
                if (pickedGang != null)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (pickedGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle added!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is already registered for that gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRemovePlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from your Gang", "Removes the vehicle type you are driving from the possible vehicle types for your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.instance.PlayerGang.RemoveGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
                        {
                            UI.ShowSubtitle("Gang vehicle removed!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is not registered for your gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        private void AddRemoveVehicleEverywhereButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from all gangs and pool", "Removes the vehicle type you are driving from the possible vehicle types for all gangs, including yours. Existing gangs will also stop using that car and get another one if needed.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        PotentialGangVehicle removedVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);

                        if (PotentialGangVehicle.RemoveVehicleAndSavePool(removedVehicle))
                        {
                            UI.ShowSubtitle("Vehicle type removed from pool!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Vehicle type not found in pool.");
                        }

                        for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++)
                        {
                            GangManager.instance.gangData.gangs[i].RemoveGangCar(removedVehicle);
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        #endregion

    }
}
