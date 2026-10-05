using System.Collections.Generic;
using UnityEngine;
public enum TransformRule { Same, Any, Higher, Lower, Different }

[System.Serializable]
public struct TransformParams
{
    public TransformRule rarityRule;
    public TransformRule regionRule;
    public TransformRule provisionRule;
    public bool keepMutations;
}

public class PlayerUnitManager : MonoBehaviour
{

    public static PlayerUnitManager Instance { get; private set; }

    [Header("Grid Settings (For Overflow)")]
    [Tooltip("Set these to match your actual battle grid size!")]
    public int battleGridRows = 3;
    public int battleGridCols = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryAcquireUnit(UnitDefinition incomingDef, Rarity incomingRarity, MutationPrefixSO incomingPrefix = null, MutationSuffixSO incomingSuffix = null)
    {
        bool success = false;

        RunManager.UnitPlacement mergeTarget = FindMergeTarget(incomingDef, incomingRarity);
        if (mergeTarget != null)
        {
            MergeInto(mergeTarget, incomingPrefix, incomingSuffix);
            success = true;
        }
        else if (TryAddToBench(incomingDef, incomingRarity, incomingPrefix, incomingSuffix))
        {
            success = true;
        }
        else if (TryAddToBattleGrid(incomingDef, incomingRarity, incomingPrefix, incomingSuffix))
        {
            success = true;
        }
        if (success && MetaManager.Instance != null)
        {
            MetaManager.Instance.UnlockUnitInCompendium(incomingDef);
        }

        return success;
    }

    RunManager.UnitPlacement FindMergeTarget(UnitDefinition def, Rarity rarity)
    {
        if (rarity >= Rarity.Epic) return null;
        foreach (var placement in RunManager.Instance.playerBenchPlacements)
        {
            if (CanMerge(placement, def, rarity)) return placement;
        }

        foreach (var placement in RunManager.Instance.playerTeamPlacements)
        {
            if (CanMerge(placement, def, rarity)) return placement;
        }

        return null;
    }

    bool CanMerge(RunManager.UnitPlacement placement, UnitDefinition def, Rarity rarity)
    {
        if (placement.unitData == null) return false;
        if (placement.unitData.definition != def) return false;
        if (placement.unitData.rarity != rarity) return false;
        if (placement.unitData.rarity >= Rarity.Epic) return false;

        return true;
    }

    void MergeInto(RunManager.UnitPlacement placement, MutationPrefixSO incomingPrefix, MutationSuffixSO incomingSuffix)
    {
        placement.unitData.rarity += 1;
        if (incomingPrefix != null)
        {
            placement.unitData.prefix = incomingPrefix;
            placement.unitData.suffix = incomingSuffix;
        }
        if (TutorialManager.Instance != null)
        {
            List<TutorialStep> combatSequence = new List<TutorialStep>
                            {
                                new TutorialStep {
                                    key = "UnitMerge",
                                    title = "Merging Units",
                                    description = "When you acquire a second copy of the same unit at the same rarity, the two automatically merge into one unit of the next rarity.\n\nCollect matching copies to strengthen your squad!",
                                    highlightTarget = null
                                }
                            };
            TutorialManager.Instance.StartTutorialSequence(combatSequence);
        }

    }


    bool TryAddToBench(UnitDefinition def, Rarity rarity, MutationPrefixSO prefix, MutationSuffixSO suffix)
    {
        foreach (var placement in RunManager.Instance.playerBenchPlacements)
        {
            if (placement.unitData == null || placement.unitData.definition == null)
            {
                placement.unitData = new UnitSaveData
                {
                    definition = def,
                    rarity = rarity,
                    prefix = prefix, 
                    suffix = suffix  
                };
                return true;
            }
        }
        Debug.LogWarning("Bench full — attempting overflow...");
        return false;
    }

    bool TryAddToBattleGrid(UnitDefinition def, Rarity rarity, MutationPrefixSO prefix, MutationSuffixSO suffix)
    {
        for (int r = 0; r < battleGridRows; r++)
        {
            for (int c = 0; c < battleGridCols; c++)
            {
                bool isOccupied = RunManager.Instance.playerTeamPlacements.Exists(p => p.row == r && p.col == c);

                if (!isOccupied)
                {
                    RunManager.UnitPlacement newPlacement = new RunManager.UnitPlacement
                    {
                        row = r,
                        col = c,
                        unitData = new UnitSaveData
                        {
                            definition = def,
                            rarity = rarity,
                            prefix = prefix, 
                            suffix = suffix  
                        }
                    };

                    RunManager.Instance.playerTeamPlacements.Add(newPlacement);
                    Debug.Log($"Overflow Success! Placed {def.unitName} on the Battle Grid at ({r}, {c}).");
                    return true;
                }
            }
        }
        UniversalPopupManager.ShowPopup("Board Full! Incoming unit was discarded.");
        return false;
    }

    public bool TransformUnit(UnitInstance targetUnit, TransformParams rules)
    {
        if (targetUnit == null || targetUnit.myPlacement == null) return false;

        UnitDefinition oldDef = targetUnit.Definition;
        Rarity oldRarity = targetUnit.CurrentRarity;

        Rarity targetRarity = oldRarity;
        if (rules.rarityRule == TransformRule.Higher && oldRarity < Rarity.Epic) targetRarity++;
        else if (rules.rarityRule == TransformRule.Lower && oldRarity > Rarity.Common) targetRarity--;
        else if (rules.rarityRule == TransformRule.Any) targetRarity = RunManager.Instance.RollRarityForDay(RunManager.Instance.Stats.CurrentDay);

        int minProv = 0;
        int maxProv = -1; 
        if (rules.provisionRule == TransformRule.Same)
        {
            minProv = oldDef.provisionCost;
            maxProv = oldDef.provisionCost;
        }
        else if (rules.provisionRule == TransformRule.Higher)
        {
            minProv = oldDef.provisionCost + 1;
        }
        else if (rules.provisionRule == TransformRule.Lower)
        {
            maxProv = Mathf.Max(0, oldDef.provisionCost - 1);
        }

        Region? targetRegion = null;
        if (rules.regionRule == TransformRule.Same) targetRegion = oldDef.region;

        UnitDefinition newDef = null;
        int attempts = 0;

        while (attempts < 50)
        {
            UnitDefinition candidate = UnitDatabase.Instance.GetRandomUnit(
                targetRarity, targetRegion, UnitTagFlags.None, minProv, maxProv
            );

            if (candidate == null) break;

            newDef = candidate;

            if (candidate != oldDef)
            {
                break;
            }

            attempts++;
        }

        if (newDef == null) return false;

        if (MetaManager.Instance != null)
        {
            MetaManager.Instance.UnlockUnitInCompendium(newDef);
        }

        RunManager.UnitPlacement placement = targetUnit.myPlacement;
        placement.unitData.definition = newDef;
        placement.unitData.rarity = targetRarity;
        placement.unitData.id = System.Guid.NewGuid();

        if (!rules.keepMutations)
        {
            placement.unitData.prefix = null;
            placement.unitData.suffix = null;
        }

        GridManager grid = targetUnit.myGrid;
        Vector2Int actualPos = grid.GetUnitPosition(targetUnit);
        int r = actualPos.x;
        int col = actualPos.y;

        bool isPlayer = targetUnit.isPlayer;
        grid.RemoveUnit(r, col, true);
        grid.PlaceUnit(placement, r, col, null, isPlayer);

        return true;
    }
}


