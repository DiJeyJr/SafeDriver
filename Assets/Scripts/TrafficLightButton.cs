using UnityEngine;
using UnityEngine.UI;

public class TrafficLightButton : MonoBehaviour
{
    [SerializeField] private IntersectionManager intersectionManager;
    [SerializeField] private float cooldown = 3f;

    private Button button;
    private float lastPressTime = -999f;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnPress);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnPress);
    }

    private void Update()
    {
        if (button == null || intersectionManager == null) return;
        // Disable button during transition or cooldown
        button.interactable = !intersectionManager.IsTransitioning &&
                              Time.time - lastPressTime >= cooldown;
    }

    private void OnPress()
    {
        if (intersectionManager != null && !intersectionManager.IsTransitioning)
        {
            intersectionManager.ToggleLights();
            lastPressTime = Time.time;
        }
    }
}
