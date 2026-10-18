using UnityEngine;

namespace Leonardo.VisualObjects
{
    public interface IVisualObjectAnimation
    {
        bool IsAnimating { get; set; }
    }

    public sealed class VisualObjectMotion : MonoBehaviour, IVisualObjectAnimation
    {
        public bool animate = true;
        public bool autoRotate = true;
        [Range(0, 30)] public float rotationSpeed = 6;
        [Range(0, 0.15f)] public float floatHeight = 0.025f;
        public bool IsAnimating { get => animate; set => animate = value; }
        Vector3 origin;
        float elapsed;

        void OnEnable() => origin = transform.localPosition;

        void Update()
        {
            if (!animate) return;
            elapsed += Time.deltaTime;
            if (autoRotate) transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            transform.localPosition = origin + Vector3.up * Mathf.Sin(elapsed * 0.7f) * floatHeight;
        }
    }
}
