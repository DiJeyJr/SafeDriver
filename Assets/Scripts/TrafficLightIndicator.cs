using UnityEngine;
using UnityEngine.UI;

public class TrafficLightIndicator : MonoBehaviour
{
    [SerializeField] private TrafficLightController targetLight;
    [SerializeField] private Image indicatorImage;
    [SerializeField] private string directionLabel = "N";

    private static readonly Color ColorRed = new Color(0.9f, 0.15f, 0.15f);
    private static readonly Color ColorYellow = new Color(0.95f, 0.8f, 0.1f);
    private static readonly Color ColorGreen = new Color(0.15f, 0.85f, 0.25f);

    private void OnEnable()
    {
        if (targetLight != null)
            targetLight.OnStateChanged += OnLightChanged;
        UpdateVisual();
    }

    private void OnDisable()
    {
        if (targetLight != null)
            targetLight.OnStateChanged -= OnLightChanged;
    }

    private void OnLightChanged(TrafficLightController light, TrafficLightController.LightState state)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (indicatorImage == null || targetLight == null) return;

        indicatorImage.color = targetLight.CurrentState switch
        {
            TrafficLightController.LightState.Red => ColorRed,
            TrafficLightController.LightState.Yellow => ColorYellow,
            TrafficLightController.LightState.Green => ColorGreen,
            _ => ColorRed
        };
    }
}
