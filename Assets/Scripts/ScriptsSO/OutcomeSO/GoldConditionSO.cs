using UnityEngine;

[CreateAssetMenu(menuName = "Events/Conditions/Gold Condition")]
public class GoldConditionSO : ChoiceConditionSO
{
    public int requiredGold;

    public override bool IsMet()
    {
        return RunManager.Instance.Stats.CurrentGold >= requiredGold;
    }

    public override string GetRequirementText()
    {
        return $"Requires {requiredGold} [GOLD]";
    }
}
