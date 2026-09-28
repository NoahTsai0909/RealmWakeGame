using UnityEngine;

public class CombatSFXManager : MonoBehaviour
{
    public static CombatSFXManager Instance;

    [Header("Fallback Sound Effects")]
    [SerializeField] private AudioClip defaultDamageSound;
    [SerializeField] private AudioClip defaultHealSound;
    [SerializeField] private AudioClip defaultShieldSound;
    [SerializeField] private AudioClip defaultBurnSound;
    [SerializeField] private AudioClip defaultPoisonSound;
    [SerializeField] private AudioClip defaultHasteSound;
    [SerializeField] private AudioClip defaultSlowSound;
    [SerializeField] private AudioClip defaultDeathSound;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // Listen to the CombatEventBus for deaths
        CombatEventBus.OnCombatEvent += HandleCombatEvent;
    }

    private void OnDisable()
    {
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    public void PlayActionSFX(CombatAction action)
    {
        if (action == null || action.isSilent) return;

        AudioClip clipToPlay = action.audioOverride;

        if (clipToPlay == null)
        {
            clipToPlay = GetFallbackSound(action.type);
        }

        if (clipToPlay != null)
        {
            AudioManager.Instance.PlaySFX(clipToPlay);
        }
    }

    private AudioClip GetFallbackSound(CombatActionType actionType)
    {
        return actionType switch
        {
            CombatActionType.Damage => defaultDamageSound,
            CombatActionType.Heal => defaultHealSound,
            CombatActionType.Shield => defaultShieldSound,
            CombatActionType.ApplyBurn => defaultBurnSound,
            CombatActionType.ApplyPoison => defaultPoisonSound,
            CombatActionType.ApplyHaste => defaultHasteSound,
            CombatActionType.ApplySlow => defaultSlowSound,
            _ => null 
        };
    }

    private void HandleCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        // Play death sound when a unit dies!
        if (type == CombatEventBus.CombatEventType.UnitDied && defaultDeathSound != null)
        {
            AudioManager.Instance.PlaySFX(defaultDeathSound);
        }
    }
}
