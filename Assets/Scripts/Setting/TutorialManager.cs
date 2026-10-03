using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public struct TutorialStep
{
    public string key;
    public string title;
    [TextArea(2, 5)] public string description;
    public Transform highlightTarget;
    public Sprite instructionalGraphic;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Image instructionalImageDisplay;

    [Header("Visual Pointers")]
    [SerializeField] private RectTransform pointerArrow;

    private Queue<TutorialStep> stepQueue = new Queue<TutorialStep>();
    private Transform currentTarget;

    private Canvas existingCanvas = null;
    private bool dynamicallyAddedCanvas = false;
    private bool originalOverrideSorting = false;
    private int originalSortingOrder = 0;
    private Tween arrowTween;

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

        if (tutorialPanel != null)
        {
            Image bgImage = tutorialPanel.GetComponent<Image>();
            if (bgImage != null) bgImage.sprite = null;
        }

        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
    }

    public void TryShowTutorial(string tutorialKey, string title, string description, Transform highlightTarget = null, Sprite graphic = null)
    {
        StartTutorialSequence(new List<TutorialStep>
        {
            new TutorialStep { key = tutorialKey, title = title, description = description, highlightTarget = highlightTarget, instructionalGraphic = graphic }
        });
    }

    public void StartTutorialSequence(List<TutorialStep> steps)
    {
        stepQueue.Clear();
        bool addedAny = false;

        foreach (var step in steps)
        {
            if (PlayerPrefs.GetInt("Tutorial_" + step.key, 0) == 0)
            {
                stepQueue.Enqueue(step);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            if (tutorialPanel != null)
            {
                RectTransform rt = tutorialPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                }
            }

            tutorialPanel.SetActive(true);
            StartCoroutine(StartSequenceRoutine());
        }
    }

    private IEnumerator StartSequenceRoutine()
    {
        yield return new WaitForEndOfFrame();
        Time.timeScale = 0f;
        ShowNextStep();
    }

    private void ShowNextStep()
    {
        CleanupSpotlight();

        if (stepQueue.Count == 0)
        {
            EndSequence();
            return;
        }

        TutorialStep currentStep = stepQueue.Dequeue();

        titleText.text = currentStep.title;
        descriptionText.SetText(TextIconUtility.ParseDescription(currentStep.description));
        currentTarget = currentStep.highlightTarget;

        arrowTween?.Kill();

        if(currentStep.instructionalGraphic != null)
        {
            if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);

            if (instructionalImageDisplay != null)
            {
                instructionalImageDisplay.sprite = currentStep.instructionalGraphic;

                instructionalImageDisplay.preserveAspect = true;
                instructionalImageDisplay.gameObject.SetActive(true);

                RectTransform imgRect = instructionalImageDisplay.rectTransform;
                imgRect.anchorMin = Vector2.zero;
                imgRect.anchorMax = Vector2.one;
                imgRect.offsetMin = new Vector2(50f, 300f);
                imgRect.offsetMax = new Vector2(-50f, -100f);
            }

            if (titleText != null && descriptionText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                RectTransform descRect = descriptionText.rectTransform;
                titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0, -50f);
                descRect.anchorMin = new Vector2(0, 0); descRect.anchorMax = new Vector2(1, 0);
                descRect.pivot = new Vector2(0.5f, 1f);

                descRect.anchoredPosition = new Vector2(0, 260f);
            }
        }
        else
        {
            if (instructionalImageDisplay != null) instructionalImageDisplay.gameObject.SetActive(false);

            if (currentTarget != null && pointerArrow != null)
            {
                pointerArrow.gameObject.SetActive(true);

                Vector2 targetScreenPos;
                bool isUpperHalf;

                Canvas.ForceUpdateCanvases();
                RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();

                if (currentTarget is RectTransform rectTransform)
                {
                    Canvas rootCanvas = rectTransform.GetComponentInParent<Canvas>();
                    Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;
                    if (cam == null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

                    Vector3[] corners = new Vector3[4];
                    rectTransform.GetWorldCorners(corners);

                    Vector3 centerWorld = (corners[0] + corners[2]) / 2f;
                    Vector2 centerScreen = (cam == null) ? new Vector2(centerWorld.x, centerWorld.y) : RectTransformUtility.WorldToScreenPoint(cam, centerWorld);

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, centerScreen, null, out Vector2 localCenter);
                    isUpperHalf = localCenter.y > 0f;

                    if (isUpperHalf)
                    {
                        Vector3 bottomEdge = (corners[0] + corners[3]) / 2f;
                        targetScreenPos = (cam == null) ? new Vector2(bottomEdge.x, bottomEdge.y) : RectTransformUtility.WorldToScreenPoint(cam, bottomEdge);
                    }
                    else
                    {
                        Vector3 topEdge = (corners[1] + corners[2]) / 2f;
                        targetScreenPos = (cam == null) ? new Vector2(topEdge.x, topEdge.y) : RectTransformUtility.WorldToScreenPoint(cam, topEdge);
                    }

                    existingCanvas = currentTarget.GetComponent<Canvas>();
                    if (existingCanvas == null)
                    {
                        existingCanvas = currentTarget.gameObject.AddComponent<Canvas>();
                        currentTarget.gameObject.AddComponent<GraphicRaycaster>();
                        dynamicallyAddedCanvas = true;
                    }
                    else
                    {
                        dynamicallyAddedCanvas = false;
                    }

                    originalOverrideSorting = existingCanvas.overrideSorting;
                    originalSortingOrder = existingCanvas.sortingOrder;
                    existingCanvas.overrideSorting = true;
                    existingCanvas.sortingOrder = 30005;
                }
                else
                {
                    targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, targetScreenPos, null, out Vector2 localCenter);
                    isUpperHalf = localCenter.y > 0f;
                }

                if (titleText != null && descriptionText != null)
                {
                    RectTransform titleRect = titleText.rectTransform;
                    RectTransform descRect = descriptionText.rectTransform;

                    if (isUpperHalf)
                    {
                        titleRect.anchorMin = new Vector2(0, 0); titleRect.anchorMax = new Vector2(1, 0);
                        descRect.anchorMin = new Vector2(0, 0); descRect.anchorMax = new Vector2(1, 0);

                        titleRect.pivot = new Vector2(0.5f, 0f);
                        titleRect.anchoredPosition = new Vector2(0, 300f);

                        descRect.pivot = new Vector2(0.5f, 1f);
                        descRect.anchoredPosition = new Vector2(0, 260f);
                    }
                    else
                    {
                        titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1);
                        descRect.anchorMin = new Vector2(0, 1); descRect.anchorMax = new Vector2(1, 1);

                        titleRect.pivot = new Vector2(0.5f, 0f);
                        titleRect.anchoredPosition = new Vector2(0, -100f);

                        descRect.pivot = new Vector2(0.5f, 1f);
                        descRect.anchoredPosition = new Vector2(0, -140f);
                    }
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, targetScreenPos, null, out Vector2 localArrowPos);
                float offsetMagnitude = (currentTarget is RectTransform) ? 75f : 150f;

                if (isUpperHalf)
                {
                    pointerArrow.anchoredPosition = localArrowPos + new Vector2(0, -offsetMagnitude);
                    pointerArrow.localEulerAngles = new Vector3(0, 0, 180f);
                    arrowTween = pointerArrow.DOAnchorPosY(pointerArrow.anchoredPosition.y - 15f, 0.5f)
                        .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                }
                else
                {
                    pointerArrow.anchoredPosition = localArrowPos + new Vector2(0, offsetMagnitude);
                    pointerArrow.localEulerAngles = Vector3.zero;
                    arrowTween = pointerArrow.DOAnchorPosY(pointerArrow.anchoredPosition.y + 15f, 0.5f)
                        .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
                }
            }
            else
            {
                if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);

                if (titleText != null && descriptionText != null)
                {
                    RectTransform titleRect = titleText.rectTransform;
                    RectTransform descRect = descriptionText.rectTransform;

                    titleRect.anchorMin = new Vector2(0, 0.5f); titleRect.anchorMax = new Vector2(1, 0.5f);
                    descRect.anchorMin = new Vector2(0, 0.5f); descRect.anchorMax = new Vector2(1, 0.5f);

                    titleRect.pivot = new Vector2(0.5f, 0f);
                    titleRect.anchoredPosition = new Vector2(0, 20f);

                    descRect.pivot = new Vector2(0.5f, 1f);
                    descRect.anchoredPosition = new Vector2(0, -20f);
                }
            }
        }

        PlayerPrefs.SetInt("Tutorial_" + currentStep.key, 1);
        PlayerPrefs.Save();
    }

    private void CleanupSpotlight()
    {
        if (existingCanvas != null && currentTarget != null)
        {
            if (dynamicallyAddedCanvas)
            {
                GraphicRaycaster gr = currentTarget.GetComponent<GraphicRaycaster>();
                if (gr != null) DestroyImmediate(gr);
                DestroyImmediate(existingCanvas);

                if (currentTarget.parent != null && currentTarget.parent is RectTransform rectParent)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectParent);
                }
            }
            else
            {
                existingCanvas.overrideSorting = originalOverrideSorting;
                existingCanvas.sortingOrder = originalSortingOrder;
            }
            existingCanvas = null;
        }
    }

    private void OnContinueClicked()
    {
        ShowNextStep();
    }

    private void EndSequence()
    {
        arrowTween?.Kill();
        CleanupSpotlight();
        currentTarget = null;
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ResetAllTutorials()
    {
        //int analyticsConsent = PlayerPrefs.GetInt("Analytics_Consent_Given", 0);
        PlayerPrefs.DeleteAll();
        //PlayerPrefs.SetInt("Analytics_Consent_Given", analyticsConsent);

        PlayerPrefs.Save();
        Debug.Log("Tutorials reset. Analytics consent preserved.");
    }
}