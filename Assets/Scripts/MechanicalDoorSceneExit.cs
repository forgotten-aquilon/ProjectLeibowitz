using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class MechanicalDoorSceneExit : MonoBehaviour
{
    [SerializeField] private MechanicalDoor door;
    [SerializeField] private string nextScene = "TheFinal";
    private bool loading;

    private void OnTriggerEnter(Collider other)
    {
        if (loading || !door.IsOpen || !other.GetComponentInParent<PlayerMovement>())
            return;

        loading = true;
        SceneManager.LoadSceneAsync(nextScene);
    }
}
