using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Define una secuencia de waypoints recorrida en loop por NPCs (autos, peatones).
    /// Los waypoints son children directos del transform — su orden en la jerarquia define
    /// el orden del recorrido. El path puede ser cerrado (vuelve del ultimo al primero) o
    /// abierto (rebota: 0..N → N..0).
    ///
    /// Uso: agregar este componente a un GameObject vacio y poner waypoints como children.
    /// Los NPCs (TrafficVehicle, TrafficPedestrian) leen `GetWaypoint(i)` para navegar.
    /// </summary>
    public class TrafficWaypointPath : MonoBehaviour
    {
        [Header("Modo de recorrido")]
        [Tooltip("Si esta activo, despues del ultimo waypoint vuelve al primero (loop cerrado). " +
                 "Si no, rebota: al llegar al ultimo invierte el sentido.")]
        public bool closedLoop = true;

        [Header("Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.2f, 0.9f);

        /// <summary>Cantidad de waypoints (children).</summary>
        public int Count => transform.childCount;

        /// <summary>Devuelve la posicion world del waypoint indicado, clampada al rango.</summary>
        public Vector3 GetPosition(int index)
        {
            if (Count == 0) return transform.position;
            int safe = Mathf.Clamp(index, 0, Count - 1);
            return transform.GetChild(safe).position;
        }

        /// <summary>
        /// Avanza el indice segun el modo de loop. Si `closedLoop` el indice se envuelve;
        /// si no, `direction` se invierte (rebote).
        /// </summary>
        public void Advance(ref int index, ref int direction)
        {
            if (Count == 0) { index = 0; direction = 1; return; }

            index += direction;
            if (closedLoop)
            {
                if (index >= Count) index = 0;
                if (index < 0) index = Count - 1;
            }
            else
            {
                if (index >= Count) { index = Count - 2; direction = -1; }
                if (index < 0)      { index = 1;         direction =  1; }
                index = Mathf.Clamp(index, 0, Count - 1);
            }
        }

        void OnDrawGizmos()
        {
            if (Count == 0) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < Count; i++)
            {
                Vector3 a = transform.GetChild(i).position;
                Gizmos.DrawSphere(a, 0.18f);
                int next = (i + 1) % Count;
                if (!closedLoop && next == 0) continue;
                Vector3 b = transform.GetChild(next).position;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
