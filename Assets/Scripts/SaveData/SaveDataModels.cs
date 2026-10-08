using System;
using System.Collections.Generic;
using Newtonsoft.Json;


[System.Serializable]
public class RunSaveData
{
    public RunStats stats;
    public Region playerRegion;
    public string activeAdventureName;
    // Core Progression
    public int totalDays;
    public int regularEventsCompleted;
    public bool isBattlePhase;
    public int currentEventPhase;
    public bool hasUsedLastChance;
    public bool eventInProgress;
    public int runSeed;
    public int currentDifficulty;
    public Dictionary<Guid, PermanentStats> permanentStatsMap = new Dictionary<Guid, PermanentStats>();
    public Dictionary<Guid, UnitLifetimeStats> masterUnitStats = new Dictionary<Guid, UnitLifetimeStats>();

    // Teams
    public List<UnitPlacementDTO> playerTeam = new List<UnitPlacementDTO>();
    public List<UnitPlacementDTO> playerBench = new List<UnitPlacementDTO>();
    public List<TacticPlacementDTO> playerTactics = new List<TacticPlacementDTO>();

    // Events
    public List<string> allDayEventNames = new List<string>();
    public List<string> currentDailyEventNames = new List<string>();
    public string selectedEventName;

    // Shop
    public ShopStateDTO shopState;
}

[System.Serializable]
public class UnitPlacementDTO
{
    public int row;
    public int col;
    public string unitDefinitionName;
    public Rarity rarity;
    public string prefixName;
    public string suffixName;
    public System.Guid id;
}

[System.Serializable]
public class TacticPlacementDTO
{
    public int orderIndex;
    public string tacticDefinitionName;
    public Rarity rarity;
    public Guid id;
}

[System.Serializable]
public class ShopStateDTO
{
    public List<UnitPlacementDTO> offeredUnits = new List<UnitPlacementDTO>();
    public List<string> purchasedUnitNames = new List<string>();

    public List<TacticPlacementDTO> offeredTactics = new List<TacticPlacementDTO>();
    public List<string> purchasedTacticNames = new List<string>();

    public int currentPage;
    public bool hasRefreshed;
    public int minProvisionFilter;
    public int maxProvisionFilter;
}