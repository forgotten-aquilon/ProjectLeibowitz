using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FinalMandelbulbDialogue : ProximityPopup
{
    [Header("Conversation")]
    [SerializeField, TextArea(2, 6)] private List<string> phrases = new List<string>
    {
        "Can you hear me?",
        "Your journey has brought you here.",
        "Now it is all over."
    };
    [SerializeField] private GameObject mandelbulb;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private FinalEndingScreens endingScreens;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text phraseLabel;
    [SerializeField] private UnityEngine.UI.Button nextButton;

    private Rigidbody body;
    private int phraseIndex;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public bool IsOpen { get; private set; }
    public bool IsComplete { get; private set; }
    public int CurrentPhraseIndex => phraseIndex;
    protected override bool CanShowPopup => !IsOpen && !IsComplete;

    private void Awake()
    {
        body = player.GetComponent<Rigidbody>();
        panel.SetActive(false);
    }

    private void OnEnable() => nextButton.onClick.AddListener(Next);

    public override void Interact() => Begin();

    public void Begin()
    {
        if (!IsNearby || IsOpen || IsComplete || !ModalWindowManager.CanInteract || !ModalWindowManager.TryOpen(this))
            return;

        IsOpen = true;
        phraseIndex = 0;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (phrases.Count == 0)
        {
            Complete();
            return;
        }

        panel.SetActive(true);
        ShowPhrase();
        EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
    }

    public void Next()
    {
        if (!IsOpen)
            return;

        phraseIndex++;
        if (phraseIndex >= phrases.Count)
            Complete();
        else
            ShowPhrase();
    }

    private void ShowPhrase() => phraseLabel.text = phrases[phraseIndex].Replace("\\n", "\n");

    private void Complete()
    {
        IsComplete = true;
        IsOpen = false;
        panel.SetActive(false);
        ModalWindowManager.Close(this);
        RestoreCursor();
        EventSystem.current.SetSelectedGameObject(null);

        mandelbulb.SetActive(false);
        player.enabled = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        playerAnimator.SetBool("Moving", false);
        playerAnimator.SetTrigger("Fall");
        endingScreens.PlayAfterFall(playerAnimator);
    }

    private void RestoreCursor()
    {
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDisable()
    {
        nextButton.onClick.RemoveListener(Next);
        if (IsOpen)
        {
            IsOpen = false;
            ModalWindowManager.Close(this);
            RestoreCursor();
        }
        if (panel)
            panel.SetActive(false);
    }
}
