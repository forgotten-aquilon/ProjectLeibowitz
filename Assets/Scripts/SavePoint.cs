using UnityEngine;

public class SavePoint : MonoBehaviour
{
    public static SavePoint Current;

    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float radius = 2f;
    [SerializeField, Tooltip("The player's respawn position and rotation. For a capsule player, place this point at the height of its center.")]
    private Transform respawnPoint;
    [SerializeField] private SavePointVfx activationVfx;
    [SerializeField, Tooltip("The blue glow of the active checkpoint.")]
    private GameObject activatedGlow;

    public Transform RespawnPoint => respawnPoint;
    public Vector3 RespawnPosition => respawnPoint.position;
    public Quaternion RespawnRotation => respawnPoint.rotation;
    public bool IsActivated { get; private set; }
    private bool targetInside;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCurrent()
    {
        Current = null;
    }

    private void Awake()
    {
        if (activatedGlow)
            activatedGlow.SetActive(false);

        // Prefabs cannot reference a scene player, so resolve it when instantiated.
        if (!target)
        {
            var player = FindAnyObjectByType<PlayerScript>();
            if (player)
                target = player.transform;
        }
    }

    public void Activate()
    {
        if (IsActivated)
            return;

        if (Current)
            Current.Deactivate();

        IsActivated = true;
        Current = this;
        if (activationVfx)
            activationVfx.Play();
        if (activatedGlow)
            activatedGlow.SetActive(true);
    }

    public void Deactivate()
    {
        IsActivated = false;
        if (activationVfx)
            activationVfx.Stop();
        if (activatedGlow)
            activatedGlow.SetActive(false);
        if (Current == this)
            Current = null;
    }

    public bool Respawn()
    {
        if (!target || !respawnPoint)
            return false;

        if (target.TryGetComponent(out PlayerMovement movement))
        {
            movement.Teleport(RespawnPosition, RespawnRotation);
            return true;
        }

        if (target.TryGetComponent(out Rigidbody body))
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.position = RespawnPosition;
            body.rotation = RespawnRotation;
        }
        target.SetPositionAndRotation(RespawnPosition, RespawnRotation);
        Physics.SyncTransforms();
        return true;
    }

    private void Update()
    {
        if (!target)
            return;
        bool inside = (target.position - transform.position).sqrMagnitude <= radius * radius;
        if (inside && !targetInside)
            Activate();
        targetInside = inside;
    }

    private void OnDisable()
    {
        targetInside = false;
        Deactivate();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
        if (respawnPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(RespawnPosition, 0.3f);
            Gizmos.DrawRay(RespawnPosition, respawnPoint.forward);
        }
    }
}
