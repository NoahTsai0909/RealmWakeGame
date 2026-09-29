using System.Collections.Generic;
using UnityEngine;

public class Scout : UnitInstance
{
    private int critBuff;
    List<UnitInstance> targets;

    protected override void UpdateRarityModifiers()
    {
        critBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 30,
            _ => 10
        };
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        targets = FindSideAllies();

        foreach (UnitInstance target in targets)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = target,
                buffStat = ModifiableStats.CritChance,
                amount = critBuff
            });
        }

    }

    public override string GetActiveDescription()
    {
        return ($"[c_side]Side[/c] allies gain [c_crit]{critBuff}[/c] [CRIT].");
    }
}
