using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Core UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundOverlay;
    [Header("Tabs")]
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject videoTab;
    [SerializeField] private GameObject gameplayTab;

    [Header("Tab Buttons")]
    [SerializeField] private Button audioButton;
    [SerializeField] private Button videoButton;
    [SerializeField] private Button gameplayButton;

    [Header("Persistent Action Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button abandonRunButton;

    [Header("Audio Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Video Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private Toggle vSyncToggle;

    [Header("Gameplay Settings")]
    [SerializeField] private TMP_Dropdown combatSpeedDropdown;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Button resetTutorialButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        settingsPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

        // Hook up the tabs
        if (audioButton != null) audioButton.onClick.AddListener(() => OpenTab(audioTab));
        if (videoButton != null) videoButton.onClick.AddListener(() => OpenTab(videoTab));
        if (gameplayButton != null) gameplayButton.onClick.AddListener(() => OpenTab(gameplayTab));

        // Hook up persistent actions
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (abandonRunButton != null) abandonRunButton.onClick.AddListener(AbandonRun);

        if (masterSlider != null)
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
        if (VideoManager.Instance != null)
        {
            // 1. Setup VSync Toggle
            if (vSyncToggle != null)
            {
                vSyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) == 1;
                vSyncToggle.onValueChanged.AddListener(VideoManager.Instance.SetVSync);
            }

            // 2. Setup Display Mode Dropdown
            if (displayModeDropdown != null)
            {
                displayModeDropdown.value = PlayerPrefs.GetInt("DisplayMode", 0);
                displayModeDropdown.onValueChanged.AddListener(VideoManager.Instance.SetDisplayMode);
            }

            // 3. Setup Resolution Dropdown dynamically
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();

                Resolution[] resList = VideoManager.Instance.availableResolutions;
                int currentResIndex = 0;
                int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", resList.Length - 1);

                for (int i = 0; i < resList.Length; i++)
                {
                    string option = resList[i].width + " x " + resList[i].height + " (" + resList[i].refreshRateRatio.value + "hz)";
                    options.Add(option);

                    if (i == savedResIndex)
                    {
                        currentResIndex = i;
                    }
                }

                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentResIndex;
                resolutionDropdown.RefreshShownValue();

                // Add the listener AFTER setting the initial value so it doesn't trigger accidentally
                resolutionDropdown.onValueChanged.AddListener(VideoManager.Instance.SetResolution);
            }
        }

        if (GameplayManager.Instance != null)
        {
            // 1. Combat Speed
            if (combatSpeedDropdown != null)
            {
                combatSpeedDropdown.value = PlayerPrefs.GetInt("CombatSpeedIndex", 0);
                combatSpeedDropdown.onValueChanged.AddListener(GameplayManager.Instance.SetCombatSpeed);
            }

            // 2. Screen Shake
            if (screenShakeToggle != null)
            {
                screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
                screenShakeToggle.onValueChanged.AddListener(GameplayManager.Instance.SetScreenShake);
            }

            // 3. Damage Numbers
            if (damageNumbersToggle != null)
            {
                damageNumbersToggle.isOn = PlayerPrefs.GetInt("DamageNumbers", 1) == 1;
                damageNumbersToggle.onValueChanged.AddListener(GameplayManager.Instance.SetDamageNumbers);
            }
            if (resetTutorialButton != null)
            {
                resetTutorialButton.onClick.AddListener(TutorialManager.Instance.ResetAllTutorials);
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        backgroundOverlay.gameObject.SetActive(true);
        OpenTab(audioTab); // Always default to Audio tab when opening

        // Get the exact name of the scene we are currently in
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 1. ABANDON RUN LOGIC
        // Only show Abandon Run if we are actually in a gameplay scene
        bool isOutOfGame = currentScene == "Bootstrap" ||
                           currentScene == "MainMenuScene" ||
                           currentScene == "AdventureSelectionScene" ||
                           currentScene == "RunSummaryScene";

        if (abandonRunButton != null)
        {
            abandonRunButton.gameObject.SetActive(!isOutOfGame);
        }

        // 2. MAIN MENU LOGIC
        // Only hide the Main Menu button if we are literally already there (or booting up)
        bool isAlreadyAtMainMenu = currentScene == "MainMenuScene" || currentScene == "Bootstrap";

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(!isAlreadyAtMainMenu);
        }
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        backgroundOverlay.gameObject.SetActive(false);
    }

    private void OpenTab(GameObject tabToOpen)
    {
        // Turn them all off
        if (audioTab != null) audioTab.SetActive(false);
        if (videoTab != null) videoTab.SetActive(false);
        if (gameplayTab != null) gameplayTab.SetActive(false);

        // Turn the requested one on
        if (tabToOpen != null) tabToOpen.SetActive(true);
    }

    private void GoToMainMenu()
    {
        CloseSettings();

        // Ensure we clean up any floating UI/Popups from the run
        if (RunHUDManager.Instance != null) RunHUDManager.Instance.SlideOutAndHide(0f);

        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.MainMenuScene);
    }

    private void AbandonRun()
    {
        CloseSettings();

        if (RunManager.Instance != null)
        {
            RunManager.Instance.Stats.PlayerHealth = 0;
            SaveLoadManager.DeleteSave(); // Nuke the save file
        }

        SceneLoader.Instance.LoadScene(SceneLoader.GameScene.RunSummaryScene);
    }
}
