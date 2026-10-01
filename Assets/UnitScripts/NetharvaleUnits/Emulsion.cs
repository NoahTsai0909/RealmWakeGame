using UnityEngine;
using static CombatEventBus;

public class Emulsion : UnitInstance
{
    private int multicastBuff = 0;
    public override string GetActiveDescription()
    {
        return ($"[c_poison]Poison[/c] a random enemy for [POISON] {stats.Poison}. [c_slow]Slow[/c] a random enemy for [SLOW] {stats.Slow}.");
    }
    public override string GetPassiveDescription()
    {
        return ($"This has +[c_multicast]multicast[/c] [MULTICAST] for each ally.");
    }

    public override void RemoveAuras()
    {
        this.TemporaryStatModify(ModifiableStats.Multicast, -multicastBuff);
        multicastBuff = 0;
        base.RemoveAuras(); 
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindRandomEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.ApplyPoison,
                source = this,
                target = target,
                amount = stats.Poison,
                reason = "Emulsion Poison",
                isCrit = abilityCrit
            }
        );
        target = FindRandomEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.ApplySlow,
                source = this,
                target = target,
                amount = stats.Slow,
                reason = "Emulsion Slow"
            }
        );
    }

    public override void ApplyAuras()
    {

        if (myGrid == null) return;

        multicastBuff = FindAllAllies().Count;

        this.TemporaryStatModify(ModifiableStats.Multicast, multicastBuff);
    }
}
