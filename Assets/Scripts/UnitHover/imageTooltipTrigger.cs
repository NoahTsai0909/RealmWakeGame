using UnityEngine;
using UnityEngine.EventSystems;

// IPointerMoveHandler is the secret to fixing the center-screen bug!
public class ImageTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Tooltip("The exact ID from your TooltipDatabaseSO (e.g., 'preview')")]
    [SerializeField] public string keywordID = "preview";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUIManager.Instance == null) return;
        TooltipUIManager.Instance.Show(keywordID, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (TooltipUIManager.Instance != null)
        {
            // Forces the tooltip to snap to the mouse instead of the center of the screen
            TooltipUIManager.Instance.UpdatePosition(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUIManager.Instance != null)
        {
            TooltipUIManager.Instance.Hide();
        }
    }

    private void OnDisable()
    {
        if (TooltipUIManager.Instance != null)
        {
            TooltipUIManager.Instance.Hide();
        }
    }
}
