using UnityEngine;
using UnityEngine.InputSystem;

// Position the hint after CameraFollow has updated the camera in LateUpdate.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class ProximityPopupPresenter : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private UnityEngine.UI.Image popupImage;
    [SerializeField] private InputAction interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

    public static ProximityPopupPresenter Instance { get; private set; }
    public ProximityPopup Current { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    private void OnEnable()
    {
        Instance = this;
        interact.Enable();
    }

    public bool IsNearby(ProximityPopup popup)
    {
        return (target.position - popup.transform.position).sqrMagnitude <= popup.Radius * popup.Radius;
    }

    private void SelectNearest()
    {
        Current = null;
        if (!ModalWindowManager.CanInteract)
            return;

        float nearestDistance = float.PositiveInfinity;
        foreach (var popup in FindObjectsByType<ProximityPopup>())
        {
            if (!popup.IsAvailable || !IsNearby(popup) || viewCamera.WorldToScreenPoint(popup.HintPosition).z <= 0f)
                continue;

            float distance = (target.position - popup.transform.position).sqrMagnitude;
            if (distance < nearestDistance || (distance == nearestDistance && Current && popup.GetEntityId() < Current.GetEntityId()))
            {
                Current = popup;
                nearestDistance = distance;
            }
        }
    }

    private void Update()
    {
        SelectNearest();
        if (Current && interact.WasPressedThisFrame())
            Current.Interact();
    }

    private void LateUpdate()
    {
        SelectNearest();
        popupImage.gameObject.SetActive(Current);
        if (!Current)
            return;

        Vector3 screenPosition = viewCamera.WorldToScreenPoint(Current.HintPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, screenPosition, uiCamera, out Vector2 position);
        popupImage.rectTransform.anchoredPosition = position;
    }

    private void OnDisable()
    {
        interact.Disable();
        Current = null;
        if (popupImage)
            popupImage.gameObject.SetActive(false);
        if (Instance == this)
            Instance = null;
    }
}
