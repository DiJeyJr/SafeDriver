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

    [Header("Right Turn (optional)")]
    [SerializeField] private Transform[] rightTurnWaypoints;
    [SerializeField, Range(0f, 1f)] private float rightTurnChance = 0.4f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 540f;

    private int currentWaypointIndex;
    private bool canProceed;
    private bool isWaitingAtStop;

    private Transform[] currentPath;
    private Quaternion initialRotationOffset = Quaternion.identity;

    public bool CanProceed => canProceed;

    private void Start()
    {
        if (linkedParts != null)
        {
            foreach (var part in linkedParts)
                if (part != null) part.SetParent(transform, true);
        }

        currentPath = waypoints;
        ComputeInitialRotationOffset();
        DecideNextPath();
    }

    private void ComputeInitialRotationOffset()
    {
        if (waypoints == null || waypoints.Length < 2 ||
            waypoints[0] == null || waypoints[1] == null) return;

        var initialDir = (waypoints[1].localPosition - waypoints[0].localPosition).normalized;
        if (initialDir.sqrMagnitude > 0.001f)
            initialRotationOffset = Quaternion.Inverse(Quaternion.LookRotation(initialDir)) * transform.localRotation;
    }

    private void Update()
    {
        if (currentPath == null || currentPath.Length == 0) return;

        Vector3 targetPos = currentPath[currentWaypointIndex].localPosition;
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
        transform.localPosition = newPos;

        if (delta.sqrMagnitude > 0.000001f)
        {
            var moveDir = delta.normalized;
            var targetRot = Quaternion.LookRotation(moveDir) * initialRotationOffset;
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (distance < stopDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Length)
            {
                transform.localPosition = waypoints[0].localPosition;

                if (waypoints.Length > 1)
                {
                    var initDir = (waypoints[1].localPosition - waypoints[0].localPosition).normalized;
                    if (initDir.sqrMagnitude > 0.001f)
                        transform.localRotation = Quaternion.LookRotation(initDir) * initialRotationOffset;
                }

                currentWaypointIndex = 0;
                DecideNextPath();
            }
        }
    }

    private void DecideNextPath()
    {
        if (rightTurnWaypoints != null && rightTurnWaypoints.Length >= 2 && Random.value < rightTurnChance)
            currentPath = rightTurnWaypoints;
        else
            currentPath = waypoints;
    }

    public void SetCanProceed(bool value)
    {
        canProceed = value;
    }
}
