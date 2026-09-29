using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SludgeVortex : UnitInstance
{
    private int enemyCount = 3;
    private int attackBuff = 15;

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
        if (action.target.isPlayer == this.isPlayer) return;
        if (action.type != CombatActionType.ApplySlow) return;
        if (action.isAoEExtraHit) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.Attack,
            amount = attackBuff
        });
    }


    protected override void UpdateRarityModifiers()
    {
        attackBuff = CurrentRarity switch
        {
            Rarity.Rare => 15,
            Rarity.Epic => 30,
            _ => 15
        };
    }

    protected override void UseAbility()
    {
        base.UseAbility();

        List<UnitInstance> targets = FindNearestEnemies(enemyCount);

        // If no enemies are left, stop here
        if (targets.Count == 0) return;

        // Loop through the list and execute the attack on each one
        foreach (UnitInstance target in targets)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.Damage,
                    source = this,
                    target = target,
                    amount = stats.Attack,
                    reason = "Sludge Vortex Attack",
                    isCrit = abilityCrit
                }
            );
        }

        foreach (UnitInstance target in targets)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.ApplySlow,
                    source = this,
                    target = target,
                    amount = stats.Slow,
                    reason = "Sludge Vortex Slow"
                }
            );
        }
    }

    public override string GetActiveDescription()
    {
        return $"[c_attack]Attack[/c] up to {enemyCount} nearest enemies for [ATK] {stats.Attack}. [c_slow]Slow[/c] each for [SLOW] {stats.Slow}.";
    }

    public override string GetPassiveDescription()
    {
        return $"When an enemy is [c_slow]slowed[/c], this gains [c_attack]{attackBuff}[/c] [ATK].";
    }
}
