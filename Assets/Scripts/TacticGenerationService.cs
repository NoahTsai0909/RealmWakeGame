using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TacticGenerationService
{
    public static RunManager.TacticSaveData GenerateTactic(Region? region = null, bool byPassExclusivity = false)
    {
        int day = RunManager.Instance.Stats.CurrentDay;
        DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day, RunManager.Instance.TOTAL_DAYS);
        Rarity rolledRarity = RarityDistributionTable.RollRarity(dist);

        // 1. Get a tactic from the database
        TacticDefinition def = TacticDatabase.Instance.GetRandomTactic(rolledRarity, region, byPassExclusivity);

        if (def == null) return null;

        // 2. Check the player's inventory immediately to adjust the rarity BEFORE the UI sees it!
        Rarity finalRarity = rolledRarity;

        var existingTactic = RunManager.Instance.playerTactics.FirstOrDefault(p =>
            p.tacticData != null &&
            p.tacticData.definition == def);

        if (existingTactic != null)
        {
            finalRarity = existingTactic.tacticData.rarity;
        }

        return new RunManager.TacticSaveData
        {
            definition = def,
            rarity = finalRarity
        };
    }

    public static List<RunManager.TacticSaveData> GenerateShopTactics(int count, Region? region = null, bool byPassExclusivity = false, bool allowCombatExclusive = false, bool onlyCombatExclusive = false)
    {
        {
            List<RunManager.TacticSaveData> generated = new List<RunManager.TacticSaveData>();
            HashSet<TacticDefinition> rolledThisShop = new HashSet<TacticDefinition>();

            int day = RunManager.Instance.Stats.CurrentDay;
            DayRarityEntry dist = RunManager.Instance.rarityDistributionTable.GetForDay(day, RunManager.Instance.TOTAL_DAYS);

            for (int i = 0; i < count; i++)
            {
                Rarity rolledRarity = RarityDistributionTable.RollRarity(dist);
                TacticDefinition def = null;

                // Re-roll loop to prevent duplicates in the shop
                int safety = 0;
                while (safety < 20)
                {
                    def = TacticDatabase.Instance.GetRandomTactic(rolledRarity, region, byPassExclusivity, allowCombatExclusive, onlyCombatExclusive);

                    if (def != null && !rolledThisShop.Contains(def))
                    {
                        break; // We found a unique one!
                    }
                    safety++;
                }

                if (def == null) continue;

                rolledThisShop.Add(def); // Remember it so we don't roll it again

                // Check if player already owns it to preserve upgrade rarity
                Rarity finalRarity = rolledRarity;
                var existingTactic = RunManager.Instance.playerTactics.FirstOrDefault(p =>
                    p.tacticData != null && p.tacticData.definition == def);

                if (existingTactic != null) finalRarity = existingTactic.tacticData.rarity;

                generated.Add(new RunManager.TacticSaveData
                {
                    definition = def,
                    rarity = finalRarity
                });
            }

            return generated;
        }
    }
}