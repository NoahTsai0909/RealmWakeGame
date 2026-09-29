using UnityEngine;

public class Jester : UnitInstance
{
    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindRandomEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Damage,
                source = this,
                target = target,
                amount = stats.Attack,
                reason = "Jester Attack",
                isCrit = abilityCrit
            }
        );
    }

    public override void CombatStartEffect()
    {
        if (FindSideAllies().Count == 0)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = this,
                buffStat = ModifiableStats.Multicast,
                amount = 1
            });
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] a random enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"Combat start: If this has no [c_side]side[/c] allies, get +1 [MULTICAST].");
    }
}
