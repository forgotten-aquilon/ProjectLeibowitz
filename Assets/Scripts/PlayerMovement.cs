using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private Transform viewCamera;
    [SerializeField, Min(0f)] private float knockbackSpeed = 6f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.2f;

    private Rigidbody body;
    private Vector2 movement;
    private Vector3 knockbackVelocity;
    private float knockbackRemaining;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void OnMove(InputValue value)
    {
        movement = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    private void FixedUpdate()
    {
        // Give the hit a short movement window so WASD cannot cancel the recoil.
        if (knockbackRemaining > 0f)
        {
            body.linearVelocity = knockbackVelocity;
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - Time.fixedDeltaTime);
            return;
        }

        Quaternion cameraRotation = Quaternion.Euler(0f, viewCamera.eulerAngles.y, 0f);
        body.linearVelocity = cameraRotation * new Vector3(movement.x, 0f, movement.y) * speed;
    }

    public void KnockBack(Vector3 direction)
    {
        direction.y = 0f;
        knockbackVelocity = direction.normalized * knockbackSpeed;
        knockbackRemaining = knockbackDuration;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        body.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
    }

    private void OnDisable()
    {
        knockbackRemaining = 0f;
        movement = Vector2.zero;
    }
}
