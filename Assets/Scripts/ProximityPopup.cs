using UnityEngine;

public abstract class ProximityPopup : MonoBehaviour
{
    [SerializeField, Min(0f)] private float radius = 3f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    public float Radius => radius;
    public Vector3 HintPosition => transform.position + worldOffset;
    public bool IsAvailable => isActiveAndEnabled && CanShowPopup;

    protected bool IsNearby => ProximityPopupPresenter.Instance && ProximityPopupPresenter.Instance.IsNearby(this);
    protected virtual bool CanShowPopup => true;

    public abstract void Interact();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
