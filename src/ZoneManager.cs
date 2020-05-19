using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using GTA.Math;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod {
	/// <summary>
	/// this script deals with gangs taking over zones. it also allows adding new zones to be taken
	/// </summary>
	public class ZoneManager {

		public enum ZoneBlipDisplay {
			none,
			fiveClosest,
			allZones,
		}

		public ZoneBlipDisplay curBlipDisplay = ZoneBlipDisplay.none;

		#region setup/save stuff

		public class TurfZoneData {
			public List<TurfZone> zoneList;

			public TurfZoneData() {
				zoneList = new List<TurfZone>();
			}
		}


		public static ZoneManager instance;
		public TurfZoneData zoneData;


		public ZoneManager() {
			instance = this;

			zoneData = PersistenceHandler.LoadFromFile<TurfZoneData>("TurfZoneData");
			if (zoneData == null) {
				zoneData = new TurfZoneData();
			}

		}

		public void SaveZoneData(bool notifySuccess = true) {
			AutoSaver.instance.zoneDataDirty = true;
			if (notifySuccess) {
				AutoSaver.instance.zoneDataNotifySave = true;
			}
		}

		public void UpdateZoneData(TurfZone newTurfZone) {
			if (!zoneData.zoneList.Contains(newTurfZone)) {
				zoneData.zoneList.Add(newTurfZone);
			}

			newTurfZone.CreateAttachedBlip();
			newTurfZone.UpdateBlipPosition();
			
			RefreshZoneBlips();

			SaveZoneData(false);
		}

		#endregion

		public TurfZone GetZoneInLocation(Vector3 location)
		{
			//prioritize custom zones
			for (int i = 0; i < zoneData.zoneList.Count; i++)
			{
				if (zoneData.zoneList[i].GetType() != typeof(TurfZone) &&
					zoneData.zoneList[i].IsLocationInside(string.Empty, location))
				{
					return zoneData.zoneList[i];
				}
			}

			//fall back to getting by zone name
			return GetZoneByName(World.GetZoneName(location));
		}

		public TurfZone GetZoneInLocation(string zoneName, Vector3 location)
		{
			//prioritize custom zones
			for (int i = 0; i < zoneData.zoneList.Count; i++)
			{
				if (zoneData.zoneList[i].GetType() != typeof(TurfZone) &&
					zoneData.zoneList[i].IsLocationInside(zoneName, location))
				{
					return zoneData.zoneList[i];
				}
			}

			//fall back to getting by zone name
			return GetZoneByName(zoneName);
		}

		public void OutputCurrentZoneInfo() {
			string zoneName = World.GetZoneName(MindControl.CurrentPlayerCharacter.Position);
			string zoneInfoMsg;
			TurfZone currentZone = GetZoneInLocation(zoneName, MindControl.CurrentPlayerCharacter.Position);

			if (currentZone != null) {
				zoneInfoMsg = "Current zone is " + currentZone.zoneName + ".";
				if (currentZone.ownerGangName != "none") {
					if (GangManager.instance.GetGangByName(currentZone.ownerGangName) == null) {
						GiveGangZonesToAnother(currentZone.ownerGangName, "none");
						currentZone.ownerGangName = "none";
						SaveZoneData(false);
						zoneInfoMsg += " It isn't owned by any gang.";
					}
					else {
						zoneInfoMsg += " It is owned by the " + currentZone.ownerGangName + ".";

						zoneInfoMsg += " Its current level is " + currentZone.value.ToString();
					}
				}
				else {
					zoneInfoMsg += " It isn't owned by any gang.";
				}
			}
			else {
				zoneInfoMsg = "Current zone is " + zoneName + ".  It hasn't been marked as takeable yet.";
			}

			UI.ShowSubtitle(zoneInfoMsg);
		}

		public static int CompareZonesByDistToPlayer(TurfZone x, TurfZone y) {
			if (x == null) {
				if (y == null) {
					return 0;
				}
				else {
					return -1;
				}
			}
			else {
				if (y == null) {
					return 1;
				}
				else {
					Vector3 playerPos = MindControl.CurrentPlayerCharacter.Position;
					return playerPos.DistanceTo2D(x.zoneBlipPosition).
						CompareTo(playerPos.DistanceTo2D(y.zoneBlipPosition));
				}
			}
		}


		public static int CompareZonesByValue(TurfZone x, TurfZone y) {
			if (x == null) {
				if (y == null) {
					return 0;
				}
				else {
					return -1;
				}
			}
			else {
				if (y == null) {
					return 1;
				}
				else {
					return y.value.CompareTo(x.value);
				}
			}
		}

		#region blip related methods


		public void ChangeBlipDisplay() {
			curBlipDisplay++;
			if (curBlipDisplay > ZoneBlipDisplay.allZones) {
				curBlipDisplay = ZoneBlipDisplay.none;
			}

			RefreshZoneBlips();
		}

		public void ChangeBlipDisplay(ZoneBlipDisplay desiredDisplayType) {
			curBlipDisplay = desiredDisplayType;
			if (curBlipDisplay > ZoneBlipDisplay.allZones) {
				curBlipDisplay = ZoneBlipDisplay.none;
			}

			RefreshZoneBlips();
		}

		public void RefreshZoneBlips() {
			switch (curBlipDisplay) {
				case ZoneBlipDisplay.none:
					for (int i = 0; i < zoneData.zoneList.Count; i++) {
						zoneData.zoneList[i].RemoveBlip();
					}
					break;
				case ZoneBlipDisplay.allZones:
					//refresh the closest since we only show area blips for the closest
					zoneData.zoneList.Sort(CompareZonesByDistToPlayer); 
					for (int i = 0; i < zoneData.zoneList.Count; i++) {
						zoneData.zoneList[i].CreateAttachedBlip(i < 5);
						zoneData.zoneList[i].UpdateBlip();
					}
					break;
				case ZoneBlipDisplay.fiveClosest:
					zoneData.zoneList.Sort(CompareZonesByDistToPlayer);
					for (int i = 0; i < zoneData.zoneList.Count; i++) {
						if (i < 5) {
							zoneData.zoneList[i].CreateAttachedBlip(true);
							zoneData.zoneList[i].UpdateBlip();
						}
						else {
							zoneData.zoneList[i].RemoveBlip();
						}
					}
					break;
				default:
					UI.Notify("Invalid blip display type");
					break;
			}
		}

		#endregion

		public void GiveGangZonesToAnother(string FromGang, string ToGang) {
			List<TurfZone> fromGangZones = GetZonesControlledByGang(FromGang);
			for (int i = 0; i < fromGangZones.Count; i++) {
				fromGangZones[i].ownerGangName = ToGang;
			}

			SaveZoneData(false);
		}

		#region getters


		public bool DoesZoneWithNameExist(string zoneName)
		{
			return GetZoneByName(zoneName) != null;
		}

		/// <summary>
		/// not exposed in favor of other zone retrieval options that better handle custom zones
		/// </summary>
		/// <param name="zoneName"></param>
		/// <returns></returns>
		private TurfZone GetZoneByName(string zoneName) {
			for (int i = 0; i < zoneData.zoneList.Count; i++) {
				if (zoneData.zoneList[i].zoneName == zoneName) {
					return zoneData.zoneList[i];
				}
			}

			return null;
		}

		/// <summary>
		/// gets the turfzone of where the player is
		/// </summary>
		/// <returns></returns>
		public TurfZone GetCurrentTurfZone() {
			return GetZoneInLocation(MindControl.CurrentPlayerCharacter.Position);
		}

		public List<TurfZone> GetZonesControlledByGang(string desiredGangName) {
			List<TurfZone> ownedZones = new List<TurfZone>();

			for (int i = 0; i < zoneData.zoneList.Count; i++) {
				if (zoneData.zoneList[i].ownerGangName == desiredGangName) {
					ownedZones.Add(zoneData.zoneList[i]);
				}
			}
			
			return ownedZones;
		}

		public TurfZone GetClosestZoneToTargetZone(TurfZone targetZone, bool hostileOrNeutralZonesOnly = false, bool randomBetween3Closest = true) {
			float smallestDistance = 0;
			//we start our top 3 closest zones list with only the zone we want to get the closest from and start replacing as we find better ones
			//the result may not be the 3 closest zones, but thats okay
			List<TurfZone> top3ClosestZones = new List<TurfZone> { targetZone, targetZone, targetZone };
			int timesFoundBetterZone = 0;
			for (int i = 0; i < zoneData.zoneList.Count; i++) {
				float distanceToThisZone = World.GetDistance(targetZone.zoneBlipPosition, zoneData.zoneList[i].zoneBlipPosition);
				if (distanceToThisZone != 0 &&
					(!hostileOrNeutralZonesOnly || targetZone.ownerGangName != zoneData.zoneList[i].ownerGangName)) {
					if (smallestDistance == 0 || smallestDistance > distanceToThisZone) {
						timesFoundBetterZone++;
						top3ClosestZones.Insert(0, zoneData.zoneList[i]);
						top3ClosestZones.RemoveAt(3);
						smallestDistance = distanceToThisZone;
					}
				}
			}

			if (randomBetween3Closest && timesFoundBetterZone >= 3) //only get a random from top 3 if we found 3 different zones
			{
				return RandoMath.GetRandomElementFromList(top3ClosestZones);
			}
			else {
				return top3ClosestZones[0];
			}

		}

		public TurfZone GetRandomZone(bool preferablyNeutralZone = false) {
			if (!preferablyNeutralZone) {
				return RandoMath.GetRandomElementFromList(zoneData.zoneList);
			}
			else {
				if (zoneData.zoneList.Count > 0) {
					List<TurfZone> possibleTurfChoices = new List<TurfZone>();

					possibleTurfChoices.AddRange(zoneData.zoneList);

					for (int i = 0; i < zoneData.zoneList.Count; i++) {
						if (possibleTurfChoices.Count == 0) {
							//we've run out of options! abort
							break;
						}
						TurfZone chosenZone = RandoMath.GetRandomElementFromList(possibleTurfChoices);
						if (!preferablyNeutralZone || chosenZone.ownerGangName == "none") {
							return chosenZone;
						}
						else {
							possibleTurfChoices.Remove(chosenZone);
						}
					}

					//if we couldn't find a neutral zone, just get any zone
					return GetRandomZone(false);
				}

			}

			return null;
		}


		#endregion




	}
}
