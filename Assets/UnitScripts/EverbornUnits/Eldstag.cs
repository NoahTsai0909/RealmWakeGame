using UnityEngine;

public class Eldstag : UnitInstance
{
    private int advanceAmount = 2;
    private int multicastBuff = 0;
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
                reason = "Eldstag Attack",
                isCrit = abilityCrit
            }
        );
        }
        target = FindRandomAlly();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Advance,
                source = this,
                target = target,
                amount = advanceAmount,
                reason = "Eldstag Advance"
            }
        );
    }

    public override void ApplyAuras()
    {

        if (myGrid == null) return;

        auraTargets = FindAdjacentAllies();

        if (auraTargets == null) return;
        foreach (UnitInstance target in auraTargets)
        {
            if (target != null && target.Definition != null)
            {
                if (target.Definition.tagFlags.HasFlag(UnitTagFlags.Beast))
                {
                    multicastBuff += 1;
                }
            }
        }
        this.TemporaryStatModify(ModifiableStats.Multicast, multicastBuff);
    }

    public override void RemoveAuras()
    {
        this.TemporaryStatModify(ModifiableStats.Multicast, -multicastBuff);
        multicastBuff = 0;
        base.RemoveAuras();
    }


    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}. [c_advance]Advance[/c] a random ally by {advanceAmount}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"This has +[c_multicast]multicast[/c] [MULTICAST] for each [c_adjacent]adjacent[/c] Beast.");
    }
}
