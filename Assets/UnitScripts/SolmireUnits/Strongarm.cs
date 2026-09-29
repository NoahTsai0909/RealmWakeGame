using UnityEngine;
using static CombatEventBus;

public class Strongarm : UnitInstance
{
    private int attackBuff = 5;

    protected override void UpdateRarityModifiers()
    {
        attackBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 30,
            _ => 10
        };
    }
    protected override void UseAbility()
    {
        base.UseAbility();
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
                    reason = "Strongarm attack",
                    isCrit = abilityCrit
                }
            );
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
        int expectedCol = isPlayer ? col - 1 : col + 1;
        if (source.row != row) return;
        if (source.col != expectedCol && source != this) return;

        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = source,
            buffStat = ModifiableStats.Attack,
            amount = attackBuff
        });
    }

    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the nearest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this or the ally behind this uses an ability, this and that ally both gain [c_attack]+{attackBuff}[/c] [ATK].");
    }
}
