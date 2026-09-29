using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class FaerieFlare : UnitInstance
{
    protected override void UseAbility()
    {
        base.UseAbility();
        if (stats.Burn >= 20)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = this,
                buffStat = ModifiableStats.Multicast,
                amount = 1
            });
        }
        UnitInstance target = FindNearestEnemy();

        if (target == null)
        {
            return;
        }

        CombatManager.Instance.ExecuteAction(
        new CombatAction
        {
            type = CombatActionType.ApplyBurn,
            source = this,
            target = target,
            amount = stats.Burn,
            reason = "Faerie Flare Burn",
            isCrit = abilityCrit
        }
        );
    }

    public override string GetActiveDescription()
    {
        return ($"If this has at least [c_burn]20[/c] [BURN], gain [c_multicast]1[/c] [MULTICAST]. [c_burn]Burn[/c] the nearest enemy for [BURN] {stats.Burn}.");
    }
}
