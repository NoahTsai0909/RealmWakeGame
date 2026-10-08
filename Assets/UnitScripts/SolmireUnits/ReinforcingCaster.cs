using System.Collections.Generic;
using UnityEngine;
using static CombatEventBus;

public class ReinforcingCaster : UnitInstance
{
    private int multicastBuff = 0;

    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);
        CombatEventBus.OnCombatEvent += HandleCombatEvent;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    protected override void HandleCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (this == null || !inCombat || isDead) return;
        if (type == CombatEventBus.CombatEventType.EnergyUsed && target == this)
        {
            RemoveAuras();
            ApplyAuras();
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindRandomAlly();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Shield,
                source = this,
                target = target,
                amount = stats.Shield,
                reason = "Reinforcing Caster Shield",
                isCrit = abilityCrit
            }
        );
    }

    public override string GetActiveDescription()
    {
        return ($"[c_shield]Shield[/c] a random ally for [SHIELD] {stats.Shield}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"This has [MULTICAST] equal to current [ENERGY].");
    }

    public override void RemoveAuras()
    {
        TemporaryStatModify(ModifiableStats.Multicast, -multicastBuff);
        multicastBuff = 0;
        base.RemoveAuras();
    }

    public override void ApplyAuras()
    {

        if (myGrid == null) return;
        multicastBuff = currentEnergy;
        TemporaryStatModify(ModifiableStats.Multicast, multicastBuff);
        base.ApplyAuras();

    }

}
