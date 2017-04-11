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

        private List<TurfZone> myZones;

        public int ticksSinceLastFightWithPlayer = 0;

        public override void Update()
        {
            if (!GangWarManager.instance.isOccurring)
            {
                ticksSinceLastFightWithPlayer++;
            }
            
            //everyone tries to expand before anything else;
            //that way, we don't end up with isolated gangs or some kind of peace
            myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);
            myZones.Sort(ZoneManager.CompareZonesByValue);
            TryExpand();

            switch (watchedGang.upgradeTendency)
            {
                case Gang.AIUpgradeTendency.bigGuns:
                    TryUpgradeGuns();
                    TryUpgradeGuns(); //yeah, we like guns
                    TryUpgradeZones();
                    if(RandoMath.RandomBool()) TryUpgradeMembers(); //but we're not buff
                    break;
                case Gang.AIUpgradeTendency.moreExpansion:
                    TryExpand();
                    TryExpand(); //that's some serious expansion
                    TryUpgradeGuns();
                    TryUpgradeMembers();
                    if (RandoMath.RandomBool()) TryUpgradeZones(); //...but zone strength isn't our priority
                    break;
                case Gang.AIUpgradeTendency.toughMembers:
                    TryUpgradeMembers();
                    TryUpgradeMembers();
                    TryUpgradeGuns();
                    if (RandoMath.RandomBool()) TryUpgradeZones(); //tough members, but tend to be few in number
                    break;
                case Gang.AIUpgradeTendency.toughTurf:
                    TryUpgradeZones();
                    TryUpgradeZones(); //lots of defenders!
                    TryUpgradeMembers();
                    if (RandoMath.RandomBool()) TryUpgradeGuns(); //...with below average guns
                    break;
            }

            //lets check our financial situation:
            //are we running very low on cash (unable to take even a neutral territory)?
            //do we have any turf? is any war going on?
            //if not, we no longer exist
            if(watchedGang.moneyAvailable < ModOptions.instance.baseCostToTakeTurf)
            {
                if (!GangWarManager.instance.isOccurring)
                {
                    myZones = ZoneManager.instance.GetZonesControlledByGang(watchedGang.name);
                    if (myZones.Count == 0)
                    {
                        if (ModOptions.instance.gangsCanBeWipedOut)
                        {
                            GangManager.instance.KillGang(this);
                        }
                        else
                        {
                            //we get some money then, at least to keep trying to fight
                            watchedGang.moneyAvailable += ModOptions.instance.baseCostToTakeTurf * 5;
                        }
                        
                    }
                }
            }
            
        }

        void TryExpand()
        {
            //lets attack!
            //pick a random zone owned by us, get the closest hostile zone and attempt to take it

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
        }

        void TryUpgradeGuns()
        {
            //try to buy the weapons we like
            if (watchedGang.preferredWeaponHashes.Count == 0)
            {
                watchedGang.SetPreferredWeapons();
            }

            WeaponHash chosenWeapon = RandoMath.GetRandomElementFromList(watchedGang.preferredWeaponHashes);

            if (!watchedGang.gangWeaponHashes.Contains(chosenWeapon))
            {
                if (watchedGang.moneyAvailable >= ModOptions.instance.GetBuyableWeaponByHash(chosenWeapon).price)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.GetBuyableWeaponByHash(chosenWeapon).price;
                    watchedGang.gangWeaponHashes.Add(chosenWeapon);
                    watchedGang.gangWeaponHashes.Sort(watchedGang.CompareGunsByPrice);
                    GangManager.instance.SaveGangData(false);
                }
            }
        }

        void TryUpgradeMembers()
        {
            //since we've got some extra cash, lets upgrade our members!
            switch (RandoMath.CachedRandom.Next(3))
            {
                case 0: //accuracy!
                        //wait for the player here. we don't want enemies to start spawnkilling the protagonist, 
                        //so invest on this only if the player also did
                    if (watchedGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy &&
                watchedGang.moneyAvailable >= GangManager.CalculateAccuracyUpgradeCost(watchedGang.memberAccuracyLevel) &&
                watchedGang.memberAccuracyLevel <= GangManager.instance.PlayerGang.memberAccuracyLevel + 20)
                    {
                        watchedGang.moneyAvailable -= GangManager.CalculateAccuracyUpgradeCost(watchedGang.memberAccuracyLevel);
                        watchedGang.memberAccuracyLevel += 10;
                        if (watchedGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy)
                        {
                            watchedGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
                        }

                        GangManager.instance.SaveGangData(false);
                    }
                    break;
                case 1: //armor!
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
                    break;
                
                default: //health!
                    if (watchedGang.memberHealth < ModOptions.instance.maxGangMemberHealth &&
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
                    break;
            }
            
        }

        void TryUpgradeZones()
        {
            //upgrade the whole gang strength if possible!
            //lets not get more upgrades here than the player. it may get too hard for the player to catch up otherwise
            if (watchedGang.moneyAvailable >= GangManager.CalculateGangValueUpgradeCost(watchedGang.baseTurfValue) &&
                watchedGang.baseTurfValue <= GangManager.instance.PlayerGang.baseTurfValue - 1)
            {
                watchedGang.moneyAvailable -= GangManager.CalculateGangValueUpgradeCost(watchedGang.baseTurfValue);
                watchedGang.baseTurfValue++;
                GangManager.instance.SaveGangData(false);
                return;
            }
            //if we have enough money to upgrade a zone,
            //try upgrading our toughest zone... or one that we can afford upgrading
            int lastCheckedValue = ModOptions.instance.maxTurfValue;
            for(int i = 0; i < myZones.Count; i++)
            {
                if (myZones[i].value == lastCheckedValue) continue; //we already know we can't afford upgrading from this turf level

                if (watchedGang.moneyAvailable >= GangManager.CalculateTurfValueUpgradeCost(myZones[i].value))
                {
                    watchedGang.moneyAvailable -= GangManager.CalculateTurfValueUpgradeCost(myZones[i].value);
                    myZones[i].value++;
                    ZoneManager.instance.SaveZoneData(false);
                    return;
                }
                else
                {
                    lastCheckedValue = myZones[i].value;
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
                if (watchedGang.moneyAvailable >= ModOptions.instance.baseCostToTakeTurf / 10)
                {
                    watchedGang.moneyAvailable -= ModOptions.instance.baseCostToTakeTurf / 10;
                    watchedGang.TakeZone(targetZone);
                }
            }
            else {
                Gang ownerGang = GangManager.instance.GetGangByName(targetZone.ownerGangName);

                if (ownerGang == null)
                {
                    ZoneManager.instance.GiveGangZonesToAnother(targetZone.ownerGangName, "none");
                    //this zone was controlled by a gang that no longer exists. it is neutral now
                    if (watchedGang.moneyAvailable >= ModOptions.instance.baseCostToTakeTurf / 10)
                    {
                        watchedGang.moneyAvailable -= ModOptions.instance.baseCostToTakeTurf / 10;
                        watchedGang.TakeZone(targetZone);
                    }
                }
                else
                {
                    if(GangWarManager.instance.isOccurring && GangWarManager.instance.warZone == targetZone)
                    {
                        //don't mess with this zone then, it's a warzone
                        return;
                    }
                    //we check how well defended this zone is,
                    //then figure out how large our attack should be.
                    //if we can afford that attack, we do it
                    int defenderStrength = GangManager.CalculateDefenderStrength(ownerGang, targetZone);
                    GangWarManager.attackStrength requiredStrength = GangManager.CalculateRequiredAttackStrength(watchedGang, defenderStrength);
                    int atkCost = GangManager.CalculateAttackCost(watchedGang, requiredStrength);
                    if (watchedGang.moneyAvailable >= atkCost){
                        if (targetZone.ownerGangName == GangManager.instance.PlayerGang.name)
                        {
                            if (ModOptions.instance.fightingEnabled &&
                            ModOptions.instance.warAgainstPlayerEnabled &&
                            ticksSinceLastFightWithPlayer > ModOptions.instance.minGangAITicksBetweenBattlesWithSameGang)
                            {
                                //the player may be in big trouble now
                                watchedGang.moneyAvailable -= atkCost;
                                GangWarManager.instance.StartWar(watchedGang, targetZone, GangWarManager.warType.defendingFromEnemy, requiredStrength);
                            }
                        }
                        else
                        {
                            watchedGang.moneyAvailable -= atkCost;
                            //roll dices... favor the defenders a little here
                            if (atkCost / ModOptions.instance.baseCostToTakeTurf * RandoMath.CachedRandom.Next(1, 20) >
                                defenderStrength / RandoMath.CachedRandom.Next(1, 15))
                            {
                                watchedGang.TakeZone(targetZone);
                                watchedGang.moneyAvailable += (int) (GangManager.CalculateBattleRewards(ownerGang, true) *
                                    ModOptions.instance.extraProfitForAIGangsFactor);
                            }
                            else
                            {
                                ownerGang.moneyAvailable += (int) (GangManager.CalculateBattleRewards(watchedGang, false) *
                                    ModOptions.instance.extraProfitForAIGangsFactor);
                            }

                        }
                    }else if(myZones.Count == 0)
                    {
                        //if we're out of turf and cant afford a decent attack, lets just attack anyway
                        //we use a light attack and do it even if that means our money gets negative.
                        //this should make gangs get back in the game or be wiped out instead of just staying away
                        atkCost = GangManager.CalculateAttackCost(watchedGang, GangWarManager.attackStrength.light);
                        if (targetZone.ownerGangName == GangManager.instance.PlayerGang.name)
                        {
                            if (ModOptions.instance.fightingEnabled &&
                            ModOptions.instance.warAgainstPlayerEnabled &&
                            ticksSinceLastFightWithPlayer > ModOptions.instance.minGangAITicksBetweenBattlesWithSameGang)
                            {
                                //the player may be in big trouble now
                                watchedGang.moneyAvailable -= atkCost;
                                GangWarManager.instance.StartWar(watchedGang, targetZone, GangWarManager.warType.defendingFromEnemy, requiredStrength);
                            }
                        }
                        else
                        {
                            watchedGang.moneyAvailable -= atkCost;
                            //roll dices... favor the defenders a little here
                            if (atkCost / ModOptions.instance.baseCostToTakeTurf * RandoMath.CachedRandom.Next(1, 20) >
                                defenderStrength / RandoMath.CachedRandom.Next(1, 15))
                            {
                                watchedGang.TakeZone(targetZone);
                                watchedGang.moneyAvailable += (int)(GangManager.CalculateBattleRewards(ownerGang, true) *
                                    ModOptions.instance.extraProfitForAIGangsFactor);
                            }
                            else
                            {
                                ownerGang.moneyAvailable += (int)(GangManager.CalculateBattleRewards(watchedGang, false) *
                                    ModOptions.instance.extraProfitForAIGangsFactor);
                            }
                        }
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
                watchedGang.TakeZone(chosenZone, false);
                //we took one, now we should spread the influence around it
                for (int i = 0; i < 8; i++)
                {
                    TurfZone nearbyZone = ZoneManager.instance.GetClosestZoneToTargetZone(chosenZone, true);
                    if (nearbyZone.ownerGangName == "none")
                    {
                        watchedGang.TakeZone(nearbyZone, false);
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
