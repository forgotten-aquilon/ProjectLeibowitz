using System.Collections.Generic;
using UnityEngine;

public class ShimmerOrigin : MonoBehaviour
{
    private static readonly HashSet<ShimmerOrigin> origins = new HashSet<ShimmerOrigin>();
    public static IEnumerable<ShimmerOrigin> Origins => origins;

    [SerializeField, Min(0f)] private float minRadius = 1f;
    [SerializeField, Min(0f)] private float maxRadius = 6f;

    private void OnEnable()
    {
        origins.Add(this);
    }

    private void OnDisable()
    {
        origins.Remove(this);
    }

    public int GetValue(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        return Mathf.RoundToInt(Mathf.InverseLerp(maxRadius, minRadius, distance) * 100f);
    }

    private void OnValidate()
    {
        maxRadius = Mathf.Max(minRadius + 0.01f, maxRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }
}
