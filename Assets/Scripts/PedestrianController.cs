using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Crossing")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float speed = 0.3f;

    [Header("Idle Animation")]
    [SerializeField] private float bobAmplitude = 0.002f;
    [SerializeField] private float bobFrequency = 3f;

    private bool canCross;
    private bool isCrossing;
    private bool goingToEnd = true;
    private float waitTimer;
    private float waitDuration = 2f;
    private Vector3 basePosition;

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
        basePosition = transform.position;
    }

    private void Update()
    {
        if (!isCrossing)
        {
            // Idle bobbing
            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = basePosition + Vector3.up * bob;

            if (canCross)
            {
                isCrossing = true;
                goingToEnd = true;
            }
            return;
        }

        // Crossing
        Transform target = goingToEnd ? endPoint : startPoint;
        if (target == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position, target.position, speed * Time.deltaTime
        );

        // Look toward target
        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        // Arrived at target
        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            if (goingToEnd)
            {
                // Wait at the other side, then walk back
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitDuration)
                {
                    goingToEnd = false;
                    waitTimer = 0f;
                }
            }
            else
            {
                // Back at start
                isCrossing = false;
                basePosition = transform.position;
            }
        }
    }

    public void SetCanCross(bool value)
    {
        canCross = value;
    }
}
