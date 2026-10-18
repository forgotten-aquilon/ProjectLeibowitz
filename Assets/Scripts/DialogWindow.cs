using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogWindow : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private RectTransform previewFrame;
    [SerializeField] private UnityEngine.UI.RawImage previewImage;
    [SerializeField] private UnityEngine.UI.AspectRatioFitter previewAspect;
    [SerializeField] private UnityEngine.UI.ScrollRect textScroll;
    [SerializeField] private UnityEngine.UI.Button closeButton;
    [SerializeField] private InputAction close = new InputAction("Close", InputActionType.Button, "<Keyboard>/e");

    public bool IsOpen { get; private set; }

    public event Action Closed;
    private int openedFrame;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        close.Enable();
    }

    private void Update()
    {
        if (IsOpen && Time.frameCount != openedFrame && close.WasPressedThisFrame())
            Close();
    }

    public bool Show(string text, string title = "Dialog", Texture image = null)
    {
        if (IsOpen || (string.IsNullOrEmpty(text) && !image) || !ModalWindowManager.TryOpen(this))
            return false;

        titleText.text = title;
        bodyText.text = text;
        previewFrame.gameObject.SetActive(image);
        previewImage.texture = image;
        if (image)
            previewAspect.aspectRatio = (float)image.width / image.height;
        var scrollRect = (RectTransform)textScroll.transform;
        scrollRect.offsetMin = new Vector2(image ? 696f : 48f, scrollRect.offsetMin.y);
        openedFrame = Time.frameCount;
        IsOpen = true;
        panel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(textScroll.content);
        textScroll.StopMovement();
        textScroll.verticalNormalizedPosition = 1f;
        return true;
    }

    // Existing callers can pass paragraphs; they now form one continuous text.
    public bool Show(IReadOnlyList<string> paragraphs)
    {
        return Show(string.Join("\n\n", paragraphs));
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        ModalWindowManager.Close(this);
        if (panel)
            panel.SetActive(false);
        previewImage.texture = null;
        Closed?.Invoke();
    }

    private void OnDisable()
    {
        close.Disable();
        Close();
    }
}
