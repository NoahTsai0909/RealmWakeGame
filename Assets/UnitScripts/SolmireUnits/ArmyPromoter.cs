using System.Linq;
using UnityEngine;

public class ArmyPromoter : UnitInstance
{
    private int attackBuff = 10;
    protected override void UpdateRarityModifiers()
    {
        attackBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
        };
    }
    public override string GetPassiveDescription()
    {
        return ($"At the start of each [c_day]day[/c], summon a Clumsy Knight, then give all Clumsy Knights [c_attack]+{attackBuff}[/c] [ATK].");
    }

    public override string GetMutationTriggerText()
    {
        return ($"When this summons a Clumsy Knight, it gains the [c_mutation]mutation[/c] of this. ");
    }

    public override void OnDayStart(UnitSaveData mySaveData)
    {
        UnitDefinition targetDef = mySaveData.definition.spawnDefinition;
        if (targetDef == null) return;
        int attackBuff = mySaveData.rarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
        };

        if (PlayerUnitManager.Instance != null)
        {
            PlayerUnitManager.Instance.TryAcquireUnit(targetDef, mySaveData.rarity, mySaveData.prefix, mySaveData.suffix);
        }

        var allOwned = RunManager.Instance.playerTeamPlacements.Concat(RunManager.Instance.playerBenchPlacements);
        foreach (var placement in allOwned)
        {
            if (placement != null && placement.unitData != null && placement.unitData.definition == targetDef)
            {
                PermanentStats permStats = RunManager.Instance.GetPermanentStatsForUnit(placement.unitData.id);
                permStats.bonusAttack += attackBuff;
            }
        }
    }
}
