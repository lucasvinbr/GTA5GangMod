using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System.Drawing;
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
                    myBlip.ShowsOutlineIndicator = false;
                }
                else
                {
                    myBlip.Sprite = BlipSprite.AdversaryBunker;
                    Function.Call(Hash.SET_BLIP_COLOUR, myBlip, ownerGang.blipColor);
                    myBlip.ShowsOutlineIndicator = true;

                    if (ownerGang.isPlayerOwned)
                    {
                        myBlip.SecondaryColor = Color.Green;
                    }
                    else
                    {
                        myBlip.SecondaryColor = Color.Red;
                    }

                }

                if (ownerGang != null)
                {
                    myBlip.Name = string.Concat("War Control Point (under ", ownerGang.name, " control)");
                }
                else
                {
                    myBlip.Name = string.Concat("War Control Point (neutral)");
                }

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
                myBlip.Delete();
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
                myBlip.Delete();
                myBlip = null;
            }
        }

        /// <summary>
        /// returns true if it has been captured
        /// </summary>
        /// <returns></returns>
        public bool CheckIfHasBeenCaptured()
        {
            if(!ModOptions.instance.protagonistsAreSpectators && !MindControl.HasChangedBody)
            {
                if (GangManager.instance.PlayerGang != ownerGang &&
                    World.GetDistance(position, MindControl.CurrentPlayerCharacter.Position) <= ModOptions.instance.distanceToCaptureWarControlPoint)
                {
                    //Capture!
                    if (GangManager.instance.PlayerGang == warUsingThisPoint.defendingGang || GangManager.instance.PlayerGang == warUsingThisPoint.attackingGang)
                    {
                        ownerGang = GangManager.instance.PlayerGang;
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

        /// <summary>
        /// attaches a onkilled event to check if the recently spawned member was "spawnkilled".
        /// If so, neutralizes this point, in an attempt to prevent this from happening repeatedly
        /// </summary>
        /// <param name="member"></param>
        public void AttachDeathCheckEventToSpawnedMember(SpawnedGangMember member)
        {
            if (member == null) return;

            member.OnKilled += () =>
            {
                if(ownerGang == member.myGang &&
                ((ModCore.curGameTime - member.timeOfSpawn <= 5000) || (World.GetDistance(position, member.watchedPed.Position) < 5.0f)))
                {
                    ownerGang = null;
                    warUsingThisPoint.ControlPointHasBeenCaptured(this);
                    UpdateBlipAppearance();
                }
            };
        }

    }
}
