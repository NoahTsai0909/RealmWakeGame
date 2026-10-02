using System;
using UnityEngine;

[System.Serializable]
public class RunStats
{
    private int _currentGold;
    public int CurrentGold
    {
        get => _currentGold;
        set
        {
            _currentGold = value;
            RunStatsEventBus.GoldChanged(_currentGold);
        }
    }

    private int _playerHealth;
    public int PlayerHealth
    {
        get => _playerHealth;
        set
        {
            _playerHealth = value;
            RunStatsEventBus.HealthChanged(_playerHealth);
        }
    }

    private int _currentDay;
    public int CurrentDay
    {
        get => _currentDay;
        set
        {
            _currentDay = value;
            RunStatsEventBus.DayChanged(_currentDay);
        }
    }

    private int _playerLevel;
    public int PlayerLevel
    {
        get => _playerLevel;
        set
        {
            _playerLevel = value;
            RunStatsEventBus.LevelChanged(_playerLevel);
        }
    }

    private int _reputation;
    public int Experience
    {
        get => _reputation;
        set
        {
            _reputation = value;
            RunStatsEventBus.ReputationChanged(_reputation);
        }
    }

    private int _provisionCap;
    public int ProvisionCap
    {
        get => _provisionCap;
        set
        {
            _provisionCap = value;
            RunStatsEventBus.ProvisionCapChanged(_provisionCap);
        }
    }

    // Initialize with default values
    public void Initialize(int startGold = 10, int startHealth = 12, int startProvCap = 4)
    {
        CurrentGold = startGold;
        PlayerHealth = startHealth;
        CurrentDay = 1;
        PlayerLevel = 1;
        Experience = 0;
        ProvisionCap = startProvCap;
    }
}

[System.Serializable]
public class UnitLifetimeStats
{
    public Guid id;
    public string unitName;
    public int totalDirectDamageDealt;
    public int totalBurnDamageDealt;
    public int totalPoisonDamageDealt;
    public int totalDamageTaken;
    public int totalHealingDone;
    public int totalShieldingDone;
    public int totalSlowsApplied;
    public int totalHastesApplied;
    public int totalAdvancesGiven;

    public float ContributionScore
    {
        get
        {
            float score = 0f;

            // Damage (Weight: 1.0)
            score += (totalDirectDamageDealt + totalBurnDamageDealt + totalPoisonDamageDealt) * 1.0f;

            // Mitigation (Weight: 1.5 - Healing is usually harder to output than damage)
            score += (totalHealingDone + totalShieldingDone) * 1.5f;

            // Tanking (Weight: 0.5 - Absorbing hits is good, but usually passive)
            score += totalDamageTaken * 0.5f;

            // Utility (Weight: 20.0 - Status effects are rare but highly impactful)
            score += (totalSlowsApplied + totalHastesApplied) * 20.0f;

            // Heavy Utility (Weight: 50.0 - Giving free turns is incredibly strong)
            score += totalAdvancesGiven * 50.0f;

            return score;
        }
    }
}
