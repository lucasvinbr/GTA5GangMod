
using static GTA.GangAndTurfMod.MenuScript;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting the mod's key bindings
    /// </summary>
    public class KeyBindingsSubMenu : NativeMenu
    {
        public KeyBindingsSubMenu() : base("Gang and Turf Mod", "Key Bindings")
        {
            Setup();
        }


        private NativeItem openGangMenuBtn, openZoneMenuBtn, mindControlBtn, addToGroupBtn;

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            openGangMenuBtn = new NativeItem("Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString(), "The key used to open the Gang/Mod Menu. Used with shift to open the Member Registration Menu. Default is B.");
            openZoneMenuBtn = new NativeItem("Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString(), "The key used to check the current zone's name and ownership. Used with shift to open the Zone Menu and with control to toggle zone blip display modes. Default is N.");
            addToGroupBtn = new NativeItem("Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString(), "The key used to add/remove the targeted friendly gang member to/from your group. Members of your group will follow you. Default is H.");
            mindControlBtn = new NativeItem("Take Control of Member - " + ModOptions.instance.mindControlKey.ToString(), "The key used to take control of the targeted friendly gang member. Pressing this key while already in control of a member will restore protagonist control. Default is J.");
            Add(openGangMenuBtn);
            Add(openZoneMenuBtn);
            Add(addToGroupBtn);
            Add(mindControlBtn);
            

            OnItemSelect += (sender, item, index) =>
            {
                UI.Screen.ShowSubtitle("Press the new key for this command.");
                instance.curInputType = DesiredInputType.changeKeyBinding;

                if (item == openGangMenuBtn)
                {
                    MenuScript.instance.targetKeyBindToChange = ChangeableKeyBinding.GangMenuBtn;
                }
                if (item == openZoneMenuBtn)
                {
                    MenuScript.instance.targetKeyBindToChange = ChangeableKeyBinding.ZoneMenuBtn;
                }
                if (item == addToGroupBtn)
                {
                    MenuScript.instance.targetKeyBindToChange = ChangeableKeyBinding.AddGroupBtn;
                }
                if (item == mindControlBtn)
                {
                    MenuScript.instance.targetKeyBindToChange = ChangeableKeyBinding.MindControlBtn;
                }
            };

            MenuScript.instance.OnKeyBindingChanged += RefreshKeyBindings;
        }

        public void RefreshKeyBindings()
        {
            openGangMenuBtn.Text = "Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString();
            openZoneMenuBtn.Text = "Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString();
            addToGroupBtn.Text = "Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString();
            mindControlBtn.Text = "Take Control of Member - " + ModOptions.instance.mindControlKey.ToString();
        }

    }
}
