using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    [Header("Crossing")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float speed = 0.8f;

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

        // Store offsets of linked parts relative to this transform
        if (linkedParts != null)
        {
            partOffsets = new Vector3[linkedParts.Length];
            for (int i = 0; i < linkedParts.Length; i++)
            {
                if (linkedParts[i] != null)
                    partOffsets[i] = linkedParts[i].localPosition - transform.localPosition;
            }
        }
    }

    private void Update()
    {
        if (!isCrossing)
        {
            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.localPosition = wpPosition + myOffset + Vector3.up * bob;

            if (canCross)
            {
                isCrossing = true;
                goingToEnd = true;
                transform.localPosition = wpPosition + myOffset;
            }
            return;
        }

        Transform target = goingToEnd ? endPoint : startPoint;
        if (target == null) return;

        Vector3 oldWp = wpPosition;
        wpPosition = Vector3.MoveTowards(wpPosition, target.localPosition, speed * Time.deltaTime);
        Vector3 delta = wpPosition - oldWp;

        transform.localPosition = wpPosition + myOffset;

        // Move linked parts by same delta
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
                // Arrived at end — if still allowed to cross, wait then return
                // If no longer allowed, stay and stop crossing
                if (!canCross)
                {
                    isCrossing = false;
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
                isCrossing = false;
            }
        }
    }

    public void SetCanCross(bool value)
    {
        canCross = value;
    }
}
