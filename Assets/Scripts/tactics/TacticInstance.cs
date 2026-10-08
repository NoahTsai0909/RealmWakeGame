using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TacticInstance : MonoBehaviour
{
    [SerializeField] private TacticDefinition definition;
    public TacticDefinition Definition => definition;

    public Rarity CurrentRarity { get; private set; }

    public string tacticName;
    public bool isPassive;

    [Header("Combat State")]
    public bool inCombat = false;
    protected float cooldownTimer;

    [Header("Visuals")]
    public UnityEngine.UI.Image fillIcon;
    public UnityEngine.UI.Image backgroundIcon;

    [Header("Targeting")]
    public bool isPlayer;
    protected GridManager allyGrid;
    protected GridManager enemyGrid;
    protected GridManager benchGrid;
    protected TargetingSystem targetingSystem;

    protected List<UnitInstance> auraTargets = new List<UnitInstance>();
    public bool isSpent { get; private set; }
    public float GetCooldownTimer() => cooldownTimer;

    public Guid id;
    public TacticBarManager myBar;
    public RunManager.TacticPlacement myPlacement;
    [Header("Drag State")]
    public bool isDragging = false;

    // Optional: If you want visual pulsing/outlines similar to units
    // public TacticVisualController Visuals { get; private set; }

    protected virtual void Awake()
    {
        id = Guid.NewGuid();
        if (fillIcon == null || backgroundIcon == null)
        {
            UnityEngine.UI.Image[] images = GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.Equals("fillIcon", StringComparison.OrdinalIgnoreCase))
                {
                    fillIcon = img;
                }
                else if (img.gameObject.name.Equals("BackgroundIcon", StringComparison.OrdinalIgnoreCase))
                {
                    backgroundIcon = img;
                }
            }
        }
    }

    private void Start()
    {
        if (definition != null)
        {
            tacticName = definition.tacticName;
            isPassive = definition.isPassive;
            UpdateVisuals();
        }
    }

    public virtual void InitializeFromSaveData(RunManager.TacticSaveData data)
    {
        if (data == null) return;

        definition = data.definition;
        CurrentRarity = data.rarity;
        isPassive = definition.isPassive;
        id = data.id;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (definition != null && definition.tacticSprite != null)
        {
            if (fillIcon != null)
            {
                fillIcon.sprite = definition.tacticSprite;
            }

            if (backgroundIcon != null)
            {
                backgroundIcon.sprite = definition.tacticSprite;
                backgroundIcon.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }
    }

    public virtual void EnterCombat()
    {
        inCombat = true;
        isSpent = false;
        cooldownTimer = definition.cooldown;

        if (fillIcon != null)
        {
            fillIcon.color = Color.white;
            fillIcon.fillAmount = isPassive ? 1f : 0f;
        }
    }

    /* =========================
     * TIER / MERGE LOGIC
     * ========================= */

    public bool CanUpgradeTier()
    {
        return CurrentRarity != Rarity.Epic;
    }

    public void UpgradeTier()
    {
        if (!CanUpgradeTier()) return;

        // Remove passive effects before upgrading, just in case they scale!
        if (isPassive && inCombat) RemovePassiveEffect();

        CurrentRarity = RarityScaling.GetNextRarity(CurrentRarity);

        if (myPlacement != null && myPlacement.tacticData != null)
        {
            myPlacement.tacticData.rarity = CurrentRarity;
        }

        OnTierUpgraded();

        if (isPassive && inCombat) ApplyPassiveEffect();

        // Visuals?.UpdateRarityOutline(CurrentRarity);
    }

    protected virtual void OnTierUpgraded()
    {
        // Override in specific tactics to recalculate variables if needed
    }
    public bool TickCooldown(float dt)
    {
        cooldownTimer -= dt;

        // Animate the bottom-up fill
        if (fillIcon != null && definition.cooldown > 0)
        {
            // Fills from 0 to 1 as the timer counts down
            fillIcon.fillAmount = 1f - (cooldownTimer / definition.cooldown);
        }

        return cooldownTimer <= 0;
    }
    public void ResetCooldown() { cooldownTimer = Definition.cooldown; }

    public float GetCooldown()
    {
        if (isPassive)
        {
            return 0f;
        }
        return definition.cooldown;
    }
    private System.Collections.IEnumerator BounceEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 punchScale = originalScale * 1.3f; // Bounces 30% larger

        float duration = 0.1f; // Quick pop up
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.15f; // Slightly slower settle down

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale; // Guarantee it resets perfectly
    }

    public virtual void ExecuteActiveEffect()
    {
        // Trigger the visual pop!
        StartCoroutine(BounceEffect());
        
    }

    public virtual void ApplyPassiveEffect()
    {
        // Override for always-on auras
    }

    public virtual void RemovePassiveEffect()
    {
        // Override to clean up auras
    }

    public virtual string GetDescription()
    {

        return "";
    }

    public virtual void CombatStartEffect()
    {
        // Override in specific tactics to apply one-time start-of-combat buffs
    }

    public void MarkAsSpent()
    {
        isSpent = true;

        // Gray out the icon to show it is out of commission for this combat
        if (fillIcon != null)
        {
            fillIcon.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray
        }
    }

    public virtual void SetupTargeting(bool isPlayerSide)
    {
        isPlayer = isPlayerSide;
        gameManager gm = FindFirstObjectByType<gameManager>();

        if (gm != null)
        {
            allyGrid = isPlayer ? gm.playerGrid : gm.enemyGrid;
            enemyGrid = isPlayer ? gm.enemyGrid : gm.playerGrid;
            benchGrid = isPlayer ? gm.benchGrid : null;
            targetingSystem = new TargetingSystem(allyGrid, enemyGrid, isPlayer);
        }
        else
        {
            PrepSceneManager pm = FindFirstObjectByType<PrepSceneManager>();
            if (pm != null)
            {
                allyGrid = isPlayer ? pm.battleGrid : null;
                enemyGrid = null;
                benchGrid = isPlayer ? pm.benchGrid : null;
                targetingSystem = new TargetingSystem(allyGrid, enemyGrid, isPlayer);
            }
            else
            {
                // --- NEW: Check for the Shop Scene! ---
                ShopSceneController sc = FindFirstObjectByType<ShopSceneController>();
                if (sc != null)
                {
                    allyGrid = isPlayer ? sc.battleGrid : null;
                    enemyGrid = null;
                    benchGrid = isPlayer ? sc.benchGrid : null;
                    targetingSystem = new TargetingSystem(allyGrid, enemyGrid, isPlayer);
                }
                // --------------------------------------
                else
                {
                    MapController mc = FindFirstObjectByType<MapController>();
                    if (mc != null)
                    {
                        allyGrid = mc.previewGrid;
                        enemyGrid = null;
                        benchGrid = null;
                        targetingSystem = new TargetingSystem(allyGrid, enemyGrid, isPlayer);
                    }
                }
            }
        }
    }


    protected List<UnitInstance> FindAllAllies()
    {
        List<UnitInstance> allAllies = new List<UnitInstance>();

        if (allyGrid != null) allAllies.AddRange(allyGrid.GetAllUnits());

        // Add all bench units to the valid targets list!
        if (benchGrid != null) allAllies.AddRange(benchGrid.GetAllUnits());

        return allAllies;
    }

    protected List<UnitInstance> FindAllEnemies()
    {
        if (targetingSystem != null) return targetingSystem.GetEnemies();
        return new List<UnitInstance>();
    }

    protected UnitInstance FindNearestEnemy()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Enemy,
            TargetingSystem.SortMethod.Nearest
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }

    protected UnitInstance FindLowestHealthAlly()
    {
        if (targetingSystem == null) return null;
        var criteria = new TargetingSystem.TargetCriteria(
            TargetingSystem.TargetTeam.Ally,
            TargetingSystem.SortMethod.LowestHealth
        );
        return targetingSystem.FindUnit(criteria, transform.position);
    }
}