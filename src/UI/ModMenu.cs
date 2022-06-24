using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// UIMenu, but with some methods for easier setting up of options for this mod's stuff
    /// </summary>
    public class ModMenu: UIMenu
    {
        public ModMenu() : base("Gang and Turf Mod", "")
        {
        }

        public ModMenu(string menuTitle) : base("Gang and Turf Mod", menuTitle)
        {
        }

        /// <summary>
        /// adds a toggle UI item for a boolean modOption to this menu
        /// </summary>
        /// <param name="modOptionName"></param>
        /// <param name="text"></param>
        /// <param name="description"></param>
        /// <param name="extraActionOnChanged"></param>
        /// <returns></returns>
        public UIMenuCheckboxItem AddModOptionToggle(string modOptionName, string text, string description, Action<bool> extraActionOnChanged = null)
        {
            bool valueOnUICreation = (bool) typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
            UIMenuCheckboxItem newToggle = new UIMenuCheckboxItem(text, valueOnUICreation, description);

            AddItem(newToggle);
            OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == newToggle)
                {
                    typeof(ModOptions).GetField(modOptionName).SetValue(ModOptions.instance, checked_);
                    ModOptions.instance.SaveOptions(false);
                    extraActionOnChanged?.Invoke(checked_);
                }

            };

            ModOptions.OnModOptionsReloaded += () =>
            {
                bool valueAfterReload = (bool)typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
                newToggle.Checked = valueAfterReload;
            };

            return newToggle;
        }
    }
}
