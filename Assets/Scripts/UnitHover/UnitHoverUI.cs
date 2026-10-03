using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UnitHoverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI provisionText;
    [SerializeField] private TextMeshProUGUI valueText;

    [SerializeField] private bool useFixedPosition = true;

    [SerializeField] private Sprite backgroundCommon;
    [SerializeField] private Sprite backgroundUncommon;
    [SerializeField] private Sprite backgroundRare;
    [SerializeField] private Sprite backgroundEpic;
    [SerializeField] private Sprite backgroundMythic;

    [SerializeField] private Sprite rarityGemCommon;
    [SerializeField] private Sprite rarityGemUncommon;
    [SerializeField] private Sprite rarityGemRare;
    [SerializeField] private Sprite rarityGemEpic;
    [SerializeField] private Sprite rarityGemMythic;

    [SerializeField] private TextMeshProUGUI statText;
    [SerializeField] private GameObject multicastContainer;
    [SerializeField] private TextMeshProUGUI multicastText;

    [SerializeField] private HealthBarUI healthBar;
    [SerializeField] private CooldownBarUI cooldownBar;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image rarityGemImage;

    [Header("Ability UI")]
    [SerializeField] private GameObject activeAbilityBox;
    [SerializeField] private TextMeshProUGUI activeAbilityText;

    [SerializeField] private GameObject passiveAbilityBox;
    [SerializeField] private TextMeshProUGUI passiveAbilityText;

    [Header("Mutation UI")]
    [SerializeField] private GameObject mutationDivider;
    [SerializeField] private GameObject mutationAbilityBox;
    [SerializeField] private TextMeshProUGUI mutationAbilityText;

    [Header("Tags")]
    [SerializeField] private Transform tagContainer;
    [SerializeField] private GameObject tagBadgePrefab;

    [Header("Behavior")]
    [SerializeField] private bool isPermanentUI = false;
    [Tooltip("Extra padding to prevent IgnoreLayout elements from getting cut off at screen edges!")]
    [SerializeField] private Vector2 edgePadding = new Vector2(50f, 150f);
    private RectTransform uiAnchorOverride;

    [Header("Preview State")]
    public bool isPreviewMode = false;
    private UnitHoverUI activePreviewUI;
    [SerializeField] private Sprite instructionalImage;
    [Tooltip("Drag the Inspect Notice and your new Preview Button here so they vanish on the cloned UI")]
    [SerializeField] private GameObject[] hideInPreviewMode;
    public bool placedOnRightSide = true;
    [Tooltip("Check this if this UI is sitting in the Compendium Modal. It will disable ALL movement.")]
    [SerializeField] private bool isCompendiumUI = false;

    private int lastEnergy, lastAttack, lastShield, lastHeal, lastPoison, lastBurn, lastCrit, lastMulticast;

    private Canvas canvas;
    private RectTransform rectTransform;
    private UnitInstance currentUnit;
    private Camera mainCamera;
    private Camera canvasCamera;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        // Determine which camera to use for screen-to-canvas conversion
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = null; // Overlay canvases don't use a camera
        }
        else
        {
            canvasCamera = canvas.worldCamera ?? mainCamera;
        }

        //gameObject.SetActive(false);
        if (!isPermanentUI) gameObject.SetActive(false);
    }

    void Update()
    {
        if (isPreviewMode) return;

        if (currentUnit == null)
        {
            Hide();
            return;
        }

        UpdateDynamicValues();
        UpdateDynamicStats();
        UpdatePosition();

        if (activePreviewUI != null) activePreviewUI.UpdatePreviewPosition(this);
    }

    public void Show(UnitInstance unit, RectTransform uiAnchor = null)
    {
        if (TutorialManager.Instance != null)
        {
            List<TutorialStep> combatSequence = new List<TutorialStep>
                            {
                                new TutorialStep {
                                    key = "UnitHoverUI",
                                    title = "Unit Information",
                                    description = "Hover over a unit to view its stats and abilities.\n\nPress T to pin the panel, then hover over keywords to learn what they mean.",
                                    highlightTarget = null,
                                    instructionalGraphic = instructionalImage != null ? instructionalImage : null
                                }
                            };
            TutorialManager.Instance.StartTutorialSequence(combatSequence);
        }
        uiAnchorOverride = uiAnchor;
        if (unit == null || unit.Definition == null)
            return;

        currentUnit = unit;
        unit.RecalculateStats();

        string finalName = unit.Definition.unitName;
        string mutationColorHex = null;
        if (unit.currentPrefix != null)
        {
            mutationColorHex = ColorUtility.ToHtmlStringRGB(unit.currentPrefix.runeColor);
            finalName = $"<color=#{mutationColorHex}>{unit.currentPrefix.prefixName}</color> {finalName}";
        }

        if (unit.currentSuffix != null)
        {
            if (mutationColorHex != null)
            {
                finalName = $"{finalName} of <color=#{mutationColorHex}>{unit.currentSuffix.suffixName}</color>";
            }
            else
            {
                finalName = $"{finalName} of {unit.currentSuffix.suffixName}";
            }
        }

        nameText.text = finalName;

        SetRarityBackground(unit.CurrentRarity);
        cooldownBar.SetVisuals(unit.CurrentRarity);

        StatBlock stats = unit.Stats;

        string allStats = "";
        int displayEnergy = unit.inCombat ? unit.currentEnergy : stats.maxEnergy;

        if (unit.Definition.isEnergy) allStats += TextIconUtility.FormatEnergy(displayEnergy) + "/" + stats.maxEnergy + "  ";
        if (unit.Stats.Tags.HasFlag(UnitTagFlags.Damage) && stats.Attack > 0) allStats += TextIconUtility.FormatAttack(stats.Attack) + "  ";
        if (unit.Stats.Tags.HasFlag(UnitTagFlags.Shield) && stats.Shield > 0) allStats += TextIconUtility.FormatShield(stats.Shield) + "  ";
        if (unit.Stats.Tags.HasFlag(UnitTagFlags.Heal) && stats.Heal > 0) allStats += TextIconUtility.FormatHeal(stats.Heal) + "  ";
        if (unit.Stats.Tags.HasFlag(UnitTagFlags.Poison) && stats.Poison > 0) allStats += TextIconUtility.FormatPoison(stats.Poison) + "  ";
        if (unit.Stats.Tags.HasFlag(UnitTagFlags.Burn) && stats.Burn > 0) allStats += TextIconUtility.FormatBurn(stats.Burn) + "  ";
        if (unit.GetActiveDescription() != "" && stats.CritChance > 0) allStats += TextIconUtility.FormatCrit(stats.CritChance) + "  ";
        statText.SetText(allStats);

        if (unit.Definition.isPassive)
        {
            cooldownBar.gameObject.SetActive(false);
        }
        else
        {
            cooldownBar.gameObject.SetActive(true);
        }

        provisionText.SetText(TextIconUtility.FormatProvision(unit.Definition.provisionCost));
        valueText.SetText(TextIconUtility.FormatGold(stats.Value));
        multicastContainer.SetActive(stats.Multicast > 1);
        multicastText.SetText(TextIconUtility.ParseDescription("[c_multicast]X" + (stats.Multicast) + "[/c]"));

        lastEnergy = -1;

        UpdateDynamicStats();
        UpdateDynamicValues();
        gameObject.SetActive(true);
        if (!useFixedPosition)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            UpdatePosition();
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        UpdateAbilityDescriptions();
        foreach (Transform child in tagContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        UnitTagFlags unitTags = unit.Stats.Tags;

        // 3. Loop through every possible tag defined in your UnitTagFlags enum
        foreach (UnitTagFlags flag in System.Enum.GetValues(typeof(UnitTagFlags)))
        {
            if (flag == UnitTagFlags.None) continue;

            if (flag == UnitTagFlags.Damage || flag == UnitTagFlags.Shield || flag == UnitTagFlags.Heal || flag == UnitTagFlags.Poison || flag == UnitTagFlags.Burn || flag == UnitTagFlags.Crit || flag == UnitTagFlags.Energy || flag == UnitTagFlags.Slow || flag == UnitTagFlags.MaxHP || flag == UnitTagFlags.Haste)
            {
                continue;
            }

            if (flag == UnitTagFlags.BurnRef || flag == UnitTagFlags.PoisonRef || flag == UnitTagFlags.DamageRef || flag == UnitTagFlags.HealRef || flag == UnitTagFlags.Death || flag == UnitTagFlags.Summon)
            {
                continue;
            }

            // 4. Check if the unit actually has this specific flag
            if (unitTags.HasFlag(flag))
            {
                // Spawn the dark background prefab into the container
                GameObject newBadge = Instantiate(tagBadgePrefab, tagContainer);

                // Find the TextMeshPro child inside the prefab and set the word
                TextMeshProUGUI badgeText = newBadge.GetComponentInChildren<TextMeshProUGUI>();
                if (badgeText != null)
                {
                    badgeText.text = flag.ToString();
                }
            }
        }
    }

    public void Hide()
    {
        if (activePreviewUI != null) Destroy(activePreviewUI.gameObject);

        if (isPermanentUI) return;
        currentUnit = null;
        gameObject.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (isCompendiumUI) return;
        if (canvas == null || currentUnit == null || mainCamera == null) return;

        // --- NEW SMART POSITION TRACKING ---
        Vector2 unitScreenPos;
        float unitScreenExtentsX = 0f;

        if (uiAnchorOverride != null)
        {
            unitScreenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, uiAnchorOverride.position);

            unitScreenExtentsX = (uiAnchorOverride.rect.width / 2f) * (Screen.width / 1920f);
        }
        else
        {
            // WE ARE HOVERING A 3D UNIT: Track the physics collider
            Vector3 unitWorldPos = currentUnit.transform.position;
            unitScreenPos = mainCamera.WorldToScreenPoint(unitWorldPos);

            Collider2D collider = currentUnit.GetComponent<Collider2D>();
            float unitWorldExtentsX = 1f;
            if (collider != null)
            {
                unitWorldExtentsX = collider.bounds.extents.x;
            }
            Vector2 unitEdgeRightScreen = mainCamera.WorldToScreenPoint(unitWorldPos + new Vector3(unitWorldExtentsX, 0, 0));
            unitScreenExtentsX = Mathf.Abs(unitEdgeRightScreen.x - unitScreenPos.x);
        }

        // Apply useFixedPosition logic using our new smart unitScreenPos
        if (useFixedPosition)
        {
            float flipThreshold = Screen.width * 0.7f;
            bool unitIsOnLeft = unitScreenPos.x < flipThreshold;

            if (unitIsOnLeft)
            {
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-edgePadding.x, 0f);
            }
            else
            {
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                rectTransform.anchoredPosition = new Vector2(edgePadding.x, 0f);
            }
            return; // Exit early 
        }

        float uiWidth = (rectTransform.rect.width + edgePadding.x) * canvas.scaleFactor;
        float uiHeight = (rectTransform.rect.height + edgePadding.y) * canvas.scaleFactor;

        // Extra padding to push it away from the unit's body
        float extraScreenPadding = 20f * canvas.scaleFactor;

        // 2. Check if we have enough room for TWO UIs (Main + Preview) on the right side
        float spaceNeededForTwoUIs = uiWidth * 2.1f;
        bool hasSpaceOnRight = unitScreenPos.x + unitScreenExtentsX + spaceNeededForTwoUIs < Screen.width;

        Vector2 targetScreenPos;
        targetScreenPos.y = unitScreenPos.y; // Vertically align with unit

        if (hasSpaceOnRight)
        {
            placedOnRightSide = true;
            // Push right by: Unit Edge + Half UI Width (to account for center pivot) + Padding
            targetScreenPos.x = unitScreenPos.x + unitScreenExtentsX + (uiWidth * 0.5f) + extraScreenPadding;
        }
        else
        {
            placedOnRightSide = false;
            // Push left by the exact same math
            targetScreenPos.x = unitScreenPos.x - unitScreenExtentsX - (uiWidth * 0.5f) - extraScreenPadding;
        }

        // 3. Clamp vertically so tall UIs simply slide up/down instead of going off-screen
        targetScreenPos.y = Mathf.Clamp(targetScreenPos.y, uiHeight * 0.5f, Screen.height - uiHeight * 0.5f);

        // 4. Convert final screen position to Canvas space
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 anchoredPos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, targetScreenPos, canvasCamera, out anchoredPos))
        {
            rectTransform.anchoredPosition = anchoredPos;
        }
    }

    private void SetRarityBackground(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: backgroundImage.sprite = backgroundCommon; break;
            case Rarity.Uncommon: backgroundImage.sprite = backgroundUncommon; break;
            case Rarity.Rare: backgroundImage.sprite = backgroundRare; break;
            case Rarity.Epic: backgroundImage.sprite = backgroundEpic; break;
            case Rarity.Mythic: backgroundImage.sprite = backgroundMythic; break;
        }

        switch (rarity)
        {
            case Rarity.Common: rarityGemImage.sprite = rarityGemCommon; break;
            case Rarity.Uncommon: rarityGemImage.sprite = rarityGemUncommon; break;
            case Rarity.Rare: rarityGemImage.sprite = rarityGemRare; break;
            case Rarity.Epic: rarityGemImage.sprite = rarityGemEpic; break;
            case Rarity.Mythic: rarityGemImage.sprite = rarityGemMythic; break;
        }
        if (rarityGemImage != null)
        {
            ImageTooltipTrigger gemTrigger = rarityGemImage.GetComponent<ImageTooltipTrigger>();
            if (gemTrigger == null)
            {
                gemTrigger = rarityGemImage.gameObject.AddComponent<ImageTooltipTrigger>();
            }
            gemTrigger.keywordID = rarity.ToString().ToLower();
        }
    }

    private void UpdateDynamicValues()
    {
        if (currentUnit.inCombat)
        {
            healthBar.SetHoverUIValues(currentUnit.GetCurrentHP(), currentUnit.Stats.MaxHP, currentUnit.GetCurrentShield());
            healthBar.SetTextVisible(true);
            cooldownBar.SetValues(currentUnit.GetCooldownTimer(), currentUnit.Stats.Cooldown);
        }
        else
        {
            healthBar.SetHoverUIValues(currentUnit.Stats.MaxHP, currentUnit.Stats.MaxHP, currentUnit.GetCurrentShield());
            healthBar.SetTextVisible(true);
            cooldownBar.SetValues(currentUnit.Stats.Cooldown, currentUnit.Stats.Cooldown);
        }
    }

    private void UpdateDynamicStats()
    {
        StatBlock stats = currentUnit.Stats;
        int currentEnergy = currentUnit.inCombat ? currentUnit.currentEnergy : stats.maxEnergy;

        if (lastEnergy == currentEnergy &&
            lastAttack == stats.Attack &&
            lastShield == stats.Shield &&
            lastHeal == stats.Heal &&
            lastPoison == stats.Poison &&
            lastBurn == stats.Burn &&
            lastCrit == stats.CritChance &&
            lastMulticast == stats.Multicast)
        {
            return; 
        }

        lastEnergy = currentEnergy;
        lastAttack = stats.Attack;
        lastShield = stats.Shield;
        lastHeal = stats.Heal;
        lastPoison = stats.Poison;
        lastBurn = stats.Burn;
        lastCrit = stats.CritChance;
        lastMulticast = stats.Multicast;

        System.Collections.Generic.List<string> topRow = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> botRow = new System.Collections.Generic.List<string>();

        if (currentUnit.Definition.isEnergy)
        {
            topRow.Add($"{TextIconUtility.FormatEnergy(currentEnergy)}/{stats.maxEnergy}");
            botRow.Add("ENERGY");
        }
        if (currentUnit.Stats.Tags.HasFlag(UnitTagFlags.Damage) && stats.Attack > 0)
        {
            topRow.Add(TextIconUtility.FormatAttack(stats.Attack));
            botRow.Add("ATK");
        }
        if (currentUnit.Stats.Tags.HasFlag(UnitTagFlags.Shield) && stats.Shield > 0)
        {
            topRow.Add(TextIconUtility.FormatShield(stats.Shield));
            botRow.Add("SHIELD");
        }
        if (currentUnit.Stats.Tags.HasFlag(UnitTagFlags.Heal) && stats.Heal > 0)
        {
            topRow.Add(TextIconUtility.FormatHeal(stats.Heal));
            botRow.Add("HEAL");
        }
        if (currentUnit.Stats.Tags.HasFlag(UnitTagFlags.Poison) && stats.Poison > 0)
        {
            topRow.Add(TextIconUtility.FormatPoison(stats.Poison));
            botRow.Add("POISON");
        }
        if (currentUnit.Stats.Tags.HasFlag(UnitTagFlags.Burn) && stats.Burn > 0)
        {
            topRow.Add(TextIconUtility.FormatBurn(stats.Burn));
            botRow.Add("BURN");
        }
        if (currentUnit.GetActiveDescription() != "" && stats.CritChance > 0)
        {
            topRow.Add(TextIconUtility.FormatCrit(stats.CritChance));
            botRow.Add("CRIT");
        }

        if (stats.Multicast > 1)
            multicastText.SetText(TextIconUtility.ParseDescription("[c_multicast]X" + (stats.Multicast)+"[/c]"));
        else
            multicastContainer.SetActive(false);

        string finalTop = string.Join("   ", topRow);
        string finalBot = "<size=60%><color=#A0A0A0>" + string.Join("        ", botRow) + "</color></size>";

        statText.SetText($"{finalTop}\n{finalBot}");
        UpdateAbilityDescriptions();
    }

    public void ToggleUpgradePreview()
    {
        if (activePreviewUI != null)
        {
            Destroy(activePreviewUI.gameObject);
            return;
        }

        if (!currentUnit.CanUpgradeTier()) return;

        // 1. Spawn a perfect clone of this UI
        activePreviewUI = Instantiate(this, transform.parent);
        activePreviewUI.isPreviewMode = true;

        foreach (GameObject obj in activePreviewUI.hideInPreviewMode)
        {
            if (obj != null) obj.SetActive(false);
        }

        Rarity originalRarity = currentUnit.CurrentRarity;
        currentUnit.PreviewRaritySwap(RarityScaling.GetNextRarity(originalRarity));

        activePreviewUI.Show(currentUnit);

        currentUnit.PreviewRaritySwap(originalRarity);

        activePreviewUI.UpdatePreviewPosition(this);
    }

    public void UpdatePreviewPosition(UnitHoverUI parentUI)
    {
        float offset = parentUI.rectTransform.rect.width + 20f;

        if (parentUI.useFixedPosition)
        {
            if (parentUI.rectTransform.anchorMin.x == 1)
                rectTransform.anchoredPosition = parentUI.rectTransform.anchoredPosition + new Vector2(-offset, 0);
            else
                rectTransform.anchoredPosition = parentUI.rectTransform.anchoredPosition + new Vector2(offset, 0);
        }
        else
        {
            if (parentUI.rectTransform.position.x > Screen.width / 2f)
                rectTransform.anchoredPosition = parentUI.rectTransform.anchoredPosition + new Vector2(-offset, 0);
            else
                rectTransform.anchoredPosition = parentUI.rectTransform.anchoredPosition + new Vector2(offset, 0);
        }
    }

    private void UpdateAbilityDescriptions()
    {
        string activeDesc = TextIconUtility.ParseDescription(currentUnit.GetActiveDescription());
        string passiveDesc = TextIconUtility.ParseDescription(currentUnit.GetPassiveDescription());

        if (currentUnit.currentSuffix != null)
        {
            string triggerText = currentUnit.GetMutationTriggerText();
            string actionPhrase = currentUnit.currentSuffix.GetActionPhrase(currentUnit, true);

            string suffixLabel = currentUnit.currentSuffix.suffixName;
            if (currentUnit.currentPrefix != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(currentUnit.currentPrefix.runeColor);
                suffixLabel = $"<color=#{hex}>{suffixLabel}</color>";
            }

            string mutationSentence = $"<br>{triggerText}{suffixLabel}: {actionPhrase}.";
            mutationSentence = TextIconUtility.ParseDescription(mutationSentence);

            if (currentUnit.Definition.isPassive)
                passiveDesc += mutationSentence;
            else
                activeDesc += mutationSentence;
        }

        if (!string.IsNullOrEmpty(activeDesc))
        {
            activeAbilityBox.SetActive(true);
            activeAbilityText.SetText(activeDesc);
        }
        else
        {
            activeAbilityBox.SetActive(false);
        }

        if (!string.IsNullOrEmpty(passiveDesc))
        {
            passiveAbilityBox.SetActive(true);
            passiveAbilityText.SetText(passiveDesc);
        }
        else
        {
            passiveAbilityBox.SetActive(false);
        }

        string mutationScalingDesc = currentUnit.GetMutationScalingText();
        if (!string.IsNullOrEmpty(mutationScalingDesc))
        {
            if (mutationDivider != null) mutationDivider.SetActive(true);
            if (mutationAbilityBox != null) mutationAbilityBox.SetActive(true);

            if (mutationAbilityText != null)
                mutationAbilityText.SetText(TextIconUtility.ParseDescription(mutationScalingDesc));
        }
        else
        {
            if (mutationDivider != null) mutationDivider.SetActive(false);
            if (mutationAbilityBox != null) mutationAbilityBox.SetActive(false);
        }
    }


}