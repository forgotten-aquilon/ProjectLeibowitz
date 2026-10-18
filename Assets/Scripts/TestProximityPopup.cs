using UnityEngine;

public class TestProximityPopup : ProximityPopup
{
    private DialogWindow dialog;
    private ObjectInspectionWindow inspectionWindow;
    [SerializeField] private string title = "Dialog";
    [SerializeField, TextArea(5, 20)] private string text;
    [SerializeField] private Texture2D image;
    [SerializeField] private GameObject previewModel;
    [SerializeField] private Vector3 previewAngles;

    protected override bool CanShowPopup => !dialog.IsOpen;

    private void Awake()
    {
        dialog = FindAnyObjectByType<DialogWindow>(FindObjectsInactive.Include);
        inspectionWindow = FindAnyObjectByType<ObjectInspectionWindow>(FindObjectsInactive.Include);
    }

    public override void Interact() => OpenDialog();

    public void OpenDialog()
    {
        if (!IsNearby || !ModalWindowManager.CanInteract)
            return;
        if (previewModel)
            inspectionWindow.Show(previewModel, title, text, previewAngles);
        else
            dialog.Show(text, title, image);
    }

}
