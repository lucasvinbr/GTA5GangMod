using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using NativeUI;
using System.Windows.Forms;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the nativeUI-implementing class
    /// -------------thanks to the NativeUI developers!-------------
    /// </summary>
    public class MenuScript
    {

        MenuPool menuPool;
        UIMenu zonesMenu, gangMenu, memberMenu, carMenu, gangOptionsSubMenu, 
            modSettingsSubMenu;
        Ped closestPed;
        bool saveTorsoIndex = false, saveTorsoTex = false, saveLegIndex = false, saveLegTex = false;
        
        PotentialGangMember memberToSave = new PotentialGangMember();
        int memberStyle = 0, memberColor = 0;

        int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost;

        Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem> weaponEntries = 
            new Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem>();

        Dictionary<VehicleColor, UIMenuItem> carColorEntries =
            new Dictionary<VehicleColor, UIMenuItem>();

        UIMenuItem healthButton, armorButton, accuracyButton, takeZoneButton,
            openGangMenuBtn, openZoneMenuBtn, mindControlBtn, addToGroupBtn, carBackupBtn, paraBackupBtn;

        private int ticksSinceLastCarBkp = 5000, ticksSinceLastParaBkp = 5000;

        public UIMenuListItem aggOption;

        public enum desiredInputType
        {
            none,
            enterGangName,
            changeKeyBinding
        }

        public enum changeableKeyBinding
        {
            GangMenuBtn,
            ZoneMenuBtn,
            MindControlBtn,
            AddGroupBtn,
        }

        public desiredInputType curInputType = desiredInputType.none;
        public changeableKeyBinding targetKeyBindToChange = changeableKeyBinding.AddGroupBtn;

        public static MenuScript instance;

        public MenuScript()
        {
            instance = this;
            zonesMenu = new UIMenu("Gang Mod", "Zone Controls");
            memberMenu = new UIMenu("Gang Mod", "Gang Member Registration Controls");
            carMenu = new UIMenu("Gang Mod", "Gang Vehicle Registration Controls");
            gangMenu = new UIMenu("Gang Mod", "Gang Controls");
            menuPool = new MenuPool();
            menuPool.Add(zonesMenu);
            menuPool.Add(gangMenu);
            menuPool.Add(memberMenu);
            menuPool.Add(carMenu);

            AddGangTakeoverButton();
            AddSaveZoneButton();
            //AddZoneCircleButton();
            
            AddMemberToggles();
            AddMemberStyleChoices();
            AddSaveMemberButton();
            AddNewPlayerGangMemberButton();
            AddRemovePlayerGangMemberButton();
            AddCallVehicleButton();
            AddCallParatrooperButton();
            AddSaveVehicleButton();
            AddRegisterPlayerVehicleButton();
            AddRemovePlayerVehicleButton();

            UpdateBuyableWeapons();
            AddGangOptionsSubMenu();
            AddModSettingsSubMenu();

            zonesMenu.RefreshIndex();
            gangMenu.RefreshIndex();
            memberMenu.RefreshIndex();

            aggOption.Index = (int)ModOptions.instance.gangMemberAggressiveness;

            //add mouse click as another "select" button
            menuPool.SetKey(UIMenu.MenuControls.Select, Control.PhoneSelect);
            InstructionalButton clickButton = new InstructionalButton(Control.PhoneSelect, "Select");
            zonesMenu.AddInstructionalButton(clickButton);
            gangMenu.AddInstructionalButton(clickButton);
            memberMenu.AddInstructionalButton(clickButton);

            ticksSinceLastCarBkp = ModOptions.instance.ticksCooldownBackupCar;
            ticksSinceLastParaBkp = ModOptions.instance.ticksCooldownParachutingMember;
        }

        #region menu opening methods
        public void OpenGangMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                UpdateUpgradeCosts();
                UpdateBuyableWeapons();
                gangMenu.Visible = !gangMenu.Visible;
            }
        }

        public void OpenContextualRegistrationMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                if(Game.Player.Character.CurrentVehicle == null)
                {
                    closestPed = World.GetClosestPed(Game.Player.Character.Position + Game.Player.Character.ForwardVector * 6.0f, 5.5f);
                    if (closestPed != null)
                    {
                        UI.ShowSubtitle("ped selected!");
                        World.AddExplosion(closestPed.Position, ExplosionType.WaterHydrant, 1.0f, 0.1f);
                        memberMenu.Visible = !memberMenu.Visible;
                    }
                    else
                    {
                        UI.ShowSubtitle("couldn't find a ped in front of you!");
                    }
                }
                else
                {
                    UI.ShowSubtitle("vehicle selected!");
                    carMenu.Visible = !carMenu.Visible;
                }
                
            }
        }

        public void OpenZoneMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
            {
                ZoneManager.instance.OutputCurrentZoneInfo();
                zonesMenu.Visible = !zonesMenu.Visible;
            }
        }
        #endregion


        public void Tick()
        {
            menuPool.ProcessMenus();

            if (curInputType == desiredInputType.enterGangName)
            {
                int inputFieldSituation = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);
                if(inputFieldSituation == 1)
                {
                    string typedText = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
                    if(typedText != "none")
                    {
                        ZoneManager.instance.GiveGangZonesToAnother(GangManager.instance.GetPlayerGang().name, typedText);
                        GangManager.instance.GetPlayerGang().name = typedText;
                        GangManager.instance.SaveGangData();

                        UI.ShowSubtitle("Your gang is now known as the " + typedText);
                    }
                    else
                    {
                        UI.ShowSubtitle("That name is not allowed, sorry!");
                    }

                    curInputType = desiredInputType.none;
                }
                else if(inputFieldSituation == 2 || inputFieldSituation == 3)
                {
                    curInputType = desiredInputType.none;
                }
            }

            //countdown for next backups
            ticksSinceLastCarBkp++;
            if (ticksSinceLastCarBkp > ModOptions.instance.ticksCooldownBackupCar)
                ticksSinceLastCarBkp = ModOptions.instance.ticksCooldownBackupCar;
            ticksSinceLastParaBkp++;
            if (ticksSinceLastParaBkp > ModOptions.instance.ticksCooldownParachutingMember)
                ticksSinceLastParaBkp = ModOptions.instance.ticksCooldownParachutingMember;
        }

        void UpdateUpgradeCosts()
        {
            Gang playerGang = GangManager.instance.GetPlayerGang();
            healthUpgradeCost = (playerGang.memberHealth + 20) * 20;
            armorUpgradeCost = (playerGang.memberArmor + 20) * 50;
            accuracyUpgradeCost = (playerGang.memberAccuracyLevel + 10) * 250;

            healthButton.Text = "Upgrade Member Health - " + healthUpgradeCost.ToString();
            armorButton.Text = "Upgrade Member Armor - " + armorUpgradeCost.ToString();
            accuracyButton.Text = "Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString();
        }

        void UpdateTakeOverBtnText()
        {
            takeZoneButton.Description = "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
                ModOptions.instance.costToTakeNeutralTurf.ToString() + ". If it belongs to another gang, a battle will begin, for half that price.";
        }

        void UpdateBuyableWeapons()
        {
            Gang playerGang = GangManager.instance.GetPlayerGang();
            List<ModOptions.BuyableWeapon> weaponsList = ModOptions.instance.buyableWeapons;

            for(int i = 0; i < weaponsList.Count; i++)
            {
                if (weaponEntries.ContainsKey(weaponsList[i])){
                    weaponEntries[weaponsList[i]].Checked = playerGang.gangWeaponHashes.Contains(weaponsList[i].wepHash);
                }
                else
                {
                    weaponEntries.Add(weaponsList[i], new UIMenuCheckboxItem
                        (string.Concat(weaponsList[i].wepHash.ToString(), " - ", weaponsList[i].price.ToString()),
                        playerGang.gangWeaponHashes.Contains(weaponsList[i].wepHash)));
                }
            }

        }

        void FillCarColorEntries()
        {
            foreach(ModOptions.GangColorTranslation colorList in ModOptions.instance.similarColors)
            {
                for(int i = 0; i < colorList.vehicleColors.Count; i++)
                {
                    carColorEntries.Add(colorList.vehicleColors[i], new UIMenuItem(colorList.vehicleColors[i].ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
                }
            }
        }

        public void RefreshKeyBindings()
        {
            openGangMenuBtn.Text = "Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString();
            openZoneMenuBtn.Text = "Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString();
            addToGroupBtn.Text = "Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString();
            mindControlBtn.Text = "Take Control of Member - " + ModOptions.instance.mindControlKey.ToString();
        }

        #region Zone Menu Stuff

        void AddSaveZoneButton()
        {
            UIMenuItem newButton = new UIMenuItem("Add Current Zone to Takeables", "Makes the zone you are in become takeable by gangs and sets your position as the zone's reference position (if toggled, this zone's blip will show here).");
            zonesMenu.AddItem(newButton);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    string curZoneName = World.GetZoneName(Game.Player.Character.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        //add a new zone then
                        curZone = new TurfZone(curZoneName);
                    }

                    //update the zone's blip position even if it already existed
                    curZone.zoneBlipPosition = Game.Player.Character.Position;
                    ZoneManager.instance.UpdateZoneData(curZone);
                    UI.ShowSubtitle("Zone Data Updated!");
                }
            };

        }

        //void AddZoneCircleButton()
        //{
        //    UIMenuItem newButton = new UIMenuItem("New Zone Circle", "Create a new map circle to represent this zone's boundaries.");
        //    zonesMenu.AddItem(newButton);
        //    zonesMenu.OnItemSelect += (sender, item, index) =>
        //    {
        //        if (item == newButton)
        //        {
        //            string curZoneName = World.GetZoneName(Game.Player.Character.Position);
        //            TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);

        //            ZoneManager.instance.AddNewCircleBlip(Game.Player.Character.Position, curZone);
                    
        //            ZoneManager.instance.UpdateZoneData(curZone);
        //            UI.ShowSubtitle("Zone Data Updated!");
        //        }
        //    };
        //}

        void AddGangTakeoverButton()
        {
            takeZoneButton = new UIMenuItem("Take current zone",
                "Makes the zone you are in become part of your gang's turf. If it's not controlled by any gang, it will instantly become yours for a price of $" +
                ModOptions.instance.costToTakeNeutralTurf.ToString() +". If it belongs to another gang, a battle will begin, for half that price.");
            zonesMenu.AddItem(takeZoneButton);
            zonesMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == takeZoneButton)
                {
                    string curZoneName = World.GetZoneName(Game.Player.Character.Position);
                    TurfZone curZone = ZoneManager.instance.GetZoneByName(curZoneName);
                    if (curZone == null)
                    {
                        UI.ShowSubtitle("this zone isn't marked as takeable.");
                    }
                    else
                    {
                       if(curZone.ownerGangName == "none")
                        {
                            if (Game.Player.Money >= ModOptions.instance.costToTakeNeutralTurf)
                            {
                                Game.Player.Money -= ModOptions.instance.costToTakeNeutralTurf;
                                GangManager.instance.GetPlayerGang().TakeZone(curZone);
                                UI.ShowSubtitle("This zone is " + GangManager.instance.GetPlayerGang().name + " turf now!");
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have the resources to take over a neutral zone.");
                            }
                        }
                        else
                        {
                            if(curZone.ownerGangName == GangManager.instance.GetPlayerGang().name)
                            {
                                UI.ShowSubtitle("Your gang already owns this zone.");
                            }
                            else
                            if (Game.Player.Money >= ModOptions.instance.costToTakeNeutralTurf / 2)
                            {
                                zonesMenu.Visible = !zonesMenu.Visible;
                                if (GangManager.instance.fightingEnabled)
                                {
                                    if (!GangWarManager.instance.StartWar(GangManager.instance.GetGangByName(curZone.ownerGangName), curZone, GangWarManager.warType.attackingEnemy))
                                    {
                                        UI.ShowSubtitle("A war is already in progress.");
                                    }
                                    else
                                    {
                                        Game.Player.Money -= ModOptions.instance.costToTakeNeutralTurf / 2;
                                    }
                                }
                                else
                                {
                                    UI.ShowSubtitle("Gang Fights must be enabled in order to start a war!");
                                }
                                
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have the resources to start a battle against another gang.");
                            }
                        }
                    }
                }
            };
        }

        #endregion

        #region register Member/Vehicle Stuff

        void AddMemberToggles()
        {
            UIMenuItem saveTorsoToggle = new UIMenuCheckboxItem("Save torso variation", saveTorsoIndex, "If checked, this member will always spawn with this torso variation.");
            UIMenuItem saveTorsoTexToggle = new UIMenuCheckboxItem("Save torso variation color", saveTorsoTex, "If checked, this member will always spawn with this torso color and torso variation (the variation must also be saved in this case!).");
            UIMenuItem saveLegToggle = new UIMenuCheckboxItem("Save leg variation", saveLegIndex, "If checked, this member will always spawn with this leg variation.");
            UIMenuItem saveLegTexToggle = new UIMenuCheckboxItem("Save leg variation color", saveLegTex, "If checked, this member will always spawn with this leg color and variation (the leg variation must also be saved in this case!).");

            memberMenu.AddItem(saveTorsoToggle);
            memberMenu.AddItem(saveTorsoTexToggle);
            memberMenu.AddItem(saveLegToggle);
            memberMenu.AddItem(saveLegTexToggle);

            memberMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == saveTorsoToggle)
                {
                    saveTorsoIndex = checked_;
                }
                else if(item == saveTorsoTexToggle)
                {
                    saveTorsoTex = checked_;
                }
                else if (item == saveLegToggle)
                {
                    saveLegIndex = checked_;
                }
                else if (item == saveLegTexToggle)
                {
                    saveLegTex = checked_;
                }
            };
        }

        void AddMemberStyleChoices()
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
            memberMenu.AddItem(styleList);
            memberMenu.AddItem(colorList);
            memberMenu.OnListChange += (sender, item, index) =>
            {
                if (item == styleList)
                {
                    memberStyle = item.Index;
                }
                else if(item == colorList)
                {
                    memberColor = item.Index;
                }

            };

        }

        void AddSaveMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save Potential Member", "Saves the selected ped as a potential gang member with the specified data. AI gangs will be able to choose him\\her.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    memberToSave.modelHash = closestPed.Model.Hash;

                    if (saveTorsoIndex)
                    {
                        memberToSave.torsoDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 3);
                        if (saveTorsoTex)
                        {
                            memberToSave.torsoTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 3);
                        }
                        else
                        {
                            memberToSave.torsoTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.torsoDrawableIndex = -1;
                        memberToSave.torsoTextureIndex = -1;
                    }

                    if (saveLegIndex)
                    {
                        memberToSave.legsDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 4);
                        if (saveLegTex)
                        {
                            memberToSave.legsTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 4);
                        }
                        else
                        {
                            memberToSave.legsTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.legsDrawableIndex = -1;
                        memberToSave.legsTextureIndex = -1;
                    }

                    memberToSave.myStyle = (PotentialGangMember.dressStyle) memberStyle;
                    memberToSave.linkedColor = (PotentialGangMember.memberColor) memberColor;

                    if (PotentialGangMember.AddMemberAndSavePool
                    (new PotentialGangMember(memberToSave.modelHash, memberToSave.myStyle, memberToSave.linkedColor,
                        memberToSave.torsoDrawableIndex, memberToSave.torsoTextureIndex,
                        memberToSave.legsDrawableIndex, memberToSave.legsTextureIndex)))
                    {
                        UI.ShowSubtitle("Potential member added!");
                    }
                    else
                    {
                        UI.ShowSubtitle("A similar potential member already exists.");
                    }
                }
            };
        }

        void AddNewPlayerGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Save ped type as your gang member", "Saves the selected ped type as a member of your gang, with the specified data. The selected ped himself won't be a member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {

                    memberToSave.modelHash = closestPed.Model.Hash;

                    if (saveTorsoIndex)
                    {
                        memberToSave.torsoDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 3);
                        if (saveTorsoTex)
                        {
                            memberToSave.torsoTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 3);
                        }
                        else
                        {
                            memberToSave.torsoTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.torsoDrawableIndex = -1;
                        memberToSave.torsoTextureIndex = -1;
                    }

                    if (saveLegIndex)
                    {
                        memberToSave.legsDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 4);
                        if (saveLegTex)
                        {
                            memberToSave.legsTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 4);
                        }
                        else
                        {
                            memberToSave.legsTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.legsDrawableIndex = -1;
                        memberToSave.legsTextureIndex = -1;
                    }

                    memberToSave.myStyle = (PotentialGangMember.dressStyle)memberStyle;
                    memberToSave.linkedColor = (PotentialGangMember.memberColor)memberColor;

                    if (GangManager.instance.GetPlayerGang().AddMemberVariation(new PotentialGangMember(memberToSave.modelHash,
                        memberToSave.myStyle, memberToSave.linkedColor,
                        memberToSave.torsoDrawableIndex, memberToSave.torsoTextureIndex,
                        memberToSave.legsDrawableIndex, memberToSave.legsTextureIndex)))
                    {
                        UI.ShowSubtitle("Member added successfully!");
                    }
                    else
                    {
                        UI.ShowSubtitle("Your gang already has a similar member.");
                    }
                }
            };
        }

        void AddRemovePlayerGangMemberButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove ped type from your gang", "If the selected ped type was a member of your gang, it will no longer be. The selected ped himself will still be a member, however.");
            memberMenu.AddItem(newButton);
            memberMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {

                    memberToSave.modelHash = closestPed.Model.Hash;

                    if (saveTorsoIndex)
                    {
                        memberToSave.torsoDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 3);
                        if (saveTorsoTex)
                        {
                            memberToSave.torsoTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 3);
                        }
                        else
                        {
                            memberToSave.torsoTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.torsoDrawableIndex = -1;
                        memberToSave.torsoTextureIndex = -1;
                    }

                    if (saveLegIndex)
                    {
                        memberToSave.legsDrawableIndex = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, closestPed, 4);
                        if (saveLegTex)
                        {
                            memberToSave.legsTextureIndex = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, closestPed, 4);
                        }
                        else
                        {
                            memberToSave.legsTextureIndex = -1;
                        }
                    }
                    else
                    {
                        memberToSave.legsDrawableIndex = -1;
                        memberToSave.legsTextureIndex = -1;
                    }

                    memberToSave.myStyle = (PotentialGangMember.dressStyle)memberStyle;
                    memberToSave.linkedColor = (PotentialGangMember.memberColor)memberColor;

                    if (GangManager.instance.GetPlayerGang().RemoveMemberVariation(new PotentialGangMember(memberToSave.modelHash,
                        memberToSave.myStyle, memberToSave.linkedColor,
                        memberToSave.torsoDrawableIndex, memberToSave.torsoTextureIndex,
                        memberToSave.legsDrawableIndex, memberToSave.legsTextureIndex)))
                    {
                        UI.ShowSubtitle("Member removed successfully!");
                    }
                    else
                    {
                        UI.ShowSubtitle("Your gang doesn't seem to have a similar member. Make sure you've selected the registration options (color and style don't matter) just like the ones you did when registering!", 8000);
                    }
                }
            };
        }

        void AddSaveVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register vehicle as usable by AI Gangs", "Makes the vehicle type you are driving become chooseable as one of the types used by AI gangs.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = Game.Player.Character.CurrentVehicle;
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

        void AddRegisterPlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register as Gang Vehicle", "Makes the vehicle type you are driving become one of the default types used by your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = Game.Player.Character.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.instance.GetPlayerGang().AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
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

        void AddRemovePlayerVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type for your Gang", "Removes the vehicle type you are driving from the possible vehicle types for your gang.");
            carMenu.AddItem(newButton);
            carMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    Vehicle curVehicle = Game.Player.Character.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.instance.GetPlayerGang().RemoveGangCar(new PotentialGangVehicle(curVehicle.Model.Hash)))
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

        #endregion

        #region Gang Control Stuff

        void AddCallVehicleButton()
        {
            carBackupBtn = new UIMenuItem("Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")", "Calls one of your gang's vehicles to your position. All passengers leave the vehicle once it arrives.");
            gangMenu.AddItem(carBackupBtn);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if(item == carBackupBtn)
                {
                    if(ticksSinceLastCarBkp < ModOptions.instance.ticksCooldownBackupCar)
                    {
                        UI.ShowSubtitle("You must wait before calling for car backup again! (This is configurable)");
                        return;
                    }
                    if(Game.Player.Money >= ModOptions.instance.costToCallBackupCar)
                    {
                        Gang playergang = GangManager.instance.GetPlayerGang();
                        if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Length > 0)
                        {
                            Math.Vector3 destPos = Game.Player.Character.Position;

                            Math.Vector3 spawnPos = GangManager.instance.FindGoodSpawnPointForCar();

                            Vehicle spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.GetPlayerGang(),
                                    spawnPos, destPos, true, false, true);
                            if (spawnedVehicle != null)
                            {
                                GangManager.instance.TryPlaceVehicleOnStreet(spawnedVehicle, spawnPos);
                                spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver).Task.DriveTo(spawnedVehicle, destPos, 25, 100);
                                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver), 4457020); //ignores roads, avoids obstacles

                                gangMenu.Visible = !gangMenu.Visible;
                                ticksSinceLastCarBkp = 0;
                                Game.Player.Money -= ModOptions.instance.costToCallBackupCar;
                                UI.ShowSubtitle("A vehicle is on its way!", 1000);
                            }
                            else
                            {
                                UI.ShowSubtitle("There are too many gang members around or you haven't registered any member or car.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You need $" + ModOptions.instance.costToCallBackupCar.ToString() + " to call a vehicle!");
                    }
                    
                }
               
            };
        }

        void AddCallParatrooperButton()
        {
            paraBackupBtn = new UIMenuItem("Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")", "Calls a gang member who parachutes to your position (member survival not guaranteed!).");
            gangMenu.AddItem(paraBackupBtn);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == paraBackupBtn)
                {
                    if(ticksSinceLastParaBkp < ModOptions.instance.ticksCooldownParachutingMember)
                    {
                        UI.ShowSubtitle("You must wait before calling for parachuting backup again! (This is configurable)");
                        return;
                    }

                    if (Game.Player.Money >= ModOptions.instance.costToCallParachutingMember)
                    {
                        Gang playergang = GangManager.instance.GetPlayerGang();
                        if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Length > 0)
                        {
                            Math.Vector3 destPos = Game.Player.Character.Position;
                            Ped spawnedPed = GangManager.instance.SpawnGangMember(GangManager.instance.GetPlayerGang(),
                       Game.Player.Character.Position + Math.Vector3.WorldUp * 50);
                            if (spawnedPed != null)
                            {
                                spawnedPed.Task.ParachuteTo(destPos);
                                ticksSinceLastParaBkp = 0;
                                Game.Player.Money -= ModOptions.instance.costToCallParachutingMember;
                                gangMenu.Visible = !gangMenu.Visible;
                            }
                            else
                            {
                                UI.ShowSubtitle("There are too many gang members around or you haven't registered any member.");
                            }
                        }
                        else
                        {
                            UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You need $" + ModOptions.instance.costToCallParachutingMember.ToString() + " to call a parachuting member!");
                    }
                   
                }

            };
        }

        void AddGangOptionsSubMenu()
        {
            gangOptionsSubMenu = menuPool.AddSubMenu(gangMenu, "Gang Customization/Upgrades Menu");

            AddGangUpgradesMenu();
            AddGangWeaponsMenu();
            AddSetCarColorMenu();
            AddRenameGangButton();

            gangOptionsSubMenu.RefreshIndex();
        }

        void AddGangUpgradesMenu()
        {
            UIMenu upgradesMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Upgrades...");

            //upgrade buttons
            healthButton = new UIMenuItem("Upgrade Member Health - " + healthUpgradeCost.ToString(), "Increases gang member starting and maximum health. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            armorButton = new UIMenuItem("Upgrade Member Armor - " + armorUpgradeCost.ToString(), "Increases gang member starting body armor. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            accuracyButton = new UIMenuItem("Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString(), "Increases gang member firing accuracy. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
            upgradesMenu.AddItem(healthButton);
            upgradesMenu.AddItem(armorButton);
            upgradesMenu.AddItem(accuracyButton);
            upgradesMenu.RefreshIndex();

            upgradesMenu.OnItemSelect += (sender, item, index) =>
            {
                Gang playerGang = GangManager.instance.GetPlayerGang();

                if (item == healthButton)
                {
                    if(Game.Player.Money >= healthUpgradeCost)
                    {
                        if(playerGang.memberHealth < ModOptions.instance.maxGangMemberHealth)
                        {
                            playerGang.memberHealth += 20;
                            if(playerGang.memberHealth > ModOptions.instance.maxGangMemberHealth)
                            {
                                playerGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
                            }
                            Game.Player.Money -= healthUpgradeCost;
                            GangManager.instance.SaveGangData();
                            UI.ShowSubtitle("Member health upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' health is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == armorButton)
                {
                    if (Game.Player.Money >= armorUpgradeCost)
                    {
                        if (playerGang.memberArmor < ModOptions.instance.maxGangMemberArmor)
                        {
                            playerGang.memberArmor += 20;
                            if (playerGang.memberArmor > ModOptions.instance.maxGangMemberArmor)
                            {
                                playerGang.memberArmor = ModOptions.instance.maxGangMemberArmor;
                            }
                            Game.Player.Money -= armorUpgradeCost;
                            GangManager.instance.SaveGangData();
                            UI.ShowSubtitle("Member armor upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' armor is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                if (item == accuracyButton)
                {
                    if (Game.Player.Money >= accuracyUpgradeCost)
                    {
                        if (playerGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy)
                        {
                            playerGang.memberAccuracyLevel += 10;
                            if (playerGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy)
                            {
                                playerGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
                            }
                            Game.Player.Money -= accuracyUpgradeCost;
                            GangManager.instance.SaveGangData();
                            UI.ShowSubtitle("Member accuracy upgraded!");
                        }
                        else
                        {
                            UI.ShowSubtitle("Your members' accuracy is at its maximum limit (it can be configured in the ModOptions file)");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
                    }
                }

                UpdateUpgradeCosts();

            };

        }

        void AddGangWeaponsMenu()
        {
            UIMenu weaponsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Weapons Menu");

            Gang playerGang = GangManager.instance.GetPlayerGang();

            ModOptions.BuyableWeapon[] buyableWeaponsArray = weaponEntries.Keys.ToArray();
            UIMenuCheckboxItem[] weaponCheckBoxArray = weaponEntries.Values.ToArray();

            for (int i = 0; i < weaponCheckBoxArray.Length; i++)
            {
                weaponsMenu.AddItem(weaponCheckBoxArray[i]);
            }

            weaponsMenu.RefreshIndex();

            weaponsMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                for(int i = 0; i < buyableWeaponsArray.Length; i++)
                {
                    if(item == weaponEntries[buyableWeaponsArray[i]])
                    {
                        if (playerGang.gangWeaponHashes.Contains(buyableWeaponsArray[i].wepHash)){
                            playerGang.gangWeaponHashes.Remove(buyableWeaponsArray[i].wepHash);
                            Game.Player.Money += buyableWeaponsArray[i].price;
                            GangManager.instance.SaveGangData();
                            UI.ShowSubtitle("Weapon Removed!");
                        }
                        else
                        {
                            if(Game.Player.Money >= buyableWeaponsArray[i].price)
                            {
                                Game.Player.Money -= buyableWeaponsArray[i].price;
                                playerGang.gangWeaponHashes.Add(buyableWeaponsArray[i].wepHash);
                                GangManager.instance.SaveGangData();
                                UI.ShowSubtitle("Weapon Bought!");
                            }
                            else
                            {
                                UI.ShowSubtitle("You don't have enough money to buy that weapon for your gang.");
                            }
                        }

                        break;
                    }
                }

                UpdateBuyableWeapons();
            };
        }

        void AddSetCarColorMenu()
        {
            FillCarColorEntries();

            UIMenu carColorsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Vehicle Colors Menu");

            Gang playerGang = GangManager.instance.GetPlayerGang();

            VehicleColor[] carColorsArray = carColorEntries.Keys.ToArray();
            UIMenuItem[] colorButtonsArray = carColorEntries.Values.ToArray();

            for (int i = 0; i < colorButtonsArray.Length; i++)
            {
                carColorsMenu.AddItem(colorButtonsArray[i]);
            }

            carColorsMenu.RefreshIndex();

            carColorsMenu.OnIndexChange += (sender, index) =>
            {
                Vehicle playerVehicle = Game.Player.Character.CurrentVehicle;
                if (playerVehicle != null)
                {
                    playerVehicle.PrimaryColor = carColorsArray[index];
                }
            };
          
            carColorsMenu.OnItemSelect += (sender, item, checked_) =>
            {
                for (int i = 0; i < carColorsArray.Length; i++)
                {
                    if (item == carColorEntries[carColorsArray[i]])
                    {
                        playerGang.color = carColorsArray[i];
                        GangManager.instance.SaveGangData(false);
                        UI.ShowSubtitle("Gang vehicle color changed!");
                        break;
                    }
                }

            };
        }

        void AddRenameGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Rename Gang", "Resets your gang's name.");
            gangOptionsSubMenu.AddItem(newButton);
            gangOptionsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    gangOptionsSubMenu.Visible = !gangOptionsSubMenu.Visible;
                    Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, "FMMC_KEY_TIP12N", "", "Gang Name", "", "", "", 30);
                    curInputType = desiredInputType.enterGangName;
                }
            };
        }

        void AddModSettingsSubMenu()
        {
            modSettingsSubMenu = menuPool.AddSubMenu(gangMenu, "Mod Settings Menu");

            AddMemberAggressivenessControl();
            AddEnableAmbientSpawnToggle();
            AddEnableFightingToggle();
            AddEnableWarVersusPlayerToggle();
            AddKeyBindingMenu();
            AddReloadOptionsButton();
            AddResetOptionsButton();
           
            modSettingsSubMenu.RefreshIndex();
        }

        void AddMemberAggressivenessControl()
        { 
            List<dynamic> aggModes = new List<dynamic>
            { 
                "V. Aggressive",
                "Aggressive",
                "Defensive"
            };

            aggOption = new UIMenuListItem("Member Aggressiveness", aggModes,(int) ModOptions.instance.gangMemberAggressiveness, "This controls how aggressive members from all gangs will be. Very aggressive members will shoot at cops and other gangs on sight, aggressive members will shoot only at other gangs on sight and defensive members will only shoot when one of them is attacked or aimed at."); 
            modSettingsSubMenu.AddItem(aggOption);
            modSettingsSubMenu.OnListChange += (sender, item, index) => 
            { 
                if (item == aggOption) 
                {
                    ModOptions.instance.SetMemberAggressiveness((ModOptions.gangMemberAggressivenessMode)index);
                } 
            }; 
     } 


        void AddEnableFightingToggle()
        {
            UIMenuCheckboxItem fightingToggle = new UIMenuCheckboxItem("Gang Fights Enabled?", true, "If unchecked, members from different gangs won't attack each other (including the player). Gang wars also won't happen.");

            modSettingsSubMenu.AddItem(fightingToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == fightingToggle)
                {
                    if (!GangWarManager.instance.isOccurring)
                    {
                        GangManager.instance.fightingEnabled = checked_;
                    }
                    else
                    {
                        UI.ShowSubtitle("There's a war going on, this option can't be changed now.");
                        fightingToggle.Checked = !checked_;
                    }
                }

            };
        }

        void AddEnableWarVersusPlayerToggle()
        {
            UIMenuCheckboxItem warToggle = new UIMenuCheckboxItem("Enemy gangs can attack your turf?", true, "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them.");

            modSettingsSubMenu.AddItem(warToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == warToggle)
                {
                    GangManager.instance.warAgainstPlayerEnabled = checked_;
                }

            };
        }

        void AddEnableAmbientSpawnToggle()
        {
            UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Ambient member spawning?", true, "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars.");

            modSettingsSubMenu.AddItem(spawnToggle);
            modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == spawnToggle)
                {
                    AmbientGangMemberSpawner.instance.enabled = checked_;
                }

            };
        }

        void AddKeyBindingMenu()
        {
            UIMenu bindingsMenu = menuPool.AddSubMenu(modSettingsSubMenu, "Key Bindings...");

            //the buttons
            openGangMenuBtn = new UIMenuItem("Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString(), "The key used to open the Gang/Mod Menu. Used with shift to open the Member Registration Menu. Default is B.");
            openZoneMenuBtn = new UIMenuItem("Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString(), "The key used to check the current zone's name and ownership. Used with shift to open the Zone Menu and with control to toggle zone blip display modes. Default is N.");
            addToGroupBtn = new UIMenuItem("Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString(), "The key used to add/remove the targeted friendly gang member to/from your group. Members of your group will follow you. Default is H.");
            mindControlBtn = new UIMenuItem("Take Control of Member - " + ModOptions.instance.mindControlKey.ToString(), "The key used to take control of the targeted friendly gang member. Pressing this key while already in control of a member will restore protagonist control. Default is J.");
            bindingsMenu.AddItem(openGangMenuBtn);
            bindingsMenu.AddItem(openZoneMenuBtn);
            bindingsMenu.AddItem(addToGroupBtn);
            bindingsMenu.AddItem(mindControlBtn);
            bindingsMenu.RefreshIndex();

            bindingsMenu.OnItemSelect += (sender, item, index) =>
            {
                UI.ShowSubtitle("Press the new key for this command.");
                curInputType = desiredInputType.changeKeyBinding;

                if (item == openGangMenuBtn)
                {
                    targetKeyBindToChange = changeableKeyBinding.GangMenuBtn;
                }
                if (item == openZoneMenuBtn)
                {
                    targetKeyBindToChange = changeableKeyBinding.ZoneMenuBtn;
                }
                if (item == addToGroupBtn)
                {
                    targetKeyBindToChange = changeableKeyBinding.AddGroupBtn;
                }
                if (item == mindControlBtn)
                {
                    targetKeyBindToChange = changeableKeyBinding.MindControlBtn;
                }
            };
        }

        void AddReloadOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.instance.LoadOptions();
                    GangManager.instance.ResetGangUpdateIntervals();
                    UpdateBuyableWeapons();
                    UpdateUpgradeCosts();
                    carBackupBtn.Text = "Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")";
                    paraBackupBtn.Text = "Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")";
                    UpdateTakeOverBtnText();
                }
            };
        }

        void AddResetOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
            modSettingsSubMenu.AddItem(newButton);
            modSettingsSubMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.instance.SetAllValuesToDefault();
                    UpdateBuyableWeapons();
                    UpdateUpgradeCosts();
                    UpdateTakeOverBtnText();
                }
            };
        }

        #endregion
    }
}
