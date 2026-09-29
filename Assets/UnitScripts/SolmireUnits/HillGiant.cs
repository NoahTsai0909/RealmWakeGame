using UnityEngine;

public class HillGiant :UnitInstance
{
    private int maxHealthBuffPercent = 5;

    protected override void UpdateRarityModifiers()
    {
        maxHealthBuffPercent = CurrentRarity switch
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
                    reason = "HillGiant attack",
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
            amount = (GetMaxHP() * maxHealthBuffPercent / 100)
        });
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}. Gain [ATK] equal to {maxHealthBuffPercent}% of [MAXHEALTH] ({GetMaxHP() * maxHealthBuffPercent / 100}).");
    }
}
