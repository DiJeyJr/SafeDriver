using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stopDistance = 0.15f;

    [Header("Traffic")]
    [SerializeField] private Transform stopLine;
    [SerializeField] private float stopLineThreshold = 0.2f;

    private int currentWaypointIndex;
    private bool canProceed = true;
    private bool isWaitingAtStop;

    public bool CanProceed => canProceed;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        // Check if we should stop at the stop line
        if (!canProceed && stopLine != null && !isWaitingAtStop)
        {
            float distToStop = Vector3.Distance(transform.position, stopLine.position);
            if (distToStop < stopLineThreshold)
            {
                isWaitingAtStop = true;
            }
        }

        if (isWaitingAtStop && !canProceed) return;

        if (isWaitingAtStop && canProceed)
        {
            isWaitingAtStop = false;
        }

        // Move toward current waypoint
        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime
        );

        // Rotate toward waypoint
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime
            );
        }

        // Reached waypoint — advance
        if (distance < stopDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;
                transform.position = waypoints[0].position;
            }
        }
    }

    public void SetCanProceed(bool value)
    {
        canProceed = value;
    }
}
