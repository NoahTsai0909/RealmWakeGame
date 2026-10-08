using System;

public static class CombatEventBus
{
    public enum CombatEventType
    {
        AbilityUsed,
        DamageTaken,
        Healed,
        UnitDied,
        ShieldDamaged,
        UnitSpawned,
        EnergyUsed
    }

    public static event Action<CombatAction> OnActionResolved;

    public static void PublishActionResolved(CombatAction action)
    {
        OnActionResolved?.Invoke(action);
    }

    public delegate void CombatEventHandler(CombatEventType type, UnitInstance source, UnitInstance target, int amount);

    public static event CombatEventHandler OnCombatEvent;

    public static void Publish(CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        OnCombatEvent?.Invoke(type, source, target, amount);
    }

    public static event Action<UnitInstance, StatusEffectType, int> OnStatusChanged;

    public static void PublishStatusChanged(
        UnitInstance unit,
        StatusEffectType type,
        int stacks
    )
    {
        OnStatusChanged?.Invoke(unit, type, stacks);
    }


    public static event Action OnCombatEnd;

    public static void PublishCombatEnd()
    {
        OnCombatEnd?.Invoke();
    }
}