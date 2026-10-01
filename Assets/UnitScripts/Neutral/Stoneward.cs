using UnityEngine;

public class Stoneward : UnitInstance
{
    private int advanceBuff = 1;

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
        if ((action.source.isPlayer != isPlayer) && (action.target == this))
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Advance,
                source = this,
                target = this,
                amount = advanceBuff,
                reason = "Stoneward advance"
            });
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Shield,
            source = this,
            target = this,
            amount = stats.Shield,
            reason = "Stoneward Shield",
            isCrit = abilityCrit
        });
        UnitInstance targetBehind = GetUnitBehind();
        if (targetBehind != null)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Shield,
                source = this,
                target = targetBehind,
                amount = stats.Shield,
                reason = "Stoneward Shield",
                isCrit = abilityCrit
            });
        }
    }


    private UnitInstance GetUnitBehind()
    {
        if (myGrid == null) return null;
        int behindCol = isPlayer ? (col - 1) : (col + 1);
        return myGrid.GetUnitAt(row, behindCol);
    }

    public override string GetActiveDescription()
    {
        return ($"[c_shield]Shield[/c] this and the unit behind this for [SHIELD] {stats.Shield}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this is targeted by an enemy, [c_advance]advance[/c] this by {advanceBuff}.");
    }
}
