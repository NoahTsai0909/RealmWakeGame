using System.Collections.Generic;
using UnityEngine;

public class MetaManager : MonoBehaviour
{
    public static MetaManager Instance { get; private set; }

    public MetaSaveData metaData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        metaData = SaveManager.Load<MetaSaveData>("meta_save.json");
    }

    public void UnlockUnitInCompendium(UnitDefinition def)
    {
        if (def == null) return;

        string unitName = def.name;

        if (!metaData.unlockedCompendiumUnits.Contains(unitName))
        {
            metaData.unlockedCompendiumUnits.Add(unitName);
            Debug.Log($"[Compendium] New unit unlocked: {unitName}!");

            SaveManager.Save("meta_save.json", metaData);
        }
    }

    public void RegisterWinningTeam(List<RunManager.UnitPlacement> finalTeam)
    {
        bool madeChanges = false;

        foreach (var placement in finalTeam)
        {
            if (placement == null || placement.unitData == null || placement.unitData.definition == null) continue;

            string unitName = placement.unitData.definition.name;

            if (!metaData.crownedUnits.Contains(unitName))
            {
                metaData.crownedUnits.Add(unitName);
                madeChanges = true;
                Debug.Log($"[Compendium] {unitName} was crowned!");
            }
        }

        if (madeChanges)
        {
            metaData.totalRunsCompleted++;
            SaveManager.Save("meta_save.json", metaData);
        }
    }

    public int GetUnlockedDifficulty(string adventureName)
    {
        if (metaData == null || metaData.difficultyUnlocks == null) return 1;

        foreach (var unlock in metaData.difficultyUnlocks)
        {
            if (unlock.adventureName == adventureName)
            {
                return unlock.maxDifficultyUnlocked;
            }
        }

        return 1;
    }

    public void UnlockNextDifficulty(string adventureName, int completedDifficulty)
    {
        if (metaData == null) return;
        if (metaData.difficultyUnlocks == null) metaData.difficultyUnlocks = new List<AdventureDifficulty>();

        bool found = false;
        bool madeChanges = false;

        foreach (var unlock in metaData.difficultyUnlocks)
        {
            if (unlock.adventureName == adventureName)
            {
                found = true;
                if (completedDifficulty >= unlock.maxDifficultyUnlocked)
                {
                    unlock.maxDifficultyUnlocked = completedDifficulty + 1;
                    madeChanges = true;
                    Debug.Log($"[Meta] Unlocked Difficulty {unlock.maxDifficultyUnlocked} for {adventureName}!");
                    UniversalPopupManager.ShowPopup($"Difficulty {unlock.maxDifficultyUnlocked} unlocked for {adventureName}!");
                }
                break;
            }
        }

        if (!found)
        {
            metaData.difficultyUnlocks.Add(new AdventureDifficulty
            {
                adventureName = adventureName,
                maxDifficultyUnlocked = completedDifficulty + 1
            });
            madeChanges = true;
            UniversalPopupManager.ShowPopup($"First completion of {adventureName}! Difficulty {completedDifficulty + 1} unlocked!");
        }

        if (madeChanges)
        {
            SaveManager.Save("meta_save.json", metaData);
        }
    }

    public bool IsRewardUnitUnlocked(string unitName)
    {
        if (metaData == null || metaData.unlockedRewardUnits == null) return false;
        return metaData.unlockedRewardUnits.Contains(unitName);
    }

    public void UnlockRewardUnit(UnitDefinition def)
    {
        if (def == null || metaData == null) return;

        if (metaData.unlockedRewardUnits == null)
            metaData.unlockedRewardUnits = new List<string>();

        if (!metaData.unlockedRewardUnits.Contains(def.name))
        {
            metaData.unlockedRewardUnits.Add(def.name);
            SaveManager.Save("meta_save.json", metaData);
            Debug.Log($"[Meta] Unlocked new unit for future runs: {def.name}!");
            UniversalPopupManager.ShowPopup($"New unit unlocked for future runs: {def.unitName}!");
        }
    }

    public bool IsAdventureUnlocked(string adventureName)
    {
        if (adventureName == "Ruins Below") return true;

        if (metaData == null || metaData.unlockedAdventures == null) return false;
        return metaData.unlockedAdventures.Contains(adventureName);
    }

    public void UnlockAdventure(AdventureDefinitionSO adv)
    {
        if (adv == null || metaData == null) return;
        if (metaData.unlockedAdventures == null) metaData.unlockedAdventures = new List<string>();

        if (!metaData.unlockedAdventures.Contains(adv.adventureName))
        {
            metaData.unlockedAdventures.Add(adv.adventureName);
            SaveManager.Save("meta_save.json", metaData);
            UniversalPopupManager.ShowPopup($"New Adventure Unlocked: {adv.adventureName}!");
        }
    }
    public bool IsRegionUnlocked(Region region)
    {
        if (region == Region.Solmire) return true;

        if (metaData == null || metaData.unlockedRegions == null) return false;
        return metaData.unlockedRegions.Contains(region);
    }

    public void UnlockRegion(Region region)
    {
        if (metaData == null) return;
        if (metaData.unlockedRegions == null) metaData.unlockedRegions = new List<Region>();

        if (!metaData.unlockedRegions.Contains(region))
        {
            metaData.unlockedRegions.Add(region);
            SaveManager.Save("meta_save.json", metaData);
            UniversalPopupManager.ShowPopup($"New Playable Region Unlocked: {region}!");
        }
    }
}
