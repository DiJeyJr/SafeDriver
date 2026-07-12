using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Palanca de freno de mano (2 posiciones). Lee el angulo local X del pivot: en reposo ARRIBA
    /// (angulo ~0) el freno esta PUESTO (VehicleController.SetHandbrake(true)); al bajarla
    /// (angulo > releaseThreshold) se suelta, como un freno de mano real. Snap-on-release: al soltar,
    /// la palanca queda arriba (puesto) o abajo (suelto) segun de que lado del threshold quedo.
    /// Pulso haptico al enganchar/desenganchar.
    ///
    /// Se monta clonando el SteeringWheel (mismo stack de grab + OneGrabRotateTransformer), igual que el
    /// GearShifter — ver Editor/CloneWheelAsHandbrake.cs.
    /// </summary>
    public class HandbrakeController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private Transform pivot;
        [SerializeField] private Grabbable grabbable;

        [Header("Angulos (grados, eje X local)")]
        [Tooltip("Angulo de la palanca ABAJO (freno suelto). El reposo (0) es arriba, con el freno puesto.")]
        [SerializeField] private float releasedAngle = 40f;
        [Tooltip("Por encima de este angulo (palanca bajada) se considera el freno SUELTO.")]
        [SerializeField] private float releaseThreshold = 20f;

        [Header("Snap on release")]
        [SerializeField] private bool snapOnRelease = true;
        [SerializeField] private float snapLerpSeconds = 0.15f;

        [Header("Haptic")]
        [Range(0f, 1f)] [SerializeField] private float hapticAmplitude = 0.6f;
        [SerializeField] private float hapticDuration = 0.08f;

        private bool engaged;
        private Vector3 originalLocalPosition;
        private bool snapping;
        private float snapStartX, snapTargetX, snapStartTime;
        private bool wasGrabbingLastFrame;
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
            Evaluate(force: true);
        }

        void Update()
        {
            bool grabbing = grabbable != null && grabbable.SelectingPointsCount > 0;
            HandleSnapOnRelease(grabbing);
            Evaluate(force: false);
        }

        void LateUpdate()
        {
            // Solo preservamos la posicion local (la rotacion la maneja el transformer / el snap).
            if (transform.localPosition != originalLocalPosition)
                transform.localPosition = originalLocalPosition;
        }

        private void Evaluate(bool force)
        {
            float angle = NormalizeAngle(pivot.localEulerAngles.x);
            // Invertido respecto al diseño original: arriba (reposo, ~0) = puesto, abajo = suelto.
            bool nowEngaged = angle < releaseThreshold;
            if (!force && nowEngaged == engaged) return;

            bool changed = nowEngaged != engaged;
            engaged = nowEngaged;
            if (vehicle != null) vehicle.SetHandbrake(engaged);
            if (changed && !force) PulseHaptic(hapticAmplitude, hapticDuration);
        }

        private void HandleSnapOnRelease(bool grabbing)
        {
            if (!grabbing && wasGrabbingLastFrame && snapOnRelease)
            {
                float current = NormalizeAngle(pivot.localEulerAngles.x);
                snapStartX = current;
                snapTargetX = current > releaseThreshold ? releasedAngle : 0f;
                snapStartTime = Time.unscaledTime;
                snapping = true;
            }
            wasGrabbingLastFrame = grabbing;

            if (grabbing) { snapping = false; return; }
            if (!snapping) return;

            float t = snapLerpSeconds > 0f ? (Time.unscaledTime - snapStartTime) / snapLerpSeconds : 1f;
            t = Mathf.Clamp01(t);
            SetLocalRotationX(Mathf.LerpAngle(snapStartX, snapTargetX, t));
            if (t >= 1f) snapping = false;
        }

        private void SetLocalRotationX(float xDeg)
        {
            var e = pivot.localEulerAngles; e.x = xDeg; pivot.localEulerAngles = e;
            SyncTransformerAngle(xDeg);
        }

        private static System.Reflection.FieldInfo s_relAngleField, s_constrAngleField;
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
            a %= 360f; if (a > 180f) a -= 360f; if (a <= -180f) a += 360f; return a;
        }

        // ============================================================
        //   Haptic inline (replica de GearShifter)
        // ============================================================

        private void PulseHaptic(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f || grabbable == null) return;
            var go = grabbable.gameObject;

            foreach (var hgi in Object.FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None))
            {
                if (hgi == null || hgi.State != InteractorState.Select) continue;
                if (hgi.SelectedInteractable == null || hgi.SelectedInteractable.gameObject != go) continue;
                SendImpulse(hgi.gameObject, amplitude, duration);
            }
            foreach (var gi in Object.FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None))
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
                if (n.Contains("left")) return XRNode.LeftHand;
                if (n.Contains("right")) return XRNode.RightHand;
                t = t.parent;
            }
            return XRNode.RightHand;
        }
    }
}
