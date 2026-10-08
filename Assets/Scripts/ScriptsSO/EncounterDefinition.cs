using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Encounter", menuName = "Game/Encounter Definition")]
public class EncounterDefinition : ScriptableObject
{
    public string encounterName;
    public List<RunManager.UnitPlacement> enemyUnits = new();
    public List<RunManager.TacticPlacement> enemyTactics = new();

    [Header("Random Mutations")]
    [Tooltip("How many unmutated enemies should receive a random mutation?")]
    public int randomMutationCount = 0;


    public List<RunManager.UnitPlacement> GetRuntimeEnemyPlacements(int daySeed)
    {
        List<RunManager.UnitPlacement> runtimePlacements = new List<RunManager.UnitPlacement>();
        foreach (var p in enemyUnits)
        {
            RunManager.UnitPlacement copy = new RunManager.UnitPlacement
            {
                row = p.row,
                col = p.col,
                unitData = new UnitSaveData
                {
                    definition = p.unitData.definition,
                    rarity = p.unitData.rarity,
                    prefix = p.unitData.prefix,
                    suffix = p.unitData.suffix,
                    id = System.Guid.NewGuid()
                }
            };
            runtimePlacements.Add(copy);
        }

        int effectiveMutationCount = randomMutationCount;

        if (effectiveMutationCount > 0 && RunManager.Instance != null)
        {
            if (!RunManager.Instance.HasDifficultyModifier(DifficultyModifier.LateGameEnemyMutations))
            {
                effectiveMutationCount = 0;
            }
        }

        if (effectiveMutationCount > 0)
        {
            Random.State oldState = Random.state;

            Random.InitState(encounterName.GetHashCode() ^ daySeed);

            var unmutatedEnemies = runtimePlacements.Where(x => x.unitData.prefix == null && !x.unitData.definition.tagFlags.HasFlag(UnitTagFlags.Consumable)).ToList();

            for (int i = 0; i < effectiveMutationCount && unmutatedEnemies.Count > 0; i++)
            {
                int index = Random.Range(0, unmutatedEnemies.Count);
                var target = unmutatedEnemies[index];
                unmutatedEnemies.RemoveAt(index);

                var allPrefixes = RunManager.Instance.allAvailablePrefixes;
                if (allPrefixes != null && allPrefixes.Count > 0)
                {
                    target.unitData.prefix = allPrefixes[Random.Range(0, allPrefixes.Count)];

                    if (target.unitData.prefix.allowedSuffixes != null && target.unitData.prefix.allowedSuffixes.Count > 0)
                    {
                        target.unitData.suffix = target.unitData.prefix.allowedSuffixes[Random.Range(0, target.unitData.prefix.allowedSuffixes.Count)];
                    }
                }
            }

            Random.state = oldState;
        }

        return runtimePlacements;
    }
}

