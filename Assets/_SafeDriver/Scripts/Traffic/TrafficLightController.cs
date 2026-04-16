using System;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    public enum TrafficLightState { Red, Yellow, Green }

    /// <summary>
    /// State machine de un semaforo individual.
    /// Expone un evento local para observadores cercanos (InfractionDetector, NPCVehicleAI)
    /// que necesitan el contexto del semaforo especifico. Para eventos globales usar EventBus.
    /// </summary>
    public class TrafficLightController : MonoBehaviour
    {
        [Header("Config (segundos)")]
        [SerializeField] private float greenDuration  = 8f;
        [SerializeField] private float yellowDuration = 2f;
        [SerializeField] private float redDuration    = 6f;

        [SerializeField] private TrafficLightState state = TrafficLightState.Red;
        public TrafficLightState State => state;

        /// <summary>Callback local (no global). El subscriber recibe la instancia + nuevo estado.</summary>
        public event Action<TrafficLightController, TrafficLightState> StateChanged;

        // TODO: coroutine del ciclo; invocar StateChanged?.Invoke(this, state) en cada cambio
    }
}
