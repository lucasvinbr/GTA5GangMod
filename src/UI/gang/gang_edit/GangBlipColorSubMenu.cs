﻿using NativeUI;
using System.Collections.Generic;
using System.Linq;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting gang blips' color
    /// </summary>
    public class GangBlipColorSubMenu : UIMenu
    {
        public GangBlipColorSubMenu() : base(
            Localization.GetTextByKey("menu_title_mod_name", "Gang and Turf Mod"),
            Localization.GetTextByKey("menu_title_gang_blip_color","Gang Blip Color"))
        {
            Setup();
        }

        private int playerGangOriginalBlipColor = 0;

        private readonly Dictionary<string, int> blipColorEntries = new Dictionary<string, int>
        {
            {"white", 0 },
            {"white-2", 4 },
            {"white snowy", 13 },
            {"red", 1 },
            {"red-2", 6 },
            {"dark red", 76 },
            {"green", 2 },
            {"green-2", 11 },
            {"dark green", 25 },
            {"darker green", 52 },
            {"turquoise", 15 },
            {"blue", 3 },
            {"light blue", 18 },
            {"dark blue", 38 },
            {"darker blue", 54 },
            {"purple", 7 },
            {"purple-2", 19 },
            {"dark purple", 27 },
            {"dark purple-2", 83 },
            {"very dark purple", 58 },
            {"orange", 17 },
            {"orange-2", 51 },
            {"orange-3", 44 },
            {"gray", 20 },
            {"light gray", 39 },
            {"brown", 21 },
            {"beige", 56 },
            {"pink", 23 },
            {"pink-2", 8 },
            {"smooth pink", 41 },
            {"strong pink", 48 },
            {"black", 40 }, //as close as it gets
            {"yellow", 66 },
            {"gold-ish", 28 },
            {"yellow-2", 46 },
            {"light yellow", 33 },
        };

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            string[] blipColorNamesArray = blipColorEntries.Keys.ToArray();
            int[] colorCodesArray = blipColorEntries.Values.ToArray();

            for (int i = 0; i < colorCodesArray.Length; i++)
            {
                AddItem(new UIMenuItem(Localization.GetTextByKey("blip_color_name_" + colorCodesArray[i],blipColorNamesArray[i]), Localization.GetTextByKey("menu_button_desc_gang_blip_color", "The color change can be seen immediately on turf blips. Click or press enter after selecting a color to save the color change.")));
            }

            RefreshIndex();

            OnIndexChange += (sender, index) =>
            {
                GangManager.instance.PlayerGang.blipColor = colorCodesArray[index];
                ZoneManager.instance.RefreshZoneBlips();
            };

            OnMenuOpen += StoreOriginalBlipColor;

            OnMenuClose += GangBlipColorSubMenu_OnMenuClose;

            OnItemSelect += (sender, item, checked_) =>
            {
                for (int i = 0; i < blipColorNamesArray.Length; i++)
                {
                    if (item.Text == blipColorNamesArray[i])
                    {
                        GangManager.instance.PlayerGang.blipColor = colorCodesArray[i];
                        playerGangOriginalBlipColor = colorCodesArray[i];
                        GangManager.instance.SaveGangData(false);
                        UI.ShowSubtitle(Localization.GetTextByKey("subtitle_gang_blip_color_changed", "Gang blip color changed!"));
                        break;
                    }
                }

            };
        }

        private void GangBlipColorSubMenu_OnMenuClose(UIMenu sender)
        {
            GangManager.instance.PlayerGang.blipColor = playerGangOriginalBlipColor;
            ZoneManager.instance.RefreshZoneBlips();
        }

        private void StoreOriginalBlipColor(UIMenu sender)
        {
            playerGangOriginalBlipColor = GangManager.instance.PlayerGang.blipColor;
        }
    }
}
