using UnityEngine;

public class ARTouchInteraction : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float tapCooldown = 0.3f;

    private float lastTapTime;

    private void Update()
    {
        if (Time.time - lastTapTime < tapCooldown) return;

        Vector2 screenPos;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
        }
        else
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            var trafficLight = hit.collider.GetComponentInParent<TrafficLightController>();
            if (trafficLight != null)
            {
                trafficLight.CycleState();
                lastTapTime = Time.time;
            }
        }
    }
}
