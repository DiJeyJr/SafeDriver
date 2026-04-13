using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARTouchInteraction : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float tapCooldown = 0.3f;

    private float lastTapTime;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (Time.time - lastTapTime < tapCooldown) return;

        Vector2 screenPos;

        // Touch input (mobile)
        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            screenPos = Touch.activeTouches[0].screenPosition;
        }
        // Mouse input (editor testing)
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
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
