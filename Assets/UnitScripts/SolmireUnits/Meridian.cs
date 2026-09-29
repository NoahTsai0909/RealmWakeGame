using UnityEngine;

public class Meridian : UnitInstance
{
    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);

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
        if (action.isCrit != true) return;
        if (action.isAoEExtraHit) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.Multicast,
            amount = 1
        });
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindFarthestEnemy();

        if (target == null)
        {
            return;
        }

        CombatManager.Instance.ExecuteAction(
        new CombatAction
        {
            type = CombatActionType.Damage,
            source = this,
            target = target,
            amount = stats.Attack,
            reason = "Meridian Attack",
            isCrit = abilityCrit
        }
        );
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the farthest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ("When an ally [c_crit]crits[/c], this gains [c_multicast]1[/c] [MULTICAST].");
    }


}
