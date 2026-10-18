using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PrologueIntro : MonoBehaviour
{
    [Header("Content")]
    [SerializeField, TextArea(4, 12)] private string introductionText = "Before you lies a path into the labyrinth.\nCollect your equipment and prepare to descend.";
    [SerializeField] private string startButtonText = "START";
    [SerializeField, TextArea(3, 10), Tooltip("New line: Enter or the sequence \\n.")]
    private string captionText = "PROLOGUE";
    [SerializeField, Min(0.1f)] private float captionDuration = 2f;
    [SerializeField] private UnityEngine.Events.UnityEvent onStarted = new UnityEngine.Events.UnityEvent();

    [Header("UI")]
    [SerializeField] private Canvas introCanvas;
    [SerializeField] private GameObject blackBackground;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private GameObject captionPanel;
    [SerializeField] private TMP_Text introductionLabel;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private TMP_Text captionLabel;
    [SerializeField] private UnityEngine.UI.Button startButton;

    private bool started;
    private bool ownsScreen;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public bool IsComplete { get; private set; }
    public bool IsShowingCaption => started && !IsComplete;

    private void Awake()
    {
        introductionLabel.text = introductionText.Replace("\\n", "\n");
        buttonLabel.text = startButtonText;
        captionLabel.text = captionText.Replace("\\n", "\n");
        blackBackground.SetActive(true);
        introductionPanel.SetActive(true);
        captionPanel.SetActive(false);
        introCanvas.enabled = true;
        startButton.onClick.AddListener(Begin);
    }

    private void Start()
    {
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        ownsScreen = ModalWindowManager.TryOpen(this);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    public void Begin()
    {
        if (started || !ownsScreen)
            return;

        started = true;
        startButton.interactable = false;
        introductionPanel.SetActive(false);
        blackBackground.SetActive(false);
        captionPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        ReleaseScreen();
        onStarted.Invoke();
        StartCoroutine(ShowCaption());
    }

    private IEnumerator ShowCaption()
    {
        yield return new WaitForSecondsRealtime(captionDuration);
        IsComplete = true;
        introCanvas.gameObject.SetActive(false);
        ReleaseScreen();
    }

    private void ReleaseScreen()
    {
        if (!ownsScreen)
            return;

        ModalWindowManager.Close(this);
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        ownsScreen = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (ownsScreen)
            introCanvas.gameObject.SetActive(false);
        ReleaseScreen();
    }
}
