using System.Collections.Generic;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona de control invisible en senda peatonal.
    /// Agregar como componente a un GameObject con BoxCollider (isTrigger=true)
    /// cubriendo la senda peatonal completa.
    ///
    /// NPCPedestrianAI llama SetPedestriansPresent(true) cuando un peaton pisa la senda,
    /// y SetPedestriansPresent(false) cuando termina de cruzar.
    ///
    /// Flujo:
    ///   1. Peaton pisa senda → NPCPedestrianAI → SetPedestriansPresent(true)
    ///   2. Auto entra en la zona → OnTriggerEnter:
    ///      - Si hay peatones Y auto no frenó → TriggerInfraction()
    ///   3. Auto permanece en zona → OnTriggerStay:
    ///      - Si hay peatones Y auto detenido por >requiredStopDuration → premio
    ///   4. Auto sale → OnTriggerExit: reset flags
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PedestrianCrossingDetector : InfractionDetector, IPedestrianCrossingNotifier, IPedestrianCrossingMultiNotifier
    {
        [Header("Config")]
        [Tooltip("Segundos que el auto debe estar detenido para contar como 'cedió el paso'.")]
        public float requiredStopDuration = 0.8f;

        private bool pedestriansPresent;
        private float stoppedTime;
        private bool infractionFired;
        private bool rewardGiven;

        // Set de notifiers activos para soportar multiples peatones sin que se pisen el bool.
        // El SetPedestriansPresent(bool) clasico usa la id sentinel 0 (un solo notifier global).
        private readonly HashSet<int> activeNotifiers = new HashSet<int>();
        private const int LegacyNotifierId = 0;

        void Awake()
        {
            infractionType = InfractionType.PedestrianNotYielded;
            pedagogicalMessage =
                "Ley 24.449 Art. 41 — El peaton tiene prioridad absoluta en la "
                + "senda peatonal. Debes detenerte y esperar que crucen.";
        }

        /// <summary>
        /// Compat: llamado por notifiers de un solo agente (DemoPedestrianFaker, NPCPedestrianAI).
        /// Delega al sistema multi-notifier usando una id sentinel unica.
        /// </summary>
        public void SetPedestriansPresent(bool present)
            => NotifyByInstance(LegacyNotifierId, present);

        /// <summary>
        /// API multi-notifier: cada peaton se identifica con su instanceId. El estado
        /// `pedestriansPresent` es true mientras haya al menos un notifier activo.
        /// </summary>
        /// <summary>
        /// True si hay al menos un peaton cruzando ahora. Se consulta en el instante
        /// del cruce/entrada en vez de depender de un flanco temporal, asi el premio
        /// es robusto ante el timing de aparicion del peaton.
        /// </summary>
        public bool AnyPedestrianInCrossing() => activeNotifiers.Count > 0;

        public void NotifyByInstance(int notifierId, bool present)
        {
            if (present) activeNotifiers.Add(notifierId);
            else activeNotifiers.Remove(notifierId);

            bool wasPresent = pedestriansPresent;
            pedestriansPresent = activeNotifiers.Count > 0;

            // Reset al pasar de "sin peatones" a "con peatones"
            if (pedestriansPresent && !wasPresent)
            {
                stoppedTime = 0f;
                infractionFired = false;
                rewardGiven = false;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;

            if (pedestriansPresent
                && !infractionFired
                && VehicleController.Instance != null
                && !VehicleController.Instance.IsStopped())
            {
                // El auto entro a la senda con peatones SIN estar frenado.
                infractionFired = true;
                TriggerInfraction();
            }
            else if (!pedestriansPresent && !rewardGiven)
            {
                // No habia peatones cruzando — premio chico por pasar sin riesgo.
                rewardGiven = true;
                TriggerCorrectAction(ActionType.PedestrianNotPresent);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            if (rewardGiven || infractionFired) return;

            if (pedestriansPresent
                && VehicleController.Instance != null
                && VehicleController.Instance.IsStopped())
            {
                stoppedTime += Time.deltaTime;
                if (stoppedTime >= requiredStopDuration)
                {
                    rewardGiven = true;
                    TriggerCorrectAction(ActionType.YieldedToPedestrian);
                }
            }
            else
            {
                // Si el auto empezo a moverse, resetear el timer
                stoppedTime = 0f;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            stoppedTime = 0f;
        }
    }
}
