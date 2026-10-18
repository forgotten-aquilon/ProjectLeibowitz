using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// UI is drawn into the camera's small texture, but mouse input uses display pixels.
public class PixelatedGraphicRaycaster : UnityEngine.UI.GraphicRaycaster
{
    [SerializeField] private RectTransform outputImage;

    public Vector2 ScreenToRenderPosition(Vector2 screenPosition)
    {
        if (!eventCamera || !eventCamera.targetTexture)
            return screenPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(outputImage, screenPosition, null, out Vector2 local);
        Vector2 uv = (local - outputImage.rect.min) / outputImage.rect.size;
        return uv * new Vector2(eventCamera.targetTexture.width, eventCamera.targetTexture.height);
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
    {
        if (!eventCamera || !eventCamera.targetTexture)
        {
            base.Raycast(eventData, results);
            return;
        }

        Vector2 screenPosition = eventData.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(outputImage, screenPosition, null, out Vector2 local);
        Rect imageRect = outputImage.rect;
        if (!imageRect.Contains(local))
            return;

        Vector2 uv = (local - imageRect.min) / imageRect.size;
        eventData.position = uv * new Vector2(eventCamera.targetTexture.width, eventCamera.targetTexture.height);
        try
        {
            base.Raycast(eventData, results);
        }
        finally
        {
            eventData.position = screenPosition;
        }
    }
}
