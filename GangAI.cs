using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA.GangAndTurfMod
{
    public class GangAI : UpdatedClass
    {
        public Gang watchedGang;

        public override void Update()
        {
            if (watchedGang.moneyAvailable >= ModOptions.instance.costToTakeNeutralTurf)
            {
                //lets attack!
                //pick a random zone owned by us, get the closest hostile zone and attempt to take it
                TurfZone[] myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);

                if (myZones.Length > 0)
                {
                    TurfZone chosenZone = myZones[RandomUtil.CachedRandom.Next(0, myZones.Length)];
                    TurfZone closestZoneToChosen = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);

                    TryTakeTurf(closestZoneToChosen);
                }
                else
                {
                    //we're out of turf!
                    //get a random zone and try to take it
                    //but only sometimes, since we're probably on a tight spot
                    TurfZone chosenZone = ZoneManager.instance.GetRandomZone();
                    TryTakeTurf(chosenZone);

                }

                if(watchedGang.moneyAvailable >= 25000)
                {
                    if (RandomUtil.RandomBool())
                    {
                        //since we've got some extra cash, lets upgrade our members!
                        if (RandomUtil.RandomBool() && watchedGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy)
                        {
                            watchedGang.moneyAvailable -= (watchedGang.memberAccuracyLevel + 10) * 250;
                            watchedGang.memberAccuracyLevel += 10;
                            if (watchedGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy)
                            {
                                watchedGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
                            }

                            GangManager.instance.SaveGangData(false);
                        }
                        else
                        {
                            //try to buy the weapons we like
                            if(watchedGang.preferredWeaponHashes.Count == 0)
                            {
                                watchedGang.SetPreferredWeapons();
                            }

                            WeaponHash chosenWeapon = RandomUtil.GetRandomElementFromList(watchedGang.preferredWeaponHashes);

                            if (!watchedGang.gangWeaponHashes.Contains(chosenWeapon))
                            {
                                if(watchedGang.moneyAvailable >= ModOptions.instance.GetBuyableWeaponByHash(chosenWeapon).price)
                                {
                                    watchedGang.moneyAvailable -= ModOptions.instance.GetBuyableWeaponByHash(chosenWeapon).price;
                                    watchedGang.gangWeaponHashes.Add(chosenWeapon);
                                    GangManager.instance.SaveGangData(false);
                                }
                            }
                        }
                    }
                    else
                    {
                        if(watchedGang.memberHealth < ModOptions.instance.maxGangMemberHealth)
                        {
                            watchedGang.moneyAvailable -= (watchedGang.memberHealth + 20) * 20;
                            watchedGang.memberHealth += 20;

                            if (watchedGang.memberHealth > ModOptions.instance.maxGangMemberHealth)
                            {
                                watchedGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
                            }

                            GangManager.instance.SaveGangData(false);
                        }
                        else
                        {
                            if (watchedGang.memberArmor < ModOptions.instance.maxGangMemberArmor)
                            {
                                watchedGang.moneyAvailable -= (watchedGang.memberArmor + 20) * 50;
                                watchedGang.memberArmor += 20;

                                if (watchedGang.memberArmor > ModOptions.instance.maxGangMemberArmor)
                                {
                                    watchedGang.memberArmor = ModOptions.instance.maxGangMemberArmor;
                                }

                                GangManager.instance.SaveGangData(false);
                            }
                        }
                    }
                }
            }
            else
            {
                //we may be running low on cash
                //do we have any turf? is any war going on?
                //if not, we no longer exist
                if (!GangWarManager.instance.isOccurring)
                {
                    TurfZone[] myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);

                    if (myZones.Length == 0)
                    {
                        GangManager.instance.KillGang(this);
                    }
                }
            }
        }

        void TryTakeTurf(TurfZone targetZone)
        {
            if (targetZone == null) return; //whoops, there just isn't any zone available for gangs
            if (targetZone.ownerGangName == "none")
            {
                //this zone is neutral, lets just take it
                watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf;
                watchedGang.TakeZone(targetZone);
            }
            else if(GangManager.instance.GetGangByName(targetZone.ownerGangName) == null)
            {
                ZoneManager.instance.GiveGangZonesToAnother(targetZone.ownerGangName, "none");
                //this zone is neutral, lets just take it
                watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf;
                watchedGang.TakeZone(targetZone);
            }
            else if (targetZone.ownerGangName == GangManager.instance.GetPlayerGang().name)
            {
                //start a war against the player!
                if (RandomUtil.RandomBool() && GangManager.instance.fightingEnabled && GangManager.instance.warAgainstPlayerEnabled)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 4;
                    GangWarManager.instance.StartWar(watchedGang, targetZone, GangWarManager.warType.defendingFromEnemy);
                }
            }
            else
            {
                //take the turf from the other gang... or not, maybe we fail
                watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 2; //we attacked already, spend the money
                int myAttackValue = watchedGang.GetGangAIStrengthValue() / RandomUtil.CachedRandom.Next(1, 20),
                    theirDefenseValue = GangManager.instance.GetGangByName(targetZone.ownerGangName).GetGangAIStrengthValue() / RandomUtil.CachedRandom.Next(1, 20);

                if (myAttackValue > 
                    theirDefenseValue)
                {
                    watchedGang.TakeZone(targetZone);
                    GangManager.instance.GiveTurfRewardToGang(watchedGang);
                }

            }
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangAIUpdates + RandomUtil.CachedRandom.Next(100);
        }

        public GangAI(Gang watchedGang)
        {
            this.watchedGang = watchedGang;
            ResetUpdateInterval();
            //do we have vehicles?
            if(this.watchedGang.carVariations.Count == 0)
            {
                //get some vehicles!
                for(int i = 0; i < RandomUtil.CachedRandom.Next(1, 4); i++)
                {
                    PotentialGangVehicle newVeh = PotentialGangVehicle.GetMemberFromPool();
                    if(newVeh != null)
                    {
                        this.watchedGang.AddGangCar(newVeh);
                    }
                }
            }
        }
    }
}
