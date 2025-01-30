using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// generic menu for selecting a gang and then doing something with it
    /// </summary>
    public class PickAGangMenu : ModMenu
    {
        public PickAGangMenu(ObjectPool menuPool) : base("pick_a_gang", "Pick a Gang Menu")
        {
            menuPool.Add(this);
        }

        private Action<Gang> OnGangPicked;

        private List<Gang> gangOptions;

        private NativeMenu previousMenu;

        public void Open(NativeMenu previousMenu, string menuSubtitle, List<Gang> options, Action<Gang> onGangPicked)
        {
            Name = menuSubtitle;
            Clear();
            gangOptions = options;
            AddGangsToMenu(options);
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

        private void AddGangsToMenu(List<Gang> options)
        {

            foreach(Gang gang in options)
            {
                Add(new NativeItem(gang.name));
            }

        }

        protected override void RecreateItems()
        {
            Clear();
            AddGangsToMenu(gangOptions);
        }
    }
}
