using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NativeUI.UIMenuDynamicListItem;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// submenu for setting spawn points and skipping wars
    /// </summary>
    public class WarOptionsSubMenu : UIMenu
    {
        public WarOptionsSubMenu(string title, string subtitle) : base(title, subtitle)
        {   
            Setup();
        }

        /// <summary>
        /// adds all buttons and events to the menu
        /// </summary>
        public void Setup()
        {
			UIMenuItem skipWarBtn = new UIMenuItem("Skip current War",
			   "If a war is currently occurring, it will instantly end, and its outcome will be defined by the strength and reinforcements of the involved gangs and a touch of randomness.");
			UIMenuItem resetAlliedSpawnBtn = new UIMenuItem("Set allied spawn points to your region",
				"If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets all 3 spawn points to your location: one exactly where you are and 2 nearby.");
			UIMenuItem resetEnemySpawnBtn = new UIMenuItem("Force reset enemy spawn points",
				"If a war is currently occurring, the enemy spawn points will be randomly set to a nearby location. Use this if they end up spawning somewhere unreachable.");

			AddItem(skipWarBtn);
			AddItem(resetAlliedSpawnBtn);

			UIMenuItem[] setSpecificSpawnBtns = new UIMenuItem[3];
			for (int i = 0; i < setSpecificSpawnBtns.Length; i++)
			{
				setSpecificSpawnBtns[i] = new UIMenuItem(string.Concat("Set allied spawn point ", (i + 1).ToString(), " to your position"),
					string.Concat("If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets spawn point number ",
						(i + 1).ToString(), " to your exact location."));
				AddItem(setSpecificSpawnBtns[i]);
			}

			AddItem(resetEnemySpawnBtn);

			OnItemSelect += (sender, item, index) => {
				if (GangWarManager.instance.isOccurring)
				{
					if (item == skipWarBtn)
					{

						GangWarManager.instance.EndWar(GangWarManager.instance.SkipWar(0.9f));
					}
					else

					if (item == resetAlliedSpawnBtn)
					{
						if (GangWarManager.instance.playerNearWarzone)
						{
							GangWarManager.instance.ForceSetAlliedSpawnPoints(MindControl.SafePositionNearPlayer);
						}
						else
						{
							UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
						}
					}
					else

					if (item == resetEnemySpawnBtn)
					{
						if (GangWarManager.instance.playerNearWarzone)
						{
							if (GangWarManager.instance.ReplaceEnemySpawnPoint())
							{
								UI.ShowSubtitle("Enemy spawn point reset succeeded!");
							}
							else
							{
								UI.ShowSubtitle("Enemy spawn point reset failed (try again)!");
							}
						}
						else
						{
							UI.ShowSubtitle("You must be in the contested zone or close to the war blip before resetting spawn points!");
						}
					}
					else
					{
						for (int i = 0; i < setSpecificSpawnBtns.Length; i++)
						{
							if (item == setSpecificSpawnBtns[i])
							{
								if (GangWarManager.instance.playerNearWarzone)
								{
									GangWarManager.instance.SetSpecificAlliedSpawnPoint(i, MindControl.SafePositionNearPlayer);
								}
								else
								{
									UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
								}

								break;
							}

						}
					}


				}
				else
				{
					UI.ShowSubtitle("There is no war in progress.");
				}

			};

			RefreshIndex();
        }

	}
}
