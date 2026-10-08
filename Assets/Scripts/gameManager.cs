using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CombatEventBus;
using static SceneLoader;
using static UnityEngine.Rendering.DebugUI.Table;

public class gameManager : MonoBehaviour
{

    [Header("Grids")]
    public GridManager playerGrid;
    public GridManager enemyGrid;
    public GridManager benchGrid;
    [Header("Tactic Bars")]
    public TacticBarManager playerTacticBarManager;
    public TacticBarManager enemyTacticBarManager;

    [Header("UI Manager")]
    [SerializeField] private BattleUIManager battleUIManager;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject unitStatsWindowObject;
    [SerializeField] private CombatResultUI combatResultUI;

    [Header("UI & Drag Managers")]
    [SerializeField] private DragAndDropManager dragManager; 
    [SerializeField] private ProvisionManager provisionManager; 
    [SerializeField] private Button startCombatButton;
    [SerializeField] private SellZone sellZone;
    [SerializeField] private LootSummaryUI lootSummaryUI;
    private MutationPrefixSO pendingUnitRewardPrefix;
    private MutationSuffixSO pendingUnitRewardSuffix;


    [Header("Combat Settings")]
    [SerializeField] private float endCombatDelay = 1.0f;
    private int enemiesKilledThisCombat = 0;
    [Header("Combat Speed UI")]
    [SerializeField] private Button speed1xButton;
    [SerializeField] private Button speed1_5xButton;
    [SerializeField] private Button speed2xButton;

    [Header("Disaster System")]
    [SerializeField] private DisasterManager disasterManager;

    [Header("Audio")]
    [SerializeField] private AudioClip combatMusic;

    private bool combatActive = true;
    public bool isCombatActive() => combatActive;

    private UnitDefinition pendingUnitRewardDef;
    private Rarity pendingUnitRewardRarity;

    private TacticDefinition pendingTacticRewardDef;
    private Rarity pendingTacticRewardRarity;
    private int goldFromKills = 0;

    void Start()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (unitStatsWindowObject != null) unitStatsWindowObject.SetActive(false);
        if (startCombatButton != null)
        {
            startCombatButton.onClick.AddListener(() =>
            {
                StartActualCombat();
            });
        }
        if (speed1xButton != null) speed1xButton.onClick.AddListener(() => SetCombatSpeed(0));
        if (speed1_5xButton != null) speed1_5xButton.onClick.AddListener(() => SetCombatSpeed(1));
        if (speed2xButton != null) speed2xButton.onClick.AddListener(() => SetCombatSpeed(2));
        UpdateSpeedButtonVisuals(PlayerPrefs.GetInt("CombatSpeedIndex", 0));

        combatActive = false;
        TeamDefinition playerTeam = RunManager.Instance.GetTeamForCombat();
        EncounterDefinition currentEncounter = RunManager.Instance.currentEncounter;
        if (playerTeam != null && currentEncounter != null)
        {
            InitializeBattlefield(playerTeam, currentEncounter, false);
            InitializeBench();
            InitializeTacticsBar();
            InitializeEnemyTacticsBar(currentEncounter);
            playerGrid.RefreshAllAuras();
            playerGrid.RefreshAllAuras();
            if (benchGrid != null) benchGrid.RefreshAllAuras();
            if (playerTacticBarManager != null) playerTacticBarManager.RefreshAllTacticAuras();
            if (enemyTacticBarManager != null) enemyTacticBarManager.RefreshAllTacticAuras();
        
        }
        if (!HasLivingUnits(playerGrid))
        {
            CheckCombatEnd();
        }
        if (battleUIManager != null)
        {
            battleUIManager.Initialize(playerGrid, enemyGrid);
        }
        CombatEventBus.OnCombatEvent += OnCombatEvent;
        if (RunHUDManager.Instance != null)
        {
            RunHUDManager.Instance.HideSquadButton();
            if (unitStatsWindowObject != null)
            {
                RunHUDManager.Instance.EnableInspectStats(unitStatsWindowObject);
            }
        }
        AudioManager.Instance.StopMusicWithFade(1.5f);
        if (TutorialManager.Instance != null)
        {
            List<TutorialStep> combatSequence = new List<TutorialStep>
            {
                new TutorialStep {
                    key = "CombatIntro",
                    title = "Combat Preparations",
                    description = "Position your units before starting battle. The fight ends when one side has no units left on the battlefield.",
                    highlightTarget = null
                },
                new TutorialStep {
                    key = "CombatBench",
                    title = "Bench",
                    description = "Your bench holds up to 5 reserve units. They stay out of battle until you drag and drop them onto your team grid, swapping them with deployed units during combat.",
                    highlightTarget = benchGrid.transform
                },
                new TutorialStep {
                    key = "CombatPlayerGrid",
                    title = "Player Team",
                    description = "Deploy up to 9 units on this grid. Their positions determine which allies and enemies their abilities can reach.",
                    highlightTarget = playerGrid.transform
                },
                new TutorialStep {
                    key = "CombatEnemyGrid",
                    title = "Enemy Team",
                    description = "This grid holds up to 9 enemy units. Inspect their abilities and positions to help plan your formation.",
                    highlightTarget = enemyGrid.transform
                },
                new TutorialStep {
                    key = "CombatTacticsBar",
                    title = "Tactics Bar",
                    description = "Both teams can have tactics that influence battle through passive effects or active abilities.",
                    highlightTarget = playerTacticBarManager.transform
                },
                new TutorialStep {
                    key = "CombatStartButton",
                    title = "Ready for Battle",
                    description = "When you’re happy with your formation, click here to start battle!",
                    highlightTarget = startCombatButton.transform
                }
            };

            TutorialManager.Instance.StartTutorialSequence(combatSequence);
        }   
    }

    private void OnDestroy()
    {
        CombatEventBus.OnCombatEvent -= OnCombatEvent;
    }

    private void OnCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (type == CombatEventBus.CombatEventType.UnitDied && combatActive)
        {
            if (target != null && !target.isPlayer)
            {
                enemiesKilledThisCombat++;
            }
            playerGrid.RefreshAllAuras();
            enemyGrid.RefreshAllAuras();
            CheckCombatEnd();
        }
    }

    private void CheckCombatEnd()
    {
        if (!combatActive) return;

        bool playerHasUnits = HasLivingUnits(playerGrid);
        bool enemyHasUnits = HasLivingUnits(enemyGrid);

        if (!playerHasUnits && !enemyHasUnits)
        {
            // Draw - both teams dead
            EndCombat(true, false);
        }
        else if (!playerHasUnits)
        {
            // Player lost
            EndCombat(false, true);
        }
        else if (!enemyHasUnits)
        {
            // Player won
            EndCombat(true, false);
        }
    }

    private bool HasLivingUnits(GridManager grid)
    {
        // Check if grid has any non-null units (alive)
        List<UnitInstance> units = grid.GetAllUnits();
        return units.Count > 0;
    }

    private void EndCombat(bool playerWon, bool isDraw)
    {
        if (!combatActive) return;
        combatActive = false;
        CombatEventBus.PublishCombatEnd();
        if (playerTacticBarManager != null) playerTacticBarManager.StopCombat();
        if (enemyTacticBarManager != null) enemyTacticBarManager.StopCombat();

        foreach (var unit in playerGrid.GetAllUnits()) if (unit != null) unit.inCombat = false;
        foreach (var unit in enemyGrid.GetAllUnits()) if (unit != null) unit.inCombat = false;

        TransferCombatStatsToRunManager();
        if (disasterManager != null) disasterManager.StopDisaster();
        Time.timeScale = 0.5f;
        AudioManager.Instance.StopMusicWithFade(0.5f);
        //Give incremental gold only
        var combatEvent = RunManager.Instance.selectedEvent as CombatEventSO;
        if (combatEvent != null)
        {
            goldFromKills = Mathf.Min(enemiesKilledThisCombat, combatEvent.goldReward);
            RunManager.Instance.Stats.CurrentGold += goldFromKills;

            if (playerWon)
            {
                RunManager.Instance.Stats.CurrentGold += (combatEvent.goldReward - goldFromKills);
                RunManager.Instance.Stats.Experience += combatEvent.experienceReward;
                if (TutorialManager.Instance != null)
                {
                    Transform hudCanvas = RunHUDManager.Instance.transform;
                    List<TutorialStep> combatSequence = new List<TutorialStep>
                    {
                        new TutorialStep {
                            key = "RunHUDStatsButton",
                            title = "Combat Statistics",
                            description = "Click here to track each unit’s damage, mitigation, and utility. These statistics update live as the battle unfolds!",
                            highlightTarget = hudCanvas.Find("TopBar/rightSideButtons/inspectStatsButton")
                        }
                    };
                    TutorialManager.Instance.StartTutorialSequence(combatSequence);
                }
            }
            else
            {
                RunManager.Instance.Stats.PlayerHealth -= RunManager.Instance.Stats.CurrentDay;
                if (TutorialManager.Instance != null)
                {
                    Transform hudCanvas = RunHUDManager.Instance.transform;
                    List<TutorialStep> combatSequence = new List<TutorialStep>
                    {
                        new TutorialStep {
                            key = "RunHUDPlayerHealth",
                            title = "Player Health",
                            description = "You lose [c_playerhealth]health[/c] equal to the current [c_day]day[/c] when you’re defeated in combat. If your health reaches zero, you’ll be forced to retire from the current adventure!",
                            highlightTarget = hudCanvas.Find("TopBar/playerBasicStats/playerHealthContainer")
                        }
                    };
                    TutorialManager.Instance.StartTutorialSequence(combatSequence);
                }
            }
        }

        StartCoroutine(TransitionAfterDelay(playerWon));
    }

    private IEnumerator TransitionAfterDelay(bool playerWon)
    {
        yield return new WaitForSeconds(endCombatDelay);
        ResetBattlefieldToStasis();

        if (combatResultUI != null) combatResultUI.ShowResult(playerWon);
        yield return new WaitForSeconds(1.4f);
        if (lootSummaryUI != null)
        {
            lootSummaryUI.gameObject.SetActive(true);
            var combatEvent = RunManager.Instance.selectedEvent as CombatEventSO;

            int goldEarned = (playerWon && combatEvent != null) ? combatEvent.goldReward : goldFromKills;
            int xpEarned = (playerWon && combatEvent != null) ? combatEvent.experienceReward : 0;

            if (playerWon)
            {
                lootSummaryUI.ShowSummary(goldEarned, xpEarned, pendingUnitRewardDef, pendingUnitRewardRarity, pendingTacticRewardDef, pendingTacticRewardRarity, pendingUnitRewardPrefix, pendingUnitRewardSuffix);
            }
            else
            {
                lootSummaryUI.ShowSummary(goldEarned, xpEarned, null, Rarity.Common, null, Rarity.Common);
            }
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                if (RunHUDManager.Instance != null)
                {
                    RunHUDManager.Instance.DisableInspectStats();
                    RunHUDManager.Instance.ShowSquadButton();
                }

                bool isFinalDay = RunManager.Instance.Stats.CurrentDay >= RunManager.Instance.TOTAL_DAYS;
                bool canUseLastChance = RunManager.Instance.Stats.PlayerHealth <= 0 && !isFinalDay && !RunManager.Instance.hasUsedLastChance;
                bool isRunAlive = playerWon || RunManager.Instance.Stats.PlayerHealth > 0 || canUseLastChance;

                if (isRunAlive)
                {
                    if (RunManager.Instance.selectedEvent != null)
                        RunManager.Instance.selectedEvent.OnCompleted();

                    if (!isFinalDay)
                    {
                        SceneLoader.Instance.LoadScene(GameScene.MapScene);
                    }
                }
                else
                {
                    SaveLoadManager.DeleteSave(); 
                    SceneLoader.Instance.LoadScene(GameScene.RunSummaryScene); 
                }
            });
        }
    }

    public void InitializeBattlefield(TeamDefinition playerTeam, EncounterDefinition encounter, bool startCombat = true)
    {
        foreach (var placement in playerTeam.units)
        {
            if (placement.unitData == null || placement.unitData.definition == null || placement.unitData.definition.unitPrefab == null)
                continue;

            SpawnPlayerUnit(placement, playerGrid, startCombat);
        }

        var runtimeEnemyList = encounter.GetRuntimeEnemyPlacements(RunManager.Instance.Stats.CurrentDay);

        foreach (var enemyPlacement in runtimeEnemyList)
        {
            SpawnEnemyUnit(enemyPlacement, enemyGrid, startCombat);
        }

        if (startCombat)
        {
            foreach (var playerUnit in playerGrid.GetAllUnits())
            {
                playerUnit.CombatStartEffect();
                playerGrid.RefreshAllAuras();
                if (playerTacticBarManager != null) playerTacticBarManager.RefreshAllTacticAuras();
            }
            foreach (var enemyUnit in enemyGrid.GetAllUnits())
            {
                enemyUnit.CombatStartEffect();
                enemyGrid.RefreshAllAuras();
                if (enemyTacticBarManager != null) enemyTacticBarManager.RefreshAllTacticAuras();
            }
        }

        pendingUnitRewardDef = null;
        pendingTacticRewardDef = null;

        List<UnitInstance> enemyUnits = enemyGrid.GetAllUnits();
        List<TacticInstance> allEnemyTactics = enemyTacticBarManager != null ? enemyTacticBarManager.GetAllTactics() : new List<TacticInstance>();
        List<TacticInstance> validEnemyTactics = new List<TacticInstance>();

        AdventureDefinitionSO currentAdventure = RunManager.Instance.allAdventures.FirstOrDefault(a => a.adventureName == RunManager.Instance.activeAdventureName);
        List<TacticDefinition> phantomDefsToIgnore = new List<TacticDefinition>();
        if (currentAdventure != null && currentAdventure.modifierPhantomTactics != null)
        {
            phantomDefsToIgnore = currentAdventure.modifierPhantomTactics.Select(m => m.phantomTactic).ToList();
        }

        foreach (var tactic in allEnemyTactics)
        {
            if (tactic == null || tactic.Definition == null) continue;
            if (phantomDefsToIgnore.Contains(tactic.Definition)) continue;
            var ownedCopy = RunManager.Instance.playerTactics.FirstOrDefault(p =>
                p.tacticData != null &&
                p.tacticData.definition == tactic.Definition);
            if (ownedCopy == null || ownedCopy.tacticData.rarity == tactic.CurrentRarity)
            {
                validEnemyTactics.Add(tactic);
            }
        }

        int totalSpoils = enemyUnits.Count + validEnemyTactics.Count;

        if (totalSpoils > 0)
        {
            int roll = UnityEngine.Random.Range(0, totalSpoils);

            if (roll < enemyUnits.Count)
            {
                pendingUnitRewardDef = enemyUnits[roll].Definition;
                pendingUnitRewardRarity = enemyUnits[roll].CurrentRarity;
                pendingUnitRewardPrefix = enemyUnits[roll].currentPrefix;
                pendingUnitRewardSuffix = enemyUnits[roll].currentSuffix;
            }
            else
            {
                int tacticIndex = roll - enemyUnits.Count;
                pendingTacticRewardDef = validEnemyTactics[tacticIndex].Definition;
                pendingTacticRewardRarity = validEnemyTactics[tacticIndex].CurrentRarity;
            }
        }

    }


    private UnitInstance SpawnPlayerUnit(RunManager.UnitPlacement placement, GridManager grid, bool startCombat)
    {
        UnitInstance unit = Instantiate(placement.unitData.definition.unitPrefab);
        unit.InitializeFromSaveData(placement.unitData);
        unit.myPlacement = placement;
        unit.EnterCombat(grid, placement.row, placement.col, true, startCombat);

        return unit;
    }

    private UnitInstance SpawnEnemyUnit(RunManager.UnitPlacement placement, GridManager grid, bool startCombat)
    {
        UnitInstance unit = Instantiate(placement.unitData.definition.unitPrefab);
        unit.InitializeEnemy(placement.unitData);
        unit.myPlacement = placement;

        unit.EnterCombat(grid, placement.row, placement.col, false, startCombat);

        return unit;
    }

    private void ResetBattlefieldToStasis()
    {
        if (battleUIManager != null)
        {
            foreach (var unit in playerGrid.GetAllUnits())
            {
                battleUIManager.RemoveUnitUI(unit);
            }
            foreach (var unit in enemyGrid.GetAllUnits())
            {
                battleUIManager.RemoveUnitUI(unit);
            }
        }
        foreach (var unit in playerGrid.GetAllUnits()) if (unit != null) Destroy(unit.gameObject);
        foreach (var unit in enemyGrid.GetAllUnits()) if (unit != null) Destroy(unit.gameObject);
        playerGrid.ClearAllUnits();
        enemyGrid.ClearAllUnits();

        TeamDefinition playerTeam = RunManager.Instance.GetTeamForCombat();
        EncounterDefinition currentEncounter = RunManager.Instance.currentEncounter;

        // Respawn them, but pass "false" to tell them NOT to fight
        if (playerTeam != null && currentEncounter != null)
        {
            InitializeBattlefield(playerTeam, currentEncounter, false);
            InitializeTacticsBar();
            InitializeEnemyTacticsBar(currentEncounter);
            playerGrid.RefreshAllAuras();
            if (benchGrid != null) benchGrid.RefreshAllAuras();
            if (playerTacticBarManager != null) playerTacticBarManager.RefreshAllTacticAuras();
        }
    }

    private void StartActualCombat()
    {
        if (provisionManager != null && !provisionManager.IsProvisionValid())
        {
            UniversalPopupManager.ShowPopup($"Provision exceeded provision cap!\nProvision cap: {RunManager.Instance.Stats.ProvisionCap}");
            if (TutorialManager.Instance != null)
            {
                Transform hudCanvas = RunHUDManager.Instance.transform;
                List<TutorialStep> combatSequence = new List<TutorialStep>
                            {
                                new TutorialStep {
                                    key = "RunHUDMaxProvision",
                                    title = "Max Provision",
                                    description = "Each unit has a [PROVISION] cost. Your deployed units’ combined costs cannot exceed your [MAXPROVISION]. Increase this limit to field more units or those with higher [PROVISION] costs!",
                                    highlightTarget = hudCanvas.Find("TopBar/playerBasicStats/playerProvisionCapContainer")
                                }
                            };
                TutorialManager.Instance.StartTutorialSequence(combatSequence);
            }
            return;
        }
        if (combatMusic != null)
        {
            AudioManager.Instance.PlayMusicWithFade(combatMusic);
        }
        if (startCombatButton != null) startCombatButton.gameObject.SetActive(false);
        if (provisionManager != null) provisionManager.HideProvisionText();
        if (dragManager != null) dragManager.enabled = false;
        if (sellZone != null) sellZone.gameObject.SetActive(false);

        SaveFormationToRunManager();

        if (benchGrid != null)
        {
            StartCoroutine(SlideBenchOutRoutine(0.5f));
        }

        if (battleUIManager != null)
        {
            foreach (var unit in playerGrid.GetAllUnits()) battleUIManager.RemoveUnitUI(unit);
            foreach (var unit in enemyGrid.GetAllUnits()) battleUIManager.RemoveUnitUI(unit);
        }
        foreach (var unit in playerGrid.GetAllUnits()) if (unit != null) Destroy(unit.gameObject);
        foreach (var unit in enemyGrid.GetAllUnits()) if (unit != null) Destroy(unit.gameObject);
        playerGrid.ClearAllUnits();
        enemyGrid.ClearAllUnits();

        combatActive = true;
        if (GameplayManager.Instance != null)
        {
            Time.timeScale = GameplayManager.Instance.CombatSpeedMultiplier;
        }
        else
        {
            Time.timeScale = 1f;
        }
        TeamDefinition playerTeam = RunManager.Instance.GetTeamForCombat();
        EncounterDefinition currentEncounter = RunManager.Instance.currentEncounter;

        if (playerTeam != null && currentEncounter != null)
        {
            InitializeBattlefield(playerTeam, currentEncounter, true);
        }

        // Pass "true" for the player, and "false" for the enemy!
        if (playerTacticBarManager != null) playerTacticBarManager.StartCombat(true);
        if (enemyTacticBarManager != null) enemyTacticBarManager.StartCombat(false);
        NotificationManager.Instance.ShowNotification("Combat Started!");
        enemiesKilledThisCombat = 0;
        CheckCombatEnd();
    }

    private IEnumerator SlideBenchOutRoutine(float duration)
    {
        Transform benchTransform = benchGrid.transform;
        Vector3 originalBenchPos = benchTransform.position;
        Vector3 targetBenchPos = originalBenchPos + new Vector3(0, -10f, 0);

        List<UnitInstance> units = benchGrid.GetAllUnits();
        List<Vector3> originalUnitPos = new List<Vector3>();
        foreach (var u in units)
        {
            originalUnitPos.Add(u != null ? u.transform.position : Vector3.zero);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            benchTransform.position = Vector3.Lerp(originalBenchPos, targetBenchPos, t);

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null)
                {
                    units[i].transform.position = Vector3.Lerp(originalUnitPos[i], originalUnitPos[i] + new Vector3(0, -10f, 0), t);
                }
            }
            yield return null;
        }

        benchGrid.gameObject.SetActive(false);
        foreach (var u in units)
        {
            if (u != null) Destroy(u.gameObject);
        }
        benchGrid.ClearAllUnits();
        benchTransform.position = originalBenchPos;
    }

    private void SaveFormationToRunManager()
    {
        if (RunManager.Instance == null) return;

        // Save Battle Grid
        List<RunManager.UnitPlacement> battleTeam = new List<RunManager.UnitPlacement>();
        foreach (UnitInstance unit in playerGrid.GetAllUnits())
        {
            if (unit.myPlacement != null)
            {
                unit.myPlacement.row = playerGrid.GetUnitPosition(unit).x;
                unit.myPlacement.col = playerGrid.GetUnitPosition(unit).y;
                battleTeam.Add(unit.myPlacement);
            }
        }
        RunManager.Instance.playerTeamPlacements = battleTeam;

        if (benchGrid != null)
        {
            for (int i = 0; i < RunManager.Instance.playerBenchPlacements.Count; i++)
            {
 
                UnitInstance unitInSlot = benchGrid.GetUnitAtPosition(0, i);

                if (unitInSlot != null && unitInSlot.myPlacement != null)
                {
                    RunManager.Instance.playerBenchPlacements[i].unitData = unitInSlot.myPlacement.unitData;
                }
                else
                {
                    RunManager.Instance.playerBenchPlacements[i].unitData = null;
                }
                RunManager.Instance.playerBenchPlacements[i].row = -1;
                RunManager.Instance.playerBenchPlacements[i].col = -1;
            }
        }

        if (playerTacticBarManager != null)
        {
            RunManager.Instance.playerTactics.Clear();
            var activeTactics = playerTacticBarManager.GetAllTactics();
            for (int i = 0; i < activeTactics.Count; i++)
            {
                if (activeTactics[i].myPlacement != null)
                {
                    activeTactics[i].myPlacement.orderIndex = i;
                    RunManager.Instance.playerTactics.Add(activeTactics[i].myPlacement);
                }
            }
        }
    }

    private void InitializeBench()
    {
        if (benchGrid == null) return;

        benchGrid.gameObject.SetActive(true);

        TeamDefinition benchTeam = RunManager.Instance.GetTeamForBench();
        if (benchTeam != null && benchTeam.units != null)
        {
            int col = 0;
            foreach (var placement in benchTeam.units)
            {
                if (placement.unitData != null && placement.unitData.definition != null && placement.unitData.definition.unitPrefab != null)
                {
                    UnitInstance unit = Instantiate(placement.unitData.definition.unitPrefab);
                    unit.InitializeFromSaveData(placement.unitData);
                    unit.myPlacement = placement;
                    unit.EnterCombat(benchGrid, 0, col, true, false);
                }
                col++;
            }
        }
    }

    private void InitializeTacticsBar()
    {
        if (playerTacticBarManager == null) return;

        playerTacticBarManager.ClearAllTactics();

        // Ensure tactics are sorted by their saved orderIndex
        var sortedTactics = RunManager.Instance.playerTactics;
        sortedTactics.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

        foreach (var placement in sortedTactics)
        {
            if (placement.tacticData == null || placement.tacticData.definition == null) continue;

            // Spawn the tactic prefab
            TacticInstance tactic = Instantiate(placement.tacticData.definition.tacticPrefab);
            tactic.InitializeFromSaveData(placement.tacticData);
            tactic.myPlacement = placement;

            // Add it to the bar
            playerTacticBarManager.AddTactic(tactic);
        }
    }

    private void InitializeEnemyTacticsBar(EncounterDefinition encounter)
    {
        if (enemyTacticBarManager == null || encounter == null) return;

        enemyTacticBarManager.ClearAllTactics();
        List<RunManager.TacticPlacement> tacticsToSpawn = new List<RunManager.TacticPlacement>();
        if (encounter.enemyTactics != null)
        {
            tacticsToSpawn.AddRange(encounter.enemyTactics);
        }

        AdventureDefinitionSO currentAdventure = RunManager.Instance.allAdventures.FirstOrDefault(a => a.adventureName == RunManager.Instance.activeAdventureName);
        if (currentAdventure != null && currentAdventure.modifierPhantomTactics != null)
        {
            int nextOrderIndex = tacticsToSpawn.Count;

            foreach (var map in currentAdventure.modifierPhantomTactics)
            {
                if (RunManager.Instance.HasDifficultyModifier(map.modifier))
                {
                    RunManager.TacticPlacement phantomPlacement = new RunManager.TacticPlacement
                    {
                        orderIndex = nextOrderIndex,
                        tacticData = new RunManager.TacticSaveData
                        {
                            definition = map.phantomTactic,
                            rarity = Rarity.Common,
                            id = Guid.NewGuid()
                        }
                    };
                    tacticsToSpawn.Add(phantomPlacement);
                    nextOrderIndex++;
                }
            }
        }

        foreach (var placement in tacticsToSpawn)
        {
            if (placement.tacticData == null || placement.tacticData.definition == null) continue;

            TacticInstance tactic = Instantiate(placement.tacticData.definition.tacticPrefab);
            tactic.InitializeFromSaveData(placement.tacticData);
            tactic.myPlacement = placement;
            enemyTacticBarManager.AddTactic(tactic);
        }
    }

    private void SetCombatSpeed(int index)
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.SetCombatSpeed(index);
            if (combatActive)
            {
                Time.timeScale = GameplayManager.Instance.CombatSpeedMultiplier;
            }
        }

        UpdateSpeedButtonVisuals(index);
    }

    private void UpdateSpeedButtonVisuals(int activeIndex)
    {
        if (speed1xButton != null) speed1xButton.interactable = (activeIndex != 0);
        if (speed1_5xButton != null) speed1_5xButton.interactable = (activeIndex != 1);
        if (speed2xButton != null) speed2xButton.interactable = (activeIndex != 2);
    }

    private void TransferCombatStatsToRunManager()
    {
        if (CombatStatsTracker.Instance == null || RunManager.Instance == null) return;

        // Retrieve the stats from the active combat scene tracker
        Dictionary<Guid, UnitCombatStats> currentFightStats = CombatStatsTracker.Instance.GetAllStats();

        foreach (var kvp in currentFightStats)
        {
            Guid unitId = kvp.Key;
            UnitCombatStats fightStats = kvp.Value;

            // If this unit isn't in the master dictionary yet, initialize them
            if (!RunManager.Instance.masterUnitStats.ContainsKey(unitId))
            {
                RunManager.Instance.masterUnitStats[unitId] = new UnitLifetimeStats
                {
                    unitName = fightStats.UnitName
                };
            }
            UnitLifetimeStats lifetime = RunManager.Instance.masterUnitStats[unitId];
            lifetime.id = unitId;
            lifetime.totalDirectDamageDealt += fightStats.DirectDamageDealt;
            lifetime.totalBurnDamageDealt += fightStats.BurnDamageDealt;
            lifetime.totalPoisonDamageDealt += fightStats.PoisonDamageDealt;
            lifetime.totalDamageTaken += fightStats.DamageTaken;
            lifetime.totalHealingDone += fightStats.HealingDone;
            lifetime.totalShieldingDone += fightStats.ShieldingDone;
            lifetime.totalSlowsApplied += fightStats.SlowsApplied;
            lifetime.totalHastesApplied += fightStats.HastesApplied;
            lifetime.totalAdvancesGiven += fightStats.AdvancesGiven;
        }
    }
}
