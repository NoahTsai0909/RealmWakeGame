using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Mucus : UnitInstance
{
    private int critModifier = 10;
    private int slowCount = 2;

    protected override void UpdateRarityModifiers()
    {
        critModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 30,
            _ => 30
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
        if ((action.type == CombatActionType.ApplySlow) && (action.target.isPlayer != this.isPlayer))
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = this,
                buffStat = ModifiableStats.CritChance,
                amount = critModifier
            });
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindRandomEnemy();

        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.ApplyPoison,
                    source = this,
                    target = target,
                    amount = stats.Poison,
                    reason = "Mucus Poison",
                    isCrit = abilityCrit
                }
            );
        }

        for (int i = 0; i < slowCount; i++)
        {
            UnitInstance slowTarget = FindRandomEnemy();

            if (target != null)
            {
                CombatManager.Instance.ExecuteAction(
                    new CombatAction
                    {
                        type = CombatActionType.ApplySlow,
                        source = this,
                        target = slowTarget,
                        amount = stats.Slow,
                        reason = "Mucus Slow"
                    }
                );
            }
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_poison]Poison[/c] a random enemy for [POISON] {stats.Poison}. [c_slow]Slow[/c] {slowCount} random enemies for [SLOW] {stats.Slow}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When an enemy is applied [c_slow]slow[/c], this gains [c_crit]{critModifier}[/c] [CRIT].");
    }
}
