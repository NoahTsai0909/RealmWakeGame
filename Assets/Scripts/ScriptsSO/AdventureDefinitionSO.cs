using System.Collections.Generic;
using UnityEngine;

public enum DifficultyModifier
{
    EnemyHpPlus20,
    LateGameEnemyMutations,
}

[System.Serializable]
public class ModifierTacticMap
{
    public DifficultyModifier modifier;
    public TacticDefinition phantomTactic;
}

[System.Serializable]
public class DifficultyTier
{
    public int difficultyLevel;
    [TextArea] public string description; 
    public List<DifficultyModifier> activeModifiers;
    public List<UnitDefinition> unitRewards;
    public List<AdventureDefinitionSO> adventureRewards;
    public List<Region> regionRewards;
}

[CreateAssetMenu(fileName = "New Adventure", menuName = "Adventure/Adventure Definition")]
public class AdventureDefinitionSO : ScriptableObject
{
    [Header("Basic Info")]
    public string adventureName;
    [TextArea] public string description;
    public Sprite artworkCard;
    [Header("Run Rules")]
    public int totalDays = 12;
    public int startingGold = 10;
    public int startingHealth = 12;
    public int startingProvisionCap = 4;

    [Header("Difficulty Settings")]
    public List<DifficultyTier> difficultyTiers = new List<DifficultyTier>();

    [Tooltip("Map the modifiers to the specific Tactic UI you want to spawn on the enemy bar.")]
    public List<ModifierTacticMap> modifierPhantomTactics = new List<ModifierTacticMap>();

    [Header("Event Pools")]
    [Tooltip("Standard encounters, shops, and story events specific to this adventure.")]
    public List<BaseEventSO> regularEvents = new List<BaseEventSO>();

    [Tooltip("Combat encounters specific to this adventure.")]
    public List<BaseEventSO> combatEvents = new List<BaseEventSO>();

    [Tooltip("Audio clips specific to this adventure.")]
    public AudioClip adventureMusic;
}