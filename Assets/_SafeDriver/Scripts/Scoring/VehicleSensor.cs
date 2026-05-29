using UnityEngine;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Base comun para todos los sensores que reaccionan al vehiculo del jugador.
    /// Unifica como se identifica al player (tag "PlayerVehicle" en el collider o
    /// en su Rigidbody, porque el rig del auto usa colliders compound) y el acceso
    /// al VehicleController. Hereda de InfractionDetector para reusar
    /// TriggerInfraction()/TriggerCorrectAction().
    /// </summary>
    public abstract class VehicleSensor : InfractionDetector
    {
        protected const string PlayerTag = "PlayerVehicle";

        /// <summary>True si el collider (o su Rigidbody) es el vehiculo del jugador.</summary>
        protected static bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (other.CompareTag(PlayerTag)) return true;
            var rb = other.attachedRigidbody;
            return rb != null && rb.CompareTag(PlayerTag);
        }

        protected static VehicleController Vehicle => VehicleController.Instance;
    }
}
