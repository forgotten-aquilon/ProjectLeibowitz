using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class PrologueExit : MonoBehaviour
{
    [SerializeField] private DoorProximityPopup door;
    [SerializeField] private string nextScene = "Labyrinth";

    private bool loading;

    private void OnTriggerEnter(Collider other)
    {
        if (loading || !door.IsOpen || !other.GetComponentInParent<PlayerMovement>())
            return;

        loading = true;
        SceneManager.LoadSceneAsync(nextScene);
    }
}
