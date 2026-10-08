using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDefinition", menuName = "Units/Unit Definition")]
public class UnitDefinition : ScriptableObject, IStatSource
{
    [Header("Basic Stats")]
    public string unitName;
    public int maxHP;
    [Header("Mutation Data")]
    public ModifiableStats mainStat = ModifiableStats.Attack;

    [Header("Combat Values")]
    public int attack;
    public int heal;
    public int shield;
    public int burn;
    public int poison;
    public int slow;
    public int haste;
    public int critChance;

    [Header("Cooldown")]
    public float cooldown;
    public bool isPassive;
    public bool isEnergy;
    public int maxEnergy;
    public int multicast = 1;

    [Header("Overrides")]
    public bool overrideCooldownScaling = false;
    public float[] cooldownByRarity = new float[4];

    [Header("Visuals")]
    public Sprite unitSprite;
    public List<Sprite> defaultProjectile;

    [Header("Meta")]
    public int cost;
    public Region region; 
    public Rarity rarity;
    public Rarity startingRarity;
    public int provisionCost;
    public bool isMelee;
    [Tooltip("If true, this item will NEVER be randomly generated in standard loot pools.")]
    public bool isEventExclusive = false;

    [Tooltip("If true, this unit is completely removed from the game until unlocked as a Difficulty Reward.")]
    public bool isLockedByDefault = false;

    [Header("Prefab Reference")]
    public UnitInstance unitPrefab;
    public UnitDefinition spawnDefinition;

    [Header("Tags")]
    public UnitTagFlags tagFlags;

    public int Attack => attack;
    public int Heal => heal;
    public int MaxHP => maxHP;

    public float Cooldown => cooldown;

    public int Shield => shield;
    public int Burn => burn;
    public int Poison => poison;

    public int MaxEnergy => maxEnergy;

    public int Slow => slow;
    public int Haste => haste;

    public int Multicast => multicast;

    public int Value => 0;

    public int CritChance => critChance;

    public UnitTagFlags TagFlags => tagFlags;
}

public enum Rarity { Common, Uncommon, Rare, Epic, Mythic }
public enum Region { Solmire, Nethervale, Everborn, Axiom, None}