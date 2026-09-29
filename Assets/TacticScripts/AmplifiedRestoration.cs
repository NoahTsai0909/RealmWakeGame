using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class AmplifiedRestoration : TacticInstance
{
    public int buffValue = 10;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Rare => 10,
            Rarity.Epic => 20,
            _ => 10
        };
    }

    public override void InitializeFromSaveData(RunManager.TacticSaveData data)
    {
        base.InitializeFromSaveData(data);
        buffValue = GetBuffValue(CurrentRarity);
    }

    protected override void OnTierUpgraded()
    {
        base.OnTierUpgraded();
        buffValue = GetBuffValue(CurrentRarity);
    }

    public override void EnterCombat()
    {
        base.EnterCombat();
        CombatEventBus.OnActionResolved += HandleActionResolved;
    }

    private void OnDestroy()
    {
        CombatEventBus.OnActionResolved -= HandleActionResolved;
    }

    private void HandleActionResolved(CombatAction action)
    {

        if (action.type != CombatActionType.Heal) return;
        if (action.source == null) return;
        if (action.target.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = null,
            target = action.target,
            buffStat = ModifiableStats.MaxHP,
            amount = buffValue
        });
    }



    public override string GetDescription()
    {
        return $"When an ally is [c_heal]healed[/c], it gets [MAXHEALTH] {buffValue}.";
    }
}

