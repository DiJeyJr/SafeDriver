using System;
using System.Collections;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Controla el ciclo de un semaforo individual: Green -> Yellow -> Red -> repeat.
    /// Asigna materiales encendidos/apagados a los renderers de cada luz.
    /// Publica EventBus.Dispatch_TrafficLightChanged(gameObject, state) en cada transicion
    /// para que NPCs, detectores y UI puedan reaccionar.
    /// </summary>
    public class TrafficLightController : MonoBehaviour
    {
        [Header("Tiempos del ciclo (segundos)")]
        public float greenDuration  = 8f;
        public float yellowDuration = 2f;
        public float redDuration    = 6f;

        [Header("Renderers de luces (asignar en Inspector)")]
        public MeshRenderer greenLight;
        public MeshRenderer yellowLight;
        public MeshRenderer redLight;

        [Header("Materiales")]
        [Tooltip("Material emisivo para cada color cuando esta encendido.")]
        public Material litGreen;
        public Material litYellow;
        public Material litRed;

        [Tooltip("Material apagado compartido (gris oscuro).")]
        public Material dimMat;

        [Header("Config")]
        [Tooltip("Estado inicial del semaforo al arrancar.")]
        [SerializeField] private LightState startState = LightState.Green;

        [Tooltip("Offset en segundos para desincronizar semaforos (ej: el de la calle perpendicular arranca desfasado).")]
        [SerializeField] private float startDelay = 0f;

        private LightState currentState;

        // --- API publica ---
        public LightState CurrentState => currentState;
        public bool IsRed()    => currentState == LightState.Red;
        public bool IsYellow() => currentState == LightState.Yellow;
        public bool IsGreen()  => currentState == LightState.Green;

        /// <summary>Callback local para observers directos (NPCVehicleAI, etc.).</summary>
        public event Action<TrafficLightController, LightState> StateChanged;

        void Start()
        {
            SetState(startState);
            StartCoroutine(CycleRoutine());
        }

        private IEnumerator CycleRoutine()
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            while (true)
            {
                SetState(LightState.Green);
                yield return new WaitForSeconds(greenDuration);

                SetState(LightState.Yellow);
                yield return new WaitForSeconds(yellowDuration);

                SetState(LightState.Red);
                yield return new WaitForSeconds(redDuration);
            }
        }

        private void SetState(LightState s)
        {
            currentState = s;

            // Actualizar materiales de los renderers
            if (greenLight != null)
                greenLight.material  = s == LightState.Green  ? litGreen  : dimMat;
            if (yellowLight != null)
                yellowLight.material = s == LightState.Yellow ? litYellow : dimMat;
            if (redLight != null)
                redLight.material    = s == LightState.Red    ? litRed    : dimMat;

            // Publicar al bus global (para Scoring, UI, etc.)
            EventBus.Dispatch_TrafficLightChanged(gameObject, s);

            // Callback local directo (para NPCs cercanos que tienen referencia al semaforo)
            StateChanged?.Invoke(this, s);
        }
    }
}
