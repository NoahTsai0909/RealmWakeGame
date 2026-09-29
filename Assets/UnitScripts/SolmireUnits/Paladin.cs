using System.Collections.Generic;
using UnityEngine;
using static CombatEventBus;

public class Paladin : UnitInstance 
{
    private int attackModifier = 10;

    protected override void UpdateRarityModifiers()
    {
        attackModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
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
        if ((action.type == CombatActionType.Heal) && (action.target.isPlayer == this.isPlayer))
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = this,
                buffStat = ModifiableStats.Attack,
                amount = attackModifier
            });
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindNearestEnemy();

        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.Damage,
                    source = this,
                    target = target,
                    amount = stats.Attack,
                    reason = "Paladin Attack",
                    isCrit = abilityCrit
                }
            );
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attacks[/c] the nearest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When an ally is [c_heal]healed[/c], this unit gains [c_attack]{attackModifier}[/c] [ATK].");
    }
}
