using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static SceneLoader;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    // Run Data
    public RunStats Stats = new RunStats();
    public int runSeed;

    public List<UnitPlacement> playerTeamPlacements = new();
    public List<UnitPlacement> playerBenchPlacements = new();
    public List<TacticPlacement> playerTactics = new();

    [System.Serializable]
    public class UnitPlacement
    {
        public UnitSaveData unitData;
        public int row;
        public int col;
    }

    [System.Serializable]
    public class TacticSaveData
    {
        public TacticDefinition definition;
        public Rarity rarity;
        public Guid id = Guid.NewGuid();
    }

    [System.Serializable]
    public class TacticPlacement
    {
        public TacticSaveData tacticData;
        public int orderIndex; 
    }
    public class ShopState
    {
        public List<UnitSaveData> offeredUnits;
        public HashSet<UnitDefinition> purchasedUnits = new();
        public List<TacticSaveData> offeredTactics = new List<TacticSaveData>();
        public HashSet<TacticDefinition> purchasedTactics = new HashSet<TacticDefinition>();
        public int currentPage;
        public bool hasRefreshed;
        public int refreshCount = 0;
        public int minProvisionFilter;  
        public int maxProvisionFilter;
    }


    [Header("Default Unit")]
    [SerializeField] private List<UnitPlacement> defaultUnits;
    [SerializeField] public int benchSize = 5;

    public int regularEventsCompleted = 0;
    public const int REGULAR_EVENTS_PER_DAY = 3;
    public bool isBattlePhase = false;
    public int currentEventPhase = 0;
    

    public List<BaseEventSO> currentDailyEvents = new();  // Current 3 events to display
    public List<BaseEventSO> allDayEvents = new();

    public BaseEventSO selectedEvent;
    public EncounterDefinition currentEncounter;
    public bool eventInProgress = false;
    public int TOTAL_DAYS { get; set; } = 12;
    public bool hasUsedLastChance = false;
    public ShopState shopState;
    public int currentDifficulty = 1;


    private Dictionary<Guid, PermanentStats> permanentStatsMap = new();
    public Dictionary<Guid, UnitLifetimeStats> masterUnitStats = new Dictionary<Guid, UnitLifetimeStats>();
    public Dictionary<Guid, PermanentStats> GetPermanentStatsMap() => permanentStatsMap;
    public void SetPermanentStatsMap(Dictionary<Guid, PermanentStats> map) => permanentStatsMap = map;
    [SerializeField] public RarityDistributionTable rarityDistributionTable;
    [Header("Region Progression")]
    public Region playerRegion;
    public RegionLevelTreeSO currentRegionTree;
    [Tooltip("Drag all your specific RegionLevelTreeSO assets into this list!")]
    [SerializeField] private List<RegionLevelTreeSO> allRegionTrees = new List<RegionLevelTreeSO>();
    [SerializeField] public LastChanceEventSO lastChanceEvent;
    [Header("Adventure Tracking")]
    public string activeAdventureName;
    [Tooltip("Drag all your AdventureDefinitionSO assets into this list!")]
    [SerializeField] public List<AdventureDefinitionSO> allAdventures = new List<AdventureDefinitionSO>();
    [Header("Mutation Pool")]
    public List<MutationPrefixSO> allAvailablePrefixes = new List<MutationPrefixSO>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AssignRegionTree();

            if (playerTeamPlacements.Count == 0)
                InitializeDefaultTeam();

            InitializeBench();
            SanitizeBench();

            Stats.Initialize();
            regularEventsCompleted = 0;
            isBattlePhase = false;
            currentEventPhase = 0;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetupNewAdventure(AdventureDefinitionSO adventure, Region selectedRegion, int difficulty)
    {
        TOTAL_DAYS = adventure.totalDays;
        activeAdventureName = adventure.adventureName;
        playerRegion = selectedRegion;
        currentDifficulty = difficulty;
        AssignRegionTree();

        masterUnitStats.Clear();
        regularEventsCompleted = 0;
        isBattlePhase = false;
        currentEventPhase = 0;
        currentDailyEvents.Clear();
        allDayEvents.Clear();
        selectedEvent = null;
        currentEncounter = null;
        eventInProgress = false;
        hasUsedLastChance = false;
        permanentStatsMap.Clear();
        playerTactics.Clear();
        runSeed = UnityEngine.Random.Range(1, 99999999);

        playerTeamPlacements.Clear();
        InitializeDefaultTeam();
        playerBenchPlacements.Clear();
        InitializeBench();

        Stats.Initialize(adventure.startingGold, adventure.startingHealth, adventure.startingProvisionCap);

        List<BaseEventSO> regionEvents = currentRegionTree != null ? currentRegionTree.regionExclusiveEvents : new List<BaseEventSO>();
        if (EventPoolManager.Instance != null)
        {
            EventPoolManager.Instance.BuildEventPool(adventure.regularEvents, adventure.combatEvents, regionEvents);
        }

        Debug.Log($"Adventure Setup Complete: {adventure.adventureName} playing as {playerRegion}.");
    }

    public void AssignRegionTree()
    {
        currentRegionTree = null; // Clear any old data

        foreach (var tree in allRegionTrees)
        {
            if (tree.regionName == playerRegion)
            {
                currentRegionTree = tree;
                Debug.Log($"Successfully loaded Level Tree for region: {playerRegion}");
                return;
            }
        }

        Debug.LogWarning($"Could not find a RegionLevelTreeSO for the region: {playerRegion}!");
    }

    void InitializeDefaultTeam()
    {
        playerTeamPlacements.Clear();

        foreach (var placement in defaultUnits)
        {
            if (placement == null || placement.unitData == null || placement.unitData.definition == null)
            {
                Debug.LogError("Default unit placement is invalid!");
                continue;
            }

            // Clone placement so runtime changes don't mutate the asset
            playerTeamPlacements.Add(new UnitPlacement
            {
                unitData = new UnitSaveData
                {
                    definition = placement.unitData.definition,
                    rarity = placement.unitData.rarity,
                    prefix = placement.unitData.prefix,
                    suffix = placement.unitData.suffix
                },
                row = placement.row,
                col = placement.col
            });
        }
    }


    void InitializeBench()
    {
        playerBenchPlacements.Clear();

        if (playerBenchPlacements == null || playerBenchPlacements.Count != benchSize)
        {
            playerBenchPlacements = new List<UnitPlacement>(benchSize);
            for (int i = 0; i < benchSize; i++)
            {
                playerBenchPlacements.Add(new UnitPlacement());
            }
        }
    }

    public void SanitizeBench()
    {
        for (int i = 0; i < playerBenchPlacements.Count; i++)
        {
            var placement = playerBenchPlacements[i];

            if (placement.unitData != null &&
                placement.unitData.definition == null)
            {
                placement.unitData = null;
            }
        }
    }


    public TeamDefinition GetTeamForCombat()
    {
        TeamDefinition team = ScriptableObject.CreateInstance<TeamDefinition>();
        team.teamName = "Player Team";

        foreach (var placement in playerTeamPlacements)
        {
            if (placement.unitData == null) continue;

            team.units.Add(new UnitPlacement
            {
                unitData = placement.unitData,
                row = placement.row,
                col = placement.col
            });
        }

        return team;
    }

    public TeamDefinition GetTeamForBench()
    {
        TeamDefinition benchTeam = ScriptableObject.CreateInstance<TeamDefinition>();
        benchTeam.teamName = "Player Bench";

        if (benchTeam.units == null)
            benchTeam.units = new List<UnitPlacement>();

        foreach (var placement in playerBenchPlacements)
        {
            benchTeam.units.Add(new UnitPlacement
            {
                unitData = placement.unitData,
                row = -1,
                col = -1
            });
        }

        return benchTeam;
    }

    public void StartNewDay()
    {
        Stats.CurrentDay++;
        regularEventsCompleted = 0;
        isBattlePhase = false;
        currentEventPhase = 0;
        allDayEvents.Clear();
        currentDailyEvents.Clear();

        GenerateDailyEvents();
    }

    public void CompleteRegularEvent()
    {
        regularEventsCompleted++;

        currentEventPhase++;

        if (regularEventsCompleted >= REGULAR_EVENTS_PER_DAY)
        {
            isBattlePhase = true;
            Debug.Log("All 3 regular events completed! Moving to battle phase!");
        }
        else
        {
            isBattlePhase = false;
        }

        GenerateDailyEvents();
    }

    public void CompleteBattleEvent()
    {
        isBattlePhase = false;
        regularEventsCompleted = 0;
        currentEventPhase = 0;
        allDayEvents.Clear();

        if (Stats.CurrentDay >= TOTAL_DAYS)
        {
            MetaManager.Instance.RegisterWinningTeam(playerTeamPlacements);
            MetaManager.Instance.UnlockNextDifficulty(activeAdventureName, currentDifficulty);
            SaveLoadManager.DeleteSave();
            SceneLoader.Instance.LoadScene(GameScene.RunSummaryScene);
            return;
        }
        StartNewDay();
    }


    public void GenerateDailyEvents()
    {
        EventPoolManager eventPool = EventPoolManager.Instance;
        if (eventPool == null) return;

        if (isBattlePhase)
        {
            Debug.Log("GENERATING COMBAT EVENTS (Battle Phase)");
            bool isLastTwoDays = (Stats.CurrentDay >= TOTAL_DAYS - 1);
            currentDailyEvents = eventPool.GetStructuredCombatEvents(isLastTwoDays);

            allDayEvents.Clear();
            Debug.Log($"Got {currentDailyEvents.Count} structured combat events for battle");

            foreach (var ev in currentDailyEvents)
            {
                Debug.Log($"  Battle option: {ev.eventName}");
            }
        }
        else
        {
            if (allDayEvents.Count == 0)
            {
                Debug.Log($"GENERATING ALL 9 REGULAR EVENTS FOR DAY {Stats.CurrentDay}");
                allDayEvents = eventPool.GetRegularEvents(9);
                Debug.Log($"Generated {allDayEvents.Count} total events for the day");

                for (int i = 0; i < allDayEvents.Count; i++)
                {
                    Debug.Log($"  Event {i + 1}: {allDayEvents[i].eventName}");
                }

                if (allDayEvents.Count < 9)
                {
                    Debug.LogWarning($"Only got {allDayEvents.Count} regular events! Need 9 for a full day.");
                }
            }

            int startIndex = currentEventPhase * 3;
            currentDailyEvents.Clear();

            for (int i = 0; i < 3; i++)
            {
                int index = startIndex + i;
                if (index < allDayEvents.Count)
                {
                    currentDailyEvents.Add(allDayEvents[index]);
                }
                else
                {
                    Debug.LogError($"Not enough events! Phase {currentEventPhase}, index {index} out of {allDayEvents.Count}");
                }
            }

            Debug.Log($"Regular Phase {currentEventPhase + 1}/3: Showing 3 choices:");
            foreach (var ev in currentDailyEvents)
            {
                Debug.Log($"  - {ev.eventName}");
            }
        }

    }

    public PermanentStats GetPermanentStatsForUnit(Guid guid)
    {
        if (guid == null)
        {
            Debug.LogError("GetPermanentStatsForUnit called with null guid");
            return new PermanentStats(); // fail-safe
        }

        if (!permanentStatsMap.TryGetValue(guid, out var stats))
        {
            stats = new PermanentStats();
            permanentStatsMap[guid] = stats;
        }

        return stats;
    }

    public PermanentStats CreatePermanentStatsForUnit(Guid id)
    {
        var stats = new PermanentStats();
        permanentStatsMap[id] = stats;
        return stats;
    }

    public void InitializeShop(int unitCount, int tacticCount, Region? targetRegion, UnitTagFlags unitTags, int minProvision = 0, int maxProvision = -1, bool forceRarity = false, Rarity designatedRarity = Rarity.Common, bool forceMutation = false, Region? excludedRegion = null)
    {
        if (shopState != null) return;

        // 1. Generate Units (If this is a unit shop)
        List<UnitSaveData> generatedUnits = new List<UnitSaveData>();
        if (unitCount > 0)
        {
            generatedUnits = UnitGenerationService.GenerateShopUnits(unitCount, targetRegion, unitTags, minProvision, maxProvision, forceRarity, designatedRarity, forceMutation, excludedRegion);
        }

        // 2. Generate Tactics (If this is a tactic shop)
        List<TacticSaveData> generatedTactics = new List<TacticSaveData>();
        if (tacticCount > 0)
        {
            generatedTactics = TacticGenerationService.GenerateShopTactics(tacticCount, targetRegion);
        }

        // 3. Create the unified Payload
        shopState = new ShopState
        {
            offeredUnits = generatedUnits,
            purchasedUnits = new HashSet<UnitDefinition>(),

            offeredTactics = generatedTactics,
            purchasedTactics = new HashSet<TacticDefinition>(),

            currentPage = 0,
            hasRefreshed = false,
            minProvisionFilter = minProvision,
            maxProvisionFilter = maxProvision,
        };
    }

    public void RestoreEventPoolFromSave()
    {
        AdventureDefinitionSO savedAdventure = allAdventures.FirstOrDefault(a => a.adventureName == activeAdventureName);

        if (savedAdventure == null)
        {
            Debug.LogError($"Could not find Adventure '{activeAdventureName}' to restore the event pool! Check your allAdventures list.");
            return;
        }
        AssignRegionTree();
        List<BaseEventSO> regionEvents = currentRegionTree != null ? currentRegionTree.regionExclusiveEvents : new List<BaseEventSO>();

        if (EventPoolManager.Instance != null)
        {
            EventPoolManager.Instance.BuildEventPool(savedAdventure.regularEvents, savedAdventure.combatEvents, regionEvents);
            Debug.Log($"Event pool successfully restored from save for: {activeAdventureName}");
        }
    }

    public Rarity RollRarityForDay(int day)
    {
        if (rarityDistributionTable == null)
        {
            return Rarity.Common; // fallback
        }

        var dist = rarityDistributionTable.GetForDay(day, RunManager.Instance.TOTAL_DAYS);
        if (dist == null)
        {
            return Rarity.Common;
        }

        return RarityDistributionTable.RollRarity(dist);
    }
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene" || scene.name == "AdventureSelectionScene" || scene.name == "RunSummaryScene" || scene.name == "Bootstrap")
        {
            return;
        }

        SaveLoadManager.SaveRun();
    }

    public void SetAdventureMusic()
    {
        if (string.IsNullOrEmpty(activeAdventureName))
        {
            Debug.LogWarning("No active adventure name set. Cannot set music.");
            return;
        }
        AdventureDefinitionSO adventure = allAdventures.FirstOrDefault(a => a.adventureName == activeAdventureName);
        if (adventure == null)
        {
            Debug.LogWarning($"Could not find Adventure '{activeAdventureName}' to set music.");
            return;
        }
        if (adventure.adventureMusic != null)
        {
            AudioManager.Instance.PlayMusicWithFade(adventure.adventureMusic);
            Debug.Log($"Playing music for adventure: {activeAdventureName}");
        }
        else
        {
            Debug.LogWarning($"Adventure '{activeAdventureName}' does not have an assigned music clip.");
        }
    }

    public bool HasDifficultyModifier(DifficultyModifier modifier)
    {
        if (currentDifficulty <= 1) return false;

        AdventureDefinitionSO currentAdventure = allAdventures.FirstOrDefault(a => a.adventureName == activeAdventureName);
        if (currentAdventure == null || currentAdventure.difficultyTiers == null) return false;
        foreach (var tier in currentAdventure.difficultyTiers)
        {
            if (tier.difficultyLevel <= currentDifficulty && tier.activeModifiers.Contains(modifier))
            {
                return true;
            }
        }
        return false;
    }

    public void ResetRun()
    {

        Stats.Initialize();
        masterUnitStats.Clear();
        regularEventsCompleted = 0;
        isBattlePhase = false;
        currentEventPhase = 0;

        // Clear events
        currentDailyEvents.Clear();
        allDayEvents.Clear();
        selectedEvent = null;
        currentEncounter = null;
        eventInProgress = false;
        hasUsedLastChance = false;

        permanentStatsMap.Clear();

        // Reset team to default
        playerTeamPlacements.Clear();
        InitializeDefaultTeam();

        // Reset bench
        playerBenchPlacements.Clear();
        InitializeBench();
        AssignRegionTree();
        playerTactics.Clear();

        // Clear any other run-specific data
        Debug.Log("Run reset complete!");
    }
}
