using UnityEngine;

[DisallowMultipleComponent]
public class DefaultSpawnPoint : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    private void Start()
    {
        Respawn();
    }

    public void Respawn()
    {
        var checkpoint = SavePoint.Current;
        if (checkpoint && checkpoint.gameObject.scene == gameObject.scene && checkpoint.Respawn())
            return;

        player.Teleport(transform.position, transform.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward);
    }
}
