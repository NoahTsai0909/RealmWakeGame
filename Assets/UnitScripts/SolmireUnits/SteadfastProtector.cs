using UnityEngine;
using static CombatEventBus;

public class SteadfastProtector : UnitInstance
{
    private int mutationTriggerCount = 0;
    private int mutationTriggerThreshold = 3;

    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);

        CombatEventBus.OnCombatEvent += HandleCombatEvent;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    protected override void HandleCombatEvent(CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (source == null) return;
        if (type != CombatEventType.AbilityUsed) return;
        if (source.isPlayer != this.isPlayer) return;
        CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.Shield,
                    source = this,
                    target = source,
                    amount = stats.Shield,
                    reason = "Steadfast Protector Passive",
                    isPassive = true
                }
            );

        if (this != null && inCombat && currentSuffix != null)
        {
            mutationTriggerCount++;
            if (mutationTriggerCount == mutationTriggerThreshold)
            {
                mutationTriggerCount = 0;
                currentSuffix.ExecuteEffect(this);
            }
        }
    }

    public override string GetPassiveDescription()
    {
        return ($"When an ally uses an ability, [c_shield]shield[/c] it for [SHIELD] {stats.Shield}.");
    }

    public override string GetMutationTriggerText()
    {
        return ($"<br>Every {mutationTriggerThreshold} times an ally uses an ability, ");
    }

}
