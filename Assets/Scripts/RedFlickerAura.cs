using UnityEngine;

[DisallowMultipleComponent]
public class RedFlickerAura : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.025f, 0.015f);
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.6f, 0f);
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0f)] private float intensity = 2.5f;
    [SerializeField, Range(0f, 1f)] private float flickerAmount = 0.3f;
    [SerializeField, Min(0.1f)] private float flickerSpeed = 4f;

    private Light glowLight;
    private float phase;

    private void Awake()
    {
        var glow = new GameObject("RedFlickerLight");
        glow.transform.SetParent(transform, false);
        glowLight = glow.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.shadows = LightShadows.None;
        phase = Random.value * 100f;
    }

    private void OnEnable()
    {
        ConfigureLight();
        glowLight.intensity = intensity;
        glowLight.enabled = true;
    }

    private void Update()
    {
        phase += Time.deltaTime * flickerSpeed;
        float flicker = Mathf.PerlinNoise(phase, 0.37f) * 2f - 1f;
        glowLight.intensity = intensity * (1f + flicker * flickerAmount);
    }

    private void ConfigureLight()
    {
        glowLight.transform.localPosition = localOffset;
        glowLight.color = color;
        glowLight.range = radius;
    }

    private void OnDisable()
    {
        if (glowLight)
            glowLight.enabled = false;
    }

    private void OnDestroy()
    {
        if (glowLight)
            Destroy(glowLight.gameObject);
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.1f, radius);
        intensity = Mathf.Max(0f, intensity);
        flickerSpeed = Mathf.Max(0.1f, flickerSpeed);
        if (glowLight)
            ConfigureLight();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.TransformPoint(localOffset), radius);
    }
}
