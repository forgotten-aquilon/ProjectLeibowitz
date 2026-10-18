using System.Collections;
using UnityEngine;

public enum SavePointVfxStyle { GoldenPulse, AzureScan, VioletOrbits, VoxelBloom }

[DisallowMultipleComponent]
public class SavePointVfx : MonoBehaviour
{
    [SerializeField] private SavePointVfxStyle style;
    [SerializeField, Min(0.1f)] private float duration = 2f;
    [SerializeField, Tooltip("Loop the effect for demonstration. Keep disabled for a regular checkpoint.")]
    private bool loopPreview;
    [SerializeField, Min(0.1f)] private float loopInterval = 3.2f;
    [SerializeField, Min(0f)] private float previewOffset;
    [SerializeField] private Transform visuals;
    [SerializeField] private Transform[] rings;
    [SerializeField] private Transform[] sparks;

    private MeshRenderer[] renderers;
    private MaterialPropertyBlock properties;
    private Coroutine playback;
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    public bool IsPlaying => playback != null && visuals.gameObject.activeSelf;
    public SavePointVfxStyle Style => style;

    private void Awake()
    {
        renderers = visuals.GetComponentsInChildren<MeshRenderer>(true);
        properties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        visuals.gameObject.SetActive(false);
        if (loopPreview)
            playback = StartCoroutine(PlayEffect(previewOffset));
    }

    public void Play()
    {
        Stop();
        if (isActiveAndEnabled)
            playback = StartCoroutine(PlayEffect(0f));
    }

    public void Stop()
    {
        if (playback != null)
        {
            StopCoroutine(playback);
            playback = null;
        }
        if (visuals)
            visuals.gameObject.SetActive(false);
    }

    private IEnumerator PlayEffect(float offset)
    {
        float effectDuration = Mathf.Max(0.1f, duration);
        float cycleDuration = Mathf.Max(effectDuration, loopInterval);
        float time = loopPreview ? Mathf.Repeat(offset, cycleDuration) : 0f;
        do
        {
            while (time < effectDuration)
            {
                ApplyFrame(time / effectDuration);
                yield return null;
                time += Time.deltaTime;
            }

            visuals.gameObject.SetActive(false);
            if (!loopPreview)
                break;

            if (time < cycleDuration)
            {
                yield return new WaitForSeconds(cycleDuration - time);
                time = 0f;
            }
            else
                time = Mathf.Repeat(time, cycleDuration);
        }
        while (loopPreview);

        playback = null;
    }

    private void SetRing(int index, Vector3 position, float radius, Quaternion rotation)
    {
        rings[index].SetLocalPositionAndRotation(position, rotation);
        rings[index].localScale = Vector3.one * radius;
    }

    private void ApplyFrame(float t)
    {
        visuals.gameObject.SetActive(true);
        float fade = Mathf.Sin(t * Mathf.PI);
        properties.SetFloat(FadeId, fade);
        foreach (var renderer in renderers)
            renderer.SetPropertyBlock(properties);

        switch (style)
        {
            case SavePointVfxStyle.GoldenPulse:
                SetRing(0, Vector3.up * .04f, .6f + t * 2.1f, Quaternion.identity);
                SetRing(1, Vector3.up * .07f, .45f + t * 1.5f, Quaternion.identity);
                SetRing(2, Vector3.up * (.4f + t * 4f), .7f + t * .8f, Quaternion.identity);
                break;
            case SavePointVfxStyle.AzureScan:
                SetRing(0, Vector3.up * .045f, 1.75f + Mathf.Sin(t * Mathf.PI * 4f) * .08f, Quaternion.identity);
                SetRing(1, Vector3.up * (.15f + t * 4.2f), 1.75f, Quaternion.identity);
                SetRing(2, Vector3.up * (.05f + Mathf.Max(0,t-.15f) * 4.2f), 1.6f, Quaternion.identity);
                break;
            case SavePointVfxStyle.VioletOrbits:
                for (int i = 0; i < rings.Length; i++)
                    SetRing(i, Vector3.up * 2f, 1.9f + fade * .15f,
                        Quaternion.Euler(45f + i * 30f, t * 130f + i * 60f, i * 35f));
                break;
            case SavePointVfxStyle.VoxelBloom:
                SetRing(0, Vector3.up * .05f, .5f + t * 2.3f, Quaternion.identity);
                SetRing(1, Vector3.up * (1f + t * 2f), .6f + fade * .75f, Quaternion.Euler(0, t*180f, 45));
                SetRing(2, Vector3.up * (1f + t * 2f), .6f + fade * .75f, Quaternion.Euler(45, -t*180f, 0));
                break;
        }

        for (int i = 0; i < sparks.Length; i++)
        {
            float seed = i / (float)sparks.Length;
            float angle = i * 2.399963f;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 position;
            Vector3 size;
            Quaternion rotation = Quaternion.Euler(i * 23f + t * 180f, i * 41f, t * 270f);
            switch (style)
            {
                case SavePointVfxStyle.AzureScan:
                    float corner = (i % 4) * Mathf.PI * .5f + Mathf.PI * .25f;
                    position = new Vector3(Mathf.Cos(corner)*1.8f, .15f + Mathf.Repeat(t+seed,1f)*4.1f, Mathf.Sin(corner)*1.8f);
                    size = new Vector3(.045f,.22f + seed*.2f,.045f) * fade;
                    rotation = Quaternion.identity;
                    break;
                case SavePointVfxStyle.VioletOrbits:
                    float orbitAngle = angle + t * Mathf.PI * 2f;
                    Quaternion orbitPlane = Quaternion.Euler(45f + (i%3)*30f,t*130f+(i%3)*60f,(i%3)*35f);
                    position = Vector3.up*2f + orbitPlane * new Vector3(Mathf.Cos(orbitAngle)*2f,0,Mathf.Sin(orbitAngle)*2f);
                    size = Vector3.one * (.07f + .045f*seed) * fade;
                    break;
                case SavePointVfxStyle.VoxelBloom:
                    Vector3 centre;
                    if (i%8 < 4) centre = Vector3.up * (.5f+i%8);
                    else centre = Vector3.up*2.5f + Quaternion.Euler(0,(i%8-4)*90f,0)*Vector3.right;
                    position = centre + radial * (.2f + t*1.8f) + Vector3.up*t*(.5f+seed);
                    size = Vector3.one * (.08f+.18f*fade) * (1f-t*.65f);
                    break;
                default:
                    position = radial * (.45f+t*(.6f+seed)) + Vector3.up*(.15f+t*(2f+seed*2f));
                    size = Vector3.one * (.07f+.05f*seed) * fade;
                    break;
            }
            sparks[i].SetLocalPositionAndRotation(position,rotation);
            sparks[i].localScale = size;
        }
    }

    private void OnDisable()
    {
        Stop();
    }
}
