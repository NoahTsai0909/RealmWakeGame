using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SearingHalo : UnitInstance
{
    List<UnitInstance> enemies = new List<UnitInstance>();

    private int advanceCount = 1;
    private int burnBuff = 2;

    protected override void UpdateRarityModifiers()
    {
        burnBuff = CurrentRarity switch
        {
            Rarity.Rare => 2,
            Rarity.Epic => 4,
            _ => 2
        };
    }

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
        if (action.type != CombatActionType.Heal || action.target != this) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Advance,
                source = this,
                target = this,
                amount = advanceCount,
                reason = "Searing Halo Passive"
            }
        );

        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.Burn,
            amount = burnBuff
        });
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        enemies = FindAllEnemies();
        foreach (UnitInstance enemy in enemies)
        {
            if (enemy != null && enemy != this)
            {
                CombatManager.Instance.ExecuteAction(
                    new CombatAction
                    {
                        type = CombatActionType.ApplyBurn,
                        source = this,
                        target = enemy,
                        amount = stats.Burn,
                        reason = "Searing Halo Burn",
                        isCrit = abilityCrit
                    }
                );
            }
        }

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Heal,
                source = this,
                target = this,
                amount = stats.Heal,
                reason = "Searing Halo Heal",
                isCrit = abilityCrit
            }
        );
    }
    public override string GetActiveDescription()
    {
        return ($"[c_burn]Burn[/c] all enemies for [BURN] {stats.Burn}. [c_heal]Heal[/c] this for [HEAL] {stats.Heal}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this is [c_heal]healed[/c], advance this 1 second and gain +{burnBuff} [BURN].");
    }
}
