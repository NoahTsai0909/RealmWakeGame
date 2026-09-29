using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EssenceOfLife : TacticInstance
{
    public int buffValue = 2;

    private int GetBuffValue(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => 2,
            Rarity.Rare => 4,
            Rarity.Epic => 8,
            _ => 2
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

        if (action.type != CombatActionType.Heal && action.type != CombatActionType.Shield) return;
        if (action.source == null) return;
        if (action.source.isPlayer != this.isPlayer) return;
        if (allyGrid == null) return;
        if (action.type == CombatActionType.Heal)
        {
            List<UnitInstance> allies = FindAllAllies();
            List<UnitInstance> shieldAllies = allies.Where(unit => unit != null && unit.Definition != null && unit.Definition.tagFlags.HasFlag(UnitTagFlags.Shield)).ToList();
            if (shieldAllies.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, shieldAllies.Count);
                UnitInstance target = shieldAllies[randomIndex];
                CombatManager.Instance.ExecuteAction(new CombatAction
                {
                    type = CombatActionType.Buff,
                    source = null,
                    target = target,
                    buffStat = ModifiableStats.Shield,
                    amount = buffValue
                });
            }
        }
        else if (action.type == CombatActionType.Shield)
        {
            List<UnitInstance> allies = FindAllAllies();
            List<UnitInstance> healAllies = allies.Where(unit => unit != null && unit.Definition != null && unit.Definition.tagFlags.HasFlag(UnitTagFlags.Heal)).ToList();
            if (healAllies.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, healAllies.Count);
                UnitInstance target = healAllies[randomIndex];
                CombatManager.Instance.ExecuteAction(new CombatAction
                {
                    type = CombatActionType.Buff,
                    source = null,
                    target = target,
                    buffStat = ModifiableStats.Heal,
                    amount = buffValue
                });
            }
        }
    }



    public override string GetDescription()
    {
        return $"When an ally [c_heal]heals[/c], a random ally gets [SHIELD] {buffValue}. When an ally [c_shield]shields[/c], a random ally gets [HEAL] {buffValue}.";
    }
}