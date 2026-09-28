using System.Collections.Generic;
using UnityEngine;

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

    [Header("Event Pools")]
    [Tooltip("Standard encounters, shops, and story events specific to this adventure.")]
    public List<BaseEventSO> regularEvents = new List<BaseEventSO>();

    [Tooltip("Combat encounters specific to this adventure.")]
    public List<BaseEventSO> combatEvents = new List<BaseEventSO>();

    [Tooltip("Audio clips specific to this adventure.")]
    public AudioClip adventureMusic;
}