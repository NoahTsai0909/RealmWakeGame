using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

public class CombatVFXManager : MonoBehaviour
{
    public static CombatVFXManager Instance;

    [Header("Defaults")]
    [SerializeField] private float projectileTravelTime;

    [Header("Fallback Projectile Prefabs")]
    [SerializeField] private GameObject defaultDamageProjectilePrefab;
    [SerializeField] private GameObject defaultHealProjectilePrefab;
    [SerializeField] private GameObject defaultShieldProjectilePrefab;
    [SerializeField] private GameObject defaultBurnProjectilePrefab;
    [SerializeField] private GameObject defaultPoisonProjectilePrefab;
    [SerializeField] private GameObject defaultSlowProjectilePrefab;
    [SerializeField] private GameObject defaultHasteProjectilePrefab;
    [SerializeField] private GameObject defaultMeleeProjectilePrefab;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject healImpactPrefab;
    [SerializeField] private GameObject attackImpactPrefab;
    [SerializeField] private GameObject shieldImpactPrefab;
    [SerializeField] private GameObject burnImpactPrefab;
    [SerializeField] private GameObject poisonImpactPrefab;
    [SerializeField] private GameObject slowImpactPrefab;
    [SerializeField] private GameObject hasteImpactPrefab;
    [SerializeField] private GameObject exitDustPrefab;

    [Header("Fallback Colors")]
    [SerializeField] private Color defaultDamageColor = Color.red;
    [SerializeField] private Color defaultHealColor = Color.green;
    [SerializeField] private Color defaultShieldColor = Color.yellow;
    [SerializeField] private Color defaultBurnColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color defaultPoisonColor = Color.purple;
    [SerializeField] private Color defaultSlowColor = Color.brown;
    [SerializeField] private Color defaultHasteColor = Color.cyan;

    [Header("Projectile Motion")]
    public float maxArcHeight = 2.0f; // How high it arcs
    [Tooltip("Controls the speed of the projectile over its travel time.")]
    public AnimationCurve projectileSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Draw the physical shape of the arc! Start at 0, peak at 1, end at 0.")]
    public AnimationCurve projectileArcCurve = new AnimationCurve(
    new Keyframe(0f, 0f),
    new Keyframe(0.3f, 1f), // Peaks early at 30% of the flight
    new Keyframe(1f, 0f)
);

    private void Awake()
    {
        Instance = this;
    }

    public void PlayActionVFX(CombatAction action, Action onImpact)
    {
        if (action.target == null)
        {
            onImpact?.Invoke();
            return;
        }

        if (action.source == null)
        {
            PlayInstantEffect(action);
            onImpact?.Invoke();
            return;
        }

        if (RequiresProjectile(action) && action.source != action.target)
        {
            PlayProjectile(action, onImpact);
        }
        else
        {
            PlayInstantEffect(action);
            onImpact?.Invoke();
        }
    }

    private bool RequiresProjectile(CombatAction action)
    {
        switch (action.type)
        {
            case CombatActionType.Damage:
            case CombatActionType.Heal:
            case CombatActionType.ApplyBurn:
            case CombatActionType.ApplyPoison:
            case CombatActionType.Shield:
            case CombatActionType.ApplySlow:
            case CombatActionType.ApplyHaste:
                return true;

            default:
                return false;
        }
    }

    private void PlayProjectile(CombatAction action, Action onImpact)
    {
        GameObject projectilePrefab = GetProjectileForAction(action);

        if (projectilePrefab == null)
        {
            onImpact?.Invoke();
            return;
        }

        Vector3 start = action.source.transform.position;
        GameObject proj = Instantiate(projectilePrefab, start, Quaternion.identity);

        TrailRenderer tr = proj.GetComponent<TrailRenderer>();
        if (tr != null)
        {
            Color projColor = GetColorForAction(action);
            /*tr.startColor = projColor;

            // Fade out to the same color but with 0 Alpha
            tr.endColor = new Color(projColor.r, projColor.g, projColor.b, 0f);*/
        }
        Action wrappedOnImpact = () =>
        {
            if (CombatSFXManager.Instance != null)
            {
                CombatSFXManager.Instance.PlayActionSFX(action);
            }
            if (action.type == CombatActionType.Heal && action.target != null && healImpactPrefab != null)
            {
                Instantiate(healImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.Damage && action.target != null && attackImpactPrefab != null)
            {
                Instantiate(attackImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.Shield && action.target != null && shieldImpactPrefab != null)
            {
                Instantiate(shieldImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.ApplyBurn && action.target != null && burnImpactPrefab != null)
            {
                Instantiate(burnImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.ApplyPoison && action.target != null && poisonImpactPrefab != null)
            {
                Instantiate(poisonImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.ApplySlow && action.target != null && slowImpactPrefab != null)
            {
                Instantiate(slowImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
            else if (action.type == CombatActionType.ApplyHaste && action.target != null && hasteImpactPrefab != null)
            {
                Instantiate(hasteImpactPrefab, action.target.transform.position, Quaternion.identity);
            }
                onImpact?.Invoke();
        };

        bool isMeleeSlash = action.type == CombatActionType.Damage &&
                    action.source != null &&
                    action.source.Definition != null &&
                    action.source.Definition.isMelee;

        StartCoroutine(
            TravelProjectile(
                proj.transform,
                action.target.transform,
                projectileTravelTime,
                wrappedOnImpact,
                isMeleeSlash
            )
        );
    }

    private GameObject GetProjectileForAction(CombatAction action)
    {
        if (action.projectileOverride != null)
        {
            return action.projectileOverride;
        }
        if (action.type == CombatActionType.Damage &&
            action.source != null &&
            action.source.Definition != null &&
            action.source.Definition.isMelee)
        {
            return defaultMeleeProjectilePrefab != null
                ? defaultMeleeProjectilePrefab
                : defaultDamageProjectilePrefab;
        }

        return GetFallbackProjectile(action.type);
    }


    private IEnumerator TravelProjectile(
    Transform projectile,
    Transform target,
    float duration,
    Action onImpact,
    bool straightLine = false)
    {
        float elapsed = 0f;
        Vector3 startPos = projectile.position;

        while (elapsed < duration)
        {
            if (target == null)
            {
                onImpact?.Invoke();
                Destroy(projectile.gameObject);
                yield break;
            }

            elapsed += Time.deltaTime;

            float linearT = Mathf.Clamp01(elapsed / duration);

            float t = straightLine
                ? linearT
                : projectileSpeedCurve.Evaluate(linearT);

            Vector3 currentTargetPos = target.position;
            Vector3 currentPos = Vector3.Lerp(startPos, currentTargetPos, t);

            if (!straightLine)
            {
                float arc = projectileArcCurve.Evaluate(linearT) * maxArcHeight;
                currentPos.y += arc;
            }
            Vector3 moveDirection = currentPos - projectile.position;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                projectile.rotation = Quaternion.Euler(0, 0, angle);
            }

            projectile.position = currentPos;

            yield return null;
        }

        if (target != null)
            onImpact?.Invoke();

        Destroy(projectile.gameObject);
    }

    private void PlayInstantEffect(CombatAction action)
    {
        if (CombatSFXManager.Instance != null)
        {
            CombatSFXManager.Instance.PlayActionSFX(action);
        }
        if (action.target == null) return;
        if (action.target.Visuals != null)
        {
            switch (action.type)
            {
                case CombatActionType.Shield:
                    action.target.Visuals.Flash(Color.gold, false);
                    break;
                case CombatActionType.Heal:
                    action.target.Visuals.Flash(Color.green, false);
                    break;
            }
        }
        switch (action.type)
        {
            case CombatActionType.Heal:
                if (healImpactPrefab != null) Instantiate(healImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.Damage:
                if (attackImpactPrefab != null) Instantiate(attackImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.Shield:
                if (shieldImpactPrefab != null) Instantiate(shieldImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.ApplyBurn:
                if (burnImpactPrefab != null) Instantiate(burnImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.ApplyPoison:
                if (poisonImpactPrefab != null) Instantiate(poisonImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.ApplySlow:
                if (slowImpactPrefab != null) Instantiate(slowImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
            case CombatActionType.ApplyHaste:
                if (hasteImpactPrefab != null) Instantiate(hasteImpactPrefab, action.target.transform.position, Quaternion.identity);
                break;
        }
    }

    private GameObject GetFallbackProjectile(CombatActionType actionType)
    {
        return actionType switch
        {
            CombatActionType.Damage => defaultDamageProjectilePrefab,
            CombatActionType.Heal => defaultHealProjectilePrefab,
            CombatActionType.Shield => defaultShieldProjectilePrefab,
            CombatActionType.ApplyBurn => defaultBurnProjectilePrefab,
            CombatActionType.ApplyHaste => defaultHasteProjectilePrefab,
            CombatActionType.ApplySlow => defaultSlowProjectilePrefab,
            CombatActionType.ApplyPoison => defaultPoisonProjectilePrefab, 
            _ => defaultDamageProjectilePrefab 
        };
    }

    private Color GetColorForAction(CombatAction action)
    {
        return action.type switch
        {
            CombatActionType.Damage => defaultDamageColor,
            CombatActionType.Heal => defaultHealColor,
            CombatActionType.Shield => defaultShieldColor,
            CombatActionType.ApplyBurn => defaultBurnColor,
            CombatActionType.ApplyPoison => defaultPoisonColor,
            CombatActionType.ApplySlow => defaultSlowColor,
            CombatActionType.ApplyHaste => defaultHasteColor,
            _ => Color.white
        };
    }

    private void OnEnable()
    {
        CombatEventBus.OnCombatEvent += HandleCombatEvent;
    }

    private void OnDisable()
    {
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    private void HandleCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (type == CombatEventBus.CombatEventType.UnitDied)
        {
            if (target != null && exitDustPrefab != null)
            {
                Instantiate(exitDustPrefab, target.transform.position, Quaternion.identity);
            }
        }
    }

}

