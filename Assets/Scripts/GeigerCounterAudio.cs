using UnityEngine;

public class GeigerCounterAudio : MonoBehaviour
{
    [SerializeField] private GeigerCounterNeedle counter;
    [SerializeField] private AudioSource staticSource;
    [SerializeField] private AudioSource clickSource;
    [SerializeField, Min(0.1f)] private float minimumClicksPerSecond = 0.8f;
    [SerializeField, Min(0.1f)] private float maximumClicksPerSecond = 26f;
    [SerializeField, Range(0f, 1f)] private float minimumStaticVolume = 0.008f;
    [SerializeField, Range(0f, 1f)] private float maximumStaticVolume = 0.12f;
    [SerializeField, Range(0f, 1f)] private float minimumClickVolume = 0.10f;
    [SerializeField, Range(0f, 1f)] private float maximumClickVolume = 0.50f;

    private readonly System.Random random = new System.Random();
    private float nextClick;

    public float Intensity { get; private set; }
    public float ClicksPerSecond => Mathf.Lerp(minimumClicksPerSecond, maximumClicksPerSecond, Intensity);

    private void OnEnable()
    {
        Intensity = 0f;
        staticSource.volume = minimumStaticVolume;
        nextClick = NextInterval();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            staticSource.Stop();
            clickSource.Stop();
            return;
        }

        float previousRate = ClicksPerSecond;
        Intensity = Mathf.MoveTowards(Intensity, counter.Reading / 100f, Time.deltaTime * 3f);
        nextClick *= previousRate / ClicksPerSecond;
        staticSource.volume = Mathf.Lerp(minimumStaticVolume, maximumStaticVolume, Intensity);
        clickSource.volume = Mathf.Lerp(minimumClickVolume, maximumClickVolume, Intensity);
        if (!staticSource.isPlaying)
            staticSource.Play();

        nextClick -= Time.deltaTime;
        if (nextClick > 0f)
            return;

        clickSource.PlayOneShot(clickSource.clip, Mathf.Lerp(0.85f, 1f, (float)random.NextDouble()));
        nextClick = NextInterval();
    }

    // Irregular pulse spacing gives a Geiger-like crackle rather than a metronome.
    private float NextInterval() => -Mathf.Log(Mathf.Max(0.0001f, (float)random.NextDouble())) / ClicksPerSecond;

    private void OnDisable()
    {
        staticSource.Stop();
        clickSource.Stop();
    }
}
