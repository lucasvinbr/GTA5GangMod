using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// a key point in a gang war zone.
    /// A gang owns this point if they have members near it.
    /// The gang that controls less points in a war will start losing reinforcements
    /// </summary>
    public class WarControlPoint
    {

        private GangWar warUsingThisPoint;

        public Vector3 position;

        private Blip myBlip;

        public Gang ownerGang;

        /// <summary>
        /// after being captured, the control point must wait one "capture check" before being available as a spawn point
        /// </summary>
        public bool onCaptureCooldown = false;

        /// <summary>
        /// if the blip is being displayed, refreshes its size and color
        /// </summary>
        public virtual void UpdateBlipAppearance()
        {
            if (myBlip != null)
            {
                if (ownerGang == null)
                {
                    myBlip.Sprite = BlipSprite.Bunker;
                    myBlip.Color = BlipColor.White;
                }
                else
                {
                    myBlip.Sprite = BlipSprite.AdversaryBunker;
                    Function.Call(Hash.SET_BLIP_COLOUR, myBlip, ownerGang.blipColor);

                    if (ownerGang.isPlayerOwned)
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, myBlip, 0f, 255, 0f);
                    }
                    else
                    {
                        Function.Call(Hash.SET_BLIP_SECONDARY_COLOUR, myBlip, 255, 0f, 0f);
                    }

                }

                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                if (ownerGang != null)
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("War Control Point (under ", ownerGang.name, " control)"));
                }
                else
                {
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, string.Concat("War Control Point (neutral)"));
                }

                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, myBlip);
            }

        }

        public virtual void CreateAttachedBlip()
        {
            if (myBlip == null)
            {
                myBlip = World.CreateBlip(position);
            }
        }

        public void SetupAtPosition(Vector3 pos, Gang ownerGang, GangWar warUsingThisPoint)
        {
            this.warUsingThisPoint = warUsingThisPoint;
            this.ownerGang = ownerGang;
            position = pos;
            CreateAttachedBlip();
            UpdateBlipAppearance();
        }

        public virtual void Disable()
        {
            if (myBlip != null)
            {
                myBlip.Remove();
                myBlip = null;
            }

            ownerGang = null;
            onCaptureCooldown = false;
            warUsingThisPoint = null;
        }

        /// <summary>
        /// only hides the point's blip; it still retains control data and can be captured
        /// </summary>
        public virtual void HideBlip()
        {
            if (myBlip != null)
            {
                myBlip.Remove();
                myBlip = null;
            }
        }

        /// <summary>
        /// returns true if it has been captured
        /// </summary>
        /// <returns></returns>
        public bool CheckIfHasBeenCaptured()
        {
            foreach (SpawnedGangMember member in SpawnManager.instance.memberAIs)
            {
                if (member.watchedPed != null && member.watchedPed.IsAlive && member.myGang != ownerGang)
                {
                    if (World.GetDistance(position, member.watchedPed.Position) <= ModOptions.instance.distanceToCaptureWarControlPoint)
                    {
                        //Capture!
                        if (member.myGang == warUsingThisPoint.defendingGang || member.myGang == warUsingThisPoint.attackingGang)
                        {
                            ownerGang = member.myGang;
                        }
                        else
                        {
                            //gangs "interfering" in the war should only neutralize points instead of capturing
                            ownerGang = null;
                        }

                        warUsingThisPoint.ControlPointHasBeenCaptured(this);
                        UpdateBlipAppearance();
                        return true;
                    }
                }
            }

            return false;

        }

    }
}
