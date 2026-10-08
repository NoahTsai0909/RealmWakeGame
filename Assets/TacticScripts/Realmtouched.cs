using System.Collections.Generic;
using UnityEngine;

public class Realmtouched : TacticInstance
{
    private int maxHealthBuffPercent = 20;


    public override void CombatStartEffect()
    {
        List<UnitInstance> allies = FindAllAllies();
        foreach (UnitInstance ally in allies)
        {
            int originalMaxHP = ally.GetCurrentHP();
            int buffAmount = (originalMaxHP * maxHealthBuffPercent) / 100;
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = null,
                target = ally,
                buffStat = ModifiableStats.MaxHP,
                amount = buffAmount
            });
        }
    }

    public override string GetDescription()
    {
        return ($"Combat Start: All allies gain [c_maxhealth]+{maxHealthBuffPercent}%[/c] [MAXHEALTH].");
    }
}