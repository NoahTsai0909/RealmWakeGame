using System.Collections.Generic;
using UnityEngine;

public class Seerward : UnitInstance
{
    private int advanceBuff = 2;
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
            type = CombatActionType.Advance,
            source = this,
            target = sideAlly,
            amount = advanceBuff,
            reason = "Seerward advance"
        });

    }

    public override string GetActiveDescription()
    {
        return ($"[c_advance]Advance[/c] a random ally to the [c_side]side[/c] of this by {advanceBuff}.");
    }
}
