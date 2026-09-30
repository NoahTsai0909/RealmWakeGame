using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteMask))]
public class AutoSpriteMaskSize : MonoBehaviour
{
    void Update()
    {
        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
        SpriteMask mask = GetComponent<SpriteMask>();

        if (parentRect != null && mask != null && mask.sprite != null)
        {
            Vector2 spriteSize = mask.sprite.bounds.size;
            if (spriteSize.x > 0 && spriteSize.y > 0)
            {
                transform.localScale = new Vector3(
                    parentRect.rect.width / spriteSize.x,
                    parentRect.rect.height / spriteSize.y,
                    1f
                );
            }
        }
    }
}
