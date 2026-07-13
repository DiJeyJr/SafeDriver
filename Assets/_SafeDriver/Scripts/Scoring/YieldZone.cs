using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona de espera ANTES de la senda peatonal: premia ceder el paso cuando el auto se
    /// detiene en el area de aproximacion mientras un peaton cruza. Complementa al
    /// PedestrianCrossingDetector (que evalua sobre la cebra misma, donde el auto que
    /// cede nunca esta parado).
    ///
    /// Va en un hijo del kit del peaton con BoxCollider trigger cubriendo los metros
    /// previos a la cebra. Un premio por episodio de cruce (se rearma cuando el peaton
    /// termina de cruzar).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class YieldZone : MonoBehaviour
    {
        [Tooltip("Detector de la senda (dice si hay un peaton cruzando ahora).")]
        [SerializeField] private PedestrianCrossingDetector detector;

        [Tooltip("Segundos detenido para contar como 'cedio el paso'.")]
        [SerializeField] private float requiredStopSeconds = 0.8f;

        [Tooltip("Puntos del premio (YieldedToPedestrian).")]
        [SerializeField] private int points = 15;

        private bool playerInside;
        private float stoppedTime;
        private bool rewardedThisEpisode;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("PlayerVehicle")) playerInside = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("PlayerVehicle")) { playerInside = false; stoppedTime = 0f; }
        }

        void Update()
        {
            if (detector == null) return;

            // El episodio termina cuando no queda nadie cruzando: rearmar el premio.
            if (!detector.AnyPedestrianInCrossing())
            {
                rewardedThisEpisode = false;
                stoppedTime = 0f;
                return;
            }

            if (!playerInside || rewardedThisEpisode) return;
            if (VehicleController.Instance == null) return;

            if (VehicleController.Instance.IsStopped())
            {
                stoppedTime += Time.deltaTime;
                if (stoppedTime >= requiredStopSeconds)
                {
                    rewardedThisEpisode = true;
                    EventBus.Dispatch_CorrectAction(ActionType.YieldedToPedestrian, points);
                }
            }
            else
            {
                stoppedTime = 0f;
            }
        }
    }
}
