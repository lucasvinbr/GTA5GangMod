using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the script is responsble for ticking and detecting input for the more sensitive scripts.
    /// some, like the gang war manager, are still ticking on their own.
    /// this one exists to make sure the sensitive ones start running in the correct order
    /// </summary>
    internal class ModCore : Script
    {
        public GangManager gangManagerScript;
        public MenuScript menuScript;
        public ZoneManager zoneManagerScript;

        public static int curGameTime;

        public ModCore()
        {
            curGameTime = Game.GameTime;

            Logger.ClearLog();
            Logger.Log("mod started!", 2);

            zoneManagerScript = new ZoneManager();

            MindControl.SetupData();
            gangManagerScript = new GangManager();
            

            menuScript = new MenuScript();

            this.Aborted += OnAbort;

            this.KeyUp += OnKeyUp;
            this.Tick += OnTick;


            bool successfulInit = GangMemberUpdater.Initialize();

            while (successfulInit == false)
            {
                Yield();
                successfulInit = GangMemberUpdater.Initialize();
            }

            successfulInit = GangVehicleUpdater.Initialize();

            while (successfulInit == false)
            {
                Yield();
                successfulInit = GangVehicleUpdater.Initialize();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            curGameTime = Game.GameTime;
            gangManagerScript.Tick();
            MindControl.Tick();
            menuScript.Tick();

            //war stuff that should happen every frame
            if (GangWarManager.instance.shouldDisplayReinforcementsTexts)
            {
                GangWarManager.instance.alliedNumText.Draw();
                GangWarManager.instance.enemyNumText.Draw();

                if (ModOptions.instance.emptyZoneDuringWar)
                {
                    Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                    Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0);
                }

            }

            //zix attempt controller recruit
            if (ModOptions.instance.joypadControls)
            {
                if (Game.IsControlPressed(0, Control.Aim) || Game.IsControlPressed(0, Control.AccurateAim))
                {
                    if (Game.IsControlJustPressed(0, Control.ScriptPadRight))
                    {
                        RecruitGangMember();
                    }

                    if (Game.IsControlJustPressed(0, Control.ScriptPadLeft))
                    {
                        GangManager.instance.CallCarBackup();
                    }

                    if (Game.IsControlJustPressed(0, Control.ScriptPadUp))
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                    }
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (menuScript.curInputType == MenuScript.DesiredInputType.changeKeyBinding)
            {
                if (e.KeyCode != Keys.Enter)
                {
                    ModOptions.instance.SetKey(menuScript.targetKeyBindToChange, e.KeyCode);
                    menuScript.curInputType = MenuScript.DesiredInputType.none;
                    menuScript.OnKeyBindingChanged?.Invoke();
                }
            }
            else
            {

                if (e.KeyCode == ModOptions.instance.openGangMenuKey)
                {
                    //note: numpad keys dont seem to go along well with shift
                    if (e.Modifiers == Keys.None)
                    {
                        menuScript.OpenGangMenu();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        menuScript.OpenContextualRegistrationMenu();
                    }
                    else if (e.Modifiers == Keys.Alt && PotentialSpawnsForWars.showingBlips)
                    {
                        if (PotentialSpawnsForWars.HasNearbyEntry(MindControl.SafePositionNearPlayer))
                        {
                            if (PotentialSpawnsForWars.RemovePositionAndSave(MindControl.SafePositionNearPlayer))
                            {
                                UI.ShowSubtitle("Potential Spawn Removed!");
                            }
                        }
                        else
                        {
                            if (PotentialSpawnsForWars.AddPositionAndSave(MindControl.SafePositionNearPlayer))
                            {
                                UI.ShowSubtitle("Potential Spawn Added!");
                            }
                        }
                    }
                }
                else if (e.KeyCode == ModOptions.instance.openZoneMenuKey)
                {
                    if (e.Modifiers == Keys.None)
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                        menuScript.OpenZoneMenu();
                    }
                    else if (e.Modifiers == Keys.Control)
                    {
                        zoneManagerScript.ChangeBlipDisplay();
                    }

                }
                else if (e.KeyCode == ModOptions.instance.addToGroupKey)
                {
                    if(e.Modifiers == Keys.Shift && ModOptions.instance.loggerLevel >= 3)
                    {
                        PrintDebugInfoOnMember();
                    }
                    
                    RecruitGangMember();
                    
                    
                }
                else if (e.KeyCode == ModOptions.instance.mindControlKey)
                {
                    MindControl.TryBodyChange();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    if (MindControl.HasChangedBody)
                    {
                        MindControl.RespawnIfPossible();
                    }
                }
            }

        }

        /// <summary>
        /// adds a friendly member the player is aiming at to the player's group, or tells a friendly vehicle to behave like a backup vehicle
        /// </summary>
        public void RecruitGangMember()
        {
            RaycastResult hit;
            if (MindControl.CurrentPlayerCharacter.IsInVehicle())
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything,
                    MindControl.CurrentPlayerCharacter.CurrentVehicle);
            }
            else
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything);
            }

            if (hit.HitEntity != null)
            {
                List<Ped> playerGangMembers;

                if (hit.HitEntity.Model.IsVehicle)
                {
                    Vehicle hitVeh = (Vehicle)hit.HitEntity;
                    //only do vehicle-related stuff if we're not inside said vehicle!
                    if (!MindControl.CurrentPlayerCharacter.IsInVehicle(hitVeh))
                    {
                        List<SpawnedDrivingGangMember> playerGangDrivers = SpawnManager.instance.GetSpawnedDriversOfGang(gangManagerScript.PlayerGang);
                        for (int i = 0; i < playerGangDrivers.Count; i++)
                        {
                            if (playerGangDrivers[i].vehicleIAmDriving != null &&
                                (playerGangDrivers[i].vehicleIAmDriving == hitVeh))
                            {
                                //car should now behave as a backup vehicle: come close and drop passengers if player is on foot, follow player if not
                                playerGangDrivers[i].playerAsDest = true;
                                playerGangDrivers[i].deliveringCar = true;
                                playerGangDrivers[i].destination = Math.Vector3.WorldEast; //just something that isn't zero will do to wake the driver up
                                playerGangDrivers[i].Update();
                                UI.Notify("Car told to back you up!");
                                return;
                            }
                        }

                        //we've hit a vehicle that's not marked as a gang vehicle!
                        //maybe it's no longer persistent and refs have already been cleared
                        //in any case, try to find gang members inside it and tell them to leave
                        playerGangMembers = SpawnManager.instance.GetSpawnedPedsOfGang(gangManagerScript.PlayerGang);
                        for (int i = 0; i < playerGangMembers.Count; i++)
                        {
                            if (playerGangMembers[i].IsInVehicle(hitVeh) && playerGangMembers[i].IsAlive)
                            {

                                if (playerGangMembers[i].IsInGroup)
                                {
                                    Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                                    UI.Notify("A member has left your group");
                                }
                                else
                                {
                                    int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);
                                    playerGangMembers[i].Task.ClearAll();
                                    Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                                    if (playerGangMembers[i].IsInGroup)
                                    {
                                        UI.Notify("A member has joined your group");
                                    }
                                    else
                                    {
                                        playerGangMembers[i].Task.LeaveVehicle();
                                    }
                                }
                            }
                        }
                    }


                }
                else if (hit.HitEntity.Model.IsPed)
                {
                    //maybe we're just targeting a ped then?
                    playerGangMembers = SpawnManager.instance.GetSpawnedPedsOfGang(gangManagerScript.PlayerGang);
                    for (int i = 0; i < playerGangMembers.Count; i++)
                    {
                        if (playerGangMembers[i] == hit.HitEntity)
                        {
                            if (playerGangMembers[i].IsInGroup)
                            {
                                Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                                UI.Notify("A member has left your group");
                            }
                            else
                            {
                                int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);
                                playerGangMembers[i].Task.ClearAll();
                                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                                if (playerGangMembers[i].IsInGroup)
                                {
                                    UI.Notify("A member has joined your group");
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// does ui.notify with some info on the target member's data (if the ped we're aiming at is a member)
        /// </summary>
        public void PrintDebugInfoOnMember()
        {
            RaycastResult hit;
            if (MindControl.CurrentPlayerCharacter.IsInVehicle())
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything,
                    MindControl.CurrentPlayerCharacter.CurrentVehicle);
            }
            else
            {
                hit = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 250, IntersectOptions.Everything);
            }

            if (hit.HitEntity != null)
            {

                if (hit.HitEntity.Model.IsPed)
                {
                    //maybe we're just targeting a ped then?
                    SpawnedGangMember pedAI = SpawnManager.instance.GetTargetMemberAI((Ped) hit.HitEntity);
                    if(pedAI != null)
                    {
                        UI.Notify(pedAI.ToString());
                    }
                }
            }
        }


        private void OnAbort(object sender, EventArgs e)
        {
            UI.Notify("Gang and Turf mod: removing blips. If you didn't press Insert, please check your log and report any errors.");
            zoneManagerScript.ChangeBlipDisplay(ZoneManager.ZoneBlipDisplay.none);
            if (MindControl.HasChangedBody)
            {
                MindControl.RestorePlayerBody();
            }
            SpawnManager.instance.RemoveAllMembers();
            SpawnManager.instance.RemoveAllDeadBodies();

            PotentialSpawnsForWars.ToggleBlips(false);

            Logger.Log("mod aborted!", 2);

        }
    }
}
