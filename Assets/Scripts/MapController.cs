using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SceneLoader;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        // 1. If currently pinned, listen for unpin triggers
        if (isPinned)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame ||
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                isPinned = false;

                // Turn raycasts back off so it doesn't cause glitches when it unpins!
                CanvasGroup cg = eventInfoPanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;

                HideEventInfo(); // Force it to hide once unpinned
                if (TooltipUIManager.Instance != null) TooltipUIManager.Instance.Hide();
            }
            return;
        }

        // 2. regular pinning for the main panel
        if (eventInfoPanel.activeSelf && Keyboard.current.tKey.wasPressedThisFrame)
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
            // === ANTI-SAVE-SCUM LOGIC ===
            if (RunManager.Instance.eventInProgress)
            {
                BaseEventSO abandonedEvent = RunManager.Instance.selectedEvent;

                if (abandonedEvent is CombatEventSO combatEvent)
                {
                    Debug.LogWarning("Player fled combat! Applying damage penalty.");

                    // 1. Apply the exact same penalty as losing in gameManager
                    RunManager.Instance.Stats.PlayerHealth -= RunManager.Instance.Stats.CurrentDay;

                    // 2. Did the penalty kill them?
                    if (RunManager.Instance.Stats.PlayerHealth <= 0)
                    {
                        if (!RunManager.Instance.hasUsedLastChance)
                        {
                            // Trigger Last Chance
                            RunManager.Instance.hasUsedLastChance = true;
                            RunManager.Instance.Stats.PlayerHealth = 1;
                            RunManager.Instance.lastChanceEvent.OnSelected();
                            return; // Stop right here, we are changing scenes!
                        }
                        else
                        {
                            // They are dead for good. Wipe save and go to summary.
                            SaveLoadManager.DeleteSave();
                            SceneLoader.Instance.LoadScene(GameScene.RunSummaryScene);
                            return; // Stop right here!
                        }
                    }

                    // 3. They survived the penalty! Finish the battle phase and advance the day.
                    RunManager.Instance.CompleteBattleEvent();
                }
                else if (abandonedEvent is ShopEventSO shopEvent)
                {
                    Debug.LogWarning("Player abandoned a shop.");
                    RunManager.Instance.shopState = null; // Clean up the shop memory
                    RunManager.Instance.CompleteRegularEvent();
                }
                else // Story Event or anything else
                {
                    Debug.LogWarning("Player abandoned an event.");
                    RunManager.Instance.CompleteRegularEvent();
                }

                // Clear the flag so the punishment only happens once
                RunManager.Instance.eventInProgress = false;
                RunManager.Instance.selectedEvent = null;
            }

            // Generate new events if none exist (e.g., we just advanced to a new day)
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

            return true;
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

            if (encounter.enemyTactics != null)
            {
                foreach (var placement in encounter.enemyTactics)
                {
                    if (placement.tacticData == null || placement.tacticData.definition == null) continue;

                    TacticInstance tactic = Instantiate(placement.tacticData.definition.tacticPrefab);

                    tactic.InitializeFromSaveData(placement.tacticData);
                    tactic.myPlacement = placement;

                    enemyTacticBarManager.AddTactic(tactic);
                }
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
