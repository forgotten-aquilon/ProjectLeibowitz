using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private GameCameraSettings settings;
    [SerializeField] private Volume volume;

    private Camera viewCamera;
    private UniversalAdditionalCameraData cameraData;

    public GameCameraSettings Settings => settings;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();
        cameraData = viewCamera.GetUniversalAdditionalCameraData();
        cameraData.SetRenderer(-1);
        FollowTarget();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            FollowTarget();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            FollowTarget();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.update -= RefreshPreview;
        UnityEditor.EditorApplication.update += RefreshPreview;
    }

    private void RefreshPreview()
    {
        UnityEditor.EditorApplication.update -= RefreshPreview;
        if (this && isActiveAndEnabled && !Application.isPlaying)
            FollowTarget();
    }
#endif

    public void RefreshView() => FollowTarget();

    private void FollowTarget()
    {
        if (!settings)
            return;

        viewCamera.orthographic = true;
        viewCamera.orthographicSize = settings.orthographicSize;
        viewCamera.nearClipPlane = settings.nearClipPlane;
        viewCamera.farClipPlane = settings.farClipPlane;
        viewCamera.clearFlags = settings.clearFlags;
        viewCamera.backgroundColor = settings.backgroundColor;
        viewCamera.cullingMask = settings.cullingMask;
        viewCamera.allowHDR = settings.allowHDR;
        viewCamera.allowMSAA = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.renderPostProcessing = settings.postProcessing;
        if (volume)
            volume.sharedProfile = settings.volumeProfile;

        if (!target)
            return;
        Quaternion rotation = Quaternion.Euler(settings.angles);
        Vector3 position = target.position - rotation * Vector3.forward * settings.distance;
        transform.SetPositionAndRotation(position, rotation);
    }
}
