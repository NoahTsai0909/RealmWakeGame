using UnityEngine;

public class EssenceImbiber : UnitInstance
{
    private int attackModifier = 10;

    protected override void UpdateRarityModifiers()
    {
        attackModifier = CurrentRarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 20,
            Rarity.Epic => 40,
            _ => 10
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
        if (action.type == CombatActionType.LifestealHeal && action.target == this)
        {
            if (GetCurrentHP() >= stats.MaxHP)
            {
                CombatManager.Instance.ExecuteAction(new CombatAction
                {
                    type = CombatActionType.Buff,
                    source = this,
                    target = this,
                    buffStat = ModifiableStats.Attack,
                    amount = attackModifier
                });
            }
        }
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindRandomEnemy();
        if (target != null)
        {
            CombatManager.Instance.ExecuteAction(new CombatAction
            {
                type = CombatActionType.Damage,
                source = this,
                target = target,
                amount = stats.Attack,
                reason = "Essence Imbiber Attack",
                isCrit = abilityCrit,
                isLifesteal = true
            });
        }
    }

    public override string GetActiveDescription()
    {
        return ($"[c_lifesteal]Lifesteal[/c]. \n[c_attack]Attack[/c] a random enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When this fully [c_heal]heals[/c] through [c_lifesteal]lifesteal[/c], this gains +[c_attack]{attackModifier}[/c] [ATK].");
    }
}