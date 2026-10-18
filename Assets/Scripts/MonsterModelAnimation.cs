using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterModelAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.1f)] private float walkReferenceSpeed = 1.5f;

    private NavMeshAgent agent;
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int WalkSpeed = Animator.StringToHash("WalkSpeed");

    private void Awake() => agent = GetComponent<NavMeshAgent>();

    private void Update()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        animator.SetBool(Moving, speed > 0.1f);
        animator.SetFloat(WalkSpeed, Mathf.Max(0.1f, speed / walkReferenceSpeed));
    }
}
