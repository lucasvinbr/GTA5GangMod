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
            if (RandoMath.RandomBool())
            {
                //lets attack!
                //pick a random zone owned by us, get the closest hostile zone and attempt to take it
                List<TurfZone> myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);

                if (myZones.Count > 0)
                {
                    TurfZone chosenZone = RandoMath.GetRandomElementFromList(myZones);
                    TurfZone closestZoneToChosen = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);

                    TryTakeTurf(closestZoneToChosen);
                }
                else
                {
                    //we're out of turf!
                    //get a random zone (preferably neutral, since it's cheaper for the AI) and try to take it
                    //but only sometimes, since we're probably on a tight spot
                    TurfZone chosenZone = ZoneManager.instance.GetRandomZone(true);
                    TryTakeTurf(chosenZone);

                }

                if(watchedGang.moneyAvailable >= 25000)
                {
                    if (RandoMath.RandomBool())
                    {
                        //since we've got some extra cash, lets upgrade our members!
                        if (RandoMath.RandomBool() && watchedGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy &&
                            watchedGang.moneyAvailable >= GangManager.CalculateAccuracyUpgradeCost(watchedGang.memberAccuracyLevel))
                        {
                            watchedGang.moneyAvailable -= GangManager.CalculateAccuracyUpgradeCost(watchedGang.memberAccuracyLevel);
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

                            WeaponHash chosenWeapon = RandoMath.GetRandomElementFromList(watchedGang.preferredWeaponHashes);

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
                        if(watchedGang.memberHealth < ModOptions.instance.maxGangMemberHealth &&
                            watchedGang.moneyAvailable >= GangManager.CalculateHealthUpgradeCost(watchedGang.memberHealth))
                        {
                            watchedGang.moneyAvailable -= GangManager.CalculateHealthUpgradeCost(watchedGang.memberHealth);
                            watchedGang.memberHealth += 20;

                            if (watchedGang.memberHealth > ModOptions.instance.maxGangMemberHealth)
                            {
                                watchedGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
                            }

                            GangManager.instance.SaveGangData(false);
                        }
                        else
                        {
                            if (watchedGang.memberArmor < ModOptions.instance.maxGangMemberArmor &&
                            watchedGang.moneyAvailable >= GangManager.CalculateArmorUpgradeCost(watchedGang.memberArmor))
                            {
                                watchedGang.moneyAvailable -= GangManager.CalculateArmorUpgradeCost(watchedGang.memberArmor);
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
                    List<TurfZone> myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);

                    if (myZones.Count == 0)
                    {
                        GangManager.instance.KillGang(this);
                    }
                }
            }
        }

        void TryTakeTurf(TurfZone targetZone)
        {
            if (targetZone == null || targetZone.ownerGangName == watchedGang.name) return; //whoops, there just isn't any zone available for our gang
            if (targetZone.ownerGangName == "none")
            {
                //this zone is neutral, lets just take it
                //we make it cheaper for the AI to get neutral zones in order to not make the world a gangless place haha
                if(watchedGang.moneyAvailable >= ModOptions.instance.costToTakeNeutralTurf / 10)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 10;
                    watchedGang.TakeZone(targetZone);
                }
            }
            else if(GangManager.instance.GetGangByName(targetZone.ownerGangName) == null)
            {
                ZoneManager.instance.GiveGangZonesToAnother(targetZone.ownerGangName, "none");
                //this zone was controlled by a gang that no longer exists. it is neutral now
                if (watchedGang.moneyAvailable >= ModOptions.instance.costToTakeNeutralTurf / 10)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 10;
                    watchedGang.TakeZone(targetZone);
                }
            }
            else if (targetZone.ownerGangName == GangManager.instance.GetPlayerGang().name)
            {
                //start a war against the player!
                if(watchedGang.moneyAvailable >= ModOptions.instance.costToTakeNeutralTurf / 4)
                {
                    if (RandoMath.RandomBool() && ModOptions.instance.fightingEnabled && ModOptions.instance.warAgainstPlayerEnabled)
                    {
                        watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 4;
                        GangWarManager.instance.StartWar(watchedGang, targetZone, GangWarManager.warType.defendingFromEnemy);
                    }
                }
                
            }
            else
            {
                //take the turf from the other gang... or not, maybe we fail
                if(watchedGang.moneyAvailable >= ModOptions.instance.costToTakeNeutralTurf / 2)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.costToTakeNeutralTurf / 2; //we attacked already, spend the money
                    int myAttackValue = watchedGang.GetGangAIStrengthValue() / RandoMath.CachedRandom.Next(1, 20),
                        theirDefenseValue = GangManager.instance.GetGangByName(targetZone.ownerGangName).GetGangAIStrengthValue() / RandoMath.CachedRandom.Next(1, 20);

                    if (myAttackValue >
                        theirDefenseValue)
                    {
                        watchedGang.TakeZone(targetZone);
                        GangManager.instance.GiveTurfRewardToGang(watchedGang);
                    }
                }
            }
        }

        void DoInitialTakeover()
        {
            //if this gang seems to be new,
            //makes this gang instantly take up to 8 neutral territories
            //it may be short on luck and take less hahaha

            if (watchedGang.gangWeaponHashes.Count > 0 || ZoneManager.instance.GetZonesControlledByGang(watchedGang.name).Count > 2)
            {
                //we've been around for long enough to get weapons or get turf, abort
                return;
            }

            TurfZone chosenZone = ZoneManager.instance.GetRandomZone(true);

            if(chosenZone.ownerGangName == "none")
            {
                watchedGang.TakeZone(chosenZone);
                //we took one, now we should spread the influence around it
                for (int i = 0; i < 8; i++)
                {
                    TurfZone nearbyZone = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);
                    if (nearbyZone.ownerGangName == "none")
                    {
                        watchedGang.TakeZone(nearbyZone);
                        //and use this new zone as reference from now on
                        chosenZone = nearbyZone;
                    }
                }
            }
            else
            {
                //no neutral turf available, abort!
                return;
            }
        }

        public void ResetUpdateInterval()
        {
            ticksBetweenUpdates = ModOptions.instance.ticksBetweenGangAIUpdates + RandoMath.CachedRandom.Next(100);
        }

        public GangAI(Gang watchedGang)
        {
            this.watchedGang = watchedGang;
            ResetUpdateInterval();

            //have some turf for free! but only if you're new around here
            DoInitialTakeover();

            //do we have vehicles?
            if(this.watchedGang.carVariations.Count == 0)
            {
                //get some vehicles!
                for(int i = 0; i < RandoMath.CachedRandom.Next(1, 4); i++)
                {
                    PotentialGangVehicle newVeh = PotentialGangVehicle.GetCarFromPool();
                    if(newVeh != null)
                    {
                        this.watchedGang.AddGangCar(newVeh);
                    }
                }
            }
        }
    }
}
