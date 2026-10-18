using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Game/Camera Settings")]
public class GameCameraSettings : ScriptableObject
{
    [Header("Camera")]
    public Vector3 angles = new Vector3(60f, 40f, 0f);
    [Min(0.01f)] public float distance = 20f;
    [Min(0.01f)] public float orthographicSize = 7f;
    [Min(0.01f)] public float nearClipPlane = 0.3f;
    [Min(0.02f)] public float farClipPlane = 100f;
    public CameraClearFlags clearFlags = CameraClearFlags.Skybox;
    public Color backgroundColor = new Color(0.003f, 0.004f, 0.006f);
    public LayerMask cullingMask = ~((1 << 8) | (1 << 9));

    [Header("Pixelation")]
    public Vector2Int resolution = new Vector2Int(640, 360);
    [Tooltip("Use whole-number scaling steps. Disable to fit the image to the window.")]
    public bool integerScaling;

    [Header("Rendering")]
    public bool allowHDR = true;
    public bool postProcessing = true;
    public VolumeProfile volumeProfile;

#if UNITY_EDITOR
    private void OnValidate()
    {
        resolution = new Vector2Int(Mathf.Max(64, resolution.x), Mathf.Max(36, resolution.y));
        farClipPlane = Mathf.Max(nearClipPlane + 0.01f, farClipPlane);
        UnityEditor.EditorApplication.delayCall -= RefreshCameras;
        UnityEditor.EditorApplication.delayCall += RefreshCameras;
    }

    private void RefreshCameras()
    {
        if (!this)
            return;
        foreach (CameraFollow follow in FindObjectsByType<CameraFollow>())
            if (follow.Settings == this)
                follow.RefreshView();
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif
}
