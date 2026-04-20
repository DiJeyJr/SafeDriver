using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

namespace SafeDriver.VR
{
    /// <summary>
    /// Grab manual del volante. Bypassa el sistema de overlap de ISDK porque el rig de
    /// Building Blocks "Controller and Hand" tiene los colliders del controller en
    /// GameObjects hermanos del Rigidbody, por lo que Collider.attachedRigidbody
    /// resuelve al Rigidbody del chasis y rompe la tabla de candidatos de ISDK.
    ///
    /// Este componente:
    ///   - Lee la posicion del WristPoint de cada HandGrabInteractor cada frame
    ///   - Si esta dentro del SphereCollider del volante Y se aprieta el grip → grab
    ///   - Rota el volante siguiendo el angulo de la mano alrededor del eje,
    ///     usando tracking incremental (delta por frame) para no saltar con el wrap
    ///     y respetar el tope del maximo angulo
    ///   - Al soltar grip, retorno suave al centro
    ///   - Empuja el steering normalizado a VehicleInput y EventBus
    ///   - Opcionalmente snappea el HandVisual al volante, o directamente lo oculta
    ///     mientras dura el grab si el snap no funciona con el rig
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public class SimpleWheelGrab : MonoBehaviour
    {
        [Header("Hitbox")]
        [Tooltip("SphereCollider que define la zona de grab. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private SphereCollider hitbox;

        [Header("Steering")]
        [SerializeField] private float maxSteeringAngle = 180f;
        [SerializeField] private float returnSpeed = 180f;
        [Tooltip("Eje local del volante alrededor del cual rota (0=X, 1=Y, 2=Z). Tipicamente 1 (Y).")]
        [SerializeField] private int rotationAxis = 1;
        [Tooltip("Invertir signo de rotacion si el volante gira al reves de la mano.")]
        [SerializeField] private bool invertRotation = false;

        [Header("Hand Visual")]
        [Tooltip("Snappear el HandVisual al punto donde se agarro el volante y seguir su rotacion.")]
        [SerializeField] private bool snapHandVisual = true;
        [Tooltip("Si el snap no puede pegar la mano al volante, ocultar el HandVisual mientras dura el grab (evita ver la mano desfasada).")]
        [SerializeField] private bool hideHandVisualOnGrab = false;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        private Quaternion originalLocalRotation;
        private float currentAngle;          // angulo aplicado al volante (clampeado)
        private float unclampedAngle;        // acumulador crudo de la mano (puede pasar el tope)

        // Soporte de grab con una o ambas manos a la vez. Cada registro tiene su propio
        // estado de tracking y snap, asi cada mano se pega a su propio punto del volante.
        private class GrabRecord
        {
            public HandGrabInteractor Interactor;
            public OVRInput.Controller Controller;
            public Vector3 LastHandDirLocal;
            public HandVisual SnappedHandVisual;
            public Vector3 SnappedRootLocalPos;
            public Quaternion SnappedRootLocalRot;
            public Vector3 SnappedGoLocalPos;
            public Quaternion SnappedGoLocalRot;
            public bool PreviousActive;
            public bool PreviousEnabled;
        }
        private readonly List<GrabRecord> grabs = new List<GrabRecord>();

        void Awake()
        {
            originalLocalRotation = transform.localRotation;
            if (hitbox == null) hitbox = GetComponent<SphereCollider>();
        }

        void OnEnable()
        {
            StartCoroutine(LateSnapRoutine());
        }

        void OnDisable()
        {
            for (int i = grabs.Count - 1; i >= 0; i--) UnsnapGrab(grabs[i]);
            grabs.Clear();
            StopAllCoroutines();
        }

        void Update()
        {
            TryStartGrab(); // permite sumar la otra mano aunque ya haya una agarrando
            if (grabs.Count > 0) UpdateGrabbed();
            else ReturnToCenter();
            PushSteering();
        }

        void LateUpdate()
        {
            ApplyHandSnap();
        }

        // Corrutina que corre DESPUES de todos los LateUpdate (incluido el HandVisual),
        // justo antes del render. Garantiza que el snap tenga la ultima palabra.
        private IEnumerator LateSnapRoutine()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitForEndOfFrame;
                ApplyHandSnap();
            }
        }

        private void ApplyHandSnap()
        {
            for (int i = 0; i < grabs.Count; i++)
            {
                var g = grabs[i];
                if (g.SnappedHandVisual == null) continue;
                g.SnappedHandVisual.transform.SetPositionAndRotation(
                    transform.TransformPoint(g.SnappedGoLocalPos),
                    transform.rotation * g.SnappedGoLocalRot);
                if (g.SnappedHandVisual.Root != null)
                {
                    g.SnappedHandVisual.Root.SetPositionAndRotation(
                        transform.TransformPoint(g.SnappedRootLocalPos),
                        transform.rotation * g.SnappedRootLocalRot);
                }
            }
        }

        private void TryStartGrab()
        {
            if (hitbox == null) return;

            var interactors = FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
            Vector3 worldCenter = WheelCenterWorld();
            float worldRadius = WorldRadius();
            float sqrR = worldRadius * worldRadius;

            foreach (var hgi in interactors)
            {
                if (hgi == null || hgi.WristPoint == null || hgi.Hand == null) continue;
                if (IsAlreadyGrabbing(hgi)) continue;

                Vector3 wristPos = hgi.WristPoint.position;
                if ((wristPos - worldCenter).sqrMagnitude > sqrR) continue;

                bool isLeft = hgi.Hand.Handedness == Handedness.Left;
                var rawBtn = isLeft ? OVRInput.RawButton.LHandTrigger : OVRInput.RawButton.RHandTrigger;
                if (!OVRInput.Get(rawBtn)) continue;

                // Evitar que la misma mano (izq o der) ocupe dos slots si hubiera varios
                // interactors por mano
                if (IsHandAlreadyGrabbing(hgi.Hand.Handedness)) continue;

                var record = new GrabRecord
                {
                    Interactor = hgi,
                    Controller = isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch,
                    LastHandDirLocal = HandDirInWheelLocalUnrotated(wristPos),
                };
                grabs.Add(record);

                // Solo la PRIMERA mano sincroniza el unclampedAngle con el angulo actual.
                // Las manos subsiguientes no resetean, para no romper el colchon del tope.
                if (grabs.Count == 1) unclampedAngle = currentAngle;

                if (snapHandVisual) TrySnapHandVisual(record);

                if (logDebug) Debug.Log($"[SimpleWheelGrab] Grabbed by {(isLeft ? "Left" : "Right")} hand (total={grabs.Count})");
            }
        }

        private bool IsAlreadyGrabbing(HandGrabInteractor hgi)
        {
            for (int i = 0; i < grabs.Count; i++) if (grabs[i].Interactor == hgi) return true;
            return false;
        }

        private bool IsHandAlreadyGrabbing(Handedness h)
        {
            for (int i = 0; i < grabs.Count; i++)
            {
                var g = grabs[i];
                if (g.Interactor != null && g.Interactor.Hand != null && g.Interactor.Hand.Handedness == h) return true;
            }
            return false;
        }

        private void UpdateGrabbed()
        {
            Vector3 axisInLocal = originalLocalRotation * AxisUnit();

            // Soltar las manos que ya no apretan el grip
            for (int i = grabs.Count - 1; i >= 0; i--)
            {
                var g = grabs[i];
                var rawBtn = g.Controller == OVRInput.Controller.LTouch
                    ? OVRInput.RawButton.LHandTrigger
                    : OVRInput.RawButton.RHandTrigger;
                if (!OVRInput.Get(rawBtn) || g.Interactor == null || g.Interactor.WristPoint == null)
                {
                    if (logDebug) Debug.Log($"[SimpleWheelGrab] Released hand (remaining={grabs.Count - 1})");
                    UnsnapGrab(g);
                    grabs.RemoveAt(i);
                }
            }

            if (grabs.Count == 0) return;

            // Promediar los deltas de todas las manos que siguen agarrando. Asi una mano
            // sola tiene control total, y con las dos manos el volante responde al
            // "consenso" del movimiento (no se duplica).
            float summedDelta = 0f;
            for (int i = 0; i < grabs.Count; i++)
            {
                var g = grabs[i];
                Vector3 wristPos = g.Interactor.WristPoint.position;
                Vector3 currentDirLocal = HandDirInWheelLocalUnrotated(wristPos);
                float frameDelta = Vector3.SignedAngle(g.LastHandDirLocal, currentDirLocal, axisInLocal);
                if (invertRotation) frameDelta = -frameDelta;
                summedDelta += frameDelta;
                g.LastHandDirLocal = currentDirLocal;
            }
            float avgDelta = summedDelta / grabs.Count;

            unclampedAngle += avgDelta;
            currentAngle = Mathf.Clamp(unclampedAngle, -maxSteeringAngle, maxSteeringAngle);

            if (logDebug && Mathf.Abs(unclampedAngle) > maxSteeringAngle - 0.1f)
            {
                Debug.Log($"[SimpleWheelGrab] TOPE unclamped={unclampedAngle:F1} current={currentAngle:F1} avgDelta={avgDelta:F2} hands={grabs.Count}");
            }

            ApplyAngle();
        }

        // Direccion mano relativa al centro del volante, en espacio del padre del volante,
        // SIN aplicar la rotacion del volante. Asi el vector solo cambia cuando la mano se
        // mueve, no cuando el volante rota por el propio grab.
        private Vector3 HandDirInWheelLocalUnrotated(Vector3 worldHand)
        {
            Transform parent = transform.parent != null ? transform.parent : transform;
            Vector3 local = parent.InverseTransformPoint(worldHand) - transform.localPosition;
            Vector3 axis = originalLocalRotation * AxisUnit();
            return Vector3.ProjectOnPlane(local, axis);
        }

        private Vector3 AxisUnit()
        {
            switch (rotationAxis)
            {
                case 0: return Vector3.right;
                case 2: return Vector3.forward;
                default: return Vector3.up;
            }
        }

        private Vector3 WheelCenterWorld()
        {
            return hitbox != null ? hitbox.transform.TransformPoint(hitbox.center) : transform.position;
        }

        private float WorldRadius()
        {
            return hitbox != null
                ? hitbox.radius * Mathf.Max(hitbox.transform.lossyScale.x, hitbox.transform.lossyScale.y, hitbox.transform.lossyScale.z)
                : 0.18f;
        }

        private void ApplyAngle()
        {
            // Visual invertido respecto al steering output (mesh espejado en el modelo).
            Vector3 euler = Vector3.zero;
            euler[rotationAxis] = -currentAngle;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(euler);
        }

        private void ReturnToCenter()
        {
            if (Mathf.Abs(currentAngle) < 0.5f)
            {
                currentAngle = 0f;
                unclampedAngle = 0f;
                transform.localRotation = originalLocalRotation;
                return;
            }
            currentAngle = Mathf.MoveTowards(currentAngle, 0f, returnSpeed * Time.deltaTime);
            unclampedAngle = currentAngle;
            ApplyAngle();
        }

        private void PushSteering()
        {
            float normalized = Mathf.Clamp(currentAngle / maxSteeringAngle, -1f, 1f);
            if (VehicleInput.Instance != null) VehicleInput.Instance.SetSteering(normalized);
            EventBus.Dispatch_SteeringChanged(normalized);
        }

        // ============================================================
        //   Hand Visual snap
        // ============================================================
        private void TrySnapHandVisual(GrabRecord record)
        {
            HandVisual hv = FindHandVisualForInteractor(record.Interactor);
            if (hv == null)
            {
                if (logDebug) Debug.LogWarning("[SimpleWheelGrab] No HandVisual found for interactor");
                return;
            }

            // Si esta mano ya estaba snappeada por otro grab (no deberia pero por las dudas)
            if (IsHandVisualAlreadySnapped(hv)) return;

            if (hideHandVisualOnGrab)
            {
                record.SnappedHandVisual = hv;
                record.PreviousActive = hv.gameObject.activeSelf;
                hv.gameObject.SetActive(false);
                if (logDebug) Debug.Log($"[SimpleWheelGrab] Hidden HandVisual '{hv.name}'");
                return;
            }

            if (hv.Root == null)
            {
                if (logDebug) Debug.LogWarning($"[SimpleWheelGrab] HandVisual '{hv.name}' has null Root");
                return;
            }

            record.SnappedHandVisual = hv;
            record.PreviousActive = hv.gameObject.activeSelf;
            record.PreviousEnabled = hv.enabled;

            record.SnappedGoLocalPos = transform.InverseTransformPoint(hv.transform.position);
            record.SnappedGoLocalRot = Quaternion.Inverse(transform.rotation) * hv.transform.rotation;
            record.SnappedRootLocalPos = transform.InverseTransformPoint(hv.Root.position);
            record.SnappedRootLocalRot = Quaternion.Inverse(transform.rotation) * hv.Root.rotation;

            hv.InjectOptionalUpdateRootPose(false);
            hv.enabled = false;
            if (logDebug) Debug.Log($"[SimpleWheelGrab] Snapped + disabled HandVisual '{hv.name}'");
        }

        private bool IsHandVisualAlreadySnapped(HandVisual hv)
        {
            for (int i = 0; i < grabs.Count; i++) if (grabs[i].SnappedHandVisual == hv) return true;
            return false;
        }

        private void UnsnapGrab(GrabRecord g)
        {
            if (g.SnappedHandVisual == null) return;
            if (hideHandVisualOnGrab)
            {
                g.SnappedHandVisual.gameObject.SetActive(g.PreviousActive);
            }
            else
            {
                g.SnappedHandVisual.enabled = g.PreviousEnabled;
                g.SnappedHandVisual.InjectOptionalUpdateRootPose(true);
            }
            g.SnappedHandVisual = null;
        }

        private static HandVisual FindHandVisualForInteractor(HandGrabInteractor hgi)
        {
            if (hgi == null || hgi.Hand == null) return null;
            var targetHand = hgi.Hand.Handedness;
            var allVisuals = FindObjectsByType<HandVisual>(FindObjectsSortMode.None);

            HandVisual best = null;
            int bestScore = int.MinValue;
            foreach (var hv in allVisuals)
            {
                if (hv == null) continue;
                string n = hv.gameObject.name;
                bool nameMatch = (targetHand == Handedness.Left && n.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                              || (targetHand == Handedness.Right && n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (!nameMatch) continue;

                // Preferimos el HandVisual de la mano VISIBLE (hijo directo de
                // OVRInteractionComprehensive) por sobre los "reticles" de distance-grab
                // que viven dentro de DistanceHandGrabInteractor/Visuals/...HandReticle.
                string path = BuildPath(hv.transform);
                int score = 0;
                if (path.IndexOf("Reticle", System.StringComparison.OrdinalIgnoreCase) >= 0) score -= 100;
                if (path.IndexOf("DistanceHandGrab", System.StringComparison.OrdinalIgnoreCase) >= 0) score -= 100;
                if (path.IndexOf("DistanceControllerHandGrab", System.StringComparison.OrdinalIgnoreCase) >= 0) score -= 100;
                if (path.IndexOf("Synthetic", System.StringComparison.OrdinalIgnoreCase) >= 0) score -= 50;
                // Los reticles estan mas profundos; preferimos los de jerarquia mas chata
                int depth = path.Split('/').Length;
                score -= depth;
                if (score > bestScore) { bestScore = score; best = hv; }
            }
            if (best != null) return best;

            // Fallback: subir por jerarquia
            Transform t = hgi.transform;
            while (t != null)
            {
                var hv = t.GetComponentInChildren<HandVisual>(true);
                if (hv != null) return hv;
                t = t.parent;
            }
            return null;
        }

        private static string BuildPath(Transform t)
        {
            if (t == null) return "";
            var sb = new System.Text.StringBuilder(t.name);
            var p = t.parent;
            while (p != null)
            {
                sb.Insert(0, "/");
                sb.Insert(0, p.name);
                p = p.parent;
            }
            return sb.ToString();
        }
    }
}
