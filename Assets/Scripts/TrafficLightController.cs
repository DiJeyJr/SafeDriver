using UnityEngine;
using System;

public class TrafficLightController : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green }

    [Header("Light Renderers")]
    [SerializeField] private Renderer redLight;
    [SerializeField] private Renderer yellowLight;
    [SerializeField] private Renderer greenLight;

    [Header("Materials")]
    [SerializeField] private Material matRedOn;
    [SerializeField] private Material matRedOff;
    [SerializeField] private Material matYellowOn;
    [SerializeField] private Material matYellowOff;
    [SerializeField] private Material matGreenOn;
    [SerializeField] private Material matGreenOff;

    [Header("State")]
    [SerializeField] private LightState currentState = LightState.Red;

    public event Action<TrafficLightController, LightState> OnStateChanged;

    public LightState CurrentState => currentState;

    private void Start()
    {
        ApplyState();
    }

    public void CycleState()
    {
        currentState = currentState switch
        {
            LightState.Red => LightState.Green,
            LightState.Green => LightState.Yellow,
            LightState.Yellow => LightState.Red,
            _ => LightState.Red
        };
        ApplyState();
        OnStateChanged?.Invoke(this, currentState);
    }

    public void SetState(LightState state)
    {
        if (currentState == state) return;
        currentState = state;
        ApplyState();
        OnStateChanged?.Invoke(this, currentState);
    }

    private void ApplyState()
    {
        if (redLight) redLight.material = currentState == LightState.Red ? matRedOn : matRedOff;
        if (yellowLight) yellowLight.material = currentState == LightState.Yellow ? matYellowOn : matYellowOff;
        if (greenLight) greenLight.material = currentState == LightState.Green ? matGreenOn : matGreenOff;
    }
}
