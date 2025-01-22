using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// generic menu for a yes/no procedure, for confirming destructive actions etc
    /// </summary>
    public class YesNoConfirmationMenu : ModMenu
    {
        public YesNoConfirmationMenu(ObjectPool menuPool) : base("yes_no", "Confirmation Menu")
        {
            menuPool.Add(this);
        }

        private Action OnYesClicked, OnNoClicked;

        private NativeMenu previousMenu;

        public void Open(NativeMenu previousMenu, string menuSubtitle, Action onYesPicked, Action onNoPicked)
        {
            Name = menuSubtitle;
            Clear();
            CreateButtons();
            OnYesClicked = onYesPicked;
            OnNoClicked = onNoPicked;
            this.previousMenu = previousMenu;
            Visible = true;
        }

        private void CreateButtons()
        {
            NativeItem yesButton = new NativeItem(Localization.GetTextByKey("button_confirmation_yes", "Yes"));
            Add(yesButton);

            yesButton.Activated += (sender, args) =>
            {
                OnYesClicked?.Invoke();
                Visible = false;
            };

            NativeItem noButton = new NativeItem(Localization.GetTextByKey("button_confirmation_no", "No"));
            Add(noButton);

            noButton.Activated += (sender, args) =>
            {
                OnNoClicked?.Invoke();
                Visible = false;
            };
        }

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        protected override void Setup()
        {
            Closed += (sender, args) =>
            {
                if (previousMenu != null)
                {
                    previousMenu.Visible = true;
                    previousMenu = null;
                }
            };
        }

        protected override void RecreateItems()
        {
            Clear();
            CreateButtons();
        }
    }
}
