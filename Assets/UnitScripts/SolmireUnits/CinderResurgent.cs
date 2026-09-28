using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CinderResurgent : UnitInstance
{
    List<UnitInstance> enemies = new List<UnitInstance>();
    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindNearestEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Damage,
                source = this,
                target = target,
                amount = stats.Attack,
                reason = "Cinder Resurgent Attack",
                isCrit = abilityCrit
            }
        );

        enemies = FindAllEnemies();
        foreach (UnitInstance enemy in enemies)
        {
            bool isFirstTarget = true;
            if (enemy != null && enemy != this)
            {
                CombatManager.Instance.ExecuteAction(
                    new CombatAction
                    {
                        type = CombatActionType.ApplyBurn,
                        source = this,
                        target = enemy,
                        amount = stats.Burn,
                        reason = "Cinder Resurgent Burn",
                        isCrit = abilityCrit,
                        isAoEExtraHit = !isFirstTarget
                    }
                );
                isFirstTarget = false;
            }
        }
    }
    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}. [c_burn]Burn[/c] all enemies for [BURN] {stats.Burn}.");
    }

    public override string GetPassiveDescription()
    {
        return ("When this dies, summon a Scattered Pyre.");
    }

    protected override void OnDeathEffect()
    {
        if (Definition.spawnDefinition != null)
        {
            UnitSpawner.Instance.SpawnUnit(Definition.spawnDefinition, row, col, isPlayer, this, CurrentRarity);
            Debug.Log($"Cinder Resurgent has died and summoned a Scattered Pyre at ({row},{col})");
        }
    }
}
