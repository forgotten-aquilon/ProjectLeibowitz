using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ProximityReveal : MonoBehaviour
{
    [SerializeField, Tooltip("Uses the scene player when left empty.")]
    private Transform target;
    [SerializeField, Min(0.1f)] private float triggerRadius = 5f;
    [SerializeField, Min(0.1f)] private float visibleDuration = 1.5f;
    [SerializeField, Min(0.1f), Tooltip("Minimum hidden time after each reveal before another trigger is accepted.")]
    private float cooldown = 3f;

    private Renderer[] renderers;
    private bool[] initialVisibility;
    private Coroutine revealRoutine;
    private bool wasInside;
    private float nextRevealTime;

    public bool IsRevealed { get; private set; }
    public bool IsOnCooldown => Time.time < nextRevealTime;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        initialVisibility = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            initialVisibility[i] = renderers[i].enabled;

        if (!target)
        {
            var player = FindAnyObjectByType<PlayerScript>();
            if (player)
                target = player.transform;
        }
    }

    private void OnEnable()
    {
        wasInside = false;
        SetVisible(false);
    }

    private void Update()
    {
        if (!target || Time.timeScale <= 0f)
            return;

        bool inside = (target.position - transform.position).sqrMagnitude <= triggerRadius * triggerRadius;
        if (inside && !wasInside)
            TryReveal();
        wasInside = inside;
    }

    public bool TryReveal()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || IsRevealed || IsOnCooldown)
            return false;

        nextRevealTime = Time.time + Mathf.Max(0.1f, visibleDuration) + Mathf.Max(0.1f, cooldown);
        revealRoutine = StartCoroutine(Reveal());
        return true;
    }

    private IEnumerator Reveal()
    {
        IsRevealed = true;
        SetVisible(true);
        yield return new WaitForSeconds(Mathf.Max(0.1f, visibleDuration));
        SetVisible(false);
        IsRevealed = false;
        nextRevealTime = Time.time + Mathf.Max(0.1f, cooldown);
        revealRoutine = null;
    }

    private void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i])
                renderers[i].enabled = visible && initialVisibility[i];
    }

    private void OnDisable()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
        if (IsRevealed)
            nextRevealTime = Time.time + Mathf.Max(0.1f, cooldown);
        IsRevealed = false;
        SetVisible(true);
    }

    private void OnValidate()
    {
        triggerRadius = Mathf.Max(0.1f, triggerRadius);
        visibleDuration = Mathf.Max(0.1f, visibleDuration);
        cooldown = Mathf.Max(0.1f, cooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
