using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static CombatEventBus;
using static UnityEngine.EventSystems.EventTrigger;

public class MasterOrb : UnitInstance
{
    private int energyBuff = 3;
    protected override void UpdateRarityModifiers()
    {
        energyBuff = CurrentRarity switch
        {
            Rarity.Rare => 3,
            Rarity.Epic => 4,
            _ => 3
        };
    }

    public override void RemoveAuras()
    {
        foreach (UnitInstance target in auraTargets)
        {
            if (target != null)
            {
                target.TemporaryStatModify(ModifiableStats.MaxEnergy, -energyBuff);
            }
        }
        base.RemoveAuras();
    }

    public override void ApplyAuras()
    {

        if (myGrid == null) return;

        auraTargets = FindAllAllies()?.Where(target => target != null && target.isEnergy).ToList();

        if (auraTargets == null) return;

        foreach (UnitInstance target in auraTargets)
        {
            target.TemporaryStatModify(ModifiableStats.MaxEnergy, energyBuff);
        }
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
        if (source.isEnergy == false) return;

        UnitInstance targetEnemy = FindFarthestEnemy();
        if (targetEnemy == null) return;
        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Damage,
                source = this,
                target = targetEnemy,
                amount = stats.Attack,
                reason = "Master Orb Passive",
                isPassive = true
            }
        );

        if (this != null && inCombat && currentSuffix != null)
        {
            currentSuffix.ExecuteEffect(this);
        }
    }

    public override string GetPassiveDescription()
    {
        return ($"Allies have [c_energy]+{energyBuff}[/c] [ENERGY]. When an [ENERGY] ally uses an ability, [c_attack]attack[/c] the farthest enemy by [ATK] {stats.Attack}.");
    }

    public override string GetMutationTriggerText()
    {
        return ($"<br>Also ");
    }
}
