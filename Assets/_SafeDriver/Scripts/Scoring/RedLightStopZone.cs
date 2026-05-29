using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Traffic;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona de aproximacion ANTES de la linea del semaforo. Premia detenerse en
    /// rojo; nunca dispara infraccion (de eso se encarga TrafficLightCrossLine al
    /// cruzar la linea). Separar ambos resuelve el bug donde aproximarse en rojo
    /// marcaba infraccion y bloqueaba el premio de frenar.
    ///
    /// Colocar con un BoxCollider trigger cubriendo el tramo previo a la linea de
    /// detencion. Asignar el TrafficLightController de la interseccion.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RedLightStopZone : VehicleSensor
    {
        [Header("Referencia")]
        [Tooltip("TrafficLightController de este semaforo.")]
        [SerializeField] private TrafficLightController trafficLight;

        [Header("Config")]
        [Tooltip("Segundos detenido en rojo antes de dar el premio.")]
        [SerializeField] private float minStopTimeForReward = 0.5f;

        private float stoppedTime;
        private bool rewardGiven;
        private bool inZone;

        void Awake()
        {
            infractionType = InfractionType.RanRedLight; // sin uso real: esta zona no penaliza
            pedagogicalMessage =
                "Ley 24.449 Art. 43 — Detenete por completo ante la luz roja y espera la verde.";
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            inZone = true;
            stoppedTime = 0f;
            rewardGiven = false;
        }

        void OnTriggerStay(Collider other)
        {
            if (!inZone || rewardGiven || !IsPlayer(other)) return;
            if (trafficLight == null || Vehicle == null) return;
            if (!trafficLight.IsRed()) { stoppedTime = 0f; return; }

            if (Vehicle.IsStopped())
            {
                stoppedTime += Time.deltaTime;
                if (stoppedTime >= minStopTimeForReward)
                {
                    rewardGiven = true;
                    TriggerCorrectAction(ActionType.StoppedAtRedLight);
                }
            }
            else
            {
                stoppedTime = 0f;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            inZone = false;
            stoppedTime = 0f;
        }
    }
}
