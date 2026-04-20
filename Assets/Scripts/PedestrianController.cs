using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Crossing")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private float rushSpeed = 2.2f;

    [Header("Sidewalk Pacing")]
    [SerializeField] private float paceRadius = 0.25f;
    [SerializeField] private float paceSpeed = 0.35f;
    [SerializeField] private float paceWaitMin = 0.8f;
    [SerializeField] private float paceWaitMax = 2.5f;
    [SerializeField] private Transform[] farPacePoints;
    [SerializeField, Range(0f, 1f)] private float crossChance = 0.65f;

    [Header("Idle Animation")]
    [SerializeField] private float bobAmplitude = 0.01f;
    [SerializeField] private float bobFrequency = 3f;

    [Header("Group - all transforms to move together")]
    [SerializeField] private Transform[] linkedParts;

    private bool canCross;
    private bool isCrossing;
    private bool goingToEnd = true;
    private float waitTimer;
    private float waitDuration = 2f;

    private Vector3 wpPosition;
    private Vector3 myOffset;
    private Vector3[] partOffsets;

    private Vector3 paceTarget;
    private float paceTimer;
    private bool paused;
    private bool returningToStart;

    private void Start()
    {
        if (startPoint != null)
        {
            wpPosition = startPoint.localPosition;
            myOffset = transform.localPosition - wpPosition;
        }
        else
        {
            wpPosition = transform.localPosition;
            myOffset = Vector3.zero;
        }

        if (linkedParts != null)
        {
            partOffsets = new Vector3[linkedParts.Length];
            for (int i = 0; i < linkedParts.Length; i++)
            {
                if (linkedParts[i] != null)
                    partOffsets[i] = linkedParts[i].localPosition - transform.localPosition;
            }
        }

        paceTarget = wpPosition;
        paused = true;
        paceTimer = Random.Range(0f, paceWaitMax);
    }

    private void Update()
    {
        if (!isCrossing)
        {
            UpdateIdleOrPace();
            return;
        }

        Transform target = goingToEnd ? endPoint : startPoint;
        if (target == null) return;

        float currentSpeed = canCross ? speed : rushSpeed;

        Vector3 oldWp = wpPosition;
        wpPosition = Vector3.MoveTowards(wpPosition, target.localPosition, currentSpeed * Time.deltaTime);
        Vector3 delta = wpPosition - oldWp;

        transform.localPosition = wpPosition + myOffset;

        if (linkedParts != null)
        {
            for (int i = 0; i < linkedParts.Length; i++)
            {
                if (linkedParts[i] != null)
                    linkedParts[i].localPosition += delta;
            }
        }

        if (Vector3.Distance(wpPosition, target.localPosition) < 0.2f)
        {
            if (goingToEnd)
            {
                if (!canCross)
                {
                    EnterIdle();
                    return;
                }
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitDuration)
                {
                    goingToEnd = false;
                    waitTimer = 0f;
                }
            }
            else
            {
                EnterIdle();
            }
        }
    }

    private void EnterIdle()
    {
        isCrossing = false;
        returningToStart = false;
        paceTarget = wpPosition;
        paused = true;
        paceTimer = Random.Range(paceWaitMin, paceWaitMax);
    }

    private void UpdateIdleOrPace()
    {
        if (startPoint == null) return;

        if (paused)
        {
            paceTimer -= Time.deltaTime;
            if (paceTimer <= 0f)
            {
                paused = false;
                PickNewPaceTarget();
            }
        }
        else
        {
            float distance = Vector3.Distance(wpPosition, paceTarget);
            if (distance > 0.05f)
            {
                Vector3 oldWp = wpPosition;
                wpPosition = Vector3.MoveTowards(wpPosition, paceTarget, paceSpeed * Time.deltaTime);
                Vector3 delta = wpPosition - oldWp;

                if (linkedParts != null)
                {
                    for (int i = 0; i < linkedParts.Length; i++)
                    {
                        if (linkedParts[i] != null)
                            linkedParts[i].localPosition += delta;
                    }
                }
            }
            else
            {
                if (returningToStart)
                {
                    returningToStart = false;
                    if (canCross)
                    {
                        isCrossing = true;
                        goingToEnd = IsOnStartSide();
                        transform.localPosition = wpPosition + myOffset;
                        return;
                    }
                }
                paused = true;
                paceTimer = Random.Range(paceWaitMin, paceWaitMax);
            }
        }

        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = wpPosition + myOffset + Vector3.up * bob;
    }

    private bool IsOnStartSide()
    {
        if (startPoint == null || endPoint == null) return true;
        return Vector3.Distance(wpPosition, startPoint.localPosition)
             < Vector3.Distance(wpPosition, endPoint.localPosition);
    }

    private void PickNewPaceTarget()
    {
        bool onStart = IsOnStartSide();

        // When the light allows crossing, roll a dice to decide: cross or keep pacing
        if (canCross && Random.value < crossChance)
        {
            Transform anchor = onStart ? startPoint : endPoint;
            if (anchor != null)
            {
                paceTarget = anchor.localPosition;
                returningToStart = true;
                return;
            }
        }

        // Pace around the current side only — never cross the street while pacing
        Vector3 sideAnchor;
        Transform[] localFars;
        if (onStart)
        {
            sideAnchor = startPoint != null ? startPoint.localPosition : wpPosition;
            localFars = farPacePoints;
        }
        else
        {
            sideAnchor = endPoint != null ? endPoint.localPosition : wpPosition;
            localFars = null;
        }

        int anchorCount = 1;
        if (localFars != null)
        {
            for (int i = 0; i < localFars.Length; i++)
                if (localFars[i] != null) anchorCount++;
        }

        int pick = Random.Range(0, anchorCount);
        Vector3 basePos = sideAnchor;
        if (pick > 0 && localFars != null)
        {
            int idx = 0;
            for (int i = 0; i < localFars.Length; i++)
            {
                if (localFars[i] == null) continue;
                idx++;
                if (idx == pick) { basePos = localFars[i].localPosition; break; }
            }
        }

        Vector2 rnd = Random.insideUnitCircle * paceRadius;
        paceTarget = basePos + new Vector3(rnd.x, 0f, rnd.y);
    }

    public void SetCanCross(bool value)
    {
        canCross = value;
    }
}
