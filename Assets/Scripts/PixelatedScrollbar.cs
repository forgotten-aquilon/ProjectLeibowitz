using UnityEngine;
using UnityEngine.EventSystems;

public class PixelatedScrollbar : UnityEngine.UI.Scrollbar
{
    [SerializeField] private PixelatedGraphicRaycaster raycaster;

    public override void OnDrag(PointerEventData eventData)
    {
        Vector2 screen = eventData.position;
        eventData.position = raycaster.ScreenToRenderPosition(screen);
        try { base.OnDrag(eventData); }
        finally { eventData.position = screen; }
    }
}
