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
}
