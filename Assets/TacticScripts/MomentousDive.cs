using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MomentousDive : TacticInstance
{
    public int buffValue = 6;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Rare => 6,
            Rarity.Epic => 12,
            _ => 6
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
        if (action.type != CombatActionType.ApplyHaste || action.target.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = null,
            target = action.target,
            buffStat = ModifiableStats.CritChance,
            amount = buffValue
        });
    }



    public override string GetDescription()
    {
        return $"When an ally is [c_haste]hasted[/c], it gets [CRIT]{buffValue}.";
    }
}
