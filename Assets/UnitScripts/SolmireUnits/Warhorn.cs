using System.Collections.Generic;
using UnityEngine;

public class Warhorn : UnitInstance
{
    int attackBuff = 20;

    protected override void UpdateRarityModifiers()
    {
        attackBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 20,
            Rarity.Rare => 40,
            Rarity.Epic => 60,
            _ => 20
        };
    }

    protected override void UseAbility()
    {

        List<UnitInstance> targets = FindAllAllies();
        foreach (UnitInstance target in targets)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = target,
                buffStat = ModifiableStats.Attack,
                amount = attackBuff
            });
        }
        base.UseAbility();
    }

    public override string GetActiveDescription()
    {
        return ($"All allies gain [c_attack]+{attackBuff}[/c] [ATK].");
    }
}
