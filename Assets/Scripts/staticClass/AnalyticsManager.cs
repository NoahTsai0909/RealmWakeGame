using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject privacyPanel;
    [SerializeField] private Button sendDataButton;
    [SerializeField] private Button doNotSendButton;
    [SerializeField] private Button privacyPolicyButton;

    [Header("Settings")]
    [SerializeField] private string privacyPolicyURL = "https://yourstudio.com/privacy";

    private const string ConsentKey = "Analytics_Consent_Given";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (privacyPanel)
            {
                Destroy(privacyPanel.gameObject);
            }
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (sendDataButton != null) sendDataButton.onClick.AddListener(AcceptAnalytics);
        if (doNotSendButton != null) doNotSendButton.onClick.AddListener(DeclineAnalytics);
        if (privacyPolicyButton != null) privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);

        CheckConsentStatus();
    }

    private void CheckConsentStatus()
    {
        int consentStatus = PlayerPrefs.GetInt(ConsentKey, 0);

        if (consentStatus == 0)
        {
            Time.timeScale = 0f;
            SetPanelActive(true);
        }
        else if (consentStatus == 1)
        {
            SetPanelActive(false);
            InitializeAnalyticsSDK();
        }
        else
        {
            SetPanelActive(false);
        }
    }

    private void AcceptAnalytics()
    {
        PlayerPrefs.SetInt(ConsentKey, 1);
        PlayerPrefs.Save();

        SetPanelActive(false);
        Time.timeScale = 1f;

        InitializeAnalyticsSDK();
    }

    private void DeclineAnalytics()
    {
        PlayerPrefs.SetInt(ConsentKey, -1);
        PlayerPrefs.Save();

        SetPanelActive(false);
        Time.timeScale = 1f;
        try
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                AnalyticsService.Instance.StopDataCollection();
                Debug.Log("Unity Analytics data collection stopped by user.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Analytics stop bypassed: " + e.Message);
        }
    }

    private void SetPanelActive(bool state)
    {
        try
        {
            if (privacyPanel)
            {
                privacyPanel.SetActive(state);
            }
        }
        catch (MissingReferenceException)
        {
            privacyPanel = null;
        }
    }

    private void OpenPrivacyPolicy()
    {
        Application.OpenURL(privacyPolicyURL);
    }

    private async void InitializeAnalyticsSDK()
    {
        Debug.Log("Initializing Unity Analytics SDK...");
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            AnalyticsService.Instance.StartDataCollection();
            Debug.Log("Unity Analytics data collection started.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Analytics: {e.Message}");
        }
    }

    public void ToggleAnalyticsConsent()
    {
        int currentStatus = PlayerPrefs.GetInt(ConsentKey, 0);

        if (currentStatus == 1)
        {
            DeclineAnalytics();
        }
        else
        {
            AcceptAnalytics();
        }
    }

    public bool IsConsentGranted()
    {
        return PlayerPrefs.GetInt(ConsentKey, 0) == 1;
    }
}