using TMPro;
using UnityEngine;

public class PlayerLivesDisplay : MonoBehaviour
{
    [SerializeField] private PlayerScript player;
    [SerializeField] private TMP_Text livesText;

    private void OnEnable()
    {
        player.LivesChanged += Refresh;
        Refresh(player.Lives);
    }

    private void Start()
    {
        // All Awake calls have completed, including the player's life initialization.
        Refresh(player.Lives);
    }

    private void OnDisable()
    {
        player.LivesChanged -= Refresh;
    }

    private void Refresh(int lives)
    {
        livesText.SetText("Lives: {0} / {1}", lives, player.MaxLives);
    }
}
