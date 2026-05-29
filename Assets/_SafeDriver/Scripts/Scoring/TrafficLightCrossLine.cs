using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Traffic;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Linea de detencion del semaforo. Evalua el cruce: si el auto la atraviesa
    /// hacia adelante en rojo es infraccion (RanRedLight); en verde, premio
    /// (PassedGreenLight). En amarillo, configurable. Cruzar hacia atras (retroceso)
    /// no evalua.
    ///
    /// Colocar sobre la linea de detencion con un BoxCollider trigger; la normal de
    /// cruce (localForward) debe apuntar en el sentido de avance legal.
    /// </summary>
    public class TrafficLightCrossLine : CrossingLineSensor
    {
        [Header("Referencia")]
        [Tooltip("TrafficLightController de este semaforo.")]
        [SerializeField] private TrafficLightController trafficLight;

        [Header("Config")]
        [Tooltip("Si true, cruzar en amarillo tambien cuenta como infraccion.")]
        [SerializeField] private bool yellowIsInfraction = false;

        void Awake()
        {
            infractionType = InfractionType.RanRedLight;
            pedagogicalMessage =
                "Ley 24.449 Art. 43 — Cruzar con la luz roja es una infraccion grave.";
        }

        protected override void OnCrossed(bool forward, VehicleController vehicle)
        {
            if (!forward || trafficLight == null) return;

            if (trafficLight.IsRed())
            {
                infractionType = InfractionType.RanRedLight;
                TriggerInfraction();
            }
            else if (trafficLight.IsGreen())
            {
                TriggerCorrectAction(ActionType.PassedGreenLight);
            }
            else if (yellowIsInfraction)
            {
                infractionType = InfractionType.RanRedLight;
                TriggerInfraction();
            }
        }
    }
}
