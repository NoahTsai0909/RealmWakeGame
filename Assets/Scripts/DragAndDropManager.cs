using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static RunManager;

public class DragAndDropManager : MonoBehaviour
{
    [SerializeField] private GridManager battleGrid;
    [SerializeField] private GridManager benchGrid;
    [SerializeField] private SellZone sellZone;
    [SerializeField] private ProvisionManager provisionManager;
    [SerializeField] private TacticBarManager playerTacticBar;

    private UnitInstance draggedUnit;
    private RunManager.UnitPlacement draggedPlacement;
    private GridManager sourceGrid;
    private Vector2Int sourcePos;

    private TacticInstance draggedTactic;
    private RunManager.TacticPlacement draggedTacticPlacement;

    private Camera mainCamera;
    private Mouse mouse;
    private bool wasMouseDown = false;
    private GridManager currentHoveredGrid;
    private Vector2Int currentHoveredCell = new Vector2Int(-1, -1);

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject consumeVFXPrefab; 
    [SerializeField] private GameObject receiveBuffVFXPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip consumeVFXSound;
    [SerializeField] private AudioClip dragUnitSound;
    [SerializeField] private AudioClip dropUnitSound;
    [SerializeField] private AudioClip sellSound;
    [SerializeField] private float volume = 1f;

    void Start()
    {
        mainCamera = Camera.main;
        mouse = Mouse.current;

        if (battleGrid == null || benchGrid == null)
            Debug.LogError("DragAndDropManager: Grid references not set!");
        if (provisionManager == null)
            provisionManager = FindFirstObjectByType<ProvisionManager>();
        if (battleGrid != null) battleGrid.RefreshAllAuras();
    }

    void Update()
    {
        if (mouse == null) return;

        bool isMouseDown = mouse.leftButton.isPressed;

        if (draggedUnit != null)
        {
            Vector3 worldPos = GetMouseWorldPosition();
            bool overSellZone = IsInSellZone(worldPos);

            if (sellZone != null)
                sellZone.Highlight(overSellZone);

            SpriteRenderer sr = draggedUnit.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = overSellZone ? Color.red : Color.white;
            }
        }

        if (isMouseDown && !wasMouseDown)
            TryStartDrag();

        if (!isMouseDown && wasMouseDown)
        {
            if (draggedUnit != null) StopDrag();
            else if (draggedTactic != null) StopDragTactic();
            else
            {
                //FAILSAFE: Always hide grids on release just in case
                if (battleGrid != null) battleGrid.HideGridVisuals();
                if (benchGrid != null) benchGrid.HideGridVisuals();
                currentHoveredGrid = null;
                currentHoveredCell = new Vector2Int(-1, -1);
            }
        }

        if (isMouseDown)
        {
            if (draggedUnit != null)
            {
                Vector3 mousePos = GetMouseWorldPosition();
                draggedUnit.transform.position = mousePos;
                GridManager targetGrid = GetClosestGrid(mousePos);
                Vector2Int targetPos = targetGrid.GetNearestGridPosition(mousePos);

                // Only trigger the animation if we moved to a new cell
                if (currentHoveredGrid != targetGrid || currentHoveredCell != targetPos)
                {
                    // If we swapped grids entirely, reset the old grid's hover state
                    if (currentHoveredGrid != null && currentHoveredGrid != targetGrid)
                    {
                        currentHoveredGrid.SetHoveredCell(-1, -1);
                    }

                    currentHoveredGrid = targetGrid;
                    currentHoveredCell = targetPos;
                    targetGrid.SetHoveredCell(targetPos.x, targetPos.y);
                }
            }
            else if (draggedTactic != null)
            {
                Vector3 mousePos = GetMouseWorldPosition();
                draggedTactic.transform.position = mousePos;
                int hoverIndex = playerTacticBar.GetInsertIndexFromPosition(mousePos);
                playerTacticBar.InsertTactic(hoverIndex, draggedTactic);
            }
        }

        wasMouseDown = isMouseDown;
    }


    void TryStartDrag()
    {

        Vector3 worldPos = GetMouseWorldPosition();
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        foreach (var hit in hits)
        {
            UnitInstance unit = hit.GetComponentInParent<UnitInstance>();
            if (unit != null && unit.myPlacement != null)
            {
                if (unit.inCombat) return;
                GridManager grid = GetUnitGrid(unit);
                if (grid != null)
                {
                    StartDrag(unit, grid);
                    return;
                }
            }

            TacticInstance tactic = hit.GetComponentInParent<TacticInstance>();
            if (tactic != null && tactic.myPlacement != null && playerTacticBar != null)
            {
                if (tactic.myBar == playerTacticBar)
                {
                    StartDragTactic(tactic);
                    return;
                }
            }
        }
    }


    void StartDragTactic(TacticInstance tactic)
    {
        draggedTactic = tactic;
        draggedTacticPlacement = tactic.myPlacement;

        tactic.isDragging = true;

        if (dragUnitSound != null)
        {
            AudioManager.Instance.PlaySFX(dragUnitSound, volume);
        }
        SetTacticDragVisuals(tactic, true);
    }

    void StopDragTactic()
    {
        draggedTactic.isDragging = false;

        playerTacticBar.UpdateVisualLayout();

        SyncTacticPlacements();

        SetTacticDragVisuals(draggedTactic, false);
        
        if (dropUnitSound != null)
        {
            AudioManager.Instance.PlaySFX(dropUnitSound, volume);
        }

        draggedTactic = null;
        draggedTacticPlacement = null;
        if (playerTacticBar != null) playerTacticBar.RefreshAllTacticAuras();
    }

    void SyncTacticPlacements()
    {
        var allTactics = playerTacticBar.GetAllTactics();
        RunManager.Instance.playerTactics.Clear();

        for (int i = 0; i < allTactics.Count; i++)
        {
            var placement = allTactics[i].myPlacement;
            placement.orderIndex = i; // Update its position
            RunManager.Instance.playerTactics.Add(placement);
        }
    }

    void SetTacticDragVisuals(TacticInstance tactic, bool isDragging)
    {
        Canvas canvas = tactic.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = isDragging ? 10000 : 0;
        }
        CanvasGroup cg = tactic.GetComponent<CanvasGroup>();
        if (cg == null) cg = tactic.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = isDragging ? 0.6f : 1f;
    }


    GridManager GetUnitGrid(UnitInstance unit)
    {
        if (battleGrid.GetUnitPosition(unit) != new Vector2Int(-1, -1)) return battleGrid;
        if (benchGrid.GetUnitPosition(unit) != new Vector2Int(-1, -1)) return benchGrid;
        return null;
    }

    void StartDrag(UnitInstance unit, GridManager grid)
    {
        draggedUnit = unit;
        draggedPlacement = unit.myPlacement;
        sourceGrid = grid;
        sourcePos = grid.GetUnitPosition(unit);

        sourceGrid.RemoveUnit(sourcePos.x, sourcePos.y, destroyVisual: false);
        SetUnitDragVisuals(unit, true);

        if (dragUnitSound != null)
        {
            AudioManager.Instance.PlaySFX(dragUnitSound, volume);
        }
        if (battleGrid != null) battleGrid.ShowGridVisuals();
        if (benchGrid != null) benchGrid.ShowGridVisuals();
    }

    void StopDrag()
    {
        Vector3 dropPos = GetMouseWorldPosition();

        if (IsInSellZone(dropPos))
        {
            SellUnit();
        }
        else
        {
            GridManager targetGrid = GetClosestGrid(dropPos);
            Vector2Int targetPos = targetGrid.GetNearestGridPosition(dropPos);
            UnitInstance targetUnit = targetGrid.GetUnitAtPosition(targetPos.x, targetPos.y);

            if (targetUnit != null && draggedUnit is IConsumable consumable)
            {
                if (targetUnit == draggedUnit)
                {
                    RevertDrag();
                    return;
                }

                bool consumeSuccessful = consumable.OnConsume(targetUnit);

                if (consumeSuccessful)
                {
                    if (consumeVFXPrefab != null)
                    {
                        Instantiate(consumeVFXPrefab, draggedUnit.transform.position, Quaternion.identity);
                    }

                    if (consumeVFXSound != null)
                    {
                        AudioManager.Instance.PlaySFX(consumeVFXSound, volume);
                    }

                    if (receiveBuffVFXPrefab != null)
                    {
                        Instantiate(receiveBuffVFXPrefab, targetUnit.transform.position, Quaternion.identity);
                    }

                    sourceGrid.ClearUnitReference(draggedPlacement);
                    RemoveFromRunManager(draggedUnit, draggedPlacement);
                    Destroy(draggedUnit.gameObject);
                    SetUnitDragVisuals(draggedUnit, false);
                    if (sellZone != null) sellZone.Highlight(false);

                    draggedUnit = null;
                    draggedPlacement = null;
                    sourceGrid = null;

                    if (provisionManager != null) provisionManager.CalculateCurrentProvision();

                    if (battleGrid != null) battleGrid.HideGridVisuals();
                    if (benchGrid != null) benchGrid.HideGridVisuals();
                    currentHoveredGrid = null;
                    currentHoveredCell = new Vector2Int(-1, -1);
                    if (UnitHoverDetector.Instance != null)
                    {
                        UnitHoverDetector.Instance.ForceInstantRecheck();
                    }
                }
                else
                {
                    RevertDrag();
                }
                return;
            }

            if (targetUnit == null)
            {
                if (targetGrid == battleGrid && sourceGrid == benchGrid)
                {
                    if (provisionManager != null && !provisionManager.CanAddUnitToBattleGrid(draggedUnit))
                    {
                        UniversalPopupManager.ShowPopup($"Provision exceeded provision cap!\nProvision cap: {RunManager.Instance.Stats.ProvisionCap}");
                        RevertDrag();
                        return;
                    }
                }
            }
            else
            {
                bool movingToBattle = targetGrid == battleGrid;
                bool movingToBench = targetGrid == benchGrid;
                bool fromBattle = sourceGrid == battleGrid;
                bool fromBench = sourceGrid == benchGrid;

                if (movingToBattle && fromBench)
                {
                    if (provisionManager != null && !provisionManager.CanSwapUnits(targetUnit, draggedUnit))
                    {
                        RevertDrag();
                        return;
                    }
                }
                else if (movingToBench && fromBattle)
                {
                    if (provisionManager != null && !provisionManager.CanSwapUnits(draggedUnit, targetUnit))
                    {
                        RevertDrag();
                        return;
                    }
                }
            }

            RunManager.UnitPlacement targetPlacement = targetUnit != null ? targetUnit.myPlacement : null;

            if (targetUnit != null)
            {
                sourceGrid.PlaceUnit(targetPlacement, sourcePos.x, sourcePos.y, targetUnit);
                targetPlacement.row = sourcePos.x;
                targetPlacement.col = sourcePos.y;
            }

            draggedPlacement.row = targetPos.x;
            draggedPlacement.col = targetPos.y;

            sourceGrid.ClearUnitReference(draggedPlacement);
            targetGrid.PlaceUnit(draggedPlacement, targetPos.x, targetPos.y, draggedUnit);
        }

        SetUnitDragVisuals(draggedUnit, false);

        if (sellZone != null) sellZone.Highlight(false);

        draggedUnit = null;
        draggedPlacement = null;
        sourceGrid = null;

        if (dropUnitSound != null)
        {
            AudioManager.Instance.PlaySFX(dropUnitSound, volume);
        }

        if (provisionManager != null) provisionManager.CalculateCurrentProvision();
        if (battleGrid != null) battleGrid.RefreshAllAuras();
        if (benchGrid != null) benchGrid.RefreshAllAuras();
        if (playerTacticBar != null) playerTacticBar.RefreshAllTacticAuras();
        if (battleGrid != null) battleGrid.HideGridVisuals();
        if (benchGrid != null) benchGrid.HideGridVisuals();

        currentHoveredGrid = null;
        currentHoveredCell = new Vector2Int(-1, -1);
    }

    void RevertDrag()
    {
        if (dropUnitSound != null)
        {
            AudioManager.Instance.PlaySFX(dropUnitSound, volume);
        }
        sourceGrid.PlaceUnit(draggedPlacement, sourcePos.x, sourcePos.y, draggedUnit);
        draggedPlacement.row = sourcePos.x;
        draggedPlacement.col = sourcePos.y;

        SetUnitDragVisuals(draggedUnit, false);

        draggedUnit = null;
        draggedPlacement = null;
        sourceGrid = null;

        if (battleGrid != null) battleGrid.HideGridVisuals();
        if (benchGrid != null) benchGrid.HideGridVisuals();

        currentHoveredGrid = null;
        currentHoveredCell = new Vector2Int(-1, -1);
    }

    GridManager GetClosestGrid(Vector3 worldPos)
    {
        float distToBattle = Vector2.Distance(worldPos, battleGrid.transform.position);
        float distToBench = Vector2.Distance(worldPos, benchGrid.transform.position);

        float battleDist = Mathf.Min(distToBattle, battleGrid.DistanceToNearestEmptyCell(worldPos));
        float benchDist = Mathf.Min(distToBench, benchGrid.DistanceToNearestEmptyCell(worldPos));

        return (battleDist < benchDist) ? battleGrid : benchGrid;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 pos = mouse.position.ReadValue();
        pos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(pos);
    }

    void SetUnitDragVisuals(UnitInstance unit, bool isDragging)
    {
        SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = isDragging ? 0.6f : 1f;
            sr.color = c;
            sr.sortingOrder = isDragging ? 100 : 0;
        }
    }

    bool IsInSellZone(Vector3 worldPos)
    {
        if (sellZone == null) return false;
        Collider2D sellCollider = sellZone.GetComponent<Collider2D>();
        return sellCollider != null && sellCollider.OverlapPoint(worldPos);
    }

    void SellUnit()
    {
        if (draggedUnit == null || draggedPlacement == null) return;

        int sellPrice = draggedUnit.Stats.Value;
        RunManager.Instance.Stats.CurrentGold += sellPrice;

        if (sourceGrid != null) sourceGrid.RemoveUnit(sourcePos.x, sourcePos.y, destroyVisual: true);
        RemoveFromRunManager(draggedUnit, draggedPlacement);
        Destroy(draggedUnit.gameObject);

        if (battleGrid != null) battleGrid.RefreshAllAuras();
        if (benchGrid != null) benchGrid.RefreshAllAuras();
        if (playerTacticBar != null) playerTacticBar.RefreshAllTacticAuras();

        if (sellSound != null)
        {
            AudioManager.Instance.PlaySFX(sellSound, volume);
        }
    }

    void RemoveFromRunManager(UnitInstance unit, UnitPlacement placement)
    {
        if (RunManager.Instance == null) return;
        bool isInBattleGrid = sourceGrid == battleGrid;

        if (isInBattleGrid)
        {
            RunManager.Instance.playerTeamPlacements.RemoveAll(p => p.unitData == placement.unitData);
        }
        else
        {
            foreach (var benchPlacement in RunManager.Instance.playerBenchPlacements)
            {
                if (benchPlacement.unitData == placement.unitData)
                {
                    benchPlacement.unitData = null;
                    benchPlacement.row = -1;
                    benchPlacement.col = -1;
                    break;
                }
            }
        }
    }
}