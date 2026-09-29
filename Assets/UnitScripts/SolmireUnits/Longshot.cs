using UnityEngine;

public class Longshot : UnitInstance
{
    private int critModifier = 10;
    protected override void UpdateRarityModifiers()
    {
        critModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
        };
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindFarthestEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
        new CombatAction
        {
            type = CombatActionType.Damage,
            source = this,
            target = target,
            amount = stats.Attack,
            reason = "Longshot Attack",
            isCrit = abilityCrit
            }
        );
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.CritChance,
            amount = critModifier
        });

    }
    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the farthest enemy for [ATK] {stats.Attack}. Gain [c_crit]{critModifier}[/c] [CRIT].");
    }

}
