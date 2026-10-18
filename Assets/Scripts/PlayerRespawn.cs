using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerScript))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private DefaultSpawnPoint defaultSpawnPoint;

    private PlayerScript player;

    private void Awake()
    {
        player = GetComponent<PlayerScript>();
    }

    private void OnEnable()
    {
        player.Died += Respawn;
    }

    private void OnDisable()
    {
        player.Died -= Respawn;
    }

    private void Respawn()
    {
        defaultSpawnPoint.Respawn();
        player.RestoreLives();
    }
}
