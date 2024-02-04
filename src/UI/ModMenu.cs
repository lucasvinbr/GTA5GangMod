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

        public void RefreshDisplay()
        {
            bool valueAfterReload = (bool)typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
            Checked = valueAfterReload;
        }

        /// <summary>
        /// this should be run while clearing the menu, to make sure we don't end up with event hooks pointing to invalid ui elements
        /// </summary>
        public void DetachModOptionReloadEvent()
        {
            ModOptions.OnModOptionsReloaded -= RefreshDisplay;
        }
    }

    /// <summary>
    /// NativeMenu, but with some methods for easier setting up of localized options for this mod's stuff
    /// </summary>
    public abstract class ModMenu: NativeMenu
    {

        protected readonly List<ModOptionCheckBox> modOptionCheckBoxes = new List<ModOptionCheckBox>();
        protected bool shouldRebuildItemsWhenShown = false;

        public ModMenu() : base("Gang and Turf Mod", "")
        {
            Setup();
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
        /// <param name="titleText"></param>
        /// <param name="descriptionText"></param>
        /// <param name="extraActionOnChanged"></param>
        /// <returns></returns>
        public NativeCheckboxItem AddModOptionToggle(string modOptionName, string titleText, string descriptionText, Action<bool> extraActionOnChanged = null)
        {
            bool valueOnUICreation = (bool) typeof(ModOptions).GetField(modOptionName).GetValue(ModOptions.instance);
            var newToggle = new ModOptionCheckBox
                (modOptionName,
                titleText,
                descriptionText,
                valueOnUICreation,
                extraActionOnChanged);

            Add(newToggle);

            newToggle.CheckboxChanged += ModOptionToggle_CheckboxChanged;

            ModOptions.OnModOptionsReloaded += newToggle.RefreshDisplay;

            modOptionCheckBoxes.Add(newToggle);

            return newToggle;
        }

        private void ModOptionToggle_CheckboxChanged(object sender, EventArgs _)
        {
            var senderCheckBox = (ModOptionCheckBox)sender;
            typeof(ModOptions).GetField(senderCheckBox.modOptionName).SetValue(ModOptions.instance, senderCheckBox.Checked);
            ModOptions.instance.SaveOptions(false);
            senderCheckBox.extraActionOnChanged?.Invoke(senderCheckBox.Checked);
        }

        protected void DetachEventsForModOptionEntries()
        {
            foreach(var modoptionToggle in modOptionCheckBoxes)
            {
                modoptionToggle.DetachModOptionReloadEvent();
            }
        }

        /// <summary>
        /// create buttons, their events etc. Should only be run once, usually.
        /// Base: add language changed hooks + recreate items
        /// </summary>
        protected virtual void Setup()
        {
            Localization.OnLanguageChanged += OnLocalesChanged;
            Shown += RebuildItemsIfNeeded;

            RecreateItems();
        }

        protected virtual void OnLocalesChanged()
        {
            shouldRebuildItemsWhenShown = true;
            RebuildItemsIfNeeded(this, null);
        }

        protected virtual void RebuildItemsIfNeeded(object sender, EventArgs _)
        {
            if (shouldRebuildItemsWhenShown)
            {
                shouldRebuildItemsWhenShown = false;
                RecreateItems();
            }
        }

        protected abstract void RecreateItems();

    }
}
