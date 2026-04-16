using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// IA basica de vehiculo NPC: sigue waypoints, respeta semaforos, frena ante obstaculos.
    /// </summary>
    public class NPCVehicleAI : MonoBehaviour
    {
        [SerializeField] private float cruiseSpeedKmh = 35f;
        [SerializeField] private float stopDistance = 3f;
        [SerializeField] private Transform[] waypoints;

        // TODO: navigation + raycasts de deteccion de semaforo y obstaculos
    }
}
