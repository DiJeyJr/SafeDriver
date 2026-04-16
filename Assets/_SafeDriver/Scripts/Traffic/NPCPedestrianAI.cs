using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// IA basica de peaton: esperar en esquina, cruzar cuando hay verde, reaccionar si un vehiculo se acerca.
    /// </summary>
    public class NPCPedestrianAI : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 1.4f; // m/s, velocidad normal
        [SerializeField] private Transform[] waypoints;

        // TODO: state machine Idle -> Waiting -> Crossing -> Walking
    }
}
