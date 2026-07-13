using System.Collections.Generic;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Ruta de checkpoints con flechas en el piso estilo juego de carreras.
    ///
    /// Armado (level designer): arrastrar el prefab CheckpointRoute a la escena y mover /
    /// duplicar sus hijos vacios a lo largo del recorrido — un checkpoint por esquina o
    /// giro (las flechas van EN LINEA RECTA entre checkpoints consecutivos). Nada mas:
    /// en Start la ruta agrega collider trigger + Checkpoint a cada hijo automaticamente.
    ///
    /// En juego: se muestran las flechas del tramo actual (del ultimo checkpoint pasado
    /// al siguiente) y, opcional, un cilindro translucido estilo GTA sobre el proximo
    /// checkpoint. Al pasar por un checkpoint, el guiado avanza al tramo siguiente.
    /// Al completar el ultimo, todo se oculta.
    /// </summary>
    public class CheckpointRoute : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Tag del vehiculo del jugador.")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        [Header("Flechas")]
        [Tooltip("Material de las flechas (unlit transparente con la textura de flecha).")]
        [SerializeField] private Material arrowMaterial;

        [Tooltip("Distancia entre flechas a lo largo del tramo, en metros.")]
        [SerializeField] private float arrowSpacing = 6f;

        [Tooltip("Tamano de cada flecha (metros).")]
        [SerializeField] private float arrowSize = 2.2f;

        [Tooltip("Altura sobre el piso para evitar z-fighting con el asfalto.")]
        [SerializeField] private float arrowHeight = 0.06f;

        [Header("Cilindro (estilo GTA, opcional)")]
        [Tooltip("Mostrar un cilindro translucido sobre el proximo checkpoint.")]
        [SerializeField] private bool showCylinder = true;

        [Tooltip("Material translucido del cilindro.")]
        [SerializeField] private Material cylinderMaterial;

        [Tooltip("Diametro y altura del cilindro.")]
        [SerializeField] private Vector2 cylinderSize = new Vector2(4f, 5f);

        [Header("Checkpoints (auto)")]
        [Tooltip("Radio del trigger que se agrega a cada checkpoint hijo que no tenga collider.")]
        [SerializeField] private float checkpointRadius = 5f;

        [Header("Meta")]
        [Tooltip("Al llegar al ultimo checkpoint, dispara la accion ReachedGoal (la cuenta la mision 'Llegar a la meta' del MissionKit del prefab).")]
        [SerializeField] private bool dispatchGoalAction = true;

        [Tooltip("Puntos de bonus al llegar a la meta.")]
        [SerializeField] private int goalPoints = 10;

        /// <summary>Tag del player (lo consulta cada Checkpoint hijo).</summary>
        public string PlayerTag => playerTag;

        private readonly List<Transform> checkpoints = new List<Transform>();
        private readonly List<GameObject> arrows = new List<GameObject>();
        private Transform arrowContainer;
        private GameObject cylinder;
        private int currentIndex; // proximo checkpoint a alcanzar

        void Start()
        {
            // Auto-wiring: cada hijo directo es un checkpoint. Le aseguramos collider
            // trigger + componente Checkpoint, en el orden de la jerarquia.
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == arrowContainer) continue;

                var col = child.GetComponent<Collider>();
                if (col == null)
                {
                    var sphere = child.gameObject.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = checkpointRadius;
                }
                else col.isTrigger = true;

                var cp = child.GetComponent<Checkpoint>();
                if (cp == null) cp = child.gameObject.AddComponent<Checkpoint>();
                cp.route = this;
                cp.index = checkpoints.Count;
                checkpoints.Add(child);
            }

            if (checkpoints.Count < 2)
            {
                Debug.LogWarning("[CheckpointRoute] Hacen falta al menos 2 checkpoints (hijos).", this);
                enabled = false;
                return;
            }

            var containerGo = new GameObject("_Arrows");
            containerGo.transform.SetParent(transform, false);
            arrowContainer = containerGo.transform;

            if (showCylinder && cylinderMaterial != null)
            {
                cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = "_Cylinder";
                Destroy(cylinder.GetComponent<Collider>());
                cylinder.transform.SetParent(transform, false);
                cylinder.transform.localScale = new Vector3(cylinderSize.x, cylinderSize.y * 0.5f, cylinderSize.x);
                cylinder.GetComponent<MeshRenderer>().sharedMaterial = cylinderMaterial;
            }

            currentIndex = 1; // el 0 es el punto de partida; guiamos hacia el 1
            RebuildSegment();
        }

        /// <summary>Un checkpoint avisa que el player lo alcanzo.</summary>
        public void NotifyReached(int index)
        {
            // Racing-style laxo: alcanzar cualquier checkpoint igual o posterior al
            // esperado avanza el guiado (si el jugador corta camino, no se traba).
            if (index < currentIndex) return;

            currentIndex = index + 1;
            if (currentIndex >= checkpoints.Count)
            {
                // Ruta completa: ocultar todo y avisar la llegada a la meta.
                ClearArrows();
                if (cylinder != null) cylinder.SetActive(false);
                if (dispatchGoalAction && enabled)
                    EventBus.Dispatch_CorrectAction(ActionType.ReachedGoal, goalPoints);
                enabled = false;
                return;
            }
            RebuildSegment();
        }

        // Regenera las flechas del tramo actual (checkpoint anterior -> proximo) y mueve el cilindro.
        private void RebuildSegment()
        {
            ClearArrows();

            Vector3 from = checkpoints[currentIndex - 1].position;
            Vector3 to = checkpoints[currentIndex].position;
            Vector3 dir = to - from;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.5f) return;
            dir /= dist;

            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(90f, yaw, 0f); // quad plano en el piso, apuntando al proximo

            // Flechas desde un poco despues del origen hasta un poco antes del destino.
            for (float d = arrowSpacing * 0.5f; d < dist - arrowSpacing * 0.25f; d += arrowSpacing)
            {
                Vector3 pos = from + dir * d;
                pos.y = Mathf.Max(from.y, to.y) + arrowHeight;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Arrow";
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(arrowContainer, false);
                quad.transform.SetPositionAndRotation(pos, rot);
                quad.transform.localScale = Vector3.one * arrowSize;
                if (arrowMaterial != null)
                    quad.GetComponent<MeshRenderer>().sharedMaterial = arrowMaterial;
                arrows.Add(quad);
            }

            if (cylinder != null)
            {
                cylinder.SetActive(true);
                Vector3 cpos = to;
                cpos.y += cylinderSize.y * 0.5f;
                cylinder.transform.position = cpos;
            }
        }

        private void ClearArrows()
        {
            foreach (var a in arrows) if (a != null) Destroy(a);
            arrows.Clear();
        }

        // Guia visual en el editor para ubicar los checkpoints sin entrar a Play.
        void OnDrawGizmos()
        {
            Transform prev = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("_")) continue;

                Gizmos.color = new Color(0.29f, 0.76f, 0.38f, 0.9f);
                Gizmos.DrawWireSphere(child.position, checkpointRadius);
                if (prev != null)
                {
                    Gizmos.color = new Color(0.18f, 0.56f, 0.9f, 0.9f);
                    Gizmos.DrawLine(prev.position + Vector3.up * 0.5f, child.position + Vector3.up * 0.5f);
                }
                prev = child;
            }
        }
    }
}
