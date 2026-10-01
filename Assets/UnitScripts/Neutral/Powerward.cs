using System.Collections.Generic;
using UnityEngine;

public class Powerward : UnitInstance
{

    protected override void UseAbility()
    {
        base.UseAbility();
        List<UnitInstance> targets = FindSideAllies();
        if (targets.Count == 0)
        {
            return;
        }
        int randomIndex = UnityEngine.Random.Range(0, targets.Count);
        UnitInstance sideAlly = targets[randomIndex];
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = sideAlly,
            buffStat = ModifiableStats.Attack,
            amount = sideAlly.Stats.Attack
        });
        
    }

    public override string GetActiveDescription()
    {
        return ($"Double the [ATK] of a random ally to the [c_side]side[/c] of this.");
    }
}
