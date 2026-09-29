using UnityEngine;

public class SkilledBandaging : TacticInstance
{
    public int buffValue = 3;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => 3,
            Rarity.Rare => 6,
            Rarity.Epic => 9,
            _ => 3
        };
    }

    public override void InitializeFromSaveData(RunManager.TacticSaveData data)
    {
        base.InitializeFromSaveData(data);
        buffValue = GetBuffValue(CurrentRarity);
    }

    protected override void OnTierUpgraded()
    {
        base.OnTierUpgraded();
        buffValue = GetBuffValue(CurrentRarity);
    }

    public override void EnterCombat()
    {
        base.EnterCombat();
        CombatEventBus.OnActionResolved += HandleActionResolved;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnActionResolved -= HandleActionResolved;
    }

    private void HandleActionResolved(CombatAction action)
    {
        if (action.source == null) return;
        if (action.type != CombatActionType.Heal || action.source.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = null,
            target = action.source,
            buffStat = ModifiableStats.CritChance,
            amount = buffValue
        });
    }



    public override string GetDescription()
    {
        return $"When an ally [c_heal]heals[/c], it gets [CRIT]{buffValue}.";
    }
}

