using System.Collections.Generic;
using UnityEngine;

public class Pulseward : UnitInstance
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
        // Unsubscribe to prevent memory leaks
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    protected override void HandleCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (this == null || !inCombat || isDead) return;
        if (type == CombatEventBus.CombatEventType.AbilityUsed)
        {
            if (auraTargets.Contains(source))
            {
                List<UnitInstance> targets = FindAllAllies();
                bool isFirstTarget = true;
                foreach (UnitInstance healtarget in targets)
                {
                    CombatManager.Instance.ExecuteAction(
                    new CombatAction
                    {
                        type = CombatActionType.Heal,
                        source = this,
                        target = healtarget,
                        amount = stats.Heal,
                        reason = "Pulseward Heal",
                        isAoEExtraHit = !isFirstTarget
                    }
                    );
                    isFirstTarget = false;
                }
                if (this.currentSuffix != null)
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
    }

    public override void ApplyAuras()
    {

        if (myGrid == null) return;
        UnitInstance unitInFront = GetUnitInFront();
        if (unitInFront != null)
        {
            auraTargets.Add(unitInFront);
        }
    }

    private UnitInstance GetUnitInFront()
    {
        if (myGrid == null) return null;
        int behindCol = isPlayer ? (col + 1) : (col - 1);
        return myGrid.GetUnitAt(row, behindCol);
    }

    public override string GetPassiveDescription()
    {
        return ($"When the unit in front of this uses an ability, [c_heal]heal[/c] all allies for [HEAL] {stats.Heal}.");
    }
}
