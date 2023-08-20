using GTA.Native;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace GTA.GangAndTurfMod
{
    public class ModOptionCheckBox : NativeCheckboxItem
    {
        public string modOptionName;
        public Action<bool> extraActionOnChanged;

        public ModOptionCheckBox(string modOptionName, string title, string description, bool initialValue, Action<bool> extraActionOnChanged = null) : base(title, description, initialValue)
        {
            this.modOptionName = modOptionName;
            this.extraActionOnChanged = extraActionOnChanged;
        }
    }

    /// <summary>
    /// NativeMenu, but with some methods for easier setting up of localized options for this mod's stuff
    /// </summary>
    public class ModMenu: NativeMenu
    {

        public ModMenu() : base("Gang and Turf Mod", "")
        {
        }

        public ModMenu(string menuTitleLocaleKeySuffix, string fallbackMenuTitle) : 
            base("Gang and Turf Mod", Localization.GetTextByKey("mod_menu_title_" + menuTitleLocaleKeySuffix, fallbackMenuTitle))
        {
            Name = Localization.GetTextByKey("mod_menu_title_" + menuTitleLocaleKeySuffix, fallbackMenuTitle);
            Localization.OnLanguageChanged += () => 
                Name = Localization.GetTextByKey("mod_menu_title_" + menuTitleLocaleKeySuffix, fallbackMenuTitle);
        }

        /// <summary>
        /// adds a toggle UI item for a boolean modOption to this menu
        /// </summary>
        /// <param name="modOptionName"></param>
        /// <param name="fallbackText"></param>
        /// <param name="fallbackDescription"></param>
        /// <param name="extraActionOnChanged"></param>
        /// <returns></returns>
        public NativeCheckboxItem AddModOptionToggle(string modOptionName, string fallbackText, string fallbackDescription, Action<bool> extraActionOnChanged = null)
        {
            bool valueOnUICreation = (bool) typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
            NativeCheckboxItem newToggle = new ModOptionCheckBox
                (modOptionName,
                Localization.GetTextByKey(string.Concat("modoption_", modOptionName, "_name"), fallbackText),
                Localization.GetTextByKey(string.Concat("modoption_", modOptionName, "_desc"), fallbackDescription),
                valueOnUICreation,
                extraActionOnChanged);

            Add(newToggle);

            newToggle.CheckboxChanged += NewToggle_CheckboxChanged;

            ModOptions.OnModOptionsReloaded += () =>
            {
                bool valueAfterReload = (bool)typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
                newToggle.Checked = valueAfterReload;
            };

            Localization.OnLanguageChanged += () =>
            {
                newToggle.Title = Localization.GetTextByKey(string.Concat("modoption_", modOptionName, "_name"), fallbackText);
                newToggle.Description = Localization.GetTextByKey(string.Concat("modoption_", modOptionName, "_desc"), fallbackDescription);
            };

            return newToggle;
        }

        private void NewToggle_CheckboxChanged(object sender, EventArgs e)
        {
            var senderCheckBox = (ModOptionCheckBox)sender;
            typeof(ModOptions).GetField(senderCheckBox.modOptionName).SetValue(ModOptions.instance, senderCheckBox.Checked);
            ModOptions.instance.SaveOptions(false);
            senderCheckBox.extraActionOnChanged?.Invoke(senderCheckBox.Checked);
        }
    }
}
