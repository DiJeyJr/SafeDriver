using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using SafeDriver.Core;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Palanca de cambios D/N/R. Lee el angulo local X del transform y lo mapea a un GearState,
    /// notificando a `VehicleController.SetGearFromShifter`.
    ///
    /// Zonas:
    ///   Reverse: angulo < -neutralHalfRange   (centro al snap = -snapAngle)
    ///   Neutral: |angulo| <= neutralHalfRange (centro = 0)
    ///   Drive:   angulo > +neutralHalfRange   (centro al snap = +snapAngle)
    ///
    /// Comportamiento:
    ///   - Snap on release: al soltar la palanca se recentra al medio de la zona actual (lerp).
    ///   - Lock-when-moving: si el auto NO esta detenido, modificamos los CONSTRAINTS del
    ///     OneGrabRotateTransformer dinamicamente al rango de la zona en la que arranco el auto.
    ///     El transformer respeta el limite naturalmente; cuando el usuario empuja contra el
    ///     limite, el `GrabLimitFeedback` adjunto detecta el overshoot (relative - constrained)
    ///     y dispara los tiers de vibracion y, eventualmente, el release del grab.
    ///   - Pulso breve al cambiar de zona D <-> N <-> R.
    /// </summary>
    public class GearShifter : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("VehicleController al que se le setean los gears. Si queda vacio, se busca el de la escena en Start.")]
        [SerializeField] private VehicleController vehicle;

        [Tooltip("Transform cuya rotacion X local define el gear. Tipicamente el pivot de la palanca.")]
        [SerializeField] private Transform pivot;

        [Tooltip("Grabbable de la palanca. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private Grabbable grabbable;

        [Header("Config de zonas (grados sobre eje X local)")]
        [Tooltip("Medio ancho de la zona Neutral. Fuera de ±este valor entra a D o R.")]
        [SerializeField] private float neutralHalfRange = 20f;

        [Tooltip("Margen de histeresis para no alternar zonas cuando el angulo jiterea cerca del borde.")]
        [SerializeField] private float zoneHysteresis = 2f;

        [Tooltip("Margen entre el constraint del lock y la frontera de la zona — evita que la palanca clampada caiga justo en el borde y haga flicker D/N.")]
        [SerializeField] private float lockMargin = 1.5f;

        [Tooltip("Angulo objetivo (grados) al snapear a D (positivo) o R (negativo).")]
        [SerializeField] private float snapAngle = 40f;

        [Header("Snap on release")]
        [Tooltip("Si esta activo, al soltar la palanca se recentra al medio de la zona actual.")]
        [SerializeField] private bool snapOnRelease = true;

        [Tooltip("Segundos para completar el snap suavemente.")]
        [SerializeField] private float snapLerpSeconds = 0.18f;

        [Header("Lock when moving")]
        [Tooltip("Si el auto se mueve, fuerza la palanca a quedarse dentro de la zona donde arranco.")]
        [SerializeField] private bool lockWhenMoving = true;

        [Header("Haptic de cambio de marcha")]
        [Range(0f, 1f)] [SerializeField] private float shiftHapticAmplitude = 0.6f;
        [SerializeField] private float shiftHapticDuration = 0.07f;

        private GearState lastGear = GearState.Neutral;
        private Vector3 originalLocalPosition;

        private bool snapping;
        private float snapStartX;
        private float snapTargetX;
        private float snapStartTime;
        private bool wasGrabbingLastFrame;

        // Lock state
        private bool lockActive;
        private GearState lockedZone = GearState.Neutral;
        private bool wasStoppedLastFrame = true;

        // Rango "completo" del transformer (cuando no hay lock). Se captura en Start.
        private float fullRangeMin = -60f;
        private float fullRangeMax =  60f;

        private OneGrabRotateTransformer _transformer;

        void Awake()
        {
            originalLocalPosition = transform.localPosition;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            _transformer = GetComponent<OneGrabRotateTransformer>();
        }

        void Start()
        {
            if (vehicle == null) vehicle = FindFirstObjectByType<VehicleController>();
            if (pivot == null) pivot = transform;

            if (_transformer != null && _transformer.Constraints != null)
            {
                fullRangeMin = _transformer.Constraints.MinAngle.Value;
                fullRangeMax = _transformer.Constraints.MaxAngle.Value;
            }

            EvaluateAndDispatch(force: true);
        }

        void Update()
        {
            bool grabbing = grabbable != null && grabbable.SelectingPointsCount > 0;

            UpdateLockState();
            HandleSnapOnRelease(grabbing);

            EvaluateAndDispatch(force: false);
        }

        void LateUpdate()
        {
            // Solo trasladar lock: NUNCA tocamos la rotacion (la maneja el transformer).
            if (transform.localPosition != originalLocalPosition)
                transform.localPosition = originalLocalPosition;
        }

        // ============================================================
        //   Zonas y dispatch del gear
        // ============================================================

        /// <summary>
        /// Mapea un angulo a su zona D/N/R con histeresis: si ya estabamos en una zona,
        /// solo salimos cuando el angulo cruza el threshold + hysteresis. Eso evita el
        /// flicker D <-> N cuando el angulo jitterea cerca de la frontera (e.g. mientras
        /// la palanca esta clampada por el lock-when-moving o cuando un HandGrabInteractor
        /// le mete un poco de jitter de tracking).
        /// </summary>
        private GearState ZoneFor(float angle)
        {
            // Mientras estamos en Drive, no salimos hasta que angulo < neutralHalfRange - hysteresis.
            // Mientras estamos en Reverse, no salimos hasta que angulo > -(neutralHalfRange - hysteresis).
            // Entrando desde Neutral exige cruzar neutralHalfRange + hysteresis.
            float upperEnter = neutralHalfRange + zoneHysteresis;
            float upperExit  = neutralHalfRange - zoneHysteresis;
            float lowerEnter = -upperEnter;
            float lowerExit  = -upperExit;

            switch (lastGear)
            {
                case GearState.Drive:
                    if (angle < upperExit) return angle < lowerEnter ? GearState.Reverse : GearState.Neutral;
                    return GearState.Drive;
                case GearState.Reverse:
                    if (angle > lowerExit) return angle > upperEnter ? GearState.Drive : GearState.Neutral;
                    return GearState.Reverse;
                default: // Neutral
                    if (angle > upperEnter)  return GearState.Drive;
                    if (angle < lowerEnter)  return GearState.Reverse;
                    return GearState.Neutral;
            }
        }

        private float SnapTargetFor(GearState zone)
        {
            switch (zone)
            {
                case GearState.Drive:   return  snapAngle;
                case GearState.Reverse: return -snapAngle;
                default:                return 0f;
            }
        }

        private void EvaluateAndDispatch(bool force)
        {
            float angle = NormalizeAngle(pivot.localEulerAngles.x);
            GearState gear = ZoneFor(angle);

            if (!force && gear == lastGear) return;

            bool changed = gear != lastGear;
            lastGear = gear;

            if (vehicle != null) vehicle.SetGearFromShifter(gear);

            if (changed && !force)
                PulseHaptic(shiftHapticAmplitude, shiftHapticDuration);
        }

        // ============================================================
        //   Snap on release
        // ============================================================

        private void HandleSnapOnRelease(bool grabbing)
        {
            if (!grabbing && wasGrabbingLastFrame && snapOnRelease)
            {
                float current = NormalizeAngle(pivot.localEulerAngles.x);
                // Si el auto se mueve (lock activo), snapeamos al centro de la zona lockeada
                // sin importar donde quedo la palanca — asi no se queda en N por jitter en el borde.
                GearState targetZone = lockActive ? lockedZone : ZoneFor(current);
                snapStartX = current;
                snapTargetX = SnapTargetFor(targetZone);
                snapStartTime = Time.unscaledTime;
                snapping = true;
            }
            wasGrabbingLastFrame = grabbing;

            if (grabbing) { snapping = false; return; }
            if (!snapping) return;

            float t = snapLerpSeconds > 0f ? (Time.unscaledTime - snapStartTime) / snapLerpSeconds : 1f;
            t = Mathf.Clamp01(t);
            float x = Mathf.LerpAngle(snapStartX, snapTargetX, t);
            SetLocalRotationX(x);
            if (t >= 1f) snapping = false;
        }

        // ============================================================
        //   Lock when moving: cambia los CONSTRAINTS del transformer
        // ============================================================

        private void UpdateLockState()
        {
            if (vehicle == null) return;
            bool stoppedNow = vehicle.IsStopped();

            if (!stoppedNow && wasStoppedLastFrame)
            {
                // Transicion stopped -> moving: capturar zona y aplicar constraints estrechos
                lockedZone = ZoneFor(NormalizeAngle(pivot.localEulerAngles.x));
                lockActive = true;
                if (lockWhenMoving) ApplyConstraintsForZone(lockedZone);
            }
            else if (stoppedNow && !wasStoppedLastFrame)
            {
                // Transicion moving -> stopped: liberar lock, restaurar rango completo
                lockActive = false;
                RestoreFullConstraints();
            }
            wasStoppedLastFrame = stoppedNow;
        }

        private void ApplyConstraintsForZone(GearState zone)
        {
            if (_transformer == null || _transformer.Constraints == null) return;

            // Aplicamos margen por DENTRO de la zona para que el clamp deje la palanca firmemente
            // dentro de Drive/Reverse/Neutral y no oscile en el borde con el ZoneFor.
            float min, max;
            switch (zone)
            {
                case GearState.Drive:
                    min = neutralHalfRange + lockMargin;
                    max = fullRangeMax;
                    break;
                case GearState.Reverse:
                    min = fullRangeMin;
                    max = -(neutralHalfRange + lockMargin);
                    break;
                default:
                    min = -(neutralHalfRange - lockMargin);
                    max =  (neutralHalfRange - lockMargin);
                    break;
            }
            SetConstraints(min, max);
        }

        private void RestoreFullConstraints() => SetConstraints(fullRangeMin, fullRangeMax);

        private void SetConstraints(float min, float max)
        {
            if (_transformer == null || _transformer.Constraints == null) return;
            _transformer.Constraints.MinAngle.Constrain = true;
            _transformer.Constraints.MinAngle.Value = min;
            _transformer.Constraints.MaxAngle.Constrain = true;
            _transformer.Constraints.MaxAngle.Value = max;
        }

        // ============================================================
        //   Helpers de rotacion (solo usado por el snap-on-release)
        // ============================================================

        private void SetLocalRotationX(float xDeg)
        {
            var e = pivot.localEulerAngles;
            e.x = xDeg;
            pivot.localEulerAngles = e;

            SyncTransformerAngle(xDeg);
        }

        private static System.Reflection.FieldInfo s_relAngleField;
        private static System.Reflection.FieldInfo s_constrAngleField;

        private void SyncTransformerAngle(float xDeg)
        {
            if (_transformer == null) return;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            if (s_relAngleField == null)
            {
                s_relAngleField = typeof(OneGrabRotateTransformer).GetField("_relativeAngle", F);
                s_constrAngleField = typeof(OneGrabRotateTransformer).GetField("_constrainedRelativeAngle", F);
            }
            if (s_relAngleField != null) s_relAngleField.SetValue(_transformer, xDeg);
            if (s_constrAngleField != null) s_constrAngleField.SetValue(_transformer, xDeg);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a <= -180f) a += 360f;
            return a;
        }

        // ============================================================
        //   Haptic inline (replica de GrabHaptics, sin cross-asmdef)
        // ============================================================

        private void PulseHaptic(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f || grabbable == null) return;
            var go = grabbable.gameObject;

            foreach (var hgi in UnityEngine.Object.FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None))
            {
                if (hgi == null || hgi.State != InteractorState.Select) continue;
                if (hgi.SelectedInteractable == null || hgi.SelectedInteractable.gameObject != go) continue;
                SendImpulse(hgi.gameObject, amplitude, duration);
            }

            foreach (var gi in UnityEngine.Object.FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None))
            {
                if (gi == null || gi.State != InteractorState.Select) continue;
                if (gi.SelectedInteractable == null || gi.SelectedInteractable.gameObject != go) continue;
                SendImpulse(gi.gameObject, amplitude, duration);
            }
        }

        private static void SendImpulse(GameObject interactorGO, float amplitude, float duration)
        {
            var node = ResolveNode(interactorGO);
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid) device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }

        private static XRNode ResolveNode(GameObject interactorGO)
        {
            var handRef = interactorGO.GetComponentInParent<HandRef>();
            if (handRef != null && handRef.Hand != null)
                return handRef.Handedness == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;

            var controllerRef = interactorGO.GetComponentInParent<ControllerRef>();
            if (controllerRef != null)
                return controllerRef.Handedness == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;

            var t = interactorGO.transform;
            while (t != null)
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("left"))  return XRNode.LeftHand;
                if (n.Contains("right")) return XRNode.RightHand;
                t = t.parent;
            }
            return XRNode.RightHand;
        }
    }
}
