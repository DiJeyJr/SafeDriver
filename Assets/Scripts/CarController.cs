using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float stopDistance = 0.3f;

    [Header("Traffic")]
    [SerializeField] private Transform stopLine;
    [SerializeField] private float stopLineThreshold = 0.5f;

    [Header("Linked parts (move together)")]
    [SerializeField] private Transform[] linkedParts;

    private int currentWaypointIndex;
    private bool canProceed = false;
    private bool isWaitingAtStop;

    public bool CanProceed => canProceed;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 targetPos = waypoints[currentWaypointIndex].localPosition;
        Vector3 myPos = transform.localPosition;
        float distance = Vector3.Distance(myPos, targetPos);

        if (!canProceed && stopLine != null && !isWaitingAtStop)
        {
            if (Vector3.Distance(myPos, stopLine.localPosition) < stopLineThreshold)
                isWaitingAtStop = true;
        }
        if (isWaitingAtStop && !canProceed) return;
        if (isWaitingAtStop && canProceed) isWaitingAtStop = false;

        Vector3 newPos = Vector3.MoveTowards(myPos, targetPos, speed * Time.deltaTime);
        Vector3 delta = newPos - myPos;

        // Move self
        transform.localPosition = newPos;

        // Move all linked parts by same delta
        if (linkedParts != null)
            foreach (var part in linkedParts)
                if (part != null)
                    part.localPosition += delta;

        // Next waypoint
        if (distance < stopDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                Vector3 loopDelta = waypoints[0].localPosition - transform.localPosition;
                transform.localPosition = waypoints[0].localPosition;
                if (linkedParts != null)
                    foreach (var part in linkedParts)
                        if (part != null)
                            part.localPosition += loopDelta;
                currentWaypointIndex = 0;
            }
        }
    }

    public void SetCanProceed(bool value)
    {
        canProceed = value;
    }
}
