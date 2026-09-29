using UnityEngine;

public class BluntStrike : TacticInstance
{
    public int buffValue = 5;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => 5,
            Rarity.Rare => 10,
            Rarity.Epic => 20,
            _ => 5
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
        if (action.type != CombatActionType.Shield || action.target.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;

        auraTargets = FindAllAllies();

        if (auraTargets == null) return;

        foreach (UnitInstance target in auraTargets)
        {
            if (target != null)
            {
                CombatManager.Instance.ExecuteAction(new CombatAction
                {
                    type = CombatActionType.Buff,
                    source = null,
                    target = target,
                    buffStat = ModifiableStats.Attack,
                    amount = buffValue
                });
            }
        }
    }



    public override string GetDescription()
    {
        return $"When an ally is [c_shield]shielded[/c], all allies get [ATK]{buffValue}.";
    }
}


