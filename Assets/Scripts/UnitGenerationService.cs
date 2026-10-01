using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UnitGenerationService
{
    /// <summary>
    /// Core unit generation entry point.
    /// All unit generation (shop, events, rewards) should go through this.
    /// </summary>
    public static UnitSaveData GenerateUnit(
        Region? region = null,
        UnitTagFlags requiredTags = UnitTagFlags.None,
        bool bypassExclusive = false, Region? excludedRegion = null)
    {
        int day = RunManager.Instance.Stats.CurrentDay;
        DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day, RunManager.Instance.TOTAL_DAYS);
        Rarity rolledRarity = RarityDistributionTable.RollRarity(dist);
        UnitDefinition def = UnitDatabase.Instance.GetRandomUnit(rolledRarity, region, requiredTags, 0, -1, bypassExclusive, excludedRegion);

        return new UnitSaveData
        {
            definition = def,
            rarity = rolledRarity
        };
    }

    public static List<UnitSaveData> GenerateShopUnits(int count, Region? targetRegion, UnitTagFlags unitTags, int minProvision = 0, int maxProvision = -1, bool forceRarity = false, Rarity designatedRarity = Rarity.Common, bool forceMutation = false, Region? excludedRegion = null)
    {
        var result = new List<UnitSaveData>();
        var usedDefinitions = new HashSet<UnitDefinition>();

        for (int i = 0; i < count; i++)
        {
            var rolledRarity = forceRarity ? designatedRarity : RunManager.Instance.RollRarityForDay(RunManager.Instance.Stats.CurrentDay);
            UnitDefinition def = null;
            int attempts = 0;

            while (def == null && attempts < 100)
            {
                attempts++;

                var candidate = UnitDatabase.Instance.GetRandomUnit(
                    rolledRarity,
                    targetRegion,
                    unitTags,
                    minProvision,
                    maxProvision,
                    false,
                    excludedRegion
                );

                if (candidate != null && !usedDefinitions.Contains(candidate))
                {
                    // 1. Get the lowest rarity the player owns (returns null if they don't own it)
                    Rarity? lowestOwnedRarity = GetOwnedUnitLowestRarity(candidate);

                    bool isValid = true;

                    if (lowestOwnedRarity.HasValue)
                    {
                        // Rule A: The lowest they own is Epic (meaning they ONLY own Epics). They cannot upgrade this unit anymore. Reject it.
                        if (lowestOwnedRarity.Value == Rarity.Epic)
                        {
                            isValid = false;
                        }
                        // Rule B: It's a Forced Rarity Shop, but the rarity they need to merge doesn't match what the shop is allowed to sell. Reject it.
                        else if (forceRarity && lowestOwnedRarity.Value != designatedRarity)
                        {
                            isValid = false;
                        }
                    }

                    if (isValid)
                    {
                        def = candidate;
                    }
                }
            }

            if (def != null)
            {
                usedDefinitions.Add(def);

                //Set the final rarity
                Rarity finalRarity = rolledRarity;
                Rarity? lowestOwnedRarity = GetOwnedUnitLowestRarity(def);

                if (lowestOwnedRarity.HasValue)
                {
                    finalRarity = lowestOwnedRarity.Value;
                    Debug.Log($"[Shop] Rarity Override! {def.unitName} locked to {finalRarity} to match player inventory.");
                }

                //ROLL FOR MUTATION
                MutationPrefixSO rolledPrefix = null;
                MutationSuffixSO rolledSuffix = null;

                //Get the epic probability for the current day
                int currentDay = RunManager.Instance.Stats.CurrentDay;
                DayRarityEntry currentDist = RunManager.Instance.rarityDistributionTable.GetForDay(currentDay, RunManager.Instance.TOTAL_DAYS);

                //Roll a 0-99. If it's less than the epic chance, get a mutation
                if (forceMutation || Random.Range(0, 100) < currentDist.epic)
                {
                    var allPrefixes = RunManager.Instance.allAvailablePrefixes;
                    if (allPrefixes != null && allPrefixes.Count > 0)
                    {
                        //Pick a random prefix
                        rolledPrefix = allPrefixes[Random.Range(0, allPrefixes.Count)];

                        // Pick a random suffix from that prefix's specific bucket!
                        if (rolledPrefix.allowedSuffixes != null && rolledPrefix.allowedSuffixes.Count > 0)
                        {
                            rolledSuffix = rolledPrefix.allowedSuffixes[Random.Range(0, rolledPrefix.allowedSuffixes.Count)];
                        }
                    }
                }

                //Save the unit with its new mutations
                result.Add(new UnitSaveData
                {
                    definition = def,
                    rarity = finalRarity,
                    prefix = rolledPrefix,
                    suffix = rolledSuffix 
                });
            }
        }

        return result;
    }
    private static Rarity? GetOwnedUnitLowestRarity(UnitDefinition targetDef)
    {
        Rarity? lowestRarity = null;

        foreach (var placement in RunManager.Instance.playerTeamPlacements)
        {
            if (placement.unitData != null && placement.unitData.definition == targetDef)
            {
                if (lowestRarity == null || placement.unitData.rarity < lowestRarity.Value)
                    lowestRarity = placement.unitData.rarity;
            }
        }

        foreach (var placement in RunManager.Instance.playerBenchPlacements)
        {
            if (placement.unitData != null && placement.unitData.definition == targetDef)
            {
                if (lowestRarity == null || placement.unitData.rarity < lowestRarity.Value)
                    lowestRarity = placement.unitData.rarity;
            }
        }

        return lowestRarity;
    }

    private static bool IsMaxRarityOwned(UnitDefinition targetDef)
    {
        foreach (var placement in RunManager.Instance.playerTeamPlacements)
        {
            if (placement.unitData != null && placement.unitData.definition == targetDef && placement.unitData.rarity == Rarity.Epic)
                return true;
        }

        foreach (var placement in RunManager.Instance.playerBenchPlacements)
        {
            if (placement.unitData != null && placement.unitData.definition == targetDef && placement.unitData.rarity == Rarity.Epic)
                return true;
        }

        return false;
    }
}
