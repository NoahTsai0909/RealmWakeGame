using UnityEngine;

public class Bloodmite : UnitInstance
{
    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindNearestEnemy();
        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Damage,
                source = this,
                target = target,
                amount = stats.Attack,
                reason = "Bloodmite Attack",
                isCrit = abilityCrit,
                isLifesteal = true
            });
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_lifesteal]Lifesteal[/c]. \n[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}.");
    }
}