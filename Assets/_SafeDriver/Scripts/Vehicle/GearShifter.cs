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
    ///   Reverse: < -neutralHalfRange   (centro al snap = -snapAngle)
    ///   Neutral: ±neutralHalfRange     (centro = 0)
    ///   Drive:   > +neutralHalfRange   (centro al snap = +snapAngle)
    ///
    /// Comportamiento:
    ///   - Snap on release: al soltar se recentra al medio de la zona actual (lerp).
    ///   - Lock-when-moving: si el auto NO esta detenido, clampa la rotacion al rango Neutral.
    ///     Cada vez que se intenta forzar el clamp dispara un pulso haptic de aviso.
    ///   - Pulso al cambiar de zona D <-> N <-> R.
    ///
    /// Nota: la logica de haptic esta inline aca (no usa SafeDriver.VR.GrabHaptics) porque el
    /// asmdef SafeDriver.Vehicle no puede referenciar SafeDriver.VR sin crear un ciclo.
    /// </summary>
    public class GearShifter : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("VehicleController al que se le setean los gears. Si queda vacio, se busca el de la escena en Start.")]
        [SerializeField] private VehicleController vehicle;

        [Tooltip("Transform cuya rotacion X local define el gear. Tipicamente el pivot de la palanca.")]
        [SerializeField] private Transform pivot;

        [Tooltip("Grabbable de la palanca. Si queda vacio se busca en este GameObject. Necesario para snap-on-release y haptics.")]
        [SerializeField] private Grabbable grabbable;

        [Header("Config de zonas (grados sobre eje X local)")]
        [Tooltip("Medio ancho de la zona Neutral. Fuera de ±este valor entra a D o R.")]
        [SerializeField] private float neutralHalfRange = 20f;

        [Tooltip("Angulo objetivo (grados) al snapear a D (positivo) o R (negativo).")]
        [SerializeField] private float snapAngle = 40f;

        [Header("Snap on release")]
        [Tooltip("Si esta activo, al soltar la palanca se recentra al medio de la zona actual.")]
        [SerializeField] private bool snapOnRelease = true;

        [Tooltip("Segundos para completar el snap suavemente.")]
        [SerializeField] private float snapLerpSeconds = 0.18f;

        [Header("Lock when moving")]
        [Tooltip("Si el auto se mueve, fuerza la palanca a quedarse dentro de Neutral.")]
        [SerializeField] private bool lockWhenMoving = true;

        [Tooltip("Amplitud del pulso haptic al chocar el clamp por velocidad (0-1).")]
        [Range(0f, 1f)] [SerializeField] private float lockHapticAmplitude = 0.9f;

        [Tooltip("Duracion del pulso haptic al chocar el clamp (segundos).")]
        [SerializeField] private float lockHapticDuration = 0.08f;

        [Header("Haptic de cambio de marcha")]
        [Tooltip("Amplitud del pulso al pasar de zona (0-1).")]
        [Range(0f, 1f)] [SerializeField] private float shiftHapticAmplitude = 0.6f;
        [SerializeField] private float shiftHapticDuration = 0.07f;

        private GearState lastGear = GearState.Neutral;
        private Vector3 originalLocalPosition;

        private bool snapping;
        private float snapStartX;
        private float snapTargetX;
        private float snapStartTime;
        private bool wasGrabbingLastFrame;
        private float lastLockHapticTime;

        // Zona "lockeada" mientras el auto se mueve. Se captura la primera vez que el
        // auto pasa de detenido a moviendose, y se mantiene hasta que vuelve a detenerse.
        // Asi la palanca queda atrapada en la zona en la que estaba al arrancar.
        private bool lockActive;
        private GearState lockedZone = GearState.Neutral;
        private bool wasStoppedLastFrame = true;

        void Awake()
        {
            originalLocalPosition = transform.localPosition;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
        }

        void Start()
        {
            if (vehicle == null) vehicle = FindFirstObjectByType<VehicleController>();
            if (pivot == null) pivot = transform;

            EvaluateAndDispatch(force: true);
        }

        void Update()
        {
            bool grabbing = grabbable != null && grabbable.SelectingPointsCount > 0;

            // Tomar snapshot de la zona actual cuando el auto pasa de parado a moviendose.
            UpdateLockState();

            HandleSnapOnRelease(grabbing);

            if (lockWhenMoving && grabbing && lockActive)
                ClampToZone(lockedZone);

            EvaluateAndDispatch(force: false);
        }

        private void UpdateLockState()
        {
            if (vehicle == null) { lockActive = false; return; }
            bool stoppedNow = vehicle.IsStopped();

            if (!stoppedNow && wasStoppedLastFrame)
            {
                // Transicion: el auto arranco. Capturar la zona actual y lockear.
                lockedZone = ZoneFor(NormalizeAngle(pivot.localEulerAngles.x));
                lockActive = true;
            }
            else if (stoppedNow && !wasStoppedLastFrame)
            {
                // Transicion: el auto se detuvo. Liberar el lock — la palanca se puede mover libremente.
                lockActive = false;
            }
            wasStoppedLastFrame = stoppedNow;
        }

        void LateUpdate()
        {
            // Lock posicional: solo rotamos sobre X, jamas trasladamos.
            if (transform.localPosition != originalLocalPosition)
                transform.localPosition = originalLocalPosition;
        }

        // ============================================================
        //   Zonas
        // ============================================================

        private GearState ZoneFor(float angle)
        {
            if (angle > neutralHalfRange)  return GearState.Drive;
            if (angle < -neutralHalfRange) return GearState.Reverse;
            return GearState.Neutral;
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
            // Disparar snap al pasar de grabbing a no-grabbing
            if (!grabbing && wasGrabbingLastFrame && snapOnRelease)
            {
                float current = NormalizeAngle(pivot.localEulerAngles.x);
                snapStartX = current;
                snapTargetX = SnapTargetFor(ZoneFor(current));
                snapStartTime = Time.unscaledTime;
                snapping = true;
            }
            wasGrabbingLastFrame = grabbing;

            // Cancelar snap si vuelve a agarrar
            if (grabbing) { snapping = false; return; }
            if (!snapping) return;

            float t = snapLerpSeconds > 0f ? (Time.unscaledTime - snapStartTime) / snapLerpSeconds : 1f;
            t = Mathf.Clamp01(t);
            float x = Mathf.LerpAngle(snapStartX, snapTargetX, t);
            SetLocalRotationX(x);
            if (t >= 1f) snapping = false;
        }

        // ============================================================
        //   Lock when moving — clampa al rango de la zona lockeada
        // ============================================================

        private void ClampToZone(GearState zone)
        {
            float angle = NormalizeAngle(pivot.localEulerAngles.x);
            float min, max;
            GetZoneRange(zone, out min, out max);

            float clamped = Mathf.Clamp(angle, min, max);
            if (Mathf.Approximately(angle, clamped)) return;

            SetLocalRotationX(clamped);

            // Haptic de aviso anti-spam (max ~4 Hz)
            if (Time.unscaledTime - lastLockHapticTime > 0.25f)
            {
                lastLockHapticTime = Time.unscaledTime;
                PulseHaptic(lockHapticAmplitude, lockHapticDuration);
            }
        }

        /// <summary>Devuelve los limites de angulo (en grados) de una zona D/N/R.</summary>
        private void GetZoneRange(GearState zone, out float min, out float max)
        {
            // Pequenio margen para que el clamp no choque exactamente con la frontera.
            const float margin = 1f;
            switch (zone)
            {
                case GearState.Drive:
                    min = neutralHalfRange + margin;
                    max = 90f; // suficientemente grande, el constraint del transformer ya limita
                    break;
                case GearState.Reverse:
                    min = -90f;
                    max = -(neutralHalfRange + margin);
                    break;
                default: // Neutral
                    min = -(neutralHalfRange - margin);
                    max =  (neutralHalfRange - margin);
                    break;
            }
        }

        // ============================================================
        //   Helpers de rotacion
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
            var t = GetComponent<OneGrabRotateTransformer>();
            if (t == null) return;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            if (s_relAngleField == null)
            {
                s_relAngleField = typeof(OneGrabRotateTransformer).GetField("_relativeAngle", F);
                s_constrAngleField = typeof(OneGrabRotateTransformer).GetField("_constrainedRelativeAngle", F);
            }
            if (s_relAngleField != null) s_relAngleField.SetValue(t, xDeg);
            if (s_constrAngleField != null) s_constrAngleField.SetValue(t, xDeg);
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
