using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// this script controls most things related to gang behavior and relations.
    /// </summary>
    public class GangManager
    {
        public List<GangAI> enemyGangs;
        public GangData gangData;
        public static GangManager instance;

        public Gang PlayerGang
        {
            get
            {
                if(cachedPlayerGang == null) { 
                    cachedPlayerGang = GetPlayerGang();
                }

				//if, somehow, we still don't have a player gang around, make a new one!
				if(cachedPlayerGang == null) {
					cachedPlayerGang = CreateNewPlayerGang();
				}

                return cachedPlayerGang;
            }
        }

        private Gang cachedPlayerGang;

        private int timeLastReward = 0;

        //bools below are toggled true for one Tick if an Update function for the respective type was run
        private bool gangAIUpdateRanThisFrame = false;


        #region setup/save stuff
        public class GangData
        {

            public GangData()
            {
                gangs = new List<Gang>();
            }

            public List<Gang> gangs;
        }
        public GangManager()
        {
            instance = this;

            enemyGangs = new List<GangAI>();

			//classes below are all singletons, so no need to hold their ref here
            new ModOptions();
			new SpawnManager();

            gangData = PersistenceHandler.LoadFromFile<GangData>("GangData");
            if (gangData == null)
            {
                gangData = new GangData();

				//setup initial gangs... the player's and an enemy
				CreateNewPlayerGang();

                CreateNewEnemyGang();
			}else {
				PlayerGang.AdjustStatsToModOptions();
			}

			if (gangData.gangs.Count == 1 && ModOptions.instance.maxCoexistingGangs > 1)
            {
                //we're alone.. add an enemy!
                CreateNewEnemyGang();
            }

            SetUpAllGangs();

			timeLastReward = ModCore.curGameTime;

        }
        /// <summary>
        /// basically sets relationship groups for all gangs, makes them hate each other and starts the AI for enemy gangs.
		/// also runs a few consistency checks on the gangs, like if their stats are conforming to the limits defined in modoptions
        /// </summary>
        void SetUpAllGangs()
        {
            //set up the relationshipgroups
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                gangData.gangs[i].relationGroupIndex = World.AddRelationshipGroup(gangData.gangs[i].name);
                
                //if the player owns this gang, we love him
                if (gangData.gangs[i].isPlayerOwned)
                {
                    World.SetRelationshipBetweenGroups(Relationship.Companion, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
                    World.SetRelationshipBetweenGroups(Relationship.Companion, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);
                }
                else
                {
                    //since we're checking each gangs situation...
                    //lets check if we don't have any member variation, which could be a problem
                    if (gangData.gangs[i].memberVariations.Count == 0)
                    {
                        GetMembersForGang(gangData.gangs[i]);
                    }

                    //lets also see if their colors are consistent
                    gangData.gangs[i].EnforceGangColorConsistency();

					//aaaand check if our current stats still conform to the min/max statuses defined in ModOptions
					gangData.gangs[i].AdjustStatsToModOptions();

					
                    
                    //add this gang to the enemy gangs
                    //and start the AI for it
                    enemyGangs.Add(new GangAI(gangData.gangs[i]));
                }

            }

			//set gang relations...
			SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);
            //all gangs hate cops if set to very aggressive
            SetCopRelations(ModOptions.instance.gangMemberAggressiveness == ModOptions.GangMemberAggressivenessMode.veryAgressive);
        }

		/// <summary>
		/// sets relations between gangs to a certain level according to the aggressiveness
		/// </summary>
		/// <param name="aggrLevel"></param>
		public void SetGangRelationsAccordingToAggrLevel(ModOptions.GangMemberAggressivenessMode aggrLevel) {
			Relationship targetRelationLevel = Relationship.Hate;
			Gang excludedGang = null;
			if (GangWarManager.instance.isOccurring) {
				excludedGang = GangWarManager.instance.enemyGang;
			}
			switch (aggrLevel) {
				case ModOptions.GangMemberAggressivenessMode.veryAgressive:
					targetRelationLevel = Relationship.Hate;
					break;
				case ModOptions.GangMemberAggressivenessMode.agressive:
					targetRelationLevel = Relationship.Dislike;
					break;
				case ModOptions.GangMemberAggressivenessMode.defensive:
					targetRelationLevel = Relationship.Neutral;
					break;
			}
			for (int i = gangData.gangs.Count - 1; i > -1; i--) {
				for (int j = 0; j < i; j++) {
					if((gangData.gangs[i] != excludedGang || gangData.gangs[j] != PlayerGang) &&
						(gangData.gangs[j] != excludedGang || gangData.gangs[i] != PlayerGang)) {
						World.SetRelationshipBetweenGroups(targetRelationLevel, gangData.gangs[i].relationGroupIndex, gangData.gangs[j].relationGroupIndex);
						World.SetRelationshipBetweenGroups(targetRelationLevel, gangData.gangs[j].relationGroupIndex, gangData.gangs[i].relationGroupIndex);
					}
				}
				if (!gangData.gangs[i].isPlayerOwned && gangData.gangs[i] != excludedGang) {
					World.SetRelationshipBetweenGroups(targetRelationLevel, gangData.gangs[i].relationGroupIndex, Game.Player.Character.RelationshipGroup);
					World.SetRelationshipBetweenGroups(targetRelationLevel, Game.Player.Character.RelationshipGroup, gangData.gangs[i].relationGroupIndex);
				}
			}
		}

        public void SetCopRelations(bool hate)
        {
            int copHash = Function.Call<int>(Hash.GET_HASH_KEY, "COP");
            int relationLevel = 3; //neutral
            if (hate) relationLevel = 5; //hate

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, copHash, gangData.gangs[i].relationGroupIndex);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, relationLevel, gangData.gangs[i].relationGroupIndex, copHash);
            }
        }

        public void SaveGangData(bool notifySuccess = true)
        {
			AutoSaver.instance.gangDataDirty = true;
			if (notifySuccess) {
				AutoSaver.instance.gangDataNotifySave = true;
			}
        }
        #endregion


        public void Tick()
        {
            TickGangs();
        }

		#region gang general control stuff


		/// <summary>
		/// this controls the gang AI decisions and rewards for the player and AI gangs
		/// </summary>
		void TickGangs()
        {
            gangAIUpdateRanThisFrame = false;
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ticksSinceLastUpdate++;
                if (!gangAIUpdateRanThisFrame)
                {
                    if (enemyGangs[i].ticksSinceLastUpdate >= enemyGangs[i].ticksBetweenUpdates)
                    {
                        enemyGangs[i].ticksSinceLastUpdate = 0 - RandoMath.CachedRandom.Next(enemyGangs[i].ticksBetweenUpdates / 3);
                        enemyGangs[i].Update();
                        //lets also check if there aren't too many gangs around
                        //if there aren't, we might create a new one...
                        if (enemyGangs.Count < ModOptions.instance.maxCoexistingGangs - 1)
                        {
                            if (RandoMath.CachedRandom.Next(enemyGangs.Count) == 0)
                            {
                                Gang createdGang = CreateNewEnemyGang();
                                if (createdGang != null)
                                {
                                    enemyGangs.Add(new GangAI(createdGang));
                                }

                            }
                        }

                        gangAIUpdateRanThisFrame = true; //max is one update per tick
                    }
                }
               
            }

			if (ModCore.curGameTime - timeLastReward > ModOptions.instance.msTimeBetweenTurfRewards) {
				timeLastReward = ModCore.curGameTime;
				for (int i = 0; i < enemyGangs.Count; i++) {
					GiveTurfRewardToGang(enemyGangs[i].watchedGang);
				}

				//this also counts for the player's gang
				GiveTurfRewardToGang(PlayerGang);
			}
        }

        /// <summary>
        /// makes all AI gangs do an Update run immediately
        /// </summary>
        public void ForceTickAIGangs()
        {
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].Update();
            }
        }

		/// <summary>
		/// creates a new "player's gang" (there should be only one!)
		/// and adds it to the gangdata gangs list
		/// </summary>
		/// <param name="notifyMsg"></param>
		/// <returns></returns>
		public Gang CreateNewPlayerGang(bool notifyMsg = true) {
			Gang playerGang = new Gang("Player's Gang", VehicleColor.BrushedGold, true);
			//setup gangs
			gangData.gangs.Add(playerGang);

			playerGang.blipColor = (int)BlipColor.Yellow;

			if (ModOptions.instance.gangsStartWithPistols) {
				playerGang.gangWeaponHashes.Add(WeaponHash.Pistol);
			}

			if (notifyMsg) {
				UI.Notify("Created new gang for the player!");
			}

			return playerGang;
		}

        public Gang CreateNewEnemyGang(bool notifyMsg = true)
        {
            if(PotentialGangMember.MemberPool.memberList.Count <= 0)
            {
                UI.Notify("Enemy gang creation failed: bad/empty/not found memberPool file. Try adding peds as potential members for AI gangs");
                return null;
            }
            //set gang name from options
            string gangName = "Gang";
            do
            {
                gangName = string.Concat(RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangFirstNames), " ",
                RandoMath.GetRandomElementFromList(ModOptions.instance.possibleGangLastNames));
            } while (GetGangByName(gangName) != null);

            PotentialGangMember.MemberColor gangColor = (PotentialGangMember.MemberColor)RandoMath.CachedRandom.Next(9);

			//the new gang takes the wealthiest gang around as reference to define its starting money.
			//that does not mean it will be the new wealthiest one, hehe (but it may)
			Gang newGang = new Gang(gangName, RandoMath.GetRandomElementFromList(ModOptions.instance.GetGangColorTranslation(gangColor).vehicleColors),
				false, (int)(RandoMath.Max(Game.Player.Money, GetWealthiestGang().moneyAvailable) * (RandoMath.CachedRandom.Next(1, 11) / 6.5f))) {
				blipColor = RandoMath.GetRandomElementFromArray(ModOptions.instance.GetGangColorTranslation(gangColor).blipColors)
			};

			GetMembersForGang(newGang);

            //relations...
            newGang.relationGroupIndex = World.AddRelationshipGroup(gangName);

            World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, Game.Player.Character.RelationshipGroup);
            World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, newGang.relationGroupIndex);

            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                World.SetRelationshipBetweenGroups(Relationship.Hate, gangData.gangs[i].relationGroupIndex, newGang.relationGroupIndex);
                World.SetRelationshipBetweenGroups(Relationship.Hate, newGang.relationGroupIndex, gangData.gangs[i].relationGroupIndex);
            }

            gangData.gangs.Add(newGang);

            newGang.GetPistolIfOptionsRequire();

            SaveGangData();
            if (notifyMsg)
            {
                UI.Notify("The " + gangName + " have entered San Andreas!");
            }
            

            return newGang;
        }

        public void GetMembersForGang(Gang targetGang)
        {
            PotentialGangMember.MemberColor gangColor = ModOptions.instance.TranslateVehicleToMemberColor(targetGang.vehicleColor);
            PotentialGangMember.DressStyle gangStyle = (PotentialGangMember.DressStyle)RandoMath.CachedRandom.Next(3);
            for (int i = 0; i < RandoMath.CachedRandom.Next(2, 6); i++)
            {
                PotentialGangMember newMember = PotentialGangMember.GetMemberFromPool(gangStyle, gangColor);
                if (newMember != null)
                {
                    targetGang.AddMemberVariation(newMember);
                }
                else
                {
                    break;
                }

            }
        }

        public void KillGang(GangAI aiWatchingTheGang)
        {
            UI.Notify("The " + aiWatchingTheGang.watchedGang.name + " have been wiped out!");

			//save the fallen gang in a file
			AddGangToWipedOutList(aiWatchingTheGang.watchedGang);
			gangData.gangs.Remove(aiWatchingTheGang.watchedGang);
			enemyGangs.Remove(aiWatchingTheGang);
			if (enemyGangs.Count == 0 && ModOptions.instance.maxCoexistingGangs > 1)
            {
                //create a new gang right away... but do it silently to not demotivate the player too much
                Gang createdGang = CreateNewEnemyGang(false);
                if (createdGang != null)
                {
                    enemyGangs.Add(new GangAI(createdGang));
                }
            }
            SaveGangData(false);
        }

		/// <summary>
		/// adds the gang to a xml file that contains a list of gangs that have been wiped out,
		///  so that the player can reuse their data in the future
		/// </summary>
		/// <param name="gangToAdd"></param>
		public void AddGangToWipedOutList(Gang gangToAdd) {
			List<Gang> WOList = PersistenceHandler.LoadFromFile<List<Gang>>("wipedOutGangsList");
			if(WOList == null) {
				WOList = new List<Gang>();
			}
			WOList.Add(gangToAdd);
			PersistenceHandler.SaveToFile(WOList, "wipedOutGangsList");
		}

        public void GiveTurfRewardToGang(Gang targetGang)
        {

            List<TurfZone> curGangZones = ZoneManager.instance.GetZonesControlledByGang(targetGang.name);
            if (targetGang.isPlayerOwned)
            {
                if (curGangZones.Count > 0)
                {
                    int rewardedCash = 0;

                    for (int i = 0; i < curGangZones.Count; i++)
                    {
                        int zoneReward = (int)((ModOptions.instance.baseRewardPerZoneOwned * 
                            (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count)) +
                            ((curGangZones[i].value + 1) * ModOptions.instance.baseRewardPerZoneOwned * 0.25f) );

                        MindControl.instance.AddOrSubtractMoneyToProtagonist(zoneReward);
                        
                        rewardedCash += zoneReward;
                    }
                    Function.Call(Hash.PLAY_SOUND, -1, "Virus_Eradicated", "LESTER1A_SOUNDS", 0, 0, 1);
                    UI.Notify("Money won from controlled zones: " + rewardedCash.ToString());
                }
            }
            else
            {
                for (int j = 0; j < curGangZones.Count; j++)
                {
                    targetGang.moneyAvailable += (int)((curGangZones[j].value + 1) *
                        ModOptions.instance.baseRewardPerZoneOwned *
                        (1 + ModOptions.instance.rewardMultiplierPerZone * curGangZones.Count) * ModOptions.instance.extraProfitForAIGangsFactor);
                }

            }

        }


        /// <summary>
        /// when the player asks to reset mod options, we must reset these update intervals because they
        /// may have changed
        /// </summary>
        public void ResetGangUpdateIntervals()
        {
            for(int i = 0; i < enemyGangs.Count; i++)
            {
                enemyGangs[i].ResetUpdateInterval();
            }

			SpawnManager.instance.ResetSpawnedsUpdateInterval();
        }

        #endregion

        #region getters
        public Gang GetGangByName(string name)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].name == name)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public Gang GetGangByRelGroup(int relGroupIndex)
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].relationGroupIndex == relGroupIndex)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        public GangAI GetGangAI(Gang targetGang)
        {
            for (int i = 0; i < enemyGangs.Count; i++)
            {
                if (enemyGangs[i].watchedGang == targetGang)
                {
                    return enemyGangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the player's gang (it's better to use the PlayerGang property instead)
        /// </summary>
        /// <returns></returns>
        private Gang GetPlayerGang()
        {
            for (int i = 0; i < gangData.gangs.Count; i++)
            {
                if (gangData.gangs[i].isPlayerOwned)
                {
                    return gangData.gangs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns the gang with the most stocked money
        /// </summary>
        /// <returns></returns>
        public Gang GetWealthiestGang()
        {
            Gang pickedGang = null;

            for(int i = 0; i < gangData.gangs.Count; i++)
            {
                if (pickedGang != null) {
                    if (gangData.gangs[i].moneyAvailable > pickedGang.moneyAvailable)
                        pickedGang = gangData.gangs[i];
                }
                else
                {
                    pickedGang = gangData.gangs[i];
                }
            }

            return pickedGang;
        }

        #endregion

       }

}
