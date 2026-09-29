using System.Collections.Generic;
using UnityEngine;

public class Ranger : UnitInstance
{
    private int critBuff;

    protected override void UpdateRarityModifiers()
    {
        critBuff = CurrentRarity switch
        {
            Rarity.Common => 25,
            Rarity.Uncommon => 50,
            Rarity.Rare => 75,
            Rarity.Epic => 100,
            _ => 25
        };
    }


    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindFarthestEnemy();

        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.Damage,
                    source = this,
                    target = target,    
                    amount = stats.Attack,
                    reason = "Ranger attack",
                    isCrit = abilityCrit
                }
            );
            Debug.Log($"{unitName} attacks {target.unitName} for {stats.Attack}!");

        }
        else
        {
            Debug.Log("No enemy found to attack!");
        }
    }

    public override void CombatStartEffect()
    {
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.CritChance,
            amount = critBuff
        });
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the farthest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"Combat Start: Gain [c_crit]{critBuff}[/c] [CRIT].");
    }
}
