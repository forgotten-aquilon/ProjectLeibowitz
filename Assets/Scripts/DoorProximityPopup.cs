using System.Collections.Generic;
using UnityEngine;

public class DoorProximityPopup : ProximityPopup
{
    [SerializeField] private DialogWindow dialog;
    [SerializeField, Tooltip("Assign objects with a CollectibleItem component. The door opens once all of them have been collected.")]
    private List<CollectibleItem> requiredPickups = new List<CollectibleItem>();
    [SerializeField] private Transform doorLeaf;
    [SerializeField] private Collider doorCollider;
    [SerializeField] private Vector3 openAngles = new Vector3(0f, -100f, 0f);
    [SerializeField] private bool openWhenAllCollected;

    public bool IsOpen { get; private set; }
    protected override bool CanShowPopup => !IsOpen && !dialog.IsOpen;

    private void Update()
    {
        if (!openWhenAllCollected || IsOpen || !ModalWindowManager.CanInteract)
            return;

        foreach (CollectibleItem item in requiredPickups)
            if (!item || !item.IsCollected)
                return;

        Open();
    }

    public override void Interact()
    {
        if (!IsNearby || IsOpen || !ModalWindowManager.CanInteract)
            return;

        var missing = new List<string>();
        foreach (CollectibleItem item in requiredPickups)
        {
            if (!item)
                missing.Add("A required item has not been assigned to the door.");
            else if (!item.IsCollected)
                missing.Add(item.ItemName);
        }
        if (missing.Count > 0)
        {
            dialog.Show("Items still to collect:\n\n" + string.Join("\n", missing), "Door locked");
            return;
        }

        Open();
    }

    private void Open()
    {
        IsOpen = true;
        doorLeaf.localRotation = Quaternion.Euler(openAngles);
        doorCollider.enabled = false;
    }

}
