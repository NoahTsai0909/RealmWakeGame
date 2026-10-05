using System.Collections.Generic;
using UnityEngine;

public class RoyalGuard : UnitInstance
{
    private int shieldModifier = 15;
    private int advanceCount = 1;
    private int mutationTriggerThreshold = 5;
    private int mutationTriggerCount = 0;

    protected override void UpdateRarityModifiers()
    {
        shieldModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 15,
            Rarity.Rare => 30,
            Rarity.Epic => 45,
            _ => 15
        };
    }

    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);
        CombatEventBus.OnActionResolved += HandleCombatAction;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnActionResolved -= HandleCombatAction;
    }

    protected override void HandleCombatAction(CombatAction action)
    {
        if (action.source == null) return;
        if (action.target != this) return;
        if (action.type == CombatActionType.Shield)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Advance,
                source = this,
                target = this,
                amount = advanceCount
            });
        }
    }
    protected override void UseAbility()
    {
        base.UseAbility();
        List<UnitInstance> targets = FindAllAllies();
        foreach (UnitInstance target in targets)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = target,
                buffStat = ModifiableStats.Shield,
                amount = shieldModifier
            });
        }
    }

    public override string GetActiveDescription()
    {
        return ($"All allies gain [c_shield]{shieldModifier}[/c] [SHIELD].");
    }
    public override string GetPassiveDescription()
    {
        return ($"When this is shielded, [c_advance]advance[/c] this {advanceCount}.");
    }
}

