using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class MonsterChaser : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float startRadius = 5f;
    [SerializeField, Min(0f)] private float stopRadius = 8f;
    [SerializeField, Min(0f)] private float speed = 2.5f;
    [SerializeField, Min(0f)] private float roamRadius = 8f;
    [SerializeField, Min(0f)] private float roamWait = 1.5f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

    private NavMeshAgent agent;
    private NavMeshPath roamPath;
    private Vector3 roamOrigin;
    private float chaseStoppingDistance;
    private float nextRepathTime;
    private float nextRoamTime;
    private bool waitingToRoam;
    private bool chasing;

    public bool IsChasing => chasing;

    private void Awake()
    {
        if (!target)
        {
            var player = FindAnyObjectByType<PlayerScript>();
            if (player)
                target = player.transform;
        }
        agent = GetComponent<NavMeshAgent>();
        roamPath = new NavMeshPath();
        GetComponent<Rigidbody>().isKinematic = true;
        agent.speed = speed;
        chaseStoppingDistance = agent.stoppingDistance;
        roamOrigin = transform.position;
    }

    private void Update()
    {
        if (!target || Time.timeScale <= 0f || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        bool wasChasing = chasing;
        if (distance <= startRadius)
            chasing = true;
        else if (distance >= stopRadius)
            chasing = false;

        if (chasing != wasChasing)
        {
            agent.ResetPath();
            nextRepathTime = 0f;
            waitingToRoam = false;
        }

        if (chasing)
        {
            agent.stoppingDistance = chaseStoppingDistance;
            if (Time.time < nextRepathTime)
                return;
            nextRepathTime = Time.time + repathInterval;
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 2f, NavigationFilter()))
                agent.SetDestination(hit.position);
            else
                agent.ResetPath();
            return;
        }

        agent.stoppingDistance = 0.15f;
        if (agent.pathPending || (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f))
            return;

        if (!waitingToRoam)
        {
            nextRoamTime = Time.time + roamWait;
            waitingToRoam = true;
        }
        if (Time.time < nextRoamTime)
            return;

        for (int i = 0; i < 12; i++)
        {
            Vector2 offset = Random.insideUnitCircle * roamRadius;
            Vector3 point = roamOrigin + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavigationFilter()))
                continue;
            Vector3 travel = hit.position - transform.position;
            travel.y = 0f;
            if (travel.sqrMagnitude < 1f)
                continue;
            // SamplePosition can find a different island: only roam to reachable points.
            if (agent.CalculatePath(hit.position, roamPath) && roamPath.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(roamPath);
                break;
            }
        }
        waitingToRoam = false;
    }

    private NavMeshQueryFilter NavigationFilter()
    {
        return new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
    }

    private void OnDisable()
    {
        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.ResetPath();
        chasing = false;
        waitingToRoam = false;
    }

    private void OnValidate()
    {
        stopRadius = Mathf.Max(startRadius + 0.01f, stopRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, startRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopRadius);
    }
}
