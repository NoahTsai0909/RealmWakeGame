using UnityEngine;

[CreateAssetMenu(menuName = "Outcomes/Gain Specific Unit")]
public class GainSpecificUnitOutcomeSO : EventOutcomeSO
{
    [Tooltip("Fallback definition if the event choice doesn't provide a preview unit")]
    public UnitDefinition fallbackUnit;

    public override void ExecuteOutcome(EventContext context)
    {
        // 1. If the UI generated a specific preview, give them EXACTLY what they saw!
        if (context != null && context.generatedUnit != null)
        {
            PlayerUnitManager.Instance.TryAcquireUnit(context.generatedUnit.definition, context.generatedUnit.rarity);
        }
        // 2. Fallback logic if there was no preview unit assigned to the button
        else if (fallbackUnit != null)
        {
            int day = RunManager.Instance.Stats.CurrentDay;
            DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day, RunManager.Instance.TOTAL_DAYS);
            Rarity rolledRarity = RarityDistributionTable.RollRarity(dist);
            PlayerUnitManager.Instance.TryAcquireUnit(fallbackUnit, rolledRarity);
        }
    }
}
