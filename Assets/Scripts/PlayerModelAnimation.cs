using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerModelAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0.1f)] private float walkReferenceSpeed = 1.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;

    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        animator.SetBool(Moving, speed > 0.1f);
        animator.SetFloat(WalkSpeed, Mathf.Max(0.1f, speed / walkReferenceSpeed));

        if (speed > 0.1f)
            visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                Quaternion.LookRotation(velocity), turnSpeed * Time.deltaTime);
    }
}
