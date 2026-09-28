using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float volume = 1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, volume * 0.5f); 
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickSound != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, volume);
        }
    }
}
