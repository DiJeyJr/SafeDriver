using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARTouchInteraction : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float tapCooldown = 0.3f;

    private float lastTapTime;
    private IntersectionManager intersectionManager;

    private void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
        intersectionManager = FindFirstObjectByType<IntersectionManager>();
    }

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

        if (arCamera == null) return;

        Vector2 screenPos;

        // Touch input (mobile)
        if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            screenPos = Touch.activeTouches[0].screenPosition;
            // Un tap sobre un botón de UI no debe llegar al mundo 3D
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Touch.activeTouches[0].touchId))
                return;
        }
        // Mouse input (editor testing)
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
        }
        else
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            var trafficLight = hit.collider.GetComponentInParent<TrafficLightController>();
            if (trafficLight == null) return;

            // Pasar por el IntersectionManager mantiene sincronizados autos, peatones y las 4 luces;
            // CycleState suelto solo cambiaba una luz y dejaba el resto desfasado.
            if (intersectionManager != null)
            {
                if (intersectionManager.IsTransitioning) return;
                intersectionManager.ToggleLights();
            }
            else
            {
                trafficLight.CycleState();
            }
            lastTapTime = Time.time;
        }
    }
}
