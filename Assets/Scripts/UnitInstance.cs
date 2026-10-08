using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CombatEventBus;

public class UnitInstance : MonoBehaviour
{

    [SerializeField] private UnitDefinition definition;
    public UnitDefinition Definition => definition;

    public Rarity CurrentRarity { get; private set; }
    public StatusEffectController Status { get; private set; }

    protected PermanentStats permanentStats;
    protected TemporaryStats temporaryStats;
    protected StatBlock stats;

    public MutationPrefixSO currentPrefix;
    public MutationSuffixSO currentSuffix;

    public StatBlock Stats => stats;

    public string unitName;
    public bool isPassive;
    public bool isEnergy;

    /* =========================
     * Combat state
     * ========================= */

    public bool inCombat = false;
    protected bool combatFrozen = false;
    public bool isDead = false;

    private int currentHP;
    public int currentEnergy;
    private int currentShield;
    public int GetCurrentHP() => currentHP;

    public void SetCurrentHP(int newHP)
    {
        currentHP = Mathf.Clamp(newHP, 0, stats.MaxHP);
        RefreshUI();
    }
    public int GetMaxHP() => stats.MaxHP;
    public void SetMaxHP(int newMaxHP)
    {
        int oldMaxHP = stats.MaxHP;
        int delta = newMaxHP - oldMaxHP;
        TemporaryStatModify(ModifiableStats.MaxHP, delta);
    }
    public int GetCurrentShield() => currentShield;
    public int SetCurrentShield(int newShield)
    {
        currentShield = Mathf.Max(newShield, 0);
        RefreshUI();
        return currentShield;
    }

    public float GetCooldownTimer() => cooldownTimer;  

    public int GetCurrentValue() => stats.Value;

    protected float cooldownTimer;
    protected bool abilityCrit;

    public Guid id;
    public int row;
    public int col;
    public bool isPlayer;
    public GridManager myGrid;
    public RunManager.UnitPlacement myPlacement;
    private TargetingSystem targetingSystem;
    public UnitVisualController Visuals { get; private set; }

    public bool isSpawnedUnit = false;  // Track if this is a spawned unit
    public UnitInstance spawnSource;
    protected List<UnitInstance> auraTargets = new List<UnitInstance>();

    private bool isCastingSequence;
    private float castRecovery = 0.3f;

    private BattleUIManager uiManager;

    /* =========================
     * Unity lifecycle
     * ========================= */

    protected virtual void Awake()
    {
        id = Guid.NewGuid();
        temporaryStats = new TemporaryStats();
        Visuals = GetComponent<UnitVisualController>();
        Status = gameObject.AddComponent<StatusEffectController>();
    }

    private void Start()
    {
        if (definition != null)
        {
            unitName = definition.unitName;
            isPassive = definition.isPassive;
            isEnergy = definition.isEnergy;

            Visuals?.InitializeVisuals(definition);
        }
        Visuals?.SetDirection(isPlayer);
    }

    private void Update()
    {
        if (!inCombat || isPassive || combatFrozen)
            return;

        if (isEnergy && currentEnergy <= 0)
            return;

        RefreshUI();

        if (cooldownTimer > 0)
        {
            float speedMultiplier = GetCooldownSpeedMultiplier();
            cooldownTimer -= Time.deltaTime * speedMultiplier;
        }

        if (cooldownTimer <= 0 && !isCastingSequence)
        {
            StartCoroutine(CastSequence());

            if (isEnergy)
                currentEnergy = Mathf.Max(currentEnergy - 1, 0);
                CombatEventBus.Publish(CombatEventBus.CombatEventType.EnergyUsed, this, this, 1);

        }
    }

    private IEnumerator CastSequence()
    {
        isCastingSequence = true;

        cooldownTimer = stats.Cooldown;

        int multicast = stats.Multicast;

        for (int i = 0; i < multicast; i++)
        {
            UseAbility();

            if (i < multicast - 1)
                yield return new WaitForSeconds(castRecovery);
        }

        isCastingSequence = false;
    }

    public virtual void InitializeEnemy(UnitDefinition def, Rarity rarity)
    {
        definition = def;
        CurrentRarity = rarity;
        isPassive = definition.isPassive;

        // CRITICAL: Prevent save bloat
        permanentStats = null;
        temporaryStats = new TemporaryStats();
        UpdateRarityModifiers();
        RecalculateStats();

        currentEnergy = stats.maxEnergy;

        Visuals?.UpdateRarityOutline(CurrentRarity);
    }

    public virtual void InitializeEnemy(UnitSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("InitializeEnemy called with null UnitSaveData");
            return;
        }
        InitializeEnemy(data.definition, data.rarity);
        id = data.id;
        currentPrefix = data.prefix;
        currentSuffix = data.suffix;
        RecalculateStats();
        Visuals?.ApplyMutationVisuals(currentPrefix);
    }

    public virtual void InitializeFromSaveData(UnitSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("InitializeFromSaveData called with null UnitSaveData");
            return;
        }

        // Core identity
        definition = data.definition;
        CurrentRarity = data.rarity;
        isPassive = definition.isPassive;
        id = data.id;

        currentPrefix = data.prefix;
        currentSuffix = data.suffix;
        // Permanent progression (keyed by GUID, not definition long-term)
        permanentStats = RunManager.Instance.GetPermanentStatsForUnit(id)?? RunManager.Instance.CreatePermanentStatsForUnit(id);

        // Fresh combat-only modifiers
        temporaryStats = new TemporaryStats();
        UpdateRarityModifiers();
        RecalculateStats();

        currentEnergy = stats.maxEnergy;

        Visuals?.UpdateRarityOutline(CurrentRarity);
        Visuals?.ApplyMutationVisuals(currentPrefix);
    }


    public void RecalculateStats()
    {
        stats = new StatBlock(
            GetRarityAdjustedDefinition(),
            permanentStats,
            temporaryStats,
            currentPrefix,
            definition.mainStat
        );
    }

    public void SetPlayerSide(bool isPlayerSide)
    {
        isPlayer = isPlayerSide;
        Visuals?.SetDirection(isPlayer);
    }

    public virtual void EnterCombat(GridManager grid, int row, int col, bool isPlayer, bool startCombat=true)
    {
        this.row = row;
        this.col = col;
        this.myGrid = grid;

        grid.PlaceUnit(row, col, this, isPlayer);
        GetComponent<Collider2D>().isTrigger = true;
        SetPlayerSide(isPlayer);
        InitializeCombatState();
        SetupTargeting();
        if (startCombat)
        {
            SetupCombatUI();
        }

        combatFrozen = false;
        CombatEventBus.OnCombatEnd += FreezeUnit;
        inCombat = startCombat;
    }

    public void InitializeCombatState()
    {
        isDead = false;
        currentHP = stats.MaxHP;
        currentEnergy = stats.maxEnergy;
        cooldownTimer = stats.Cooldown;
        Status.ClearAllStatusEffects();
    }

    private void SetupTargeting()
    {
        gameManager gm = FindFirstObjectByType<gameManager>();
        if (gm == null) return;

        targetingSystem = new TargetingSystem(
            isPlayer ? gm.playerGrid : gm.enemyGrid,
            isPlayer ? gm.enemyGrid : gm.playerGrid,
            isPlayer
        );
    }

    public void SetupCombatUI()
    {
        uiManager = FindFirstObjectByType<BattleUIManager>();
        if (uiManager == null) return;

        uiManager.CreateUnitUI(this, transform.position);
        RefreshUI();
    }

    /* =========================
     * TIER / MERGE LOGIC
     * ========================= */

    public bool CanUpgradeTier()
    {
        return CurrentRarity != Rarity.Epic && CurrentRarity != Rarity.Mythic;
    }

    public void UpgradeTier()
    {
        if (!CanUpgradeTier())
            return;
        RemoveAuras();
        Rarity old = CurrentRarity;
        CurrentRarity = RarityScaling.GetNextRarity(CurrentRarity);
        if (myPlacement != null && myPlacement.unitData != null)
        {
            myPlacement.unitData.rarity = CurrentRarity;
        }
        RecalculateStats();
        OnTierUpgraded();
        ApplyAuras();
        Visuals?.UpdateRarityOutline(CurrentRarity);
    }
    protected virtual void OnTierUpgraded()
    {
        UpdateRarityModifiers();
    }
    protected virtual void UpdateRarityModifiers() { }
    public void PreviewRaritySwap(Rarity tempRarity)
    {
        CurrentRarity = tempRarity;
        UpdateRarityModifiers();
        RecalculateStats();
    }

    public void ApplyMutation(MutationPrefixSO newPrefix, MutationSuffixSO newSuffix)
    {
        currentPrefix = newPrefix;
        currentSuffix = newSuffix;

        RecalculateStats();

        if (myPlacement != null && myPlacement.unitData != null)
        {
            myPlacement.unitData.prefix = newPrefix;
            myPlacement.unitData.suffix = newSuffix;
        }
    }


    /* =========================
     * COMBAT ACTIONS
     * ========================= */

    protected virtual void UseAbility()
    {
        abilityCrit = RollCrit();
        Visuals?.PlayAttackAnimation();
        CombatEventBus.Publish(CombatEventBus.CombatEventType.AbilityUsed, this, null, 0);
        if (this != null && inCombat && currentSuffix != null)
        {
            currentSuffix.ExecuteEffect(this);
        }
    }

    public virtual void CombatStartEffect()
    {

    }

    public virtual int TakeDamage(int dmg, UnitInstance source = null)
    {
        if (this == null || combatFrozen || isDead) return 0;
        if (dmg <= 0) return 0;

        int remainingDamage = dmg;
        int startingHP = currentHP; 

        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
            CombatEventBus.Publish(CombatEventType.ShieldDamaged, source, this, absorbed);
        }

        if (remainingDamage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - remainingDamage);

            CombatEventBus.Publish(CombatEventType.DamageTaken, source, this, remainingDamage);

            Visuals.Flash(Color.red, true);
            if (currentHP <= 0)
                Die(source);
        }
        RefreshUI();
        return startingHP - currentHP;
    }

    public virtual void HealDamage(int dmg)
    {
        currentHP = Mathf.Min(stats.MaxHP, currentHP + dmg);
        RefreshUI();
    }

    public virtual void ShieldDamage(int dmg)
    {
        currentShield = currentShield + dmg;
        RefreshUI();
    }

    public virtual void Die(UnitInstance killer = null)
    {
        if (isDead) return;
        isDead = true;
        currentHP = 0;
        RemoveAuras();
        Status.ClearAllStatusEffects();
        if (myGrid != null)
        {
            myGrid.RemoveUnit(row, col, false);
        }

        if (uiManager != null)
            uiManager.RemoveUnitUI(this);

        OnDeathEffect();
        CombatEventBus.Publish(CombatEventBus.CombatEventType.UnitDied, killer, this, 0);
        inCombat = false;
        if (Visuals != null)
        {
            Visuals.PlayDeathAnimationAndDestroy();
        }
        else
        {
            // Fallback just in case visuals are missing
            Destroy(gameObject);
        }
    }

    protected virtual void OnDeathEffect()
    {
        // Override in derived classes for death effects
    }

    public virtual void OnDayStart(UnitSaveData mySaveData)
    {
        // Override in specific units (like Army Promoter) for daily meta-effects
    }

    private void FreezeUnit()
    {
        combatFrozen = true;
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks!
        CombatEventBus.OnCombatEnd -= FreezeUnit;
    }

    public void TakeDisasterDamage(int damage)
    {
        if (damage <= 0)
            return;
        if (this == null || combatFrozen) return;
        int remainingDamage = damage;

        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;

        }

        if (remainingDamage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - remainingDamage);

            Visuals?.Flash(Color.purple);

            if (currentHP <= 0)
                Die();
        }

        RefreshUI();
    }

    public void ApplyBurn(int amount, UnitInstance source) {GetComponent<StatusEffectController>().AddBurn(amount, source);}
    public void ApplyPoison(int amount, UnitInstance source) {GetComponent<StatusEffectController>().AddPoison(amount, source);}
    public void ApplySlow(int amount) => Status.AddSlow(amount); // amount is now treated as seconds!
    public void ApplyHaste(int amount) => Status.AddHaste(amount);

    /* =========================
    * Helpers
    * ========================= */
    private bool RollCrit()
    {
        return UnityEngine.Random.Range(0f, 100f) < stats.CritChance;
    }

    private void RefreshUI()
    {
        if (uiManager == null) return;

        uiManager.UpdateHealthBar(this);
        uiManager.UpdateCooldownBar(this);
    }

    public virtual string GetActiveDescription()
    {
        return "";
    }

    public virtual string GetPassiveDescription()
    {
        return "";
    }

    public virtual string GetMutationTriggerText()
    {
        return ""; 
    }

    private string GetStatStringForUI(ModifiableStats stat)
    {
        return stat switch
        {
            ModifiableStats.Attack => "ATK",
            ModifiableStats.MaxHP => "MAXHEALTH",
            _ => stat.ToString().ToUpper()
        };
    }

    public string GetMutationScalingText()
    {
        if (currentPrefix == null || definition == null) return "";
        ModifiableStats mainStat = definition.mainStat;
        ModifiableStats grantedStat = currentPrefix.statToGrant;
        if (grantedStat == ModifiableStats.None ||grantedStat == ModifiableStats.Slow || grantedStat == ModifiableStats.Haste) return "";
        float mainWeight = Stats.GetStatWeight(mainStat);
        float grantedWeight = Stats.GetStatWeight(grantedStat);
        int percentage = Mathf.RoundToInt((mainWeight / grantedWeight) * 100f);
        string mainStatName = GetStatStringForUI(mainStat);
        string grantedStatName = GetStatStringForUI(grantedStat);
        string prefixHex = ColorUtility.ToHtmlStringRGB(currentPrefix.runeColor);
        if (mainStat == ModifiableStats.None) return "";
        if (mainStat == grantedStat) return $"<color=#{prefixHex}>{currentPrefix.prefixName}</color>: This gains [{grantedStatName}] equal to 50% of its [{mainStatName}].";
        return $"<color=#{prefixHex}>{currentPrefix.prefixName}</color>: This gains [{grantedStatName}] equal to {percentage}% of its [{mainStatName}].";
    }

    IStatSource GetRarityAdjustedDefinition()
    {
        int delta = CurrentRarity - Definition.startingRarity;
        float multiplier = RarityScaling.GetMultiplier(delta);
        float maxHPmultiplier = RarityScaling.GetMaxHPMultiplier(delta);
        float finalCooldown = Definition.overrideCooldownScaling
    ? Definition.cooldownByRarity[(int)CurrentRarity]
    : Definition.cooldown;

        return new UnitDefinitionView(
            Mathf.RoundToInt(Definition.attack * multiplier),
            Mathf.RoundToInt(Definition.heal * multiplier),
            Mathf.RoundToInt(Definition.maxHP * maxHPmultiplier),
            finalCooldown,
            Mathf.RoundToInt(Definition.shield * multiplier),
            Mathf.RoundToInt(Definition.burn * multiplier),
            Mathf.RoundToInt(Definition.poison * multiplier),
            Definition.maxEnergy,
            Definition.slow + GetRarityAdjustedSlow(),
            Definition.haste + GetRarityAdjustedHaste(),
            Definition.multicast,
            GetRarityAdjustedValue(),
            Definition.critChance,
            Definition.tagFlags
        );
    }

    protected virtual int GetRarityAdjustedSlow() //only implement if needed in derived classes
    {
        return CurrentRarity - Definition.startingRarity;
    }

    protected virtual int GetRarityAdjustedHaste()
    {
        return CurrentRarity - Definition.startingRarity;
    }   

    private int GetRarityAdjustedValue()
    {
        int rarityMultiplier;
        if (CurrentRarity == Rarity.Common) rarityMultiplier = 1;
        else if (CurrentRarity == Rarity.Uncommon) rarityMultiplier = 2;
        else if (CurrentRarity == Rarity.Rare) rarityMultiplier = 3;
        else if (CurrentRarity == Rarity.Epic) rarityMultiplier = 4;
        else if (CurrentRarity == Rarity.Mythic) rarityMultiplier = 5;
        else rarityMultiplier = 1;
        int mutationMultiplier = 1;
        if (currentPrefix != null) mutationMultiplier += 1;
        return definition.provisionCost != 0 ? rarityMultiplier * definition.provisionCost * mutationMultiplier : rarityMultiplier;
    }

    public void TemporaryStatModify(ModifiableStats modifiableStats, int bonus)
    {
        if (modifiableStats == ModifiableStats.Burn)
        {
            temporaryStats.burnBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.Poison)
        {
            temporaryStats.poisonBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.MaxEnergy)
        {
            temporaryStats.maxEnergyBonus += bonus;
            currentEnergy += bonus;
        }
        else if (modifiableStats == ModifiableStats.Attack)
        {
            temporaryStats.attackBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.MaxHP)
        {
            temporaryStats.maxHPBonus += bonus;
            currentHP += bonus;
        }
        else if (modifiableStats == ModifiableStats.CritChance)
        {
            temporaryStats.critChanceBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.Multicast)
        {
            temporaryStats.multicastBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.Heal)
        {
            temporaryStats.healBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.Shield)
        {
            temporaryStats.shieldBonus += bonus;
        }
        else if (modifiableStats == ModifiableStats.Cooldown)
        {
            temporaryStats.cooldownDelta += bonus;
        }
        RecalculateStats();
        return;
    }

    public void ApplyBuff(ModifiableStats statToBuff, int amount)
    {
        TemporaryStatModify(statToBuff, amount);
        Visuals?.Flash(Color.yellow);
    }

    public void AddTemporaryTag(UnitTagFlags tag)
    {
        temporaryStats.tagBonus |= tag;
        RecalculateStats();
    }

    public void RemoveTemporaryTag(UnitTagFlags tag)
    {
        temporaryStats.tagBonus &= ~tag; 
        RecalculateStats();
    }

    private float GetCooldownSpeedMultiplier()
    {
        bool slowed = Status.slowTimer > 0;
        bool hasted = Status.hasteTimer > 0;

        if (slowed && hasted) return 1f;
        if (slowed) return 0.5f;
        if (hasted) return 1.5f;
        return 1f;
    }

    public void Advance(int seconds)
    {
        cooldownTimer = Mathf.Max(cooldownTimer - seconds, 0f);
    
    }

    public virtual void ApplyAuras() { }

    public virtual void RemoveAuras()
    {
        auraTargets.Clear(); 
    }

    /* =========================
     * Targeting helpers
     * ========================= */

    public UnitInstance FindNearestEnemy()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Nearest
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }

    protected List<UnitInstance> FindNearestEnemies(int count)
    {
        if (targetingSystem == null) return new List<UnitInstance>();

        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Nearest
        );

        return targetingSystem.FindMultipleUnits(criteria, count, transform.position);
    }

    public UnitInstance FindFarthestEnemy()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Farthest
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }

    protected List<UnitInstance> FindFarthestEnemies(int count)
    {
        if (targetingSystem == null) return new List<UnitInstance>();
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Farthest
        );
        return targetingSystem.FindMultipleUnits(criteria, count, transform.position);
    }

    public UnitInstance FindLowestHealthAlly()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Ally,
            TargetingSystem.SortMethod.LowestHealth
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }

    protected List<UnitInstance> FindLowestHealthAllies(int count)
    {
        if (targetingSystem == null) return new List<UnitInstance>();
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Ally,
            TargetingSystem.SortMethod.LowestHealth
        );
        return targetingSystem.FindMultipleUnits(criteria, count, transform.position);
    }

    public UnitInstance FindRandomAlly()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Ally,
            TargetingSystem.SortMethod.Random
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }
    protected UnitInstance FindRandomEnemy()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Random
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }

    protected UnitInstance FindRandomAlly(bool excludeSelf)
    {
        var allies = FindAllAllies();
        if (allies == null || allies.Count == 0) return null;

        if (excludeSelf) allies.Remove(this);

        if (allies.Count == 0) return null;
        return allies[UnityEngine.Random.Range(0, allies.Count)];
    }

    protected bool IsAdjacent(UnitInstance other)
    {
        if (other == null) return false;

        int rowDiff = Mathf.Abs(row - other.row);
        int colDiff = Mathf.Abs(col - other.col);

        // 4-direction adjacency
        return (rowDiff + colDiff) == 1;
    }

    protected bool IsSide(UnitInstance other)
    {
        if (other == null) return false;
        return Mathf.Abs(row - other.row) == 1 && col == other.col;
    }

    public List<UnitInstance> FindAdjacentAllies()
    {
        if (myGrid != null) return myGrid.GetAdjacentUnits(this);
        return new List<UnitInstance>();
    }

    protected List<UnitInstance> FindSideAllies()
    {
        if (myGrid != null) return myGrid.GetSideUnits(this);
        return new List<UnitInstance>();
    }

    public List<UnitInstance> FindAllAllies()
    {
        if (myGrid != null) return myGrid.GetAllUnits();
        return new List<UnitInstance>();
    }

    public List<UnitInstance> FindAllEnemies()
    {
        if (targetingSystem == null) return new List<UnitInstance>();
        return targetingSystem.GetEnemies();
    }


    protected virtual void HandleCombatEvent(CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
    }

    protected virtual void HandleCombatAction(CombatAction action) { }
}
