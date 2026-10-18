using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FinalEndingScreens : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delayAfterFall = 3f;
    [SerializeField, TextArea(8, 16)] private string epilogueText =
        "Half-life is the time required for half of a substance to decay.\n\n" +
        "World War II ended with two nuclear explosions, which marked all materials on Earth with a precise timestamp.\n\n" +
        "Steel produced before these events is known as low-background steel.\n\n" +
        "Cobalt-60 has a half-life of 5.2714 years.";
    [SerializeField, TextArea(8, 16)] private string creditsText =
        "This game was created for Ludum Dare 60. The theme was \"Old tech\".\n\n" +
        "I was inspired by \"A Canticle for Leibowitz\" by Walter M. Miller Jr. and the music video for \"Soulbreaker\" by A. G. Cook.\n\n" +
        "I dedicate this game to my beloved, the angel without whom I would never have moved forward.\n\n" +
        "Thanks for playing!";
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private UnityEngine.UI.Button continueButton;

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public bool IsVisible { get; private set; }
    public bool IsShowingCredits { get; private set; }

    private void Awake() => panel.SetActive(false);
    private void OnEnable() => continueButton.onClick.AddListener(Continue);

    public void PlayAfterFall(Animator animator) => StartCoroutine(ShowAfterFall(animator));

    private IEnumerator ShowAfterFall(Animator animator)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("FallToKnees") ||
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f || animator.IsInTransition(0))
            yield return null;

        yield return new WaitForSeconds(delayAfterFall);
        while (!ModalWindowManager.CanOpen)
            yield return null;

        ModalWindowManager.TryOpen(this);
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        IsVisible = true;
        IsShowingCredits = false;
        textLabel.text = epilogueText.Replace("\\n", "\n");
        continueButton.gameObject.SetActive(true);
        panel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
    }

    public void Continue()
    {
        if (!IsVisible || IsShowingCredits)
            return;

        IsShowingCredits = true;
        textLabel.text = creditsText.Replace("\\n", "\n");
        EventSystem.current.SetSelectedGameObject(null);
        continueButton.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        continueButton.onClick.RemoveListener(Continue);
        if (IsVisible)
        {
            ModalWindowManager.Close(this);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            IsVisible = false;
        }
        if (panel)
            panel.SetActive(false);
    }
}
