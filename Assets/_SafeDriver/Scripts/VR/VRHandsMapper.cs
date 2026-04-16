using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.VR
{
    /// <summary>
    /// Mapea las manos del Quest al volante y traduce el angulo de grab a input
    /// hacia VehicleInput. Tambien maneja el snap de las manos a las poses 9/3 del wheel.
    /// </summary>
    public class VRHandsMapper : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;
        [SerializeField] private Transform steeringWheel;
        [SerializeField] private VehicleInput vehicleInput;

        // TODO: detectar grab L/R, calcular angulo del volante, llamar vehicleInput.SetSteering
    }
}
