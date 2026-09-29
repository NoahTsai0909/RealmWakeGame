using UnityEngine;

public class Swiftswipe : UnitInstance
{
    private int attackModifier = 5;
    protected override void UpdateRarityModifiers()
    {
        attackModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 5,
            Rarity.Rare => 10,
            Rarity.Epic => 20,
            _ => 5
        };
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
                    reason = "Swiftswipe Attack",
                    isCrit = abilityCrit
                }
            );
        }
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.Attack,
            amount = attackModifier
        });
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attacks[/c] the nearest enemy for [ATK] {stats.Attack}, then gains [c_attack]{attackModifier}[/c] [ATK].");
    }
}
