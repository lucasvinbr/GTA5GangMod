using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;

namespace GTA
{
    public class GangAI : UpdatedClass
    {
        public Gang watchedGang;

        public override void Update()
        {
            if (watchedGang.moneyAvailable >= 10000)
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
                        if (RandomUtil.RandomBool() && watchedGang.memberAccuracyLevel < 75)
                        {
                            watchedGang.moneyAvailable -= (watchedGang.memberAccuracyLevel + 10) * 250;
                            watchedGang.memberAccuracyLevel += 10;
                            if (watchedGang.memberAccuracyLevel > 75)
                            {
                                watchedGang.memberAccuracyLevel = 75;
                            }

                            GangManager.instance.SaveGangData();
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
                                    GangManager.instance.SaveGangData();
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

                            GangManager.instance.SaveGangData();
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

                                GangManager.instance.SaveGangData();
                            }
                        }
                    }
                }
            }
            else
            {
                //we may be running low on cash
                //do we have any turf?
                //if not, we no longer exist
                TurfZone[] myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);

                if (myZones.Length == 0)
                {
                    GangManager.instance.KillGang(this);
                }
            }
        }

        void TryTakeTurf(TurfZone targetZone)
        {
            if (targetZone.ownerGangName == "none")
            {
                //this zone is neutral, lets just take it
                watchedGang.moneyAvailable -= 10000;
                watchedGang.TakeZone(targetZone);
            }
            else if (targetZone.ownerGangName == GangManager.instance.GetPlayerGang().name)
            {
                //start a war against the player!
                watchedGang.moneyAvailable -= 2500;
                GangWarManager.instance.StartWar(watchedGang, targetZone, GangWarManager.warType.defendingFromEnemy);
            }
            else
            {
                //take the turf from the other gang... or not, maybe we fail
                //TODO consider gang general strength here
                watchedGang.moneyAvailable -= 5000; //we attacked already, spend the money
                if (RandomUtil.RandomBool())
                {
                    watchedGang.TakeZone(targetZone);
                }

            }
        }

        public GangAI(Gang watchedGang)
        {
            this.watchedGang = watchedGang;
            ticksBetweenUpdates = RandomUtil.CachedRandom.Next(25000, 35000);
        }
    }
}
