using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisasterManager : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float disasterStartTime = 60f;
    [SerializeField] private float disasterTickInterval = 1f;
    [SerializeField] private int disasterInitialDamage = 1;

    [Header("Visual Settings")]
    [SerializeField] private Image screenOverlay;
    [SerializeField] private Color disasterColor = new Color(0.5f, 0f, 0.5f, 0.3f);
    [SerializeField] private float fadeInDuration = 3f;

    [Header("UI Elements")]
    [SerializeField] private GameObject timerContainer;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image disasterIcon;   

    [Header("References")]
    [SerializeField] private gameManager combatManager;

    private float combatTimer = 0f;
    private bool disasterActive = false;
    private int currentDisasterTick = 0;
    private Coroutine disasterCoroutine;

    void Start()
    {
        // Setup initial UI state
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (disasterIcon != null) disasterIcon.gameObject.SetActive(false);
        if (timerContainer != null) timerContainer.SetActive(false); 
    }

    void Update()
    {
        if (combatManager == null || !combatManager.isCombatActive())
        {
            if (timerContainer != null && timerContainer.activeSelf)
                timerContainer.SetActive(false);
            return;
        }

        if (timerContainer != null && !timerContainer.activeSelf)
            timerContainer.SetActive(true);

        combatTimer += Time.deltaTime;

        if (!disasterActive)
        {
            float timeRemaining = Mathf.Max(0, disasterStartTime - combatTimer);
            UpdateTimerUI(timeRemaining);

            if (combatTimer >= disasterStartTime)
            {
                StartDisaster();
            }
        }
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (timeRemaining <= 10f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    private void StartDisaster()
    {
        disasterActive = true;
        currentDisasterTick = 0;

        // Swap UI from Text to Icon
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (disasterIcon != null) disasterIcon.gameObject.SetActive(true);

        StartCoroutine(FadeInOverlay());
        disasterCoroutine = StartCoroutine(DisasterDamageRoutine());

        Debug.Log("DISASTER: The storm begins!");
    }

    private IEnumerator DisasterDamageRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(disasterTickInterval);

            currentDisasterTick++;
            int damageThisTick = disasterInitialDamage + currentDisasterTick - 1;

            DealDisasterDamage(damageThisTick);
            Debug.Log($"DISASTER: Dealt {damageThisTick} damage to all units");
        }
    }

    private void DealDisasterDamage(int damage)
    {
        List<UnitInstance> playerUnits = combatManager.playerGrid.GetAllUnits();
        foreach (UnitInstance unit in playerUnits)
        {
            if (unit != null) unit.TakeDisasterDamage(damage);
        }

        List<UnitInstance> enemyUnits = combatManager.enemyGrid.GetAllUnits();
        foreach (UnitInstance unit in enemyUnits)
        {
            if (unit != null) unit.TakeDisasterDamage(damage);
        }
    }

    private IEnumerator FadeInOverlay()
    {
        if (screenOverlay == null) yield break;

        screenOverlay.gameObject.SetActive(true);
        screenOverlay.color = new Color(disasterColor.r, disasterColor.g, disasterColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, disasterColor.a, elapsed / fadeInDuration);
            screenOverlay.color = new Color(disasterColor.r, disasterColor.g, disasterColor.b, alpha);
            yield return null;
        }

        screenOverlay.color = disasterColor;
    }

    public void StopDisaster()
    {
        if (disasterCoroutine != null)
        {
            StopCoroutine(disasterCoroutine);
            disasterCoroutine = null;
        }

        if (screenOverlay != null && screenOverlay.gameObject.activeSelf)
        {
            StartCoroutine(FadeOutOverlay());
        }

        disasterActive = false;
        combatTimer = 0f;
        if (timerContainer != null) timerContainer.SetActive(false);
    }

    private IEnumerator FadeOutOverlay()
    {
        Color startColor = screenOverlay.color;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / 1f);
            screenOverlay.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        screenOverlay.gameObject.SetActive(false);
    }

    private void OnCombatEnd()
    {
        StopDisaster();
    }

    void OnEnable()
    {
        CombatEventBus.OnCombatEvent += HandleCombatEvent;
    }

    void OnDisable()
    {
        CombatEventBus.OnCombatEvent -= HandleCombatEvent;
    }

    private void HandleCombatEvent(CombatEventBus.CombatEventType type, UnitInstance source, UnitInstance target, int amount)
    {
        if (type == CombatEventBus.CombatEventType.UnitDied)
        {
            if (combatManager != null && !combatManager.isCombatActive())
            {
                OnCombatEnd();
            }
        }
    }
}