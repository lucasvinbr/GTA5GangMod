using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// the script is responsble for ticking and detecting input for the more sensitive scripts.
    /// some, like the gang war manager, are still ticking on their own.
    /// this one exists to make sure these ones start running in the correct order
    /// </summary>
    class ModCore : Script
    {
        public GangManager gangManagerScript;
        public MenuScript menuScript;
        public ZoneManager zoneManagerScript;


        public ModCore()
        {
            zoneManagerScript = new ZoneManager();
            gangManagerScript = new GangManager();
            menuScript = new MenuScript();

            this.KeyUp += onKeyUp;
            this.Tick += OnTick;
        }

        void OnTick(object sender, EventArgs e)
        {
            gangManagerScript.Tick();
            menuScript.Tick();

            //zix attempt controller recruit
            if (ModOptions.instance.joypadControls)
            {
                if (Game.IsControlPressed(0, GTA.Control.Aim) || Game.IsControlPressed(0, GTA.Control.AccurateAim))
                {
                    if (Game.IsControlJustPressed(0, GTA.Control.ScriptPadRight))
                    {
                        recruitGangMember();
                    }

                    if (Game.IsControlJustPressed(0, GTA.Control.ScriptPadLeft))
                    {
                        menuScript.doCallBackup(false);
                    }

                    if (Game.IsControlJustPressed(0, GTA.Control.ScriptPadUp))
                    {
                        zoneManagerScript.OutputCurrentZoneInfo();
                    }
                }
            }
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            if(menuScript.curInputType == MenuScript.desiredInputType.changeKeyBinding)
            {
                if(e.KeyCode != Keys.Enter)
                {
                    ModOptions.instance.SetKey(menuScript.targetKeyBindToChange, e.KeyCode);
                    menuScript.curInputType = MenuScript.desiredInputType.none;
                    menuScript.RefreshKeyBindings();
                }
            }
            else
            {
                if (e.KeyCode == ModOptions.instance.openGangMenuKey)
                {
                    //numpad keys dont seem to go along well with shift
                    if (e.Modifiers == Keys.None)
                    {
                        menuScript.OpenGangMenu();
                    }
                    else if (e.Modifiers == Keys.Shift)
                    {
                        menuScript.OpenContextualRegistrationMenu();
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
                    recruitGangMember();

                }
                else if (e.KeyCode == ModOptions.instance.mindControlKey)
                {
                    gangManagerScript.TryBodyChange();
                }
                else if (e.KeyCode == Keys.Space)
                {
                    
                    if (gangManagerScript.hasChangedBody)
                    {
                        gangManagerScript.RespawnIfPossible();
                    }
                }
            }
            
        }

        public void recruitGangMember()
        {
            List<Ped> playerGangMembers = gangManagerScript.GetSpawnedMembersOfGang(gangManagerScript.GetPlayerGang());
            for (int i = 0; i < playerGangMembers.Count; i++)
            {
                if (Game.Player.IsTargetting(playerGangMembers[i]))
                {
                    int playergrp = Function.Call<int>(Hash.GET_PLAYER_GROUP, Game.Player);

                    if (playerGangMembers[i].IsInGroup)
                    {
                        Function.Call(Hash.REMOVE_PED_FROM_GROUP, playerGangMembers[i]);
                        UI.Notify("A member has left your group");
                    }
                    else
                    {
                        playerGangMembers[i].Task.ClearAll();
                        Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, playerGangMembers[i], playergrp);
                        UI.Notify("A member has joined your group");
                    }
                    break;
                }
            }
        }
        
    }
}
