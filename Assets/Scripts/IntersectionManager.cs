using UnityEngine;
using System.Collections;

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

    [Header("Transition")]
    [SerializeField] private float yellowDuration = 2.5f;

    private bool nsIsGreen = false;
    private bool isTransitioning = false;

    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        // Initial state: all red
        SetLights(northLight, southLight, TrafficLightController.LightState.Red);
        SetLights(eastLight, westLight, TrafficLightController.LightState.Red);
        UpdatePermissions();
    }

    /// <summary>
    /// Called by the UI button. Starts the transition sequence.
    /// </summary>
    public void ToggleLights()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        isTransitioning = true;

        if (nsIsGreen)
        {
            // N/S green → yellow → red, then E/W → green
            SetLights(northLight, southLight, TrafficLightController.LightState.Yellow);
            UpdatePermissions();

            yield return new WaitForSeconds(yellowDuration);

            SetLights(northLight, southLight, TrafficLightController.LightState.Red);
            SetLights(eastLight, westLight, TrafficLightController.LightState.Green);
            nsIsGreen = false;
        }
        else
        {
            // E/W green → yellow → red, then N/S → green
            // (first time: all red → just go green)
            if (eastLight.CurrentState == TrafficLightController.LightState.Green)
            {
                SetLights(eastLight, westLight, TrafficLightController.LightState.Yellow);
                UpdatePermissions();

                yield return new WaitForSeconds(yellowDuration);

                SetLights(eastLight, westLight, TrafficLightController.LightState.Red);
            }

            SetLights(northLight, southLight, TrafficLightController.LightState.Green);
            nsIsGreen = true;
        }

        UpdatePermissions();

        isTransitioning = false;
    }

    private void SetLights(TrafficLightController a, TrafficLightController b, TrafficLightController.LightState state)
    {
        if (a) a.SetState(state);
        if (b) b.SetState(state);
    }

    private void UpdatePermissions()
    {
        bool nsGreen = northLight != null &&
                       northLight.CurrentState == TrafficLightController.LightState.Green;
        bool ewGreen = eastLight != null &&
                       eastLight.CurrentState == TrafficLightController.LightState.Green;

        if (northSouthCars != null)
            foreach (var car in northSouthCars)
                car.SetCanProceed(nsGreen);

        if (eastWestCars != null)
            foreach (var car in eastWestCars)
                car.SetCanProceed(ewGreen);

        if (northSouthPedestrians != null)
            foreach (var ped in northSouthPedestrians)
                ped.SetCanCross(!nsGreen);

        if (eastWestPedestrians != null)
            foreach (var ped in eastWestPedestrians)
                ped.SetCanCross(!ewGreen);
    }
}
