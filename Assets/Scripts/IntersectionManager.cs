using UnityEngine;

public class IntersectionManager : MonoBehaviour
{
    [Header("Traffic Lights")]
    [SerializeField] private TrafficLightController northLight;
    [SerializeField] private TrafficLightController southLight;
    [SerializeField] private TrafficLightController eastLight;
    [SerializeField] private TrafficLightController westLight;

    [Header("Vehicles")]
    [SerializeField] private CarController[] northSouthCars;
    [SerializeField] private CarController[] eastWestCars;

    [Header("Pedestrians")]
    [SerializeField] private PedestrianController[] northSouthPedestrians;
    [SerializeField] private PedestrianController[] eastWestPedestrians;

    [Header("Score")]
    [SerializeField] private ScoreManager scoreManager;

    private void OnEnable()
    {
        if (northLight) northLight.OnStateChanged += OnNorthSouthChanged;
        if (southLight) southLight.OnStateChanged += OnNorthSouthChanged;
        if (eastLight) eastLight.OnStateChanged += OnEastWestChanged;
        if (westLight) westLight.OnStateChanged += OnEastWestChanged;
    }

    private void OnDisable()
    {
        if (northLight) northLight.OnStateChanged -= OnNorthSouthChanged;
        if (southLight) southLight.OnStateChanged -= OnNorthSouthChanged;
        if (eastLight) eastLight.OnStateChanged -= OnEastWestChanged;
        if (westLight) westLight.OnStateChanged -= OnEastWestChanged;
    }

    private void OnNorthSouthChanged(TrafficLightController light, TrafficLightController.LightState state)
    {
        // Sync the other N/S light
        var other = light == northLight ? southLight : northLight;
        if (other != null) other.SetState(state);

        // Set opposing lights
        if (state == TrafficLightController.LightState.Green)
        {
            if (eastLight) eastLight.SetState(TrafficLightController.LightState.Red);
            if (westLight) westLight.SetState(TrafficLightController.LightState.Red);
            if (scoreManager) scoreManager.AddPoints(10);
        }
        else if (state == TrafficLightController.LightState.Red)
        {
            if (eastLight) eastLight.SetState(TrafficLightController.LightState.Green);
            if (westLight) westLight.SetState(TrafficLightController.LightState.Green);
        }

        UpdatePermissions();
    }

    private void OnEastWestChanged(TrafficLightController light, TrafficLightController.LightState state)
    {
        var other = light == eastLight ? westLight : eastLight;
        if (other != null) other.SetState(state);

        if (state == TrafficLightController.LightState.Green)
        {
            if (northLight) northLight.SetState(TrafficLightController.LightState.Red);
            if (southLight) southLight.SetState(TrafficLightController.LightState.Red);
            if (scoreManager) scoreManager.AddPoints(10);
        }
        else if (state == TrafficLightController.LightState.Red)
        {
            if (northLight) northLight.SetState(TrafficLightController.LightState.Green);
            if (southLight) southLight.SetState(TrafficLightController.LightState.Green);
        }

        UpdatePermissions();
    }

    private void UpdatePermissions()
    {
        bool nsGreen = northLight != null &&
                       northLight.CurrentState == TrafficLightController.LightState.Green;
        bool ewGreen = eastLight != null &&
                       eastLight.CurrentState == TrafficLightController.LightState.Green;

        // Cars go on green
        if (northSouthCars != null)
            foreach (var car in northSouthCars)
                car.SetCanProceed(nsGreen);

        if (eastWestCars != null)
            foreach (var car in eastWestCars)
                car.SetCanProceed(ewGreen);

        // Pedestrians cross when cars are stopped
        if (northSouthPedestrians != null)
            foreach (var ped in northSouthPedestrians)
                ped.SetCanCross(!nsGreen);

        if (eastWestPedestrians != null)
            foreach (var ped in eastWestPedestrians)
                ped.SetCanCross(!ewGreen);

        // Score for safe pedestrian crossing
        if (scoreManager != null && (!nsGreen || !ewGreen))
            scoreManager.AddPoints(5);
    }
}
