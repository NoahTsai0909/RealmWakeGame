using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rarity Distribution Table")]
public class RarityDistributionTable : ScriptableObject
{
    [Tooltip("Define major milestones (e.g. 0.0, 0.5, 1.0) and the game blends them automatically.")]
    public List<DayRarityEntry> progressionPhases;

    public DayRarityEntry GetForDay(int currentDay, int totalDays)
    {
        if (progressionPhases == null || progressionPhases.Count == 0) return null;
        float currentProgress = Mathf.Clamp01((float)currentDay / (float)totalDays);

        DayRarityEntry previousPhase = progressionPhases[0];
        DayRarityEntry nextPhase = progressionPhases[progressionPhases.Count - 1];

        for (int i = 0; i < progressionPhases.Count; i++)
        {
            if (currentProgress <= progressionPhases[i].progressThreshold)
            {
                nextPhase = progressionPhases[i];
                previousPhase = i > 0 ? progressionPhases[i - 1] : progressionPhases[i];
                break;
            }
        }

        if (previousPhase == nextPhase) return previousPhase;

        float t = (currentProgress - previousPhase.progressThreshold) /
                  (nextPhase.progressThreshold - previousPhase.progressThreshold);

        DayRarityEntry interpolatedEntry = new DayRarityEntry();
        interpolatedEntry.progressThreshold = currentProgress;

        interpolatedEntry.common = Mathf.RoundToInt(Mathf.Lerp(previousPhase.common, nextPhase.common, t));
        interpolatedEntry.uncommon = Mathf.RoundToInt(Mathf.Lerp(previousPhase.uncommon, nextPhase.uncommon, t));
        interpolatedEntry.rare = Mathf.RoundToInt(Mathf.Lerp(previousPhase.rare, nextPhase.rare, t));

        interpolatedEntry.epic = 100 - (interpolatedEntry.common + interpolatedEntry.uncommon + interpolatedEntry.rare);
        interpolatedEntry.epic = Mathf.Max(0, interpolatedEntry.epic);

        return interpolatedEntry;
    }

    public static Rarity RollRarity(DayRarityEntry dist)
    {
        int roll = Random.Range(0, 100);
        int cumulative = 0;

        cumulative += dist.common;
        if (roll < cumulative) return Rarity.Common;

        cumulative += dist.uncommon;
        if (roll < cumulative) return Rarity.Uncommon;

        cumulative += dist.rare;
        if (roll < cumulative) return Rarity.Rare;

        return Rarity.Epic;
    }
}

[System.Serializable]
public class DayRarityEntry
{
    [Range(0f, 1f)] public float progressThreshold;

    [Range(0, 100)] public int common;
    [Range(0, 100)] public int uncommon;
    [Range(0, 100)] public int rare;
    [Range(0, 100)] public int epic;
}