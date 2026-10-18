using UnityEngine;

public class DoorActivationPopup : ProximityPopup
{
    [SerializeField, Tooltip("Assign a GameObject with a MechanicalDoor component.")]
    private GameObject target;

    private MechanicalDoor door;

    protected override bool CanShowPopup => door.RemainingInteractions > 0 && !door.IsOpening && !door.IsOpen;

    private void Awake()
    {
        door = target.GetComponent<MechanicalDoor>();
    }

    public override void Interact()
    {
        if (!IsNearby || !IsAvailable || !ModalWindowManager.CanInteract)
            return;

        door.RemainingInteractions--;
    }
}
