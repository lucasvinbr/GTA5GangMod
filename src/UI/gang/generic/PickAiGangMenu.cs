using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// generic menu for selecting an AI gang and then doing something with it
    /// </summary>
    public class PickAiGangMenu : UIMenu
    {
        public PickAiGangMenu(MenuPool menuPool) : base("Gang and Turf Mod", "Pick Ai Gang Menu")
        {
            menuPool.Add(this);

            Setup();
        }

        private Action<Gang> OnGangPicked;

        private UIMenu previousMenu;

        public void Open(UIMenu previousMenu, string menuSubtitle, Action<Gang> onGangPicked)
        {
            Subtitle.Caption = menuSubtitle;
            Clear();
            AddGangsToMenu();
            OnGangPicked = onGangPicked;
            this.previousMenu = previousMenu;
            Visible = true;
        }

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            OnItemSelect += (sender, item, checked_) =>
            {
                Gang targetGang = GangManager.instance.GetGangByName(item.Text);

                if (targetGang != null)
                {
                    OnGangPicked?.Invoke(targetGang);
                }
                else
                {
                    UI.ShowSubtitle("The gang selected could not be found! Has it been wiped out or renamed?");
                }
            };

            OnMenuClose += (sender) =>
            {
                if (previousMenu != null)
                {
                    previousMenu.Visible = true;
                    previousMenu = null;
                }
            };
        }

        private void AddGangsToMenu()
        {

            foreach(Gang gang in GangManager.instance.gangData.gangs)
            {
                if (!gang.isPlayerOwned)
                {
                    AddItem(new UIMenuItem(gang.name));
                }
            }

            RefreshIndex();
        }

    }
}
