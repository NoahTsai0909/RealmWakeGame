using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UnitHoverDetector : MonoBehaviour
{
    [SerializeField] private UnitHoverUI hoverUIPrefab;
    [SerializeField] private float hoverDelay = 0.35f;

    private Camera mainCamera;
    private Mouse mouse;

    private UnitInstance currentHoveredUnit;
    private UnitInstance pendingHoverUnit;
    private Coroutine hoverRoutine;

    private UnitHoverUI hoverUIInstance;
    private CanvasGroup hoverUICanvasGroup;

    private bool isPinned = false;
    private bool isUIHoverDriven = false;
    public static UnitHoverDetector Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        mouse = Mouse.current;

        GameObject tooltipCanvasObj = new GameObject("UnitHoverCanvas");
        Canvas tooltipCanvas = tooltipCanvasObj.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 29997;

        UnityEngine.UI.CanvasScaler scaler = tooltipCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        tooltipCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        hoverUIInstance = Instantiate(hoverUIPrefab, tooltipCanvas.transform);
        hoverUIInstance.gameObject.SetActive(false);
        hoverUIInstance.name = "UnitUI (Dynamic)";

        hoverUICanvasGroup = hoverUIInstance.GetComponent<CanvasGroup>();
        if (hoverUICanvasGroup == null)
        {
            hoverUICanvasGroup = hoverUIInstance.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private bool IsPointerOverUnitHoverUI()
    {
        if (EventSystem.current == null || hoverUIInstance == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = mouse.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.transform.IsChildOf(hoverUIInstance.transform))
            {
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        if (mouse == null || Keyboard.current == null) return;

        if (isPinned)
        {
            bool leftClickOutside = mouse.leftButton.wasPressedThisFrame && !IsPointerOverUnitHoverUI();

            if (mouse.rightButton.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame ||
                leftClickOutside)
            {
                isPinned = false;
                if (hoverUICanvasGroup != null) hoverUICanvasGroup.blocksRaycasts = false;
                CancelHover();
            }
            return;
        }
        if (currentHoveredUnit != null && mouse.rightButton.wasPressedThisFrame)
        {
            isPinned = true;
            if (hoverUICanvasGroup != null) hoverUICanvasGroup.blocksRaycasts = true;
            return;
        }

        if (isUIHoverDriven) return;

        if (mouse.leftButton.isPressed)
        {
            CancelHover();
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
        UnitInstance hitUnit = hit ? hit.GetComponent<UnitInstance>() : null;

        if (hitUnit == null)
        {
            CancelHover();
            return;
        }

        if (hitUnit == currentHoveredUnit || hitUnit == pendingHoverUnit) return;

        StartHover(hitUnit);
    }

    void StartHover(UnitInstance unit)
    {
        CancelHover();
        if (hoverUICanvasGroup != null) hoverUICanvasGroup.blocksRaycasts = false;
        pendingHoverUnit = unit;
        hoverRoutine = StartCoroutine(HoverDelayRoutine(unit));
    }

    IEnumerator HoverDelayRoutine(UnitInstance unit)
    {
        yield return new WaitForSeconds(hoverDelay);
        if (pendingHoverUnit == unit)
        {
            currentHoveredUnit = unit;
            pendingHoverUnit = null;
            hoverUIInstance.Show(unit);
        }
    }

    void CancelHover()
    {
        if (isPinned) return;
        isUIHoverDriven = false;

        if (hoverRoutine != null) StopCoroutine(hoverRoutine);
        hoverRoutine = null;
        pendingHoverUnit = null;

        if (currentHoveredUnit != null)
        {
            hoverUIInstance.Hide();
            currentHoveredUnit = null;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = mouse.position.ReadValue();
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void OnDestroy()
    {
        if (hoverUIInstance != null) Destroy(hoverUIInstance.gameObject);
    }

    public void ShowTooltipFromUI(UnitInstance unit, RectTransform uiAnchor = null)
    {
        if (isPinned) return;
        CancelHover();
        currentHoveredUnit = unit;
        isUIHoverDriven = true;
        if (hoverUICanvasGroup != null) hoverUICanvasGroup.blocksRaycasts = false;

        hoverUIInstance.Show(unit, uiAnchor);
    }

    public void HideTooltipFromUI()
    {
        isUIHoverDriven = false;
        if (!isPinned) CancelHover();
    }

    public void ForceInstantRecheck()
    {
        CancelHover();

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
        UnitInstance hitUnit = hit ? hit.GetComponent<UnitInstance>() : null;

        if (hitUnit != null)
        {
            currentHoveredUnit = hitUnit;
            hoverUIInstance.Show(hitUnit);
        }
    }
}