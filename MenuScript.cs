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
    class MenuScript
    {

        MenuPool menuPool;
        UIMenu zonesMenu, gangMenu, memberMenu;
        Ped closestPed;
        bool saveTorsoIndex = false, saveTorsoTex = false, saveLegIndex = false, saveLegTex = false;
        bool inputFieldOpen = false;
        PotentialGangMember memberToSave = new PotentialGangMember();
        int memberStyle = 0, memberColor = 0;

        int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost;

        Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem> weaponEntries = 
            new Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem>();

        UIMenuItem healthButton, armorButton, accuracyButton, takeZoneButton;

        public MenuScript()
        {
            zonesMenu = new UIMenu("Gang Mod", "Zone Controls");
            memberMenu = new UIMenu("Gang Mod", "Gang Member Registration Controls");
            gangMenu = new UIMenu("Gang Mod", "Gang Controls");
            menuPool = new MenuPool();
            menuPool.Add(zonesMenu);
            menuPool.Add(gangMenu);
            menuPool.Add(memberMenu);
            
            AddSaveZoneButton();
            AddGangTakeoverButton();
            AddMemberToggles();
            AddMemberStyleChoices();
            AddSaveMemberButton();
            AddNewPlayerGangMemberButton();
            AddRemovePlayerGangMemberButton();
            AddCallVehicleButton();
            AddRegisterVehicleButton();
            AddCallParatrooperButton();
            AddRenameGangButton();
            AddGangUpgradesMenu();
            UpdateBuyableWeapons();
            AddGangWeaponsMenu();
            AddEnableFightingToggle();
            AddReloadOptionsButton();
            AddResetOptionsButton();

            zonesMenu.RefreshIndex();
            gangMenu.RefreshIndex();
            memberMenu.RefreshIndex();

            //add mouse click as another "select" button
            menuPool.SetKey(UIMenu.MenuControls.Select, Control.PhoneSelect);
            InstructionalButton clickButton = new InstructionalButton(Control.PhoneSelect, "Select");
            zonesMenu.AddInstructionalButton(clickButton);
            gangMenu.AddInstructionalButton(clickButton);
            memberMenu.AddInstructionalButton(clickButton);
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

        public void OpenPedRegistrationMenu()
        {
            if (!menuPool.IsAnyMenuOpen())
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

            if (inputFieldOpen)
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
                    
                    inputFieldOpen = false;
                }
                else if(inputFieldSituation == 2 || inputFieldSituation == 3)
                {
                    inputFieldOpen = false;
                }
            }
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

        #region register Member Stuff

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
        #endregion

        #region Gang Control stuff

        void AddCallVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Call Backup Vehicle", "Calls one of your gang's vehicles to your position. The vehicle drops its passengers and then leaves unless it is attacked.");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if(item == newButton)
                {
                    Gang playergang = GangManager.instance.GetPlayerGang();
                    if(ZoneManager.instance.GetZonesControlledByGang(playergang.name).Length > 0)
                    {
                        Math.Vector3 destPos = World.GetNextPositionOnStreet(Game.Player.Character.Position);
                        Vehicle spawnedVehicle = GangManager.instance.SpawnGangVehicle(GangManager.instance.GetPlayerGang(),
                   Game.Player.Character.Position + RandomUtil.RandomDirection(true) * 100, destPos, true, false);
                        if (spawnedVehicle != null)
                        {
                            spawnedVehicle.PlaceOnNextStreet();
                            spawnedVehicle.GetPedOnSeat(VehicleSeat.Driver).Task.DriveTo(spawnedVehicle, destPos, 10, 50);
                            gangMenu.Visible = !gangMenu.Visible;
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
               
            };
        }

        void AddRegisterVehicleButton()
        {
            UIMenuItem newButton = new UIMenuItem("Register Gang Vehicle", "Makes the vehicle type you are driving become the default type used by your gang.");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if(item == newButton)
                {
                    Vehicle curVehicle = Game.Player.Character.CurrentVehicle;
                    if (curVehicle != null)
                    {
                        if (GangManager.instance.GetPlayerGang().SetGangCar(curVehicle))
                        {
                            UI.ShowSubtitle("Gang vehicle changed!");
                        }
                        else
                        {
                            UI.ShowSubtitle("That vehicle is already registered as the default for your gang.");
                        }
                    }
                    else
                    {
                        UI.ShowSubtitle("You are not inside a vehicle.");
                    }
                }
            };
        }

        void AddCallParatrooperButton()
        {
            UIMenuItem newButton = new UIMenuItem("Call parachuting member", "Calls a gang member who parachutes to your position (member survival not guaranteed!).");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
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

            };
        }

        void AddGangUpgradesMenu()
        {
            UIMenu upgradesMenu = menuPool.AddSubMenu(gangMenu, "Gang Upgrades Menu");

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
            UIMenu weaponsMenu = menuPool.AddSubMenu(gangMenu, "Gang Weapons Menu");

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

        void AddRenameGangButton()
        {
            UIMenuItem newButton = new UIMenuItem("Rename Gang", "Resets your gang's name.");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    gangMenu.Visible = !gangMenu.Visible;
                    Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, "FMMC_KEY_TIP12N", "", "Gang Name", "", "", "", 30);
                    inputFieldOpen = true;
                }
            };
        }

        void AddEnableFightingToggle()
        {
            UIMenuCheckboxItem fightingToggle = new UIMenuCheckboxItem("Gang Fights Enabled?", true, "If checked, members from different gangs will attack each other (including the player). Gang wars also won't happen.");

            gangMenu.AddItem(fightingToggle);
            gangMenu.OnCheckboxChange += (sender, item, checked_) =>
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

        void AddReloadOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newButton)
                {
                    ModOptions.instance.LoadOptions();
                    GangManager.instance.ResetGangUpdateIntervals();
                    UpdateBuyableWeapons();
                    UpdateUpgradeCosts();
                    UpdateTakeOverBtnText();
                }
            };
        }

        void AddResetOptionsButton()
        {
            UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
            gangMenu.AddItem(newButton);
            gangMenu.OnItemSelect += (sender, item, index) =>
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
