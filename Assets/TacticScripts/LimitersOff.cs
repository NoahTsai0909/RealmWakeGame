using UnityEngine;

public class LimitersOff : TacticInstance
{
    public int buffValue = 10;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
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
        if (action.source.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;
        if (action.source.isEnergy)
        {
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
    }



    public override string GetDescription()
    {
        return $"When an ally uses [ENERGY], all allies get [ATK]{buffValue}.";
    }
}

