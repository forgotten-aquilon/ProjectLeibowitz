using UnityEngine;

public class ObjectInspectionPopup : ProximityPopup
{
    [SerializeField] private ObjectInspectionWindow inspectionWindow;
    [SerializeField] private GameObject previewModel;
    [SerializeField] private string objectName;
    [SerializeField, TextArea(3, 8)] private string description;
    [SerializeField] private Vector3 previewAngles;

    protected override bool CanShowPopup => !inspectionWindow.IsOpen;

    public override void Interact() => OpenInspection();

    public void OpenInspection()
    {
        if (!IsNearby || !CanShowPopup || !ModalWindowManager.CanInteract)
            return;

        inspectionWindow.Show(previewModel, objectName, description, previewAngles);
        if (inspectionWindow.IsOpen && TryGetComponent(out CollectibleItem item))
            item.Collect();
    }

}
