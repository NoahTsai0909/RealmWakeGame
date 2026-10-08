using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static SceneLoader;

public class MapController : MonoBehaviour
{

    [Header("Event Display")]
    [SerializeField] private Transform eventButtonContainer;
    [SerializeField] private GameObject eventButtonPrefab;

    [Header("Event Info HUD")]
    [SerializeField] private GameObject eventInfoPanel; 
    [SerializeField] private TextMeshProUGUI infoTitleText;
    [SerializeField] private TextMeshProUGUI infoDescText;

    [Header("Encounter Preview")]
    [SerializeField] private GameObject previewOverlay; 
    [SerializeField] private Button closePreviewButton;
    [SerializeField] public GridManager previewGrid;
    [SerializeField] private TacticBarManager enemyTacticBarManager;

    [SerializeField] private AudioClip levelUpSound;

    [Header("Transition Overlay")]
    [SerializeField] private Image blackScreenOverlay;

    [Tooltip("How far from the portal should the HUD appear?")]
    [SerializeField] private Vector3 hoverOffset = new Vector3(0, 100f, 0);

    public static MapController Instance { get; private set; }

    public bool isTransitioning = false;
    private bool isPinned = false;

    void Awake()
    {
        Instance = this;
        if (RunHUDManager.Instance != null)
        {
            RunHUDManager.Instance.ResetAndShow();
        }
    }

    void Start()
    {
        if (CheckLastChance()) return;
        if (CheckLevelUp()) return;

        UpdateUI();

        DisplayEvents(RunManager.Instance.currentDailyEvents);

        if (closePreviewButton != null)
        {
            closePreviewButton.onClick.AddListener(ClosePreview);
        }

        if (previewOverlay != null) previewOverlay.SetActive(false);
        if (enemyTacticBarManager != null) enemyTacticBarManager.gameObject.SetActive(false);
        RunManager.Instance.SetAdventureMusic();
        if (TutorialManager.Instance != null)
        {
            Transform hudCanvas = RunHUDManager.Instance.transform;
            List<TutorialStep> combatSequence = new List<TutorialStep>
            {
                new TutorialStep {
                    key = "MapSceneIntro",
                    title = "Your Adventure Begins",
                    description = "The Realmwake shattered the boundaries between four worlds, colliding them into one. Venture into this newly formed world to uncover the disaster’s secrets. Rival expeditions from all four regions pursue their own ambitions—even those from your homeland may stand in your way.\r\n\r\nGather a formidable squad, survive 12 days, and defeat the adventure’s final boss on Day 12.",
                    highlightTarget = null
                },
                new TutorialStep {
                    key = "MapSceneRunHUD",
                    title = "Status Information",
                    description = "Key information about the current status of your run is located in the bar up top.",
                    highlightTarget = hudCanvas.Find("TopBar")
                },
                new TutorialStep {
                    key = "RunHUDPlayerGold",
                    title = "Gold",
                    description = "Spend [GOLD] to recruit units in Shop events. Spend wisely to build a squad that can withstand the battles ahead!",
                    highlightTarget = hudCanvas.Find("TopBar/playerBasicStats/playerGoldContainer")
                },
                new TutorialStep {
                    key = "RunHUDSquadButton",
                    title = "Your Squad",
                    description = "Click here to view your owned units and rearrange your squad’s formation.",
                    highlightTarget = hudCanvas.Find("TopBar/rightSideButtons/SquadButton")
                },
                new TutorialStep {
                    key = "MapSceneEventSelection",
                    title = "Choose Your Next Event",
                    description = "Each phase presents 3 events. Choose one to venture into.\r\n\r\nHover over an event to read its description and view its base rewards. Events come in three types: Shop, Story, and Combat.",
                    highlightTarget = null
                }
            };

            TutorialManager.Instance.StartTutorialSequence(combatSequence);
        }
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (isPinned)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                isPinned = false;

                CanvasGroup cg = eventInfoPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;

                HideEventInfo(); // Force it to hide once unpinned
                if (TooltipUIManager.Instance != null) TooltipUIManager.Instance.Hide();
            }
            return;
        }
        if (eventInfoPanel.activeSelf && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isPinned = true;

            CanvasGroup cg = eventInfoPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;
        }
    }

    public void UpdateUI()
    {
    }

    public void DisplayEvents(List<BaseEventSO> events)
    {
        foreach (Transform child in eventButtonContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < events.Count; i++)
        {
            GameObject buttonObj = Instantiate(eventButtonPrefab, eventButtonContainer);
            PortalArtifactUI portalUI = buttonObj.GetComponent<PortalArtifactUI>();

            if (portalUI != null)
            {
                portalUI.Initialize(events[i]);

                if (events.Count == 3 && i == 1)
                {
                    portalUI.SetElevation(60f);
                }
            }
        }

        UpdateUI();
    }

    void OnEnable()
    {
        isPinned = false;

        if (RunManager.Instance != null)
        {
            if (RunManager.Instance.eventInProgress)
            {
                BaseEventSO abandonedEvent = RunManager.Instance.selectedEvent;

                if (abandonedEvent is CombatEventSO combatEvent)
                {
                    Debug.LogWarning("Player fled combat! Applying damage penalty.");

                    RunManager.Instance.Stats.PlayerHealth -= RunManager.Instance.Stats.CurrentDay;

                    if (RunManager.Instance.Stats.PlayerHealth <= 0)
                    {
                        if (!RunManager.Instance.hasUsedLastChance)
                        {
                            RunManager.Instance.hasUsedLastChance = true;
                            RunManager.Instance.Stats.PlayerHealth = 1;
                            RunManager.Instance.lastChanceEvent.OnSelected();
                            return; 
                        }
                        else
                        {
                            SaveLoadManager.DeleteSave();
                            SceneLoader.Instance.LoadScene(GameScene.RunSummaryScene);
                            return; 
                        }
                    }

                    RunManager.Instance.CompleteBattleEvent();
                }
                else if (abandonedEvent is ShopEventSO shopEvent)
                {
                    Debug.LogWarning("Player abandoned a shop.");
                    RunManager.Instance.shopState = null; 
                    RunManager.Instance.CompleteRegularEvent();
                }
                else 
                {
                    Debug.LogWarning("Player abandoned an event.");
                    RunManager.Instance.CompleteRegularEvent();
                }

                RunManager.Instance.eventInProgress = false;
                RunManager.Instance.selectedEvent = null;
            }
            if (RunManager.Instance.currentDailyEvents.Count == 0)
            {
                RunManager.Instance.GenerateDailyEvents();
            }

            DisplayEvents(RunManager.Instance.currentDailyEvents);
        }

        isTransitioning = false;
        UpdateUI();
    }


    private bool CheckLastChance()
    {
        if (RunManager.Instance == null) return false;
        if (RunManager.Instance.Stats.PlayerHealth <= 0 && !RunManager.Instance.hasUsedLastChance)
        {
            RunManager.Instance.hasUsedLastChance = true;
            RunManager.Instance.Stats.PlayerHealth = 1;

            RunManager.Instance.lastChanceEvent.OnSelected();
            if (TutorialManager.Instance != null)
            {
                List<TutorialStep> combatSequence = new List<TutorialStep>
            {
                new TutorialStep {
                    key = "LastChanceEvent",
                    title = "Last Chance",
                    description = "The first time your health reaches zero or below, you are pulled back from the brink of defeat! Your health is restored to 1, and you may choose a boon to help you survive.\n\nThis rescue is available only once per run and cannot save you from defeat in the final boss battle.",
                    highlightTarget = null
                }
            };
                return true;
            }
        }

        return false;
    }


    private bool CheckLevelUp()
    {
        if (RunManager.Instance == null || RunManager.Instance.currentRegionTree == null) return false;

        int targetIndex = RunManager.Instance.Stats.PlayerLevel - 1;
        int safeIndex = Mathf.Clamp(targetIndex, 0, RunManager.Instance.currentRegionTree.levelNodes.Count - 1);

        if (RunManager.Instance.currentRegionTree.levelNodes.Count == 0) return false;

        LevelUpEventSO nextLevelEvent = RunManager.Instance.currentRegionTree.levelNodes[safeIndex];

        if (RunManager.Instance.Stats.Experience >= nextLevelEvent.xpRequired)
        {
            RunManager.Instance.Stats.Experience -= nextLevelEvent.xpRequired;
            RunManager.Instance.Stats.PlayerLevel++;
            if (levelUpSound != null)
            {
                AudioManager.Instance.PlayJingle(levelUpSound, 1f);
            }
            nextLevelEvent.OnSelected();
            if (TutorialManager.Instance != null)
            {
                Transform hudCanvas = RunHUDManager.Instance.transform;
                List<TutorialStep> combatSequence = new List<TutorialStep>
            {
                new TutorialStep {
                    key = "RunHUDPlayerLevel",
                    title = "Player Level & Experience",
                    description = "Gain [c_experience]experience[/c] to fill your experience bar and [c_level]level up[/c]. Each level up lets you choose a reward to strengthen your squad or support your adventure!",
                    highlightTarget = hudCanvas.Find("TopBar/playerBasicStats/playerLevelContainer")
                }
            };

                TutorialManager.Instance.StartTutorialSequence(combatSequence);
            }
            return true; 
        }

        return false;
    }

    public void ShowEventInfo(BaseEventSO eventData, Vector3 targetPosition)
    {
        if (isPinned) return;
        if (eventData == null) return;

        infoTitleText.text = eventData.eventName;
        string finalDescription = eventData.description;
        finalDescription += $"\n\n<size=70%>Completion: [EXPERIENCE] {eventData.experienceReward}";
        if (eventData is CombatEventSO combatEvent && combatEvent.goldReward > 0)
        {
            finalDescription += $" [GOLD] {combatEvent.goldReward}";
        }
        finalDescription += "</size>";
        infoDescText.SetText(TextIconUtility.ParseDescription(finalDescription));

        Canvas canvas = eventInfoPanel.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        eventInfoPanel.transform.position = targetPosition + (hoverOffset * scale);
        CanvasGroup cg = eventInfoPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        eventInfoPanel.SetActive(true);
    }

    public void HideEventInfo()
    {
        if (isPinned) return; //Refuse to close if the player pinned it

        eventInfoPanel.SetActive(false);
    }

    public void SetOverlayAlpha(float alpha)
    {
        if (blackScreenOverlay != null)
        {
            // Turn it on the moment we need it
            if (!blackScreenOverlay.gameObject.activeSelf && alpha > 0f)
            {
                blackScreenOverlay.gameObject.SetActive(true);
            }

            blackScreenOverlay.color = new Color(0, 0, 0, alpha);
        }
    }

    public void PreviewEncounter(EncounterDefinition encounter)
    {
        if (encounter == null) return;

        if (previewOverlay != null) previewOverlay.SetActive(true);

        if (eventButtonContainer != null) eventButtonContainer.gameObject.SetActive(false);
        if (closePreviewButton != null) closePreviewButton.gameObject.SetActive(true);
        if (previewGrid != null)
        {
            previewGrid.gameObject.SetActive(true);
        }
        previewGrid.ClearAllUnits();

        var runtimeEnemyList = encounter.GetRuntimeEnemyPlacements(RunManager.Instance.Stats.CurrentDay);

        foreach (var placement in runtimeEnemyList)
        {
            if (placement.unitData == null || placement.unitData.definition == null) continue;
            UnitInstance unit = Instantiate(placement.unitData.definition.unitPrefab);
            unit.InitializeEnemy(placement.unitData);
            unit.myPlacement = placement;
            unit.EnterCombat(previewGrid, placement.row, placement.col, false, false);
        }

        if (enemyTacticBarManager != null)
        {
            enemyTacticBarManager.gameObject.SetActive(true);
            enemyTacticBarManager.ClearAllTactics();

            List<RunManager.TacticPlacement> previewTactics = new List<RunManager.TacticPlacement>();
            if (encounter.enemyTactics != null)
            {
                previewTactics.AddRange(encounter.enemyTactics);
            }

            AdventureDefinitionSO currentAdventure = RunManager.Instance.allAdventures.FirstOrDefault(a => a.adventureName == RunManager.Instance.activeAdventureName);
            if (currentAdventure != null && currentAdventure.modifierPhantomTactics != null)
            {
                int nextOrderIndex = previewTactics.Count;
                foreach (var map in currentAdventure.modifierPhantomTactics)
                {
                    if (RunManager.Instance.HasDifficultyModifier(map.modifier))
                    {
                        previewTactics.Add(new RunManager.TacticPlacement
                        {
                            orderIndex = nextOrderIndex,
                            tacticData = new RunManager.TacticSaveData
                            {
                                definition = map.phantomTactic,
                                rarity = Rarity.Common,
                                id = System.Guid.NewGuid()
                            }
                        });
                        nextOrderIndex++;
                    }
                }
            }

            foreach (var placement in previewTactics)
            {
                if (placement.tacticData == null || placement.tacticData.definition == null || placement.tacticData.definition.tacticPrefab == null) continue;

                TacticInstance tactic = Instantiate(placement.tacticData.definition.tacticPrefab);
                tactic.InitializeFromSaveData(placement.tacticData);
                tactic.myPlacement = placement;

                enemyTacticBarManager.AddTactic(tactic);
            }
        }

        if (previewGrid != null) previewGrid.RefreshAllAuras();
        if (enemyTacticBarManager != null) enemyTacticBarManager.RefreshAllTacticAuras();
    }

    public void ClosePreview()
    {
        previewGrid.ClearAllUnits();
        previewGrid.gameObject.SetActive(false);
        if (closePreviewButton != null) closePreviewButton.gameObject.SetActive(false);
        if (previewOverlay != null) previewOverlay.SetActive(false);

        if (enemyTacticBarManager != null)
        {
            enemyTacticBarManager.ClearAllTactics();
            enemyTacticBarManager.gameObject.SetActive(false);
        }
        if (eventButtonContainer != null) eventButtonContainer.gameObject.SetActive(true);
    }

}
