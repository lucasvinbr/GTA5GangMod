using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// generic menu for selecting an AI gang and then doing something with it
    /// </summary>
    public class PickAiGangMenu : ModMenu
    {
        public PickAiGangMenu(ObjectPool menuPool) : base("pick_ai_gang", "Pick Ai Gang Menu")
        {
            menuPool.Add(this);
        }

        private Action<Gang> OnGangPicked;

        private NativeMenu previousMenu;

        public void Open(NativeMenu previousMenu, string menuSubtitle, Action<Gang> onGangPicked)
        {
            Name = menuSubtitle;
            Clear();
            AddGangsToMenu();
            OnGangPicked = onGangPicked;
            this.previousMenu = previousMenu;
            Visible = true;
        }

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        protected override void Setup()
        {
            ItemActivated += (sender, args) =>
            {
                Gang targetGang = GangManager.instance.GetGangByName(args.Item.Title);

                if (targetGang != null)
                {
                    OnGangPicked?.Invoke(targetGang);
                }
                else
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_selected_gang_not_found", "The gang selected could not be found! Has it been wiped out or renamed?"));
                }
            };

            Closed += (sender, args) =>
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
                    Add(new NativeItem(gang.name));
                }
            }

            
        }

        protected override void RecreateItems()
        {
            Clear();
            AddGangsToMenu();
        }
    }
}
