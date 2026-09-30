using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SceneLoader;

public class EventSceneController : MonoBehaviour
{
    [Header("Basic UI")]
    [SerializeField] private TextMeshProUGUI eventNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image eventBackgroundRenderer;

    [Header("Dynamic Content")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private Transform eventSpriteAnchor;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Unit Selector UI")]
    [SerializeField] private GameObject unitSelectorPanel;
    [SerializeField] private Transform unitSelectorContentParent;
    [SerializeField] private Button closeSelectorButton;

    private BaseEventSO currentEvent;

    // We now track a LIST of previews, since there can be multiple on screen
    private List<UnitInstance> spawnedPreviews = new();
    private List<TacticInstance> spawnedTacticPreviews = new();
    private GameObject spawnedIllustration;

    void Start()
    {
        currentEvent = RunManager.Instance.selectedEvent;

        if (currentEvent == null)
        {
            SceneLoader.Instance.LoadScene(GameScene.MapScene);
            return;
        }
        if (RunHUDManager.Instance != null)
        {
            RunHUDManager.Instance.ResetAndShow();
        }

        eventNameText.text = currentEvent.eventName;
        descriptionText.text = currentEvent.description;

        if (eventBackgroundRenderer != null && currentEvent.eventBackgroundImage != null)
            eventBackgroundRenderer.sprite = currentEvent.eventBackgroundImage;

        LoadEventChoices();
    }

    private void LoadEventChoices()
    {
        if (currentEvent is StoryEventSO storyEvent)
        {
            // The illustration usually stays the same across pages, so load it once here
            if (storyEvent.eventIllustration != null)
            {
                SpawnEventIllustration(storyEvent.eventIllustration);
            }

            // Start the event on the first page
            LoadEventPage(0);
        }
    }

    public void LoadEventPage(int pageIndex)
    {
        if (currentEvent is StoryEventSO storyEvent)
        {
            if (pageIndex < 0 || pageIndex >= storyEvent.pages.Count)
            {
                Debug.LogError($"Event {storyEvent.eventName} does not have a page at index {pageIndex}!");
                return;
            }

            EventPage currentPage = storyEvent.pages[pageIndex];

            // 1. Update narrative text
            descriptionText.text = currentPage.promptText;

            // 2. Clear old buttons and old previews
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
            // Ensure we destroy the physical unit instances so they don't pile up in memory
            foreach (var preview in spawnedPreviews)
            {
                if (preview != null) Destroy(preview.gameObject);
            }
            spawnedPreviews.Clear();
            foreach (var tacticPreview in spawnedTacticPreviews)
            {
                if (tacticPreview != null) Destroy(tacticPreview.gameObject);
            }
            spawnedTacticPreviews.Clear();
            // 3. Spawn new choices
            foreach (EventChoice choice in currentPage.choices)
            {
                Button newButton = Instantiate(choiceButtonPrefab, contentParent);
                TextMeshProUGUI btnText = newButton.GetComponentInChildren<TextMeshProUGUI>();

                string displayText = choice.buttonText;
                bool isInteractable = true;

                if (choice.condition != null)
                {
                    isInteractable = choice.condition.IsMet();
                    if (!isInteractable)
                    {
                        displayText += $"\n<size=70%><color=#FF4444>({choice.condition.GetRequirementText()})</color></size>";
                    }
                }

                if (btnText != null) btnText.SetText(TextIconUtility.ParseDescription(displayText));
                newButton.interactable = isInteractable;

                EventContext choiceContext = new EventContext();
                choiceContext.uiController = this;

                int uniqueSeed = RunManager.Instance.runSeed
                    + RunManager.Instance.Stats.CurrentDay
                    + RunManager.Instance.regularEventsCompleted
                    + choice.buttonText.GetHashCode();
                Random.InitState(uniqueSeed);

                if (choice.generateRandomUnitPreview)
                {
                    Region? excludedRegion = null;
                    if (choice.excludePlayerRegion && RunManager.Instance != null)
                    {
                        excludedRegion = RunManager.Instance.playerRegion;
                    }
                    Region? targetRegion = choice.anyRegion ? null : choice.randomRegion;

                    UnitSaveData randomData = UnitGenerationService.GenerateUnit(
                        region: targetRegion,
                        requiredTags: choice.preferredTags,
                        bypassExclusive: false,
                        excludedRegion: excludedRegion
                    );

                    choiceContext.generatedUnit = randomData;
                    if (randomData != null && randomData.definition != null)
                    {
                        SpawnUnitOnButton(randomData, newButton);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to generate preview unit for choice: {choice.buttonText}. Check region/tag constraints.");
                    }
                }
                else if (choice.previewUnit != null)
                {
                    Rarity finalRarity;
                    if (choice.rollRandomRarity)
                    {
                        int day = RunManager.Instance.Stats.CurrentDay;
                        DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day);
                        finalRarity = RarityDistributionTable.RollRarity(dist);
                    }
                    else
                    {
                        finalRarity = choice.previewRarity;
                    }

                    UnitSaveData generatedData = new UnitSaveData { definition = choice.previewUnit, rarity = finalRarity };
                    choiceContext.generatedUnit = generatedData;
                    SpawnUnitOnButton(generatedData, newButton);
                }
                else if (choice.generateRandomTacticPreview)
                {
                    RunManager.TacticSaveData randomData = TacticGenerationService.GenerateTactic(choice.randomRegion);
                    choiceContext.generatedTactic = randomData;
                    SpawnTacticOnButton(randomData, newButton);
                }
                else if (choice.previewTactic != null)
                {
                    Rarity finalRarity;
                    if (choice.rollRandomRarity)
                    {
                        int day = RunManager.Instance.Stats.CurrentDay;
                        DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day);
                        finalRarity = RarityDistributionTable.RollRarity(dist);
                    }
                    else
                    {
                        finalRarity = choice.previewRarity;
                    }

                    if (RunManager.Instance != null)
                    {
                        var existingTactic = RunManager.Instance.playerTactics.FirstOrDefault(p =>
                            p.tacticData != null && p.tacticData.definition == choice.previewTactic);
                        if (existingTactic != null) finalRarity = existingTactic.tacticData.rarity;
                    }

                    RunManager.TacticSaveData generatedData = new RunManager.TacticSaveData { definition = choice.previewTactic, rarity = finalRarity };
                    choiceContext.generatedTactic = generatedData;
                    SpawnTacticOnButton(generatedData, newButton);
                }
                UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

                newButton.onClick.AddListener(() => ExecutePlayerChoice(choice, choiceContext));
            }
        }
    }

    private void ExecutePlayerChoice(EventChoice selectedChoice, EventContext context)
    {
        if (selectedChoice.outcomes != null && selectedChoice.outcomes.Count > 0)
        {
            foreach (var outcome in selectedChoice.outcomes)
            {
                if (outcome != null)
                {
                    outcome.ExecuteOutcome(context);
                }
            }
            if (context.keepEventOpen)
            {
                return;
            }
        }

        CompleteEvent();
    }

    private void SpawnUnitOnButton(UnitSaveData unitData, Button parentButton, bool useMask = false)
    {
        Transform anchor = parentButton.transform.Find("UnitAnchor");
        if (anchor == null) return;

        UnitInstance preview = Instantiate(unitData.definition.unitPrefab, anchor);

        // The preview is now initialized with the real rolled rarity
        preview.InitializeFromSaveData(unitData);
        preview.isPlayer = true;
        preview.enabled = false;

        preview.transform.localPosition = Vector3.zero;
        preview.transform.localScale = Vector3.one * 25f;
        if (preview.Visuals != null) preview.Visuals.SetBaseScale(preview.transform.localScale);

        if (preview.Visuals != null)
        {
            preview.Visuals.SetBaseScale(preview.transform.localScale);
            preview.Visuals.SyncSortingOrder(100);
        }
        SpriteRenderer[] srs = preview.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.sortingOrder = 205;
            sr.maskInteraction = useMask ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
        }
        spawnedPreviews.Add(preview);
    }

    private void SpawnTacticOnButton(RunManager.TacticSaveData tacticData, Button parentButton)
    {
        Transform anchor = parentButton.transform.Find("UnitAnchor");
        if (anchor == null) return;

        TacticInstance preview = Instantiate(tacticData.definition.tacticPrefab, anchor);

        preview.InitializeFromSaveData(tacticData);
        preview.isPlayer = true;
        preview.enabled = false;

        preview.transform.localPosition = Vector3.zero;

        // Scale it up so it matches the size of unit previews
        preview.transform.localScale = Vector3.one * 25f;

        // Since tactics use Canvases, override the Canvas sorting order
        Canvas canvas = preview.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        spawnedTacticPreviews.Add(preview);
    }

    private void SpawnEventIllustration(Sprite artwork)
    {

        spawnedIllustration = new GameObject("StoryIllustration");

        spawnedIllustration.transform.SetParent(eventSpriteAnchor, false);

        SpriteRenderer renderer = spawnedIllustration.AddComponent<SpriteRenderer>();
        renderer.sprite = artwork;

        renderer.sortingOrder = 50;

        spawnedIllustration.transform.localPosition = Vector3.zero;

        spawnedIllustration.transform.localScale = Vector3.one * 7f;
    }

    public void ShowUnitSelectorPanel(UnitTargetEffectSO effectToApply, EventOutcomeSO onSuccessOutcome, EventContext context)
    {
        unitSelectorPanel.SetActive(true);

        contentParent.gameObject.SetActive(false);

        foreach (Transform child in unitSelectorContentParent)
        {
            Destroy(child.gameObject);
        }

        closeSelectorButton.onClick.RemoveAllListeners();
        closeSelectorButton.onClick.AddListener(() =>
        {
            unitSelectorPanel.SetActive(false);
            contentParent.gameObject.SetActive(true); 
        });


        System.Action<RunManager.UnitPlacement> CreateUnitButton = (placement) =>
        {

            if (placement == null || placement.unitData == null || placement.unitData.definition == null) return;

            Button newButton = Instantiate(choiceButtonPrefab, unitSelectorContentParent);
            TextMeshProUGUI btnText = newButton.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = $"Select {placement.unitData.definition.unitName} (Tier {placement.unitData.rarity})";

            SpawnUnitOnButton(placement.unitData, newButton, true);

            newButton.onClick.AddListener(() =>
            { 
                effectToApply.ApplyEffect(placement);

                if (onSuccessOutcome != null)
                {
                    context.keepEventOpen = false; 
                    onSuccessOutcome.ExecuteOutcome(context);
                }

                unitSelectorPanel.SetActive(false);
                CompleteEvent();
            });
        };

        foreach (var placement in RunManager.Instance.playerTeamPlacements)
        {
            CreateUnitButton(placement);
        }

        foreach (var placement in RunManager.Instance.playerBenchPlacements)
        {
            CreateUnitButton(placement);
        }
    }


    public void CompleteEvent()
    {
        spawnedPreviews.Clear();

        spawnedIllustration = null;
        foreach (var preview in spawnedPreviews)
        {
            if (preview != null) Destroy(preview.gameObject);
        }
        spawnedPreviews.Clear();

        foreach (var tacticPreview in spawnedTacticPreviews)
        {
            if (tacticPreview != null) Destroy(tacticPreview.gameObject);
        }
        spawnedTacticPreviews.Clear();

        currentEvent.OnCompleted();
        SceneLoader.Instance.LoadScene(GameScene.MapScene);
    }
}