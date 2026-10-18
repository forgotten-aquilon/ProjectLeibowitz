using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class SceneMusic : MonoBehaviour
{
    [SerializeField] private bool playOnStart = true;

    private AudioSource musicSource;
    private readonly InputAction toggleMusic = new InputAction("Toggle Music", InputActionType.Button, "<Keyboard>/p");

    public static bool IsMuted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMute() => IsMuted = false;

    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
        musicSource.mute = IsMuted;
    }

    private void OnEnable() => toggleMusic.Enable();

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void Update()
    {
        if (!toggleMusic.WasPressedThisFrame())
            return;

        IsMuted = !IsMuted;
        musicSource.mute = IsMuted;
    }

    private void OnDisable() => toggleMusic.Disable();
    private void OnDestroy() => toggleMusic.Dispose();
}
