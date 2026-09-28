using UnityEngine;

public class Farmer : UnitInstance
{
    private int goldReward = 1;

    protected override void UpdateRarityModifiers()
    {
        goldReward = CurrentRarity switch
        {
            Rarity.Common => 1,
            Rarity.Uncommon => 2,
            Rarity.Rare => 3,
            Rarity.Epic => 4,
            _ => 1
        };
    }


    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);

        CombatEventBus.OnCombatEnd += HandleCombatEnd;
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        // Use parent's FindNearestEnemy() method
        UnitInstance target = FindNearestEnemy();

        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(
                new CombatAction
                {
                    type = CombatActionType.Damage,
                    source = this,
                    target = target,
                    amount = stats.Attack,
                    reason = "Farmer Attack",
                    isCrit = abilityCrit
                }
            );
            Debug.Log($"{unitName} attacks {target.unitName} for {stats.Attack} damage!");

        }
        else
        {
            Debug.Log("No target found to attack!");
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this unit survives combat, + {goldReward} [GOLD].");
    }

    private void OnDestroy()
    {
        CombatEventBus.OnCombatEnd -= HandleCombatEnd;
    }

    private void HandleCombatEnd()
    {
        if (this == null) return;
        if (this.isPlayer)
        {
            RunManager.Instance.Stats.CurrentGold += goldReward;
        }
    }


}
