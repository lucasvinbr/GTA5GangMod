
using LemonUI.Menus;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for creating and editing custom zones
    /// </summary>
    public class CustomZonesSubMenu : NativeMenu
    {
        public CustomZonesSubMenu() : base("Gang and Turf Mod", Localization.GetTextByKey("AAAA", "Custom Zones Menu"))
        {
        }

        private bool editingZoneName = false;



        //options:
        //create new zone here
        //edit zone name
        //edit zone radius

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
            NativeItem createZoneBtn = new NativeItem(Localization.GetTextByKey("customzones_menu_button_create_zone", "Create New Custom Zone Here"),
                Localization.GetTextByKey("customzones_menu_button_desc_create_zone", "Opens the text input for a new Custom Zone. After a valid and unique name is entered, a new zone will be created in this position."));

            NativeItem renameZoneBtn = new NativeItem(Localization.GetTextByKey("customzones_menu_button_edit_zone_name", "Edit this Custom Zone's Name"),
                Localization.GetTextByKey("customzones_menu_button_desc_edit_zone_name", "If inside a custom zone, opens the input for setting a new name."));

            NativeSliderItem radiusEditor = new NativeSliderItem
                (Localization.GetTextByKey("customzones_menu_button_edit_zone_radius", "Edit Zone Radius"),
                Localization.GetTextByKey("customzones_menu_button_desc_edit_zone_radius", "Edit this zone's size radius."),
                (int) CustomTurfZone.MAX_ZONE_RADIUS, (int) CustomTurfZone.DEFAULT_ZONE_RADIUS);

            createZoneBtn.Activated += CreateZoneBtn_Activated;
            renameZoneBtn.Activated += RenameZoneBtn_Activated;
            radiusEditor.ValueChanged += RadiusEditor_ValueChanged;

            MenuScript.instance.OnInputFieldDone += (desiredInputType, typedText) =>
            {
                if (desiredInputType == MenuScript.DesiredInputType.enterCustomZoneName)
                {
                    if (editingZoneName)
                    {
                        TryEditZoneName(typedText);
                    }
                    else
                    {
                        TryCreateCustomZone(typedText);
                    }

                    editingZoneName = false;
                    Visible = true;
                }
            };


            Add(createZoneBtn);
            Add(radiusEditor);
            Add(renameZoneBtn);

            
        }

        private void RadiusEditor_ValueChanged(object sender, System.EventArgs e)
        {
            var slider = (NativeSliderItem)sender;
            float currentRadius = slider.Value;

            if(currentRadius < CustomTurfZone.MIN_ZONE_RADIUS)
            {
                slider.Value = (int) CustomTurfZone.MIN_ZONE_RADIUS;
                return;
            }

            CustomTurfZone zone = GetLocalCustomZone();
            if (zone != null)
            {
                zone.areaRadius = currentRadius;
                zone.RemoveBlip();
                zone.CreateAttachedBlip(true);

                ZoneManager.instance.SaveZoneData();

                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_custom_zone_radius_changed", "Radius Changed!"));
            }
            else
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_custom_zone", "You are not inside a custom zone."));
            }


        }

        private void RenameZoneBtn_Activated(object sender, System.EventArgs e)
        {
            CustomTurfZone zone = GetLocalCustomZone();
            if (zone != null)
            {
                Visible = !Visible;
                editingZoneName = true;
                MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterCustomZoneName, "FMMC_KEY_TIP12N", zone.zoneName);
            }
            else
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_custom_zone", "You are not inside a custom zone."));
            }
        }

        private void CreateZoneBtn_Activated(object sender, System.EventArgs e)
        {
            Visible = !Visible;
            MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterCustomZoneName, "FMMC_KEY_TIP12N", Localization.GetTextByKey("customzones_menu_input_placeholder_new_zone_name", "New Zone Name"));
        }


        /// <summary>
        /// returns the custom zone we're in, or null if it's not custom... or no zone at all
        /// </summary>
        /// <returns></returns>
        private CustomTurfZone GetLocalCustomZone()
        {
            TurfZone zone = ZoneManager.instance.GetCurrentTurfZone();

            if (zone != null && zone.GetType() == typeof(CustomTurfZone))
            {
                return (CustomTurfZone)zone;
            }
            else return null;
        }


        private void TryCreateCustomZone(string newName)
        {
            if (newName == "zone" || newName == "")
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_provided_name_cannot_be_used", "That name cannot be used!"));
            }

            if (ZoneManager.instance.DoesZoneWithNameExist(newName))
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_with_provided_name_already_exists", "A zone with that name already exists."));
            }
            else
            {
                CustomTurfZone newZone = new CustomTurfZone(newName)
                {
                    zoneBlipPosition = MindControl.CurrentPlayerCharacter.Position
                };

                ZoneManager.instance.UpdateZoneData(newZone);

                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_custom_zone_created", "Zone Created!"));
            }
        }

        private void TryEditZoneName(string newName)
        {
            if (newName == "zone" || newName == "")
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_provided_name_cannot_be_used", "That name cannot be used!"));
            }

            CustomTurfZone zone = GetLocalCustomZone();
            if (zone != null)
            {
                if (ZoneManager.instance.DoesZoneWithNameExist(newName))
                {
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_zone_with_provided_name_already_exists", "A zone with that name already exists."));
                }
                else
                {
                    zone.zoneName = newName;
                    UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_custom_zone_renamed", "Zone renamed!"));
                    ZoneManager.instance.SaveZoneData();
                }
            }
            else
            {
                UI.Screen.ShowSubtitle(Localization.GetTextByKey("subtitle_not_inside_custom_zone", "You are not inside a custom zone."));
            }
        }

    }
}
