using UnityEngine;

public class Shieldmate : UnitInstance
{
    private int shieldBuff = 1;

    protected override void UpdateRarityModifiers()
    {
        shieldBuff = CurrentRarity switch
        {
            Rarity.Common => 1,
            Rarity.Uncommon => 2,
            Rarity.Rare => 4,
            Rarity.Epic => 8,
            _ => 1
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
        if ((action.type == CombatActionType.Shield) && (action.target == this))
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Buff,
                source = this,
                target = this,
                buffStat = ModifiableStats.Shield,
                amount = shieldBuff
            });
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        CombatManager.Instance.ExecuteAction(
            new CombatAction
            {
                type = CombatActionType.Shield,
                source = this,
                target = this,
                amount = stats.Shield,
                reason = "Shieldmate shielding",
                isCrit = abilityCrit
            }
        );

    }

    public override string GetActiveDescription()
    {
        return ($"[c_shield]Shield[/c] this for [SHIELD] {stats.Shield}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this is shielded, this gets + [c_shield]{shieldBuff}[/c] [SHIELD].");
    }
}
