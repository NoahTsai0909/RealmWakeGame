using System.Collections.Generic;

[System.Serializable]
public class AdventureDifficulty
{
    public string adventureName;
    public int maxDifficultyUnlocked;
}
public class MetaSaveData
{
    // A list of string IDs representing units the player has seen
    public List<string> unlockedCompendiumUnits = new List<string>();

    public List<string> crownedUnits = new List<string>();

    public int totalRunsCompleted = 0;
    public int totalEnemiesDefeated = 0;
    public List<AdventureDifficulty> difficultyUnlocks = new List<AdventureDifficulty>();
    public List<string> unlockedRewardUnits = new List<string>();
    public List<string> unlockedAdventures = new List<string>();
    public List<Region> unlockedRegions = new List<Region>();
}
