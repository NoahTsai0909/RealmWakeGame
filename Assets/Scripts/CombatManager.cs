using System.Collections.Generic;
using UnityEngine;
using System;

public enum CombatActionType
{
    Damage,
    Heal,
    Shield,
    ApplyBurn,
    BurnTick,
    ApplyPoison,
    PoisonTick,
    ApplyHaste,
    ApplySlow,
    Charge,
    Freeze,
    Buff,
    Debuff,
    Advance,
    Kill,
    Summon,
    LifestealHeal,
}

public class CombatAction
{
    public CombatActionType type;
    public UnitInstance source;
    public UnitInstance target;
    public Guid sourceId;//Persistent IDs for stat tracking
    public Guid targetId;
    public int amount;
    public bool isCrit;
    public string reason; // optional (ability name, etc)
    public bool isPassive; // optional (for passive abilities)
    public GameObject projectileOverride; // Optional override
    public AudioClip audioOverride;
    public bool isSilent = false; // Optional flag for no floating combat text UI (for instance, individually attributed burn ticks)
    public bool isVisualOnly = false; // Optional flag for no stat tracking or combat log (for instance, consolidated burn damage)
    public UnitDefinition spawnPayload;
    public Vector2Int targetPos = new Vector2Int(-1, -1);
    public ModifiableStats buffStat;
    public Action onFail;
    public bool isLifesteal = false;
    [Tooltip("True if this is an additional target of a multi-target AoE cast.")]
    public bool isAoEExtraHit = false;
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [SerializeField] private GridManager playerGrid;
    [SerializeField] private GridManager enemyGrid;

    private List<CombatAction> combatLog = new();

    void Awake()
    {
        Instance = this;
    }

    public void ExecuteAction(CombatAction action)
    {
        if (action.source != null) action.sourceId = action.source.id;
        if (action.target != null) action.targetId = action.target.id;
        // Target redirection hook
        action.target = ResolveTargetRedirects(action);

        CombatVFXManager.Instance.PlayActionVFX(
        action,
        () => ResolveAction(action)
        );
    }

    public void ResolveAction(CombatAction action)
    {
        if (action.target == null && action.type != CombatActionType.Summon)
        {
            return;
        }
        if (action.isCrit)
        {
            switch (action.type)
            {
                case CombatActionType.Damage:
                case CombatActionType.BurnTick:
                case CombatActionType.PoisonTick:
                case CombatActionType.Heal:
                case CombatActionType.Shield:
                case CombatActionType.ApplyBurn:
                    action.amount *= 2;
                    break;
            }
        }
        switch (action.type)
        {
            case CombatActionType.Damage:
                int actualHealthLost = action.target.TakeDamage(action.amount, action.source);
                if (action.isLifesteal && actualHealthLost > 0 && action.source != null && action.source.GetCurrentHP() > 0)
                {
                    ExecuteAction(new CombatAction
                    {
                        type = CombatActionType.LifestealHeal,
                        source = action.source,
                        target = action.source,
                        amount = actualHealthLost,
                        reason = "Lifesteal"
                    });
                }
                break;
            case CombatActionType.BurnTick:
            case CombatActionType.PoisonTick:
                action.target.TakeDamage(action.amount);
                break;

            case CombatActionType.Heal:
                action.target.HealDamage(action.amount);
                break;

            case CombatActionType.Shield:
                action.target.ShieldDamage(action.amount);
                break;

            case CombatActionType.ApplyBurn:
                action.target.ApplyBurn(action.amount, action.source);
                break;
            case CombatActionType.ApplyPoison:
                action.target.ApplyPoison(action.amount, action.source);
                break;

            case CombatActionType.ApplySlow:
                action.target.ApplySlow(action.amount);
                break;

            case CombatActionType.ApplyHaste:
                action.target.ApplyHaste(action.amount);
                break;

            case CombatActionType.Advance:
                action.target.Advance(action.amount);
                break;
            case CombatActionType.Buff:
                action.target.ApplyBuff(action.buffStat, action.amount);
                break;
            case CombatActionType.Kill:
                action.target.Die(action.source);
                break;
            case CombatActionType.Summon:
                ResolveSummon(action);
                break;
            case CombatActionType.LifestealHeal:
                action.target.HealDamage(action.amount);
                break;
        }

        combatLog.Add(action);//record the action in the combat log
        CombatEventBus.PublishActionResolved(action); //publish the action resolved event
    }

    public void RecordStatAction(CombatAction action)
    {
        combatLog.Add(action);
    }



    private UnitInstance ResolveTargetRedirects(CombatAction action)
    {
        // Future: taunt, intercept, etc
        return action.target;
    }

    public IReadOnlyList<CombatAction> GetCombatLog() => combatLog;

    private void ResolveSummon(CombatAction action)
    {
        if (action.spawnPayload == null || action.source == null) return;

        int targetRow = action.targetPos.x;
        int targetCol = action.targetPos.y;

        if (targetRow == -1 || targetCol == -1)
        {
            targetRow = action.source.row;
            targetCol = action.source.col;
        }

        if (UnitSpawner.Instance.TryFindSpawnPosition(targetRow, targetCol, action.source.isPlayer, out int spawnRow, out int spawnCol))
        {
            UnitSpawner.Instance.SpawnUnit(action.spawnPayload, spawnRow, spawnCol, action.source.isPlayer, action.source, action.source.CurrentRarity);
        }
        else
        {
            Debug.Log("Summon failed: No empty spaces available on the grid!");
            action.onFail?.Invoke();
        }
    }


}

