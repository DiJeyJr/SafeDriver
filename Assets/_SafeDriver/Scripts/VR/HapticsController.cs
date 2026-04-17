using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using SafeDriver.Core;

namespace SafeDriver.VR
{
    /// <summary>
    /// Controlador central de haptics. Usa la API estandar de Unity XR
    /// (InputDevice.SendHapticImpulse), no la API clasica de OVR.
    ///
    /// Reacciona automaticamente a EventBus:
    ///   - OnInfractionDetected       → PlayInfractionPattern()
    ///   - OnCorrectActionPerformed   → PlaySuccessPattern()
    /// </summary>
    public class HapticsController : MonoBehaviour
    {
        public static HapticsController Instance { get; private set; }

        public enum HandSide { Left, Right, Both }

        [Header("Intensidad global (0-1)")]
        [Range(0f, 1f)]
        public float globalStrength = 1f;

        [Tooltip("Canal de haptics. Casi siempre 0.")]
        [SerializeField] private uint hapticChannel = 0u;

        private InputDevice leftDevice;
        private InputDevice rightDevice;
        private Coroutine leftRoutine;
        private Coroutine rightRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnInfractionDetected     += HandleInfraction;
            EventBus.OnCorrectActionPerformed += HandleCorrectAction;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected     -= HandleInfraction;
            EventBus.OnCorrectActionPerformed -= HandleCorrectAction;
        }

        // ============================================================
        //   API publica
        // ============================================================

        /// <summary>Doble pulso fuerte y corto. Alerta sin susto.</summary>
        public void PlayInfractionPattern() => PlayPattern(HandSide.Both, InfractionEnvelope);

        /// <summary>Rampa ascendente suave. Sensacion de "bien hecho".</summary>
        public void PlaySuccessPattern() => PlayPattern(HandSide.Both, SuccessEnvelope);

        public void PlaySoft(HandSide side)     => PlayPattern(side, SoftEnvelope);
        public void PlayPositive(HandSide side) => PlayPattern(side, SuccessEnvelope);

        /// <summary>Impulso plano one-shot (amp 0-1, duracion en segundos).</summary>
        public void PlayImpulse(HandSide side, float amplitude, float duration)
        {
            float amp = Mathf.Clamp01(amplitude * globalStrength);
            if (side == HandSide.Left  || side == HandSide.Both) SendImpulse(ref leftDevice,  XRNode.LeftHand,  amp, duration);
            if (side == HandSide.Right || side == HandSide.Both) SendImpulse(ref rightDevice, XRNode.RightHand, amp, duration);
        }

        // ============================================================
        //   Event handlers
        // ============================================================

        private void HandleInfraction(InfractionType type, string message) => PlayInfractionPattern();
        private void HandleCorrectAction(ActionType type, int bonus)       => PlaySuccessPattern();

        // ============================================================
        //   Internals
        // ============================================================

        private delegate float Envelope(float t);

        private void PlayPattern(HandSide side, Envelope env)
        {
            if (side == HandSide.Left || side == HandSide.Both)
            {
                if (leftRoutine != null) StopCoroutine(leftRoutine);
                leftRoutine = StartCoroutine(RunPattern(XRNode.LeftHand, env, true));
            }
            if (side == HandSide.Right || side == HandSide.Both)
            {
                if (rightRoutine != null) StopCoroutine(rightRoutine);
                rightRoutine = StartCoroutine(RunPattern(XRNode.RightHand, env, false));
            }
        }

        /// <summary>
        /// Emite impulsos cortos consecutivos siguiendo la envelope (0..1 sobre 0..1 segundos).
        /// Resolucion ~20ms — similar al viejo sistema basado en samples.
        /// </summary>
        private IEnumerator RunPattern(XRNode node, Envelope envelope, bool isLeft)
        {
            const float dt = 0.02f;
            const float patternDuration = 0.30f;
            int steps = Mathf.CeilToInt(patternDuration / dt);

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float amp = Mathf.Clamp01(envelope(t) * globalStrength);
                if (amp > 0.01f)
                {
                    if (isLeft) SendImpulse(ref leftDevice,  node, amp, dt * 1.5f);
                    else        SendImpulse(ref rightDevice, node, amp, dt * 1.5f);
                }
                yield return new WaitForSeconds(dt);
            }

            if (isLeft) leftRoutine = null;
            else        rightRoutine = null;
        }

        private void SendImpulse(ref InputDevice cached, XRNode node, float amp, float seconds)
        {
            if (!cached.isValid) cached = InputDevices.GetDeviceAtXRNode(node);
            if (!cached.isValid) return;
            if (cached.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                cached.SendHapticImpulse(hapticChannel, amp, seconds);
        }

        // ============================================================
        //   Envelopes
        // ============================================================

        /// <summary>Doble pulso: 60% → silencio → 80% → decay.</summary>
        private static float InfractionEnvelope(float t)
        {
            if (t < 0.20f) return 0.60f;
            if (t < 0.35f) return 0f;
            if (t < 0.60f) return 0.80f;
            return Mathf.Lerp(0.80f, 0f, (t - 0.60f) / 0.40f);
        }

        /// <summary>Rampa 25% → 65% → 95%.</summary>
        private static float SuccessEnvelope(float t)
        {
            return t < 0.7f
                ? Mathf.Lerp(0.25f, 0.65f, t / 0.7f)
                : Mathf.Lerp(0.65f, 0.95f, (t - 0.7f) / 0.3f);
        }

        /// <summary>Pulso suave corto.</summary>
        private static float SoftEnvelope(float t)
        {
            return t < 0.5f
                ? Mathf.Lerp(0f, 0.35f, t / 0.5f)
                : Mathf.Lerp(0.35f, 0f, (t - 0.5f) / 0.5f);
        }
    }
}
