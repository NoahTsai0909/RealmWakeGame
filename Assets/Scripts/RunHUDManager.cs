using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using static SceneLoader;

public class RunHUDManager : MonoBehaviour
{
    public static RunHUDManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text playerXPText;
    [SerializeField] private Image playerXPFill;
    [SerializeField] private TMP_Text provisionCapText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button squadButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button inspectStatsButton;


    [Header("XP Settings")]
    [SerializeField] private int maxReputation = 10; 

    [Header("Animation Settings")]
    private RectTransform hudRect;
    private Vector2 originalAnchoredPos;

    [Header("References")]
    [SerializeField] private GameObject runHUD;
    [SerializeField] private CanvasGroup runHUDCanvasGroup;
    private GameObject currentUnitStatsWindow;

    [Header("Behavior")]
    [SerializeField] private bool dontDestroyOnLoad = false;
    private int currentProvision = -1; // -1 means not currently tracking provision usage
    private bool isProvisionValid = true;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (runHUD != null)
        {
            hudRect = runHUD.GetComponent<RectTransform>();
            if (hudRect != null)
            {
                originalAnchoredPos = hudRect.anchoredPosition;
            }
        }
        squadButton.onClick.AddListener(() => {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "PrepScene")
            {
                PrepSceneManager prepManager = Object.FindFirstObjectByType<PrepSceneManager>();
                if (prepManager != null)
                {
                    prepManager.ReturnToMapScene();
                }
                else
                {
                    SceneLoader.Instance.LoadScene(SceneLoader.Instance.lastScene);
                }
            }
            else
            {
                SceneLoader.Instance.LoadScene(GameScene.PrepScene);
            }
        });
        settingsButton.onClick.AddListener(() => {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OpenSettings();
            }
        });
        if (inspectStatsButton != null)
        {
            inspectStatsButton.onClick.AddListener(() =>
            {
                if (currentUnitStatsWindow != null)
                {
                    currentUnitStatsWindow.SetActive(!currentUnitStatsWindow.activeSelf);
                }
            });
            inspectStatsButton.gameObject.SetActive(false);
        }
        StartCoroutine(InitializeFromRunManager());
    }

    private void OnEnable()
    {
        // Subscribe to all events
        RunStatsEventBus.OnGoldChanged += UpdateGold;
        RunStatsEventBus.OnHealthChanged += UpdateHealth;
        RunStatsEventBus.OnDayChanged += UpdateDay;
        RunStatsEventBus.OnLevelChanged += UpdateLevel;
        RunStatsEventBus.OnReputationChanged += UpdateReputation;
        RunStatsEventBus.OnProvisionCapChanged += UpdateProvisionCap;
    }

    private void OnDisable()
    {
        // Unsubscribe from all events
        RunStatsEventBus.OnGoldChanged -= UpdateGold;
        RunStatsEventBus.OnHealthChanged -= UpdateHealth;
        RunStatsEventBus.OnDayChanged -= UpdateDay;
        RunStatsEventBus.OnLevelChanged -= UpdateLevel;
        RunStatsEventBus.OnReputationChanged -= UpdateReputation;
        RunStatsEventBus.OnProvisionCapChanged -= UpdateProvisionCap;
    }

    private IEnumerator InitializeFromRunManager()
    {
        yield return null;

        if (RunManager.Instance != null)
        {
            UpdateGold(RunManager.Instance.Stats.CurrentGold);
            UpdateHealth(RunManager.Instance.Stats.PlayerHealth);
            UpdateDay(RunManager.Instance.Stats.CurrentDay);
            UpdateLevel(RunManager.Instance.Stats.PlayerLevel);
            UpdateXP(RunManager.Instance.Stats.Experience);
            UpdateReputation(RunManager.Instance.Stats.Experience);
            UpdateProvisionCap(RunManager.Instance.Stats.ProvisionCap);
        }
    }

    // UI Update Methods
    private void UpdateGold(int gold)
    {
        if (goldText != null)
            goldText.SetText(TextIconUtility.ParseDescription("[c_gold]" + gold.ToString() + "[/c]")); 
    }

    private void UpdateHealth(int health)
    {
        if (playerHealthText != null)
            playerHealthText.SetText(TextIconUtility.ParseDescription("[c_playerhealth]" + health.ToString() + "[/c]"));
    }

    private void UpdateDay(int day)
    {
        if (dayText != null)
            dayText.SetText(TextIconUtility.ParseDescription("[c_day]Day " + day.ToString() + "[/c]"));
    }

    private void UpdateLevel(int level)
    {
        if (playerXPText != null)
            playerLevelText.SetText(TextIconUtility.ParseDescription("[c_level]" + level.ToString() + "[/c]"));
    }

    private void UpdateXP(int xp)
    {
        if (playerXPFill != null)
        {
            playerXPText.SetText(TextIconUtility.ParseDescription("[c_experience]" + xp.ToString() + " / " + maxReputation.ToString() + "[/c]"));
        }
    }

    private void UpdateReputation(int reputation)
    {
        if (playerXPFill != null)
        {
            float fillAmount = (float)reputation / maxReputation;
            playerXPFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    private void UpdateProvisionCap(int cap)
    {
        if (provisionCapText == null) return;

        if (currentProvision >= 0)
        {
            if (isProvisionValid)
            {
                provisionCapText.SetText(TextIconUtility.ParseDescription($"[c_maxprovision]{currentProvision} / {cap}[/c]"));
            }
            else
            {
                provisionCapText.SetText(TextIconUtility.ParseDescription($"[c_maxprovision]<color=#FF4444>{currentProvision} / {cap}</color>[/c]"));
            }
        }
        else 
        {
            provisionCapText.SetText(TextIconUtility.ParseDescription("[c_maxprovision]" + cap.ToString() + "[/c]"));
        }
    }

    public void SetCurrentProvision(int current, bool isValid)
    {
        currentProvision = current;
        isProvisionValid = isValid;
        if (RunManager.Instance != null)
            UpdateProvisionCap(RunManager.Instance.Stats.ProvisionCap);
    }

    public void ClearCurrentProvision()
    {
        currentProvision = -1;
        isProvisionValid = true;
        if (RunManager.Instance != null)
            UpdateProvisionCap(RunManager.Instance.Stats.ProvisionCap);
    }

    public void EnableInspectStats(GameObject statsWindow)
    {
        currentUnitStatsWindow = statsWindow;
        if (inspectStatsButton != null) inspectStatsButton.gameObject.SetActive(true);
    }

    public void DisableInspectStats()
    {
        currentUnitStatsWindow = null;
        if (inspectStatsButton != null) inspectStatsButton.gameObject.SetActive(false);
    }


    public void Hide()
    {
        if (runHUD != null)
        {
            runHUD.SetActive(false);
            return;
        }

        if (runHUDCanvasGroup != null)
        {
            runHUDCanvasGroup.alpha = 0f;
            runHUDCanvasGroup.interactable = false;
            runHUDCanvasGroup.blocksRaycasts = false;
        }
    }


    public void Show()
    {
 
        if (runHUD != null)
        {
            runHUD.SetActive(true);
        }
        else if (runHUDCanvasGroup != null)
        {
            runHUDCanvasGroup.alpha = 1f;
            runHUDCanvasGroup.interactable = true;
            runHUDCanvasGroup.blocksRaycasts = true;
        }

        if (RunManager.Instance != null)
        {
            UpdateGold(RunManager.Instance.Stats.CurrentGold);
            UpdateHealth(RunManager.Instance.Stats.PlayerHealth);
            UpdateDay(RunManager.Instance.Stats.CurrentDay);
            UpdateLevel(RunManager.Instance.Stats.PlayerLevel);
            UpdateXP(RunManager.Instance.Stats.Experience);
            UpdateReputation(RunManager.Instance.Stats.Experience);
            UpdateProvisionCap(RunManager.Instance.Stats.ProvisionCap);
        }
    }

    public void SlideOutAndHide(float duration = 0.5f)
    {
        if (gameObject.activeInHierarchy && hudRect != null)
        {
            StartCoroutine(SlideOutRoutine(duration));
        }
        else
        {
            Hide();
        }
    }

    private IEnumerator SlideOutRoutine(float duration)
    {
        Vector2 startPos = hudRect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, 300f); 

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hudRect.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        hudRect.anchoredPosition = endPos;
        Hide();
    }

    public void HideSquadButton()
    {
        if (squadButton != null)
        {
            squadButton.gameObject.SetActive(false);
        }
    }

    public void ShowSquadButton()
    {
        if (squadButton != null)
        {
            squadButton.gameObject.SetActive(true);
        }
    }

    public void ResetAndShow()
    {
        if (hudRect != null)
        {
            hudRect.anchoredPosition = originalAnchoredPos; // Snap back instantly
        }
        Show();
    }
}