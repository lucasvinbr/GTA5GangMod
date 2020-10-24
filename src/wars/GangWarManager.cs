using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GTA.GangAndTurfMod
{
    public class GangWarManager : Script
    {

        public enum AttackStrength
        {
            light,
            medium,
            large,
            massive
        }

        public enum WarType
        {
            attackingEnemy,
            defendingFromEnemy
        }

        public UIResText alliedNumText, enemyNumText;

        public bool shouldDisplayReinforcementsTexts = false;


        public int timeLastWarAgainstPlayer = 0;


        public static GangWarManager instance;

        private readonly List<WarControlPoint> pooledControlPoints = new List<WarControlPoint>();

        /// <summary>
        /// the war the player is probably paying attention to. it should be updated more often than the others
        /// </summary>
        public GangWar focusedWar;

        private readonly List<GangWar> activeWars, pooledWars;


        public const int TICKS_BETWEEN_WAR_UPDATES = 12;
        private bool updateRanThisFrame = false;

        public GangWarManager()
        {
            instance = this;
            this.Tick += OnTick;
            this.Aborted += OnAbort;

            alliedNumText = new UIResText("400", new Point(), 0.5f, Color.CadetBlue);
            enemyNumText = new UIResText("400", new Point(), 0.5f, Color.Red);

            alliedNumText.Outline = true;
            enemyNumText.Outline = true;

            alliedNumText.TextAlignment = UIResText.Alignment.Centered;
            enemyNumText.TextAlignment = UIResText.Alignment.Centered;

            float screenRatio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;

            int proportionalScreenWidth = (int)(1080 * screenRatio); //nativeUI UIResText works with 1080p height

            alliedNumText.Position = new Point((proportionalScreenWidth / 2) - 120, 10);
            enemyNumText.Position = new Point((proportionalScreenWidth / 2) + 120, 10);

            activeWars = new List<GangWar>();
            pooledWars = new List<GangWar>();
        }


        #region start/end/skip war
        
        public bool TryStartWar(Gang attackingGang, TurfZone warzone, AttackStrength attackStrength)
        {
            if (IsZoneContested(warzone)) return false;

            Gang defenderGang = GangManager.instance.GetGangByName(warzone.ownerGangName);

            if(defenderGang != null && defenderGang != attackingGang)
            {
                GangWar warObj = GetUnusedWarObject();
                if(warObj.StartWar(attackingGang, defenderGang, warzone, attackStrength))
                {
                    pooledWars.Remove(warObj);
                    activeWars.Add(warObj);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        

        public bool CanStartWarAgainstPlayer
        {
            get
            {
                return (ModCore.curGameTime - timeLastWarAgainstPlayer > ModOptions.instance.minMsTimeBetweenAttacksOnPlayerTurf) &&
                    MindControl.CurrentPlayerCharacter.IsAlive; //starting a war against the player when we're in the "wasted" screen could instantly end it
            }
        }

        #endregion


        #region active wars data checks

        /// <summary>
        /// true if one of the active wars is taking place at the target zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool IsZoneContested(TurfZone zone)
        {
            foreach(GangWar war in activeWars)
            {
                if(war.warZone == zone)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// returns true if at least one of the active wars are between the target gangs
        /// </summary>
        /// <param name="gang1"></param>
        /// <param name="gang2"></param>
        /// <returns></returns>
        public bool AreGangsCurrentlyFightingEachOther(Gang gang1, Gang gang2)
        {
            foreach (GangWar war in activeWars)
            {
                if (war.attackingGang == gang1 && war.defendingGang == gang2 ||
                    war.attackingGang == gang2 && war.defendingGang == gang1)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsGangFightingAWar(Gang gang)
        {
            foreach (GangWar war in activeWars)
            {
                if (war.attackingGang == gang || war.defendingGang == gang)
                {
                    return true;
                }
            }

            return false;
        }

        public List<Gang> GetAllGangsCurrentlyFightingWars()
        {
            List<Gang> returnedList = new List<Gang>();

            foreach (GangWar war in activeWars)
            {
                if (!returnedList.Contains(war.attackingGang)){
                    returnedList.Add(war.attackingGang);
                }

                if (!returnedList.Contains(war.defendingGang))
                {
                    returnedList.Add(war.defendingGang);
                }
            }

            return returnedList;
        }

        #endregion

        #region war objects fetching

        /// <summary>
        /// gets (or adds, if the pool's empty) a war object from the pool. does NOT remove them from the pool, though!
        /// </summary>
        /// <returns></returns>
        public GangWar GetUnusedWarObject()
        {
            GangWar returnedWar;
            if (pooledWars.Count > 0)
            {
                returnedWar = pooledWars[0];
            }
            else
            {
                returnedWar = new GangWar();
                returnedWar.OnPlayerEnteredWarzone += PlayerEnteredWar;
                returnedWar.OnPlayerLeftWarzone += PlayerLeftWar;
                returnedWar.onWarEnded += WarHasEnded;
                returnedWar.OnReinforcementsChanged += WarReinforcementsChanged;
                pooledWars.Add(returnedWar);
            }

            return returnedWar;
        }

        public GangWar GetWarOccurringOnZone(TurfZone zone)
        {
            foreach (GangWar war in activeWars)
            {
                if (war.warZone == zone)
                {
                    return war;
                }
            }

            return null;
        }

        #endregion

        #region war events

        public void PlayerEnteredWar(GangWar enteredWar)
        {
            if(focusedWar == null)
            {
                focusedWar = enteredWar;

                if (focusedWar.IsPlayerGangInvolved())
                {
                    shouldDisplayReinforcementsTexts = true;
                    if (focusedWar.attackingGang.isPlayerOwned)
                    {
                        UpdateReinforcementsTexts(focusedWar.attackerReinforcements, focusedWar.defenderReinforcements);
                    }
                    else
                    {
                        UpdateReinforcementsTexts(focusedWar.defenderReinforcements, focusedWar.attackerReinforcements);
                    }
                }
            }
        }

        public void PlayerLeftWar(GangWar leftWar)
        {
            if(leftWar == focusedWar)
            {
                focusedWar = null;
                shouldDisplayReinforcementsTexts = false;
            }
        }

        public void WarHasEnded(GangWar endedWar, bool defenderVictory)
        {
            activeWars.Remove(endedWar);
            pooledWars.Add(endedWar);

            if(endedWar == focusedWar)
            {
                focusedWar = null;
                shouldDisplayReinforcementsTexts = false;
            }
        }

        public void WarReinforcementsChanged(GangWar gangWar)
        {
            if(gangWar == focusedWar)
            {
                if (gangWar.attackingGang.isPlayerOwned)
                {
                    UpdateReinforcementsTexts(gangWar.attackerReinforcements, gangWar.defenderReinforcements);
                }
                else
                {
                    UpdateReinforcementsTexts(gangWar.defenderReinforcements, gangWar.attackerReinforcements);
                }
            }
        }

        public void UpdateReinforcementsTexts(int allies, int enemies)
        {
            alliedNumText.Caption = allies.ToString();
            enemyNumText.Caption = enemies.ToString();
        }

        #endregion

        #region control point related

        /// <summary>
        /// gets an unused CP from the pool or creates a new one
        /// </summary>
        /// <returns></returns>
        public WarControlPoint GetUnusedWarControlPoint()
        {
            WarControlPoint newPoint;
            if (pooledControlPoints.Count > 0)
            {
                newPoint = pooledControlPoints[0];
                pooledControlPoints.RemoveAt(0);
            }
            else
            {
                newPoint = new WarControlPoint();
            }

            return newPoint;
        }

        /// <summary>
        /// disables and hides the target CP, but stores it so that it can be reused in another war
        /// </summary>
        /// <param name="cp"></param>
        public void PoolControlPoint(WarControlPoint cp)
        {
            cp.Disable();
            pooledControlPoints.Add(cp);
        }

        #endregion



        private void OnTick(object sender, EventArgs e)
        {
            updateRanThisFrame = false;
            for (int i = activeWars.Count - 1; i >= 0; i--)
            {
                activeWars[i].ticksSinceLastUpdate++;

                if (!updateRanThisFrame && activeWars[i].ticksSinceLastUpdate >= activeWars[i].ticksBetweenUpdates)
                {
                    updateRanThisFrame = true;
                    activeWars[i].ticksSinceLastUpdate = 0;
                    activeWars[i].Update();
                }
            }
        }

        private void OnAbort(object sender, EventArgs e)
        {
            foreach(GangWar war in activeWars)
            {
                war.Abort();
            }

            foreach (GangWar war in pooledWars)
            {
                war.Abort();
            }
        }
    }
}
