using System;
using UnityEngine;

public class SpriteCube : MonoBehaviour
{
    [Serializable]
    public class Face
    {
        public Sprite sprite;
        [Range(0f, 360f)] public float rotation;
    }

    [SerializeField] private float size = 1f;
    [SerializeField] private Face front = new Face();
    [SerializeField] private Face back = new Face();
    [SerializeField] private Face left = new Face();
    [SerializeField] private Face right = new Face();
    [SerializeField] private Face top = new Face();
    [SerializeField] private Face bottom = new Face();

    private void Start()
    {
        CreateFace("Front", front, Vector3.forward, Quaternion.identity);
        CreateFace("Back", back, Vector3.back, Quaternion.Euler(0f, 180f, 0f));
        CreateFace("Left", left, Vector3.left, Quaternion.Euler(0f, -90f, 0f));
        CreateFace("Right", right, Vector3.right, Quaternion.Euler(0f, 90f, 0f));
        CreateFace("Top", top, Vector3.up, Quaternion.Euler(-90f, 0f, 0f));
        CreateFace("Bottom", bottom, Vector3.down, Quaternion.Euler(90f, 0f, 0f));
    }

    private void CreateFace(string faceName, Face face, Vector3 direction, Quaternion orientation)
    {
        var side = new GameObject(faceName);
        side.transform.SetParent(transform, false);
        side.transform.localRotation = orientation * Quaternion.Euler(0f, 0f, face.rotation);

        var bounds = face.sprite.bounds;
        var scale = new Vector3(size / bounds.size.x, size / bounds.size.y, 1f);
        side.transform.localScale = scale;
        side.transform.localPosition = direction * (size * 0.5f)
            - side.transform.localRotation * Vector3.Scale(bounds.center, scale);

        var spriteRenderer = side.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = face.sprite;
    }
}
