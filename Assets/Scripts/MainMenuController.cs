using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SceneLoader;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button compendiumButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject compendiumPanel;
    [SerializeField] private GameObject LeftSideBar;
    [SerializeField] private GameObject FilterSideBar;
    [SerializeField] private Button filterButton;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button closeCompendiumButton;
    [SerializeField] private Button discordLinkButton;
    [SerializeField] private AudioClip mainMenuMusic;
    
    private bool isFilterSidebarOpen = false;
    void Start()
    {
        RefreshMenuState();
        Application.runInBackground = true; // Prevents pausing when tabbed out
        compendiumButton.onClick.AddListener(() => ShowCompendium());
        settingsButton.onClick.AddListener(() => {if (SettingsManager.Instance != null){SettingsManager.Instance.OpenSettings();}});
        filterButton.onClick.AddListener(() => ToggleFilterSideBar());
        discordLinkButton.onClick.AddListener(() => Application.OpenURL("https://discord.gg/jean7D3rY"));
        quitButton.onClick.AddListener(() => QuitGame());
        RunHUDManager.Instance?.Hide();
    }

    private void RefreshMenuState()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.PlayMusicWithFade(mainMenuMusic);
        if (SaveLoadManager.HasSaveFile())
        {
            playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
            abandonButton.gameObject.SetActive(true);

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                bool loadSuccess = SaveLoadManager.LoadRun();

                if (loadSuccess)
                {
                    SceneLoader.Instance.LoadScene(GameScene.MapScene);
                }
                else
                {
                    Debug.LogError("Failed to load save file! Check the console for errors.");
                }
            });

            abandonButton.onClick.RemoveAllListeners();
            abandonButton.onClick.AddListener(() =>
            {
                SaveLoadManager.DeleteSave();
                RunManager.Instance.ResetRun();
                RefreshMenuState();
            });
        }
        else
        {
            playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
            abandonButton.gameObject.SetActive(false);

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                SceneLoader.Instance.LoadScene(GameScene.AdventureSelectionScene);
            });
        }
    }


    void ShowCompendium()
    {
        mainMenuPanel.SetActive(false);
        compendiumPanel.SetActive(true);
        closeCompendiumButton.onClick.AddListener(() => CloseCompendium());
    }

    void CloseCompendium()
    {
        compendiumPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void ToggleFilterSideBar()
    {
        isFilterSidebarOpen = !isFilterSidebarOpen;
        FilterSideBar.SetActive(isFilterSidebarOpen);
        LeftSideBar.SetActive(!isFilterSidebarOpen);
    }

    public void QuitGame()
    {
        
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
