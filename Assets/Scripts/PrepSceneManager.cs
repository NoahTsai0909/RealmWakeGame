using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static SceneLoader;

public class PrepSceneManager : MonoBehaviour
{
    public GridManager battleGrid;
    public GridManager benchGrid;
    public GameObject benchGridBackground;
    [SerializeField] private ProvisionManager provisionManager;
    [Header("Tactics")]
    [SerializeField] private TacticBarManager playerTacticBarManager;

    private List<UnitInstance> spawnedUnits = new List<UnitInstance>();

    void Start()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.SanitizeBench();
        }

        DragAndDropManager dragManager = FindFirstObjectByType<DragAndDropManager>();

        LoadBattleGridFromRunManager();
        LoadBenchGridFromRunManager();
        LoadTacticBarFromRunManager();
        playerTacticBarManager.RefreshAllTacticAuras();

        if (TutorialManager.Instance != null)
        {
            Transform hudCanvas = RunHUDManager.Instance.transform;
            List<TutorialStep> combatSequence = new List<TutorialStep>
            {
                new TutorialStep {
                    key = "PrepSceneIntro",
                    title = "Your Squad",
                    description = "Every unit and tactic you currently possess will be displayed here. You may position them as you please!",
                    highlightTarget = null
                },
                new TutorialStep {
                    key = "PrepSceneGrid",
                    title = "Player Team",
                    description = "Deploy up to 9 units on this grid. Their positions determine which allies and enemies their abilities can reach.",
                    highlightTarget = battleGrid.transform
                },
                new TutorialStep {
                    key = "PrepSceneMaxProvision",
                    title = "Max Provision",
                    description = "Each unit has a provision cost. Your deployed units’ combined costs cannot exceed your max provision. Increase this limit to field more units or those with higher provision costs!",
                    highlightTarget = hudCanvas.Find("TopBar/playerBasicStats/playerProvisionCapContainer")
                },
                new TutorialStep {
                    key = "PrepSceneBench",
                    title = "Bench",
                    description = "Your bench holds up to 5 reserve units. They stay out of battle until you drag and drop them onto your team grid, swapping them with deployed units during combat.",
                    highlightTarget = benchGridBackground.transform
                },
                new TutorialStep {
                    key = "PrepSceneTacticsBar",
                    title = "Tactics Bar",
                    description = "You may collect tactics that influence battle through passive effects or active abilities.",
                    highlightTarget = playerTacticBarManager.transform
                }
            };

            TutorialManager.Instance.StartTutorialSequence(combatSequence);
        }
    }

    public void ReturnToMapScene()
    {
        if (!provisionManager.IsProvisionValid())
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
        SaveCurrentTeamToRunManager();
        SceneLoader.Instance.LoadScene(SceneLoader.Instance.lastScene);
    }

    private void LoadBattleGridFromRunManager()
    {
        if (RunManager.Instance == null) return;

        TeamDefinition playerTeam = RunManager.Instance.GetTeamForCombat();
        if (playerTeam == null) return;

        foreach (var placement in playerTeam.units)
        {
            if (placement.unitData == null)
            {
                Debug.LogWarning("Skipping placement with null UnitSaveData");
                continue;
            }

            if (placement.unitData.definition == null || placement.unitData.definition.unitPrefab == null)
            {
                Debug.LogWarning($"Skipping placement: missing prefab or definition for {placement.unitData.definition?.name ?? "NULL"}");
                continue;
            }

            // Place unit with isPlayer = true
            battleGrid.PlaceUnit(placement, placement.row, placement.col, null, true);

            // Reference to runtime UnitInstance
            UnitInstance spawned = battleGrid.GetUnitAtPosition(placement.row, placement.col);
            if (spawned == null)
            {
                Debug.LogError($"Failed to get UnitInstance at {placement.row},{placement.col} after PlaceUnit");
                continue;
            }

            spawned.isPlayer = true;
            spawned.myPlacement = placement;

            spawnedUnits.Add(spawned);
        }
    }

    private void LoadBenchGridFromRunManager()
    {
        if (RunManager.Instance == null) return;

        TeamDefinition benchTeam = RunManager.Instance.GetTeamForBench();
        if (benchTeam == null || benchTeam.units == null) return;

        int col = 0;
        foreach (var placement in benchTeam.units)
        {
            if (placement.unitData != null && placement.unitData.definition != null && placement.unitData.definition.unitPrefab != null)
            {
                // Place bench unit with isPlayer = true
                benchGrid.PlaceUnit(placement, 0, col, null, true);

                UnitInstance spawned = benchGrid.GetUnitAtPosition(0, col);
                if (spawned == null)
                {
                    Debug.LogError($"Failed to get UnitInstance at bench column {col}");
                }
                else
                {
                    spawned.myPlacement = placement;
                    spawnedUnits.Add(spawned);
                }
            }

            col++;
        }
    }
    private void LoadTacticBarFromRunManager()
    {
        if (RunManager.Instance == null || playerTacticBarManager == null) return;

        playerTacticBarManager.ClearAllTactics();
        playerTacticBarManager.isCombatRunning = false; // Strictly enforce non-combat state

        // Ensure tactics are sorted by their saved orderIndex
        var sortedTactics = RunManager.Instance.playerTactics;
        sortedTactics.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

        foreach (var placement in sortedTactics)
        {
            if (placement.tacticData == null || placement.tacticData.definition == null) continue;

            TacticInstance tactic = Instantiate(placement.tacticData.definition.tacticPrefab);
            tactic.InitializeFromSaveData(placement.tacticData);
            tactic.myPlacement = placement;

            playerTacticBarManager.AddTactic(tactic);
        }
    }

    private void SaveCurrentTeamToRunManager()
    {
        if (RunManager.Instance == null) return;

        List<RunManager.UnitPlacement> battleTeam = new List<RunManager.UnitPlacement>();

        foreach (UnitInstance unit in battleGrid.GetAllUnits())
        {
            if (unit.myPlacement == null)
            {
                Debug.LogWarning($"Unit {unit.name} has null myPlacement!");
                continue;
            }

            unit.myPlacement.row = battleGrid.GetUnitPosition(unit).x;
            unit.myPlacement.col = battleGrid.GetUnitPosition(unit).y;

            battleTeam.Add(unit.myPlacement);

            Debug.Log($"Saving {unit.Definition.name} at row={unit.myPlacement.row}, col={unit.myPlacement.col}");
        }

        RunManager.Instance.playerTeamPlacements = battleTeam;

        if (benchGrid != null)
        {
            for (int i = 0; i < RunManager.Instance.playerBenchPlacements.Count; i++)
            {
                RunManager.Instance.playerBenchPlacements[i].unitData = null;
                RunManager.Instance.playerBenchPlacements[i].row = -1;
                RunManager.Instance.playerBenchPlacements[i].col = -1;
            }

            foreach (UnitInstance unit in benchGrid.GetAllUnits())
            {
                if (unit.myPlacement == null) continue;

                int col = benchGrid.GetUnitPosition(unit).y;

                if (col >= 0 && col < RunManager.Instance.playerBenchPlacements.Count && RunManager.Instance.playerBenchPlacements[col].unitData == null)
                {
                    RunManager.Instance.playerBenchPlacements[col].unitData = unit.myPlacement.unitData;
                }
                else
                {
                    for (int i = 0; i < RunManager.Instance.playerBenchPlacements.Count; i++)
                    {
                        if (RunManager.Instance.playerBenchPlacements[i].unitData == null)
                        {
                            RunManager.Instance.playerBenchPlacements[i].unitData = unit.myPlacement.unitData;
                            break;
                        }
                    }
                }
            }
        }

        //SAVE TACTICS
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
            Debug.Log($"Saved {activeTactics.Count} tactics to RunManager");
        }

        Debug.Log("Saved battle grid and bench units to RunManager");
    }

}
