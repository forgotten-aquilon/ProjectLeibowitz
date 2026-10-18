using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera), typeof(CameraFollow))]
public class CameraPixelation : MonoBehaviour
{
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Camera outputCamera;
    [SerializeField] private Canvas outputCanvas;
    [SerializeField] private RectTransform outputImage;
    [SerializeField] private Canvas[] pixelatedCanvases;

    private Camera viewCamera;
    private CameraFollow cameraFollow;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();
        cameraFollow = GetComponent<CameraFollow>();
        if (!renderTexture || !outputCamera || !outputCanvas || !outputImage)
            return; // Inspector references are assigned together during scene setup.

        outputCamera.enabled = true;
        outputCanvas.enabled = true;
        viewCamera.targetTexture = renderTexture;
        viewCamera.allowMSAA = false;
        viewCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;

        foreach (Canvas canvas in pixelatedCanvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = viewCamera;
            canvas.planeDistance = viewCamera.nearClipPlane + 0.1f;
        }
        UpdateOutput();
    }

    private void Update()
    {
        if (renderTexture && outputCamera && outputCanvas && outputImage)
            UpdateOutput();
    }

    private void UpdateOutput()
    {
        var settings = cameraFollow.Settings;
        if (!settings)
            return;
        int width = Mathf.Max(64, settings.resolution.x);
        int height = Mathf.Max(36, settings.resolution.y);
        if (renderTexture.width != width || renderTexture.height != height)
        {
            renderTexture.Release();
            renderTexture.width = width;
            renderTexture.height = height;
        }
        renderTexture.filterMode = FilterMode.Point;
        if (!renderTexture.IsCreated())
            renderTexture.Create();

        // Use the actual presentation canvas size, including the editor Game view.
        Rect screen = outputCanvas.pixelRect;
        if (screen.width <= 0 || screen.height <= 0)
            return;
        float scale = Mathf.Min(screen.width / width, screen.height / height);
        if (settings.integerScaling && scale >= 1)
            scale = Mathf.Floor(scale);

        Vector2 size = new Vector2(Mathf.Round(width * scale), Mathf.Round(height * scale));
        outputImage.sizeDelta = size;
        // Align to physical pixel boundaries, including odd display dimensions.
        outputImage.anchoredPosition = new Vector2(
            Mathf.Floor((screen.width - size.x) * 0.5f) + (size.x - screen.width) * 0.5f,
            Mathf.Floor((screen.height - size.y) * 0.5f) + (size.y - screen.height) * 0.5f);
    }

    private void OnDisable()
    {
        if (viewCamera && viewCamera.targetTexture == renderTexture)
            viewCamera.targetTexture = null;
        if (outputCamera)
            outputCamera.enabled = false;
        if (outputCanvas)
            outputCanvas.enabled = false;
        if (pixelatedCanvases == null)
            return;
        foreach (Canvas canvas in pixelatedCanvases)
        {
            if (!canvas)
                continue;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
    }
}
