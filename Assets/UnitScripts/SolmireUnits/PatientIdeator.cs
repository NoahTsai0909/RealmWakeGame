using UnityEngine;

public class PatientIdeator : UnitInstance
{
    private int maxHealthBuff = 5;
    private int mutationTriggerThreshold = 6;
    private int mutationTriggerCount = 0;

    protected override void UpdateRarityModifiers()
    {
        maxHealthBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 5,
            Rarity.Rare => 10,
            Rarity.Epic => 20,
            _ => 20
        };
    }

    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);
        CombatEventBus.OnActionResolved += HandleCombatAction;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnActionResolved -= HandleCombatAction;
    }

    protected override void HandleCombatAction(CombatAction action)
    {
        if (action.source == null) return;
        if ((action.type == CombatActionType.Heal) && (action.target.isPlayer == this.isPlayer))
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = action.target,
                buffStat = ModifiableStats.MaxHP,
                amount = maxHealthBuff
            });
        }
        if (currentSuffix != null)
        {
            mutationTriggerCount++;
            if (mutationTriggerCount >= mutationTriggerThreshold)
            {
                mutationTriggerCount = 0;
                currentSuffix.ExecuteEffect(this);
            }
        }

    }

    public override string GetPassiveDescription()
    {
        return ($"When an ally is [c_heal]healed[/c], it also gains [c_maxhealth]+{maxHealthBuff}[/c] [MAXHEALTH]");
    }

    public override string GetMutationTriggerText()
    {
        return ($"<br>Every {mutationTriggerThreshold} times an ally is [c_heal]healed[/c], ");
    }
}
