using UnityEngine;
using Oculus.Interaction;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Palanca de luces direccionales (3 posiciones). Lee el angulo local X del pivot:
    ///   arriba (angulo > zoneHalfRange)  -> guiño derecho
    ///   centro (|angulo| <= zoneHalfRange) -> apagado
    ///   abajo (angulo < -zoneHalfRange)  -> guiño izquierdo
    /// Snap a la posicion mas cercana al soltar; queda accionada hasta volverla al centro.
    /// Clonada del SteeringWheel (mismo stack de grab) — ver Editor/SetupTurnSignals.cs.
    /// </summary>
    public class TurnSignalStalk : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TurnSignalController controller;
        [SerializeField] private Transform pivot;
        [SerializeField] private Grabbable grabbable;

        [Header("Zonas (grados, eje X local)")]
        [Tooltip("Medio ancho de la zona central (apagado). Fuera de ±esto entra a izquierda/derecha.")]
        [SerializeField] private float zoneHalfRange = 12f;
        [Tooltip("Angulo objetivo al snapear a izquierda (-) o derecha (+).")]
        [SerializeField] private float snapAngle = 25f;

        [Header("Snap on release")]
        [SerializeField] private bool snapOnRelease = true;
        [SerializeField] private float snapLerpSeconds = 0.15f;

        private TurnSignal lastState = TurnSignal.Off;
        private Vector3 originalLocalPosition;
        private bool snapping;
        private float snapStartX, snapTargetX, snapStartTime;
        private bool wasGrabbing;
        private OneGrabRotateTransformer _transformer;

        void Awake()
        {
            originalLocalPosition = transform.localPosition;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            _transformer = GetComponent<OneGrabRotateTransformer>();
        }

        void Start()
        {
            if (controller == null) controller = FindFirstObjectByType<TurnSignalController>();
            if (pivot == null) pivot = transform;
            Evaluate(force: true);
        }

        void Update()
        {
            bool grabbing = grabbable != null && grabbable.SelectingPointsCount > 0;
            HandleSnap(grabbing);
            Evaluate(force: false);
        }

        void LateUpdate()
        {
            if (transform.localPosition != originalLocalPosition)
                transform.localPosition = originalLocalPosition;
        }

        private TurnSignal ZoneFor(float a)
        {
            if (a >  zoneHalfRange) return TurnSignal.Right;
            if (a < -zoneHalfRange) return TurnSignal.Left;
            return TurnSignal.Off;
        }

        private void Evaluate(bool force)
        {
            float a = NormalizeAngle(pivot.localEulerAngles.x);
            var s = ZoneFor(a);
            if (!force && s == lastState) return;
            lastState = s;
            if (controller != null) controller.SetSignal(s);
        }

        private void HandleSnap(bool grabbing)
        {
            if (!grabbing && wasGrabbing && snapOnRelease)
            {
                float cur = NormalizeAngle(pivot.localEulerAngles.x);
                var z = ZoneFor(cur);
                snapStartX = cur;
                snapTargetX = z == TurnSignal.Right ? snapAngle : z == TurnSignal.Left ? -snapAngle : 0f;
                snapStartTime = Time.unscaledTime;
                snapping = true;
            }
            wasGrabbing = grabbing;

            if (grabbing) { snapping = false; return; }
            if (!snapping) return;

            float t = snapLerpSeconds > 0f ? (Time.unscaledTime - snapStartTime) / snapLerpSeconds : 1f;
            t = Mathf.Clamp01(t);
            SetLocalRotationX(Mathf.LerpAngle(snapStartX, snapTargetX, t));
            if (t >= 1f) snapping = false;
        }

        private void SetLocalRotationX(float x)
        {
            var e = pivot.localEulerAngles; e.x = x; pivot.localEulerAngles = e;
            SyncTransformerAngle(x);
        }

        private static System.Reflection.FieldInfo s_rel, s_con;
        private void SyncTransformerAngle(float x)
        {
            if (_transformer == null) return;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            if (s_rel == null)
            {
                s_rel = typeof(OneGrabRotateTransformer).GetField("_relativeAngle", F);
                s_con = typeof(OneGrabRotateTransformer).GetField("_constrainedRelativeAngle", F);
            }
            if (s_rel != null) s_rel.SetValue(_transformer, x);
            if (s_con != null) s_con.SetValue(_transformer, x);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f; if (a > 180f) a -= 360f; if (a <= -180f) a += 360f; return a;
        }
    }
}
