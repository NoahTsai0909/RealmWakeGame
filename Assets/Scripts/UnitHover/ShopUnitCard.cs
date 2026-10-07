using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUnitCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("UI References")]
    [Tooltip("The native UI Image that will display the unit's sprite")]
    [SerializeField] private Image unitPortrait;
    [SerializeField] private TextMeshProUGUI priceText;

    [Header("Hover Glow Settings")]
    [SerializeField] private CanvasGroup hoverGlowGroup;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.8f;

    private UnitInstance assignedUnit;
    private TacticInstance assignedTactic;
    public bool isTactic { get; private set; }

    private UnityAction onBuyClicked;
    private bool isHovered = false;

    private int myPrice;
    private GameObject dragPhantom;
    private bool isPurchased = false;
    private bool wasDragged = false;

    private UnitInstance myDummyUnit;
    private RectTransform myRect;

    public void Initialize(UnitInstance dummyUnit, int price, UnityAction onBuyClicked)
    {
        myDummyUnit = dummyUnit;
        myRect = GetComponent<RectTransform>();
        assignedUnit = dummyUnit;

        // Restore the breathing and rarity outline animations
        if (assignedUnit != null && unitPortrait != null)
        {
            UIUnitVisualController visualController = unitPortrait.GetComponent<UIUnitVisualController>();
            if (visualController != null)
            {
                visualController.InitializeVisuals(assignedUnit.Definition, assignedUnit.CurrentRarity);
                if (assignedUnit.currentPrefix != null)
                {
                    visualController.ApplyMutationVisuals(assignedUnit.currentPrefix);
                }
            }
        }

        priceText.text = $"{TextIconUtility.FormatGold(price)}";
        this.onBuyClicked = onBuyClicked;

        if (hoverGlowGroup != null)
        {
            hoverGlowGroup.alpha = 0f;
        }
    }

    public void InitializeTactic(TacticInstance dummyTactic, int price, UnityAction onBuyClicked)
    {
        assignedTactic = dummyTactic;
        assignedUnit = null;
        isTactic = true;

        myPrice = price;
        isPurchased = false;
        wasDragged = false;
        this.onBuyClicked = onBuyClicked;

        if (assignedTactic != null && unitPortrait != null)
        {
            unitPortrait.sprite = assignedTactic.Definition.tacticSprite;
            unitPortrait.color = Color.white;

            UIUnitVisualController visualController = unitPortrait.GetComponent<UIUnitVisualController>();
            if (visualController != null) visualController.enabled = false;
        }

        priceText.text = $"{TextIconUtility.FormatGold(price)}";
        if (hoverGlowGroup != null) hoverGlowGroup.alpha = 0f;
    }

    void Update()
    {
        if (isHovered && hoverGlowGroup != null)
        {
            float sineWave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            hoverGlowGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, sineWave);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPurchased || dragPhantom != null) return;

        isHovered = true;
        if (hoverGlowGroup != null)
        {
            hoverGlowGroup.gameObject.SetActive(true);
        }

        if (isTactic && assignedTactic != null)
        {
            if (TacticHoverDetector.Instance != null)
            {
                Vector3[] corners = new Vector3[4];
                GetComponent<RectTransform>().GetWorldCorners(corners);

                Vector3 leftCenterWorld = (corners[0] + corners[1]) / 2f;

                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                if (cam == null) cam = Camera.main;

                Vector2 leftCenterScreen = RectTransformUtility.WorldToScreenPoint(cam, leftCenterWorld);

                Vector2 fixedTooltipPos = new Vector2(leftCenterScreen.x - 20f, leftCenterScreen.y);

                TacticHoverDetector.Instance.ShowTooltipFromUI(
                    assignedTactic.Definition.tacticName,
                    assignedTactic.GetDescription(),
                    assignedTactic.GetCooldown(),
                    fixedTooltipPos
                );
            }
        }
        else if (!isTactic && myDummyUnit != null)
        {
            if (UnitHoverDetector.Instance != null) UnitHoverDetector.Instance.ShowTooltipFromUI(myDummyUnit, myRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (hoverGlowGroup != null)
        {
            hoverGlowGroup.alpha = 0f;
            hoverGlowGroup.gameObject.SetActive(false);
        }

        if (isTactic)
        {
            if (TacticHoverDetector.Instance != null) TacticHoverDetector.Instance.HideTooltipFromUI();
        }
        else
        {
            if (UnitHoverDetector.Instance != null) UnitHoverDetector.Instance.HideTooltipFromUI();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (wasDragged) return;

        if (!isPurchased) onBuyClicked?.Invoke();
    }

    public void MarkAsPurchased()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (isPurchased || (assignedUnit == null && assignedTactic == null)) return;

        if (RunManager.Instance.Stats.CurrentGold < myPrice) return;

        wasDragged = true;

        if (isTactic)
        {
            if (TacticHoverDetector.Instance != null) TacticHoverDetector.Instance.HideTooltipFromUI();
        }
        else
        {
            if (UnitHoverDetector.Instance != null) UnitHoverDetector.Instance.HideTooltipFromUI();
        }

        Canvas currentCanvas = GetComponentInParent<Canvas>();
        if (currentCanvas == null) return;

        dragPhantom = new GameObject("DragPhantom");
        dragPhantom.transform.SetParent(currentCanvas.transform, false);
        dragPhantom.transform.SetAsLastSibling();

        Image phantomImage = dragPhantom.AddComponent<Image>();

        if (unitPortrait != null)
        {
            phantomImage.sprite = unitPortrait.sprite;
            unitPortrait.color = new Color(1f, 1f, 1f, 0f);
        }

        phantomImage.SetNativeSize();
        phantomImage.raycastTarget = false;
        dragPhantom.transform.localScale = Vector3.one * 1.5f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPhantom != null)
        {
            Canvas currentCanvas = GetComponentInParent<Canvas>();
            if (currentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)currentCanvas.transform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint
                );
                dragPhantom.transform.localPosition = localPoint;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (unitPortrait != null)
        {
            unitPortrait.color = Color.white;
        }

        if (dragPhantom != null)
        {
            Destroy(dragPhantom);

            if (eventData.position.x < Screen.width * 0.6f)
            {
                onBuyClicked?.Invoke();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasDragged = false;
    }
}