using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "UnitDatabase", menuName = "Database/Unit Database")]
public class UnitDatabase : ScriptableObject
{
    public List<UnitDefinition> allUnits = new List<UnitDefinition>();

    // Static instance for easy access
    private static UnitDatabase _instance;
    public static UnitDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<UnitDatabase>("UnitDatabase");
                if (_instance == null)
                {
                    Debug.LogError("UnitDatabase not found in Resources folder!");
                }
            }
            return _instance;
        }
    }

    public UnitDefinition GetRandomUnit(Rarity rolledRarity, Region? region = null, UnitTagFlags requiredTags = UnitTagFlags.None, int minProvision = 0, int maxProvision = -1, bool byPassExclusivity = false, Region? excludedRegion = null)
    {
        IEnumerable<UnitDefinition> pool = allUnits;

        // IMPORTANT: rarity eligibility rule
        pool = pool.Where(u => u.startingRarity <= rolledRarity);
        if (MetaManager.Instance != null)
        {
            pool = pool.Where(u => !u.isLockedByDefault || MetaManager.Instance.IsRewardUnitUnlocked(u.name));
        }
        if (!byPassExclusivity)
        {
            pool = pool.Where(u => u.isEventExclusive == false); // Exclude event-exclusive units
        }
        else
        {
            pool = pool.Where(u => u.isEventExclusive == true); // Include only event-exclusive units
        }

        if (region.HasValue)
            pool = pool.Where(u => u.region == region.Value);
        if (excludedRegion.HasValue)
        {
            pool = pool.Where(u => u.region != excludedRegion.Value);
            pool = pool.Where(u => u.region != Region.None);
        }

        if (requiredTags != UnitTagFlags.None)
            pool = pool.Where(u =>
                (u.tagFlags & requiredTags) == requiredTags);

        pool = pool.Where(u => u.provisionCost >= minProvision);

        if (maxProvision > 0)  // Only apply if maxProvision is set to a positive number
            pool = pool.Where(u => u.provisionCost <= maxProvision);

        var list = pool.ToList();
        if (list.Count == 0)
            return null;

        return list[Random.Range(0, list.Count)];
    }

    public UnitDefinition GetRandomUnit(Rarity rolledRarity, Region? region = null, UnitTagFlags requiredTags = UnitTagFlags.None)
    {
        return GetRandomUnit(rolledRarity, region, requiredTags, 0, -1);
    }

    // Get units by tag (you'll need to add tags to UnitDefinition)
    public List<UnitDefinition> GetUnitsWithTags(UnitTagFlags requiredTags)
    {
        return allUnits.Where(unit =>
            (unit.tagFlags & requiredTags) == requiredTags).ToList();
    }

    // Get units by region
    public List<UnitDefinition> GetUnitsByRegion(Region region)
    {
        return allUnits.Where(unit => unit.region == region).ToList();
    }

    // Get random unit
    public UnitDefinition GetRandomUnit()
    {
        if (allUnits.Count == 0) return null;
        return allUnits[Random.Range(0, allUnits.Count)];
    }

    // Get random unit by region
    public UnitDefinition GetRandomUnitByRegion(Region region)
    {
        var regionalUnits = GetUnitsByRegion(region);
        if (regionalUnits.Count == 0) return GetRandomUnit();
        return regionalUnits[Random.Range(0, regionalUnits.Count)];
    }

    private List<UnitDefinition> GetRandomFromList(
    List<UnitDefinition> source,
    int count)
    {
        if (source == null || source.Count == 0)
            return new List<UnitDefinition>();

        // Fisher–Yates shuffle
        for (int i = source.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (source[i], source[j]) = (source[j], source[i]);
        }

        int take = Mathf.Min(count, source.Count);
        return source.GetRange(0, take);
    }

    public List<UnitDefinition> GetRandomUnits(int count)
    {
        return GetRandomFromList(new List<UnitDefinition>(allUnits), count);
    }

    public List<UnitDefinition> GetRandomUnitsByRegion(
    Region region,
    int count)
    {
        var regionalUnits = GetUnitsByRegion(region);
        return GetRandomFromList(regionalUnits, count);
    }

    public List<UnitDefinition> GetRandomUnitsWithTags(
    UnitTagFlags requiredTags,
    int count)
    {
        var taggedUnits = GetUnitsWithTags(requiredTags);
        return GetRandomFromList(taggedUnits, count);
    }

    public List<UnitDefinition> GetRandomUnits(
    int count,
    Region? region,
    UnitTagFlags requiredTags = UnitTagFlags.None)
    {
        List<UnitDefinition> pool = allUnits;

        if (region.HasValue)
            pool = pool.Where(u => u.region == region.Value).ToList();

        if (requiredTags != UnitTagFlags.None)
            pool = pool.Where(u =>
                (u.tagFlags & requiredTags) == requiredTags).ToList();

        return GetRandomFromList(pool, count);
    }
    public List<UnitDefinition> GetUnits(Region region,UnitTagFlags requiredTags)
    {
        IEnumerable<UnitDefinition> pool = allUnits;

        pool = pool.Where(u => u.region == region);

        if (requiredTags != UnitTagFlags.None)
            pool = pool.Where(u =>(u.tagFlags & requiredTags) == requiredTags);
        return pool.ToList();
    }



}

// Update UnitDefinition to include tags:
/*
public class UnitDefinition : ScriptableObject
{
    // ... existing fields ...
    
    [Header("Tags")]
    public List<string> tags = new List<string>();
    
    // ... rest of class ...
}
*/
