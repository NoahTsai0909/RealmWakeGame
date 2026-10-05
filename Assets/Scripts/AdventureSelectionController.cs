using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Required for DOTween
using static SceneLoader;

public class AdventureSelectionController : MonoBehaviour
{
    [Header("Adventures")]
    public List<AdventureDefinitionSO> availableAdventures;

    [Header("Carousel Cards")]
    public AdventureCardUI leftCard;
    public AdventureCardUI centerCard;
    public AdventureCardUI rightCard;

    [Header("UI Controls")]
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button startRunButton;

    [Header("Fixed Text UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Region Selectors")]
    public TextMeshProUGUI selectedRegionText;
    public Button solmireButton;
    public Button nethervaleButton;
    public Button everbornButton;
    public Button axiomButton;

    private int currentIndex = 0;
    private Region selectedRegion = Region.Solmire;

    void Start()
    {
        leftArrowButton.onClick.AddListener(ScrollLeft);
        rightArrowButton.onClick.AddListener(ScrollRight);
        startRunButton.onClick.AddListener(StartRun);

        solmireButton.onClick.AddListener(() => SetRegion(Region.Solmire));
        nethervaleButton.onClick.AddListener(() => SetRegion(Region.Nethervale));
        everbornButton.onClick.AddListener(() => SetRegion(Region.Everborn));
        axiomButton.onClick.AddListener(() => SetRegion(Region.Axiom));

        leftArrowButton.transform.DOMoveY(leftArrowButton.transform.position.y + 10f, 1f)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        rightArrowButton.transform.DOMoveY(rightArrowButton.transform.position.y + 10f, 1f)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        UpdateCarousel();
        SetRegion(Region.Solmire);
    }

    private void ScrollLeft()
    {
        AnimateCarouselBump(Vector3.right);

        currentIndex--;
        if (currentIndex < 0) currentIndex = availableAdventures.Count - 1;
        UpdateCarousel();
    }

    private void ScrollRight()
    {
        AnimateCarouselBump(Vector3.left);

        currentIndex = (currentIndex + 1) % availableAdventures.Count;
        UpdateCarousel();
    }

    private void AnimateCarouselBump(Vector3 punchDirection)
    {
        centerCard.transform.DOKill(true);
        leftCard.transform.DOKill(true);
        rightCard.transform.DOKill(true);

        float punchStrength = 40f;
        centerCard.transform.DOPunchPosition(punchDirection * punchStrength, 0.3f, 0, 1);
        leftCard.transform.DOPunchPosition(punchDirection * punchStrength, 0.3f, 0, 1);
        rightCard.transform.DOPunchPosition(punchDirection * punchStrength, 0.3f, 0, 1);
    }

    private void UpdateCarousel()
    {
        if (availableAdventures.Count == 0) return;

        int leftIndex = currentIndex - 1;
        if (leftIndex < 0) leftIndex = availableAdventures.Count - 1;
        int rightIndex = (currentIndex + 1) % availableAdventures.Count;

        centerCard.Setup(availableAdventures[currentIndex]);
        leftCard.Setup(availableAdventures[leftIndex]);
        rightCard.Setup(availableAdventures[rightIndex]);

        AdventureDefinitionSO selectedAdventure = availableAdventures[currentIndex];
        titleText.text = selectedAdventure.adventureName;
        descriptionText.text = selectedAdventure.description;

        startRunButton.interactable = true;
    }

    private void SetRegion(Region region)
    {
        if (region != Region.Solmire)
        {
            UniversalPopupManager.ShowPopup($"Region {region} is locked for the purposes of this demo! Please look forward to the future release!");
            return;
        }
        selectedRegion = region;
        if (selectedRegionText != null)
        {
            selectedRegionText.text = $"Selected Region: {selectedRegion}";
        }
    }

    private void StartRun()
    {
        AdventureDefinitionSO selectedAdventure = availableAdventures[currentIndex];
        RunManager.Instance.SetupNewAdventure(selectedAdventure, selectedRegion);
        SceneLoader.Instance.LoadScene(GameScene.MapScene);
    }
}