using UnityEngine;

public class ShimmerDisplay : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private GeigerCounterNeedle counter;

    private void LateUpdate()
    {
        int value = 0;

        foreach (ShimmerOrigin origin in ShimmerOrigin.Origins)
            value = Mathf.Max(value, origin.GetValue(target.position));

        counter.SetValue(value);
    }
}
