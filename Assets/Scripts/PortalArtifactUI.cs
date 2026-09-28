using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PortalArtifactUI : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image eventIcon;
    [SerializeField] private Button invisibleSelectButton;
    [SerializeField] private Button previewButton; 

    [Header("Portal Visuals")]
    [SerializeField] private RectTransform artifactRoot;
    [SerializeField] private RectTransform shadowTransform;
    [SerializeField] private Image energyRing;
    [SerializeField] private Image outerGlow;

    [Header("Animation Settings")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 15f;

    [Header("Glow Settings")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float idleGlowMin = 0.3f;
    [SerializeField] private float idleGlowMax = 0.6f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float volume = 1f;

    private BaseEventSO currentEvent;
    private Vector3 originalArtifactPos;
    private Vector3 originalShadowScale;
    private bool isHovered = false;
    private Color baseColor;
    private bool isTransitioning = false;
    private float currentLift = 0f;

    public void Initialize(BaseEventSO eventSO)
    {
        currentEvent = eventSO;
        eventIcon.sprite = eventSO.eventIcon;
        baseColor = eventSO.portalGlowColor;

        originalArtifactPos = artifactRoot.anchoredPosition;
        originalShadowScale = shadowTransform.localScale;

        invisibleSelectButton.onClick.RemoveAllListeners();
        invisibleSelectButton.onClick.AddListener(OnEventSelected);

        if (previewButton != null)
        {
            previewButton.onClick.RemoveAllListeners();

            if (eventSO is CombatEventSO combatEvent && combatEvent.encounter != null)
            {
                previewButton.gameObject.SetActive(true);
                previewButton.onClick.AddListener(() =>
                {
                    // Clean up the map tooltip before opening the preview overlay
                    MapController.Instance.HideEventInfo();
                    MapController.Instance.PreviewEncounter(combatEvent.encounter);
                });
            }
            else
            {
                previewButton.gameObject.SetActive(false);
            }
        }
    }

    public void SetElevation(float yOffset)
    {
        originalArtifactPos.y += yOffset;
        shadowTransform.anchoredPosition = new Vector2(
            shadowTransform.anchoredPosition.x,
            shadowTransform.anchoredPosition.y + yOffset
        );
    }

    private void Update()
    {
        if (isTransitioning) return;

        float targetLift = isHovered ? bobHeight : 0f;
        currentLift = Mathf.Lerp(currentLift, targetLift, Time.deltaTime * 10f);

        float hoverBob = Mathf.Sin(Time.time * bobSpeed) * (bobHeight * 0.3f) * (currentLift / bobHeight);

        artifactRoot.anchoredPosition = originalArtifactPos + new Vector3(0f, currentLift + hoverBob, 0f);

        if (shadowTransform != null)
        {
            float shadowScale = 1f - (currentLift / bobHeight * 0.3f);
            shadowTransform.localScale = originalShadowScale * shadowScale;
        }

        float breathingMath = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float targetGlow = isHovered ? 1f : Mathf.Lerp(idleGlowMin, idleGlowMax, breathingMath);

        if (energyRing != null)
        {
            Color ringColor = baseColor;
            ringColor.a = Mathf.Lerp(energyRing.color.a, targetGlow, Time.deltaTime * 10f);
            energyRing.color = ringColor;
        }

        if (outerGlow != null)
        {
            Color outerColor = baseColor;
            outerColor.a = Mathf.Lerp(outerGlow.color.a, targetGlow * 0.6f, Time.deltaTime * 10f);
            outerGlow.color = outerColor;

            float scaleTarget = isHovered ? 1.15f : 1.0f;
            outerGlow.rectTransform.localScale = Vector3.Lerp(outerGlow.rectTransform.localScale, Vector3.one * scaleTarget, Time.deltaTime * 10f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, volume * 0.5f);
        }
        isHovered = true;
        if (MapController.Instance == null) return;
        if (currentEvent == null) return;
        MapController.Instance.ShowEventInfo(currentEvent, transform.position);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickSound != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, volume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        MapController.Instance.HideEventInfo();
    }

    private void OnEventSelected()
    {
        if (currentEvent != null && !MapController.Instance.isTransitioning)
        {
            MapController.Instance.isTransitioning = true;
            isTransitioning = true;

            MapController.Instance.HideEventInfo();
            invisibleSelectButton.interactable = false;

            StartCoroutine(ZoomIntoVoidTransition());
        }
    }

    private System.Collections.IEnumerator ZoomIntoVoidTransition()
    {
        transform.SetAsLastSibling();

        float duration = 0.6f;
        float elapsed = 0f;

        Vector3 startPos = artifactRoot.position;
        Vector3 targetPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        Vector3 startScale = artifactRoot.localScale;
        Vector3 targetScale = Vector3.one * 50f;

        MapController.Instance.SetOverlayAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            artifactRoot.position = Vector3.Lerp(startPos, targetPos, t);
            artifactRoot.localScale = Vector3.Lerp(startScale, targetScale, t);

            MapController.Instance.SetOverlayAlpha(t);

            yield return null;
        }

        currentEvent.OnSelected();
    }

    private void OnDisable()
    {
        isHovered = false;
        currentLift = 0f;
    }

}