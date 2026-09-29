using UnityEngine;

public class Sharpshooter : UnitInstance
{
    private int attackBuff = 20;

    protected override void UpdateRarityModifiers()
    {
        attackBuff = CurrentRarity switch
        {
            Rarity.Uncommon => 20,
            Rarity.Rare => 40,
            Rarity.Epic => 80,
            _ => 20
        };
    }

    protected override void UseAbility()
    {
        base.UseAbility();
        UnitInstance target = FindFarthestEnemy();
        if (target == null) return;

        CombatManager.Instance.ExecuteAction(
        new CombatAction
        {
            type = CombatActionType.Damage,
            source = this,
            target = target,
            amount = stats.Attack,
            reason = "Sharpshooter Attack",
            isCrit = abilityCrit
        }
        );
    }

    public override void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat = true)
    {
        base.EnterCombat(grid, row, col, isPlayer, startCombat);

        CombatEventBus.OnActionResolved += HandleActionResolved;

    }

    private void OnDestroy()
    {
        CombatEventBus.OnActionResolved -= HandleActionResolved;
    }

    private void HandleActionResolved(CombatAction action)
    {
        if (action.source == null) return;
        if (action.source.isPlayer != this.isPlayer) return;
        if (action.isCrit != true) return;
        if (action.isAoEExtraHit) return;
        CombatManager.Instance.ExecuteAction(new CombatAction
        {
            type = CombatActionType.Buff,
            source = this,
            target = this,
            buffStat = ModifiableStats.Attack,
            amount = attackBuff
        });
    }


    public override string GetActiveDescription()
    {
        return ($"[c_attack]Attack[/c] the farthest enemy for [ATK] {stats.Attack}.");
    }

    public override string GetPassiveDescription()
    {
        return ($"When an ally [c_crit]crits[/c], this gains [c_attack]{attackBuff}[/c] [ATK].");
    }
}
