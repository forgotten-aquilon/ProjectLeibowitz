using UnityEngine;

[DisallowMultipleComponent]
public class GeigerCounterNeedle : MonoBehaviour
{
    [SerializeField] private Transform needle;
    [SerializeField] private float minimumAngle = 58f;
    [SerializeField] private float maximumAngle = -58f;
    [SerializeField, Min(0f)] private float rotationSpeed = 160f;
    [SerializeField, Range(0f, 100f)] private float reading;

    public float Reading => reading;

    public void SetValue(float value)
    {
        reading = Mathf.Clamp(value, 0f, 100f);
    }

    private Quaternion NeedleRotation => Quaternion.Euler(0f, 0f,
        Mathf.Lerp(minimumAngle, maximumAngle, reading / 100f));

    private void OnEnable()
    {
        needle.localRotation = NeedleRotation;
    }

    private void LateUpdate()
    {
        needle.localRotation = Quaternion.RotateTowards(needle.localRotation,
            NeedleRotation, rotationSpeed * Time.deltaTime);
    }
}
