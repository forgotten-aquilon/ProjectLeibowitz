using UnityEngine;
using UnityEngine.EventSystems;

public class PixelatedScrollRect : UnityEngine.UI.ScrollRect
{
    [SerializeField] private PixelatedGraphicRaycaster raycaster;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 screen = eventData.position;
        eventData.position = raycaster.ScreenToRenderPosition(screen);
        try { base.OnBeginDrag(eventData); }
        finally { eventData.position = screen; }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        Vector2 screen = eventData.position;
        eventData.position = raycaster.ScreenToRenderPosition(screen);
        try { base.OnDrag(eventData); }
        finally { eventData.position = screen; }
    }
}
