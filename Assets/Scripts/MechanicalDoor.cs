using System.Collections;
using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
public class MechanicalDoor : MonoBehaviour
{
    [SerializeField] private Transform doorLeaf;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, -6f, 0f);
    [SerializeField, Min(0.01f)] private float openingDuration = 2f;
    [SerializeField, Tooltip("The door opens when this value reaches zero or less.")]
    private int remainingInteractions = 1;

    private Vector3 closedPosition;
    private Tween openingTween;

    public int RemainingInteractions
    {
        get => remainingInteractions;
        set => remainingInteractions = value;
    }

    public bool IsOpening { get; private set; }
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        closedPosition = doorLeaf.localPosition;
    }

    private void Update()
    {
        if (!IsOpen && !IsOpening && remainingInteractions <= 0)
            StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        IsOpening = true;
        openingTween = Tween.LocalPosition(doorLeaf, closedPosition + openOffset,
            Mathf.Max(0.01f, openingDuration), Ease.InOutSine);
        yield return openingTween.ToYieldInstruction();

        IsOpening = false;
        IsOpen = true;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        openingTween.Stop();
        IsOpening = false;
    }
}
