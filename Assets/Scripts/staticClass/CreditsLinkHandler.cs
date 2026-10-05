using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CreditsLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI creditsText;

    private void Awake()
    {
        creditsText = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            creditsText,
            eventData.position,
            eventData.pressEventCamera
        );

        if (linkIndex < 0)
            return;

        string url = creditsText.textInfo.linkInfo[linkIndex].GetLinkID();

        if (url.StartsWith("https://") || url.StartsWith("http://"))
        {
            Application.OpenURL(url);
        }
    }
}
