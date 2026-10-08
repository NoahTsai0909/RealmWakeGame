using System.Collections.Generic;
using UnityEngine;

public class TacticBarManager : MonoBehaviour
{
    public enum BarAlignment { Left, Center, Right }
    public BarAlignment alignment = BarAlignment.Left; // Player will be Left, Enemy will be Right

    [Header("Spacing & Bounds")]
    [Tooltip("The standard distance between tactics")]
    public float defaultSpacing = 1f;
    [Tooltip("The max width the bar is allowed to take up before squishing tactics")]
    public float maxBarWidth = 20f;

    [Tooltip("Where should the tactics start spawning? (Drag an empty GameObject here)")]
    public Transform trackAnchor;
    [Tooltip("Force tactics to this size to fit the bar")]
    public Vector3 tacticScale = Vector3.one;

    [Header("Visuals")]
    public Vector2 tacticVisualOffset = new Vector2(0f, 0.5f);
    public bool useVerticalStagger = true;
    public float verticalStaggerAmount = -1f;

    [Header("Enemy Bar Settings")]
    public bool isEnemyBar = false;
    public RectTransform barBackgroundRect; 
    public GameObject tacticsTextObj;       

    private List<TacticInstance> activeTactics = new List<TacticInstance>();
    public bool isCombatRunning = false;

    void Start()
    {
        if (isEnemyBar)
        {
            alignment = BarAlignment.Right;
            if (barBackgroundRect != null)
                barBackgroundRect.localScale = new Vector3(-1, 1, 1); 
            if (tacticsTextObj != null)
                tacticsTextObj.SetActive(false); 

            if (trackAnchor != null)
            {
                Vector3 mirroredPos = trackAnchor.localPosition;
                mirroredPos.x = -mirroredPos.x; 
                trackAnchor.localPosition = mirroredPos;
            }
        }
    }

    void Update()
    {
        if (!isCombatRunning) return;

        TacticInstance currentActive = GetFirstReadyActiveTactic();
        if (currentActive != null)
        {
            if (currentActive.TickCooldown(Time.deltaTime))
            {
                currentActive.ExecuteActiveEffect();
                currentActive.MarkAsSpent();
            }
        }
    }

    private TacticInstance GetFirstReadyActiveTactic()
    {
        foreach (var tactic in activeTactics)
        {
            if (tactic != null && !tactic.isPassive && !tactic.isSpent)
                return tactic;
        }
        return null;
    }

    public void StartCombat(bool isPlayerBar)
    {
        isCombatRunning = true;
        foreach (var tactic in activeTactics)
        {
            if (tactic != null)
            {
                tactic.SetupTargeting(isPlayerBar);
                tactic.EnterCombat();
                tactic.CombatStartEffect();
            }
        }
    }

    public void StopCombat()
    {
        isCombatRunning = false;
        foreach (var tactic in activeTactics)
        {
            if (tactic != null)
            {
                tactic.inCombat = false;
                if (tactic.isPassive) tactic.RemovePassiveEffect();
            }
        }
    }

    public void AddTactic(TacticInstance tactic)
    {
        if (tactic == null) return;
        if (!activeTactics.Contains(tactic))
        {
            activeTactics.Add(tactic);
            tactic.myBar = this;
            tactic.SetupTargeting(alignment == BarAlignment.Left);
            UpdateVisualLayout();
        }
    }

    public void InsertTactic(int index, TacticInstance tactic)
    {
        if (tactic == null) return;
        if (activeTactics.Contains(tactic)) activeTactics.Remove(tactic);
        index = Mathf.Clamp(index, 0, activeTactics.Count);
        activeTactics.Insert(index, tactic);
        tactic.myBar = this;
        UpdateVisualLayout();
    }

    public void RemoveTactic(TacticInstance tactic, bool destroyVisual = true)
    {
        if (activeTactics.Contains(tactic))
        {
            activeTactics.Remove(tactic);
            if (destroyVisual && tactic != null) Destroy(tactic.gameObject);
            UpdateVisualLayout();
        }
    }

    public void RefreshAllTacticAuras()
    {
        foreach (var tactic in activeTactics)
        {
            if (tactic != null && tactic.isPassive)
            {
                tactic.RemovePassiveEffect();
                tactic.ApplyPassiveEffect();
            }
        }
    }

    public void UpdateVisualLayout()
    {
        int count = activeTactics.Count;
        if (count == 0) return;

        float currentSpacing = defaultSpacing;
        float totalWidth = (count - 1) * currentSpacing;

        if (totalWidth > maxBarWidth)
        {
            currentSpacing = maxBarWidth / (count > 1 ? count - 1 : 1);
        }

        // USE THE NEW ANCHOR IF IT EXISTS
        Vector2 anchorPos = trackAnchor != null ? trackAnchor.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            float x = anchorPos.x;
            float y = anchorPos.y;

            if (alignment == BarAlignment.Center)
            {
                float halfW = totalWidth * 0.5f;
                x = anchorPos.x + (i * currentSpacing) - halfW;
            }
            else if (alignment == BarAlignment.Left)
            {
                x = anchorPos.x + (i * currentSpacing);
            }
            else if (alignment == BarAlignment.Right)
            {
                x = anchorPos.x - (i * currentSpacing);
            }

            if (useVerticalStagger && i % 2 != 0) y += verticalStaggerAmount;

            Vector2 targetPos = new Vector2(x, y) + tacticVisualOffset;

            if (activeTactics[i] != null)
            {
                // FORCE THE SCALE
                activeTactics[i].transform.localScale = tacticScale;
                if (!activeTactics[i].isDragging)
                {
                    activeTactics[i].transform.position = targetPos;
                }
            }
        }
    }

    public int GetInsertIndexFromPosition(Vector3 worldPosition)
    {
        int count = activeTactics.Count;
        if (count == 0) return 0;

        float currentSpacing = (count - 1) * defaultSpacing > maxBarWidth
            ? maxBarWidth / (count > 1 ? count - 1 : 1)
            : defaultSpacing;

        float startX = trackAnchor != null ? trackAnchor.position.x : transform.position.x;

        if (alignment == BarAlignment.Center)
        {
            float halfW = (count - 1) * currentSpacing * 0.5f;
            startX = trackAnchor != null ? trackAnchor.position.x - halfW : transform.position.x - halfW;
        }

        float localX = worldPosition.x - startX;
        if (alignment == BarAlignment.Right) localX = startX - worldPosition.x;

        int index = Mathf.RoundToInt(localX / currentSpacing);
        return Mathf.Clamp(index, 0, count);
    }

    public List<TacticInstance> GetAllTactics()
    {
        return new List<TacticInstance>(activeTactics);
    }

    public void ClearAllTactics()
    {
        for (int i = activeTactics.Count - 1; i >= 0; i--)
        {
            RemoveTactic(activeTactics[i], true);
        }
    }
}
