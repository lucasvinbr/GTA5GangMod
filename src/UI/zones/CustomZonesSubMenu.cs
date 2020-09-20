using NativeUI;
using static NativeUI.UIMenuDynamicListItem;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for creating and editing custom zones
    /// </summary>
    public class CustomZonesSubMenu : UIMenu
    {
        public CustomZonesSubMenu() : base("Gang and Turf Mod", "Custom Zones Menu")
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
            UIMenuItem createZoneBtn = new UIMenuItem("Create New Custom Zone Here",
                "Opens the text input for a new Custom Zone. After a valid and unique name is entered, a new zone will be created in this position.");

            UIMenuItem renameZoneBtn = new UIMenuItem("Edit this Custom Zone's Name",
                "If inside a custom zone, opens the input for setting a new name.");

            UIMenuDynamicListItem radiusEditor = new UIMenuDynamicListItem
                ("Edit Zone Radius", "Edit this zone's size radius.",
                CustomTurfZone.DEFAULT_ZONE_RADIUS.ToString(), ChangeRadiusEvent);


            OnItemSelect += (sender, selectedItem, index) =>
            {
                //TODO check if there's a better menu code for this
                if (selectedItem == createZoneBtn)
                {
                    Visible = !Visible;
                    MenuScript.instance.OpenInputField(MenuScript.DesiredInputType.enterCustomZoneName, "FMMC_KEY_TIP12N", "New Zone Name");
                }
                else if (selectedItem == renameZoneBtn)
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
                        UI.ShowSubtitle("You are not inside a custom zone.");
                    }
                }
            };

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

            string ChangeRadiusEvent(UIMenuDynamicListItem sender, ChangeDirection direction)
            {
                float currentRadius = float.Parse(sender.CurrentListItem);

                CustomTurfZone zone = GetLocalCustomZone();
                if (zone != null)
                {
                    currentRadius = direction == ChangeDirection.Right ?
                    currentRadius + 10 :
                    RandoMath.Max(currentRadius - 10, CustomTurfZone.MIN_ZONE_RADIUS);

                    zone.areaRadius = currentRadius;
                    zone.RemoveBlip();
                    zone.CreateAttachedBlip(true);

                    ZoneManager.instance.SaveZoneData();

                    UI.ShowSubtitle("Radius Changed!");
                }
                else
                {
                    UI.ShowSubtitle("You are not inside a custom zone.");
                }



                return currentRadius.ToString();
            }

            AddItem(createZoneBtn);
            AddItem(radiusEditor);
            AddItem(renameZoneBtn);

            RefreshIndex();
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
                UI.ShowSubtitle("That name cannot be used!");
            }

            if (ZoneManager.instance.DoesZoneWithNameExist(newName))
            {
                UI.ShowSubtitle("A zone with that name already exists.");
            }
            else
            {
                CustomTurfZone newZone = new CustomTurfZone(newName)
                {
                    zoneBlipPosition = MindControl.CurrentPlayerCharacter.Position
                };

                ZoneManager.instance.UpdateZoneData(newZone);

                UI.ShowSubtitle("Zone Created!");
            }
        }

        private void TryEditZoneName(string newName)
        {
            if (newName == "zone" || newName == "")
            {
                UI.ShowSubtitle("That name cannot be used!");
            }

            CustomTurfZone zone = GetLocalCustomZone();
            if (zone != null)
            {
                if (ZoneManager.instance.DoesZoneWithNameExist(newName))
                {
                    UI.ShowSubtitle("A zone with that name already exists.");
                }
                else
                {
                    zone.zoneName = newName;
                    UI.ShowSubtitle("Zone renamed!");
                    ZoneManager.instance.SaveZoneData();
                }
            }
            else
            {
                UI.ShowSubtitle("You are not inside a custom zone.");
            }
        }

    }
}
