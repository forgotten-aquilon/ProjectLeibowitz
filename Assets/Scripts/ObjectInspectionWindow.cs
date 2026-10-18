using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjectInspectionWindow : MonoBehaviour
{
    [SerializeField] private DialogWindow dialog;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewPivot;
    [SerializeField] private float rotationSpeed = 35f;

    public bool IsOpen { get; private set; }

    private RenderTexture previewTexture;
    private GameObject model;
    private readonly List<Mesh> bakedMeshes = new List<Mesh>();

    private void Awake()
    {
        previewTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32)
        {
            name = "Object Inspection Preview",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            useMipMap = false
        };
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;
        previewCamera.enabled = false;
    }

    private void OnEnable()
    {
        dialog.Closed += ClearPreview;
    }

    private void Update()
    {
        if (!IsOpen)
            return;
        previewPivot.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    public void Show(GameObject source, string title, string description, Vector3 angles)
    {
        if (IsOpen || !source || !isActiveAndEnabled || !ModalWindowManager.CanOpen)
            return;

        previewPivot.localRotation = Quaternion.identity;
        model = CopyVisual(source.transform, previewPivot);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(angles);
        model.SetActive(true);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            ClearModel();
            Debug.LogWarning("Inspection model needs a MeshRenderer or SkinnedMeshRenderer.", source);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        model.transform.position += previewPivot.position - bounds.center;
        // A bounding sphere keeps long and off-centre objects in view as they rotate.
        float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
        float distance = radius * 3f + 0.5f;
        Quaternion viewAngle = Quaternion.Euler(12f, 0f, 0f);
        previewCamera.transform.SetPositionAndRotation(previewPivot.position - viewAngle * Vector3.forward * distance, viewAngle);
        previewCamera.orthographicSize = radius * 1.15f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = distance + radius * 3f;

        if (!dialog.Show(description, title, previewTexture))
        {
            ClearModel();
            return;
        }

        IsOpen = true;
        previewCamera.enabled = true;
    }

    public void Close()
    {
        if (IsOpen)
            dialog.Close();
    }

    private void ClearPreview()
    {
        IsOpen = false;
        if (previewCamera)
            previewCamera.enabled = false;
        ClearModel();
    }

    // Recreate renderers only. Gameplay scripts, cameras, colliders and rigidbodies
    // from the original object must never run in the inspection window.
    private GameObject CopyVisual(Transform source, Transform parent)
    {
        var visual = new GameObject(source.name) { layer = previewCamera.gameObject.layer };
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = source.localPosition;
        visual.transform.localRotation = source.localRotation;
        visual.transform.localScale = source.localScale;

        Mesh mesh = null;
        Renderer sourceRenderer = null;
        if (source.TryGetComponent(out MeshFilter meshFilter) && source.TryGetComponent(out MeshRenderer meshRenderer))
        {
            mesh = meshFilter.sharedMesh;
            sourceRenderer = meshRenderer;
        }
        else if (source.TryGetComponent(out SkinnedMeshRenderer skinned))
        {
            mesh = new Mesh { name = source.name + " inspection mesh" };
            skinned.BakeMesh(mesh);
            bakedMeshes.Add(mesh);
            sourceRenderer = skinned;
        }
        if (mesh)
        {
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = sourceRenderer.enabled;
        }
        foreach (Transform child in source)
            CopyVisual(child, visual.transform);
        visual.SetActive(source.gameObject.activeSelf);
        return visual;
    }

    private void ClearModel()
    {
        if (model)
        {
            model.SetActive(false);
            Destroy(model);
            model = null;
        }
        foreach (Mesh mesh in bakedMeshes)
            Destroy(mesh);
        bakedMeshes.Clear();
    }

    private void OnDisable()
    {
        Close();
        dialog.Closed -= ClearPreview;
        ClearPreview();
    }

    private void OnDestroy()
    {
        if (previewCamera)
            previewCamera.targetTexture = null;
        if (previewTexture)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }
}
