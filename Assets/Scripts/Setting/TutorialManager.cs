using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public struct TutorialStep
{
    public string key;
    public string title;
    [TextArea(2, 5)] public string description;
    public Transform highlightTarget;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button continueButton;

    [Header("Visual Pointers")]
    [SerializeField] private RectTransform pointerArrow;

    private Queue<TutorialStep> stepQueue = new Queue<TutorialStep>();
    private Transform currentTarget;

    private Canvas existingCanvas = null;
    private bool dynamicallyAddedCanvas = false;
    private bool originalOverrideSorting = false;
    private int originalSortingOrder = 0;

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

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    public void TryShowTutorial(string tutorialKey, string title, string description, Transform highlightTarget = null)
    {
        StartTutorialSequence(new List<TutorialStep>
        {
            new TutorialStep { key = tutorialKey, title = title, description = description, highlightTarget = highlightTarget }
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

        if (currentTarget != null && pointerArrow != null)
        {
            pointerArrow.gameObject.SetActive(true);

            Vector2 targetScreenPos;
            bool isUpperHalf;

            if (currentTarget is RectTransform rectTransform)
            {
                Canvas rootCanvas = rectTransform.GetComponentInParent<Canvas>();
                Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;
                if (cam == null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[2]) / 2f);
                isUpperHalf = centerScreen.y > Screen.height / 2f;

                if (isUpperHalf)
                {
                    targetScreenPos = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[3]) / 2f);
                }
                else
                {
                    targetScreenPos = RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) / 2f);
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
                isUpperHalf = targetScreenPos.y > Screen.height / 2f;
            }
            RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, targetScreenPos, null, out Vector2 localArrowPos);

            float offsetMagnitude = (currentTarget is RectTransform) ? 75f : 150f;

            if (isUpperHalf)
            {
                pointerArrow.anchoredPosition = localArrowPos + new Vector2(0, -offsetMagnitude);
                pointerArrow.localEulerAngles = new Vector3(0, 0, 180f); 
            }
            else
            {
                pointerArrow.anchoredPosition = localArrowPos + new Vector2(0, offsetMagnitude);
                pointerArrow.localEulerAngles = Vector3.zero; 
            }
        }
        else
        {
            if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);
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
                Destroy(currentTarget.GetComponent<GraphicRaycaster>());
                Destroy(existingCanvas);
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
        CleanupSpotlight();
        currentTarget = null;
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ResetAllTutorials()
    {
        PlayerPrefs.DeleteAll();
    }
}