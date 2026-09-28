using UnityEngine;
using static CombatEventBus;

public class Belcher : UnitInstance
{
    private int mutationTriggerCount = 0;
    private int mutationTriggerThreshold = 2;

    public override string GetMutationTriggerText()
    {
        return ($"<br>Every {mutationTriggerThreshold} times a [c_side]side[/c] ally uses an ability, ");
    }
    public override string GetPassiveDescription()
    {
        return ($"When a [c_side]side[/c] ally uses an ability, [c_poison]Poison[/c] a random enemy for [POISON] {stats.Poison}.");
    }


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
        if (type != CombatEventType.AbilityUsed) return;
        if (source.isPlayer != this.isPlayer) return;
        if (source.col != this.col) return;
        if (source.row != this.row - 1 && source.row != this.row + 1) return;
        UnitInstance enemy = FindRandomEnemy();
        if (enemy == null) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.ApplyPoison,
            source = this,
            target = enemy,
            amount = stats.Poison,
            reason = "Belcher Poison"
        });
        if (currentSuffix != null)
        {
            mutationTriggerCount++;
            if (mutationTriggerCount == mutationTriggerThreshold)
            {
                mutationTriggerCount = 0;
                currentSuffix.ExecuteEffect(this);
            }
        }
    }
}
