
using LemonUI.Menus;
using static GTA.GangAndTurfMod.MenuScript;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting the mod's key bindings
    /// </summary>
    public class KeyBindingsSubMenu : ModMenu
    {
        public KeyBindingsSubMenu() : base("key_bindings", "Key Bindings")
        {
        }


        private NativeItem openGangMenuBtn, openZoneMenuBtn, mindControlBtn, addToGroupBtn;

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        protected override void Setup()
        {
            base.Setup();

            ItemActivated += (sender, eventData) =>
            {
                var pickedItem = eventData.Item;
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_press_new_keybinding_for_command", "Press the new key for this command."));
                instance.curInputType = DesiredInputType.changeKeyBinding;

                if (pickedItem == openGangMenuBtn)
                {
                    instance.targetKeyBindToChange = ChangeableKeyBinding.GangMenuBtn;
                }
                if (pickedItem == openZoneMenuBtn)
                {
                    instance.targetKeyBindToChange = ChangeableKeyBinding.ZoneMenuBtn;
                }
                if (pickedItem == addToGroupBtn)
                {
                    instance.targetKeyBindToChange = ChangeableKeyBinding.AddGroupBtn;
                }
                if (pickedItem == mindControlBtn)
                {
                    instance.targetKeyBindToChange = ChangeableKeyBinding.MindControlBtn;
                }
            };

            instance.OnKeyBindingChanged += RefreshKeyBindings;
        }

        public void RefreshKeyBindings()
        {
            openGangMenuBtn.Title = Localization.GetTextByKey("menu_button_set_keybinding_gang_control", "Gang Control Key") + " - " + ModOptions.instance.openGangMenuKey.ToString();
            openZoneMenuBtn.Title = Localization.GetTextByKey("menu_button_set_keybinding_zone_control", "Zone Control Key") + " - " + ModOptions.instance.openZoneMenuKey.ToString();
            addToGroupBtn.Title = Localization.GetTextByKey("menu_button_set_keybinding_add_remove_member_from_group", "Add or Remove Member from Group") + " - " + ModOptions.instance.addToGroupKey.ToString();
            mindControlBtn.Title = Localization.GetTextByKey("menu_button_set_keybinding_mind_control", "Take Control of Member") + " - " + ModOptions.instance.mindControlKey.ToString();
        }

        protected override void RecreateItems()
        {
            Clear();

            openGangMenuBtn = new NativeItem(Localization.GetTextByKey("menu_button_set_keybinding_gang_control", "Gang Control Key") + " - " + ModOptions.instance.openGangMenuKey.ToString(), Localization.GetTextByKey("menu_button_set_keybinding_gang_control_desc", "The key used to open the Gang/Mod Menu. Used with shift to open the Member Registration Menu. Default is B."));
            openZoneMenuBtn = new NativeItem(Localization.GetTextByKey("menu_button_set_keybinding_zone_control", "Zone Control Key") + " - " + ModOptions.instance.openZoneMenuKey.ToString(), Localization.GetTextByKey("menu_button_set_keybinding_zone_control_desc", "The key used to check the current zone's name and ownership. Used with shift to open the Zone Menu and with control to toggle zone blip display modes. Default is N."));
            addToGroupBtn = new NativeItem(Localization.GetTextByKey("menu_button_set_keybinding_add_remove_member_from_group", "Add or Remove Member from Group") + " - " + ModOptions.instance.addToGroupKey.ToString(), Localization.GetTextByKey("menu_button_set_keybinding_add_remove_member_from_group_desc", "The key used to add/remove the targeted friendly gang member to/from your group. Members of your group will follow you. Default is H."));
            mindControlBtn = new NativeItem(Localization.GetTextByKey("menu_button_set_keybinding_mind_control", "Take Control of Member") + " - " + ModOptions.instance.mindControlKey.ToString(), Localization.GetTextByKey("menu_button_set_keybinding_mind_control_desc", "The key used to take control of the targeted friendly gang member. Pressing this key while already in control of a member and not aiming at another will restore protagonist control. Default is J."));
            Add(openGangMenuBtn);
            Add(openZoneMenuBtn);
            Add(addToGroupBtn);
            Add(mindControlBtn);
        }
    }
}
