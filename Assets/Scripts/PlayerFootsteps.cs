using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Min(0.1f)] private float stepDistance = 1.5f;

    private Rigidbody body;
    private Vector3 previousPosition;
    private float travelled;
    private bool wasMoving;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        previousPosition = body.position;
    }

    private void FixedUpdate()
    {
        Vector3 delta = body.position - previousPosition;
        previousPosition = body.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        // Ignore standing still and jumps in position from respawn/teleport.
        if (distance < 0.002f || distance > 1f)
        {
            travelled = 0f;
            wasMoving = false;
            return;
        }

        travelled += distance;
        if (!wasMoving || travelled >= stepDistance)
        {
            audioSource.PlayOneShot(audioSource.clip);
            travelled = 0f;
        }
        wasMoving = true;
    }

    private void Update()
    {
        if (Time.timeScale > 0f)
            return;

        audioSource.Stop();
        previousPosition = body.position;
        travelled = 0f;
        wasMoving = false;
    }

    private void OnDisable()
    {
        audioSource.Stop();
        travelled = 0f;
        wasMoving = false;
    }
}
