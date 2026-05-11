using System;
using System.Reflection;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Vibra el controller cuando un grabbable rotacional pasa del tope que define su
    /// `OneGrabRotateTransformer`. Lee `_relativeAngle` (sin constraint) y
    /// `_constrainedRelativeAngle` (con constraint) via reflection — mismo patron que
    /// `SteeringWheelController` — y calcula el `overshoot` (cuanto se paso del limite).
    ///
    /// La intensidad y la frecuencia del pulso se configuran por tier (rango de overshoot).
    /// El ultimo tier puede tener `releaseOnEnter` para forzar Unselect en el Interactor que
    /// esta agarrando, opcionalmente acompaniado de un shake breve de la camara.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class GrabLimitFeedback : MonoBehaviour
    {
        [Serializable]
        public struct Tier
        {
            [Tooltip("Inicio del rango de overshoot (grados, inclusivo).")]
            public float fromDeg;
            [Tooltip("Fin del rango de overshoot (grados, exclusivo). Usa Mathf.Infinity para 'sin tope'.")]
            public float toDeg;
            [Tooltip("Amplitud del pulso (0-1).")]
            [Range(0f, 1f)] public float amplitude;
            [Tooltip("Duracion de cada pulso (segundos).")]
            public float duration;
            [Tooltip("Segundos entre pulsos. 0 = un unico pulso al entrar al tier.")]
            public float intervalSeconds;
            [Tooltip("Si esta marcado, al entrar a este tier se fuerza Unselect en el grabbable.")]
            public bool releaseOnEnter;
        }

        [Header("Referencias")]
        [Tooltip("Grabbable que vamos a monitorear. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private Grabbable grabbable;

        [Tooltip("Transformer rotacional. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private OneGrabRotateTransformer transformer;

        [Header("Tiers (overshoot en grados, en orden ascendente)")]
        [SerializeField] private Tier[] tiers = new Tier[0];

        [Header("Camera shake al forzar release")]
        [Tooltip("Magnitud del shake (metros). VR: mantener por debajo de 0.05.")]
        [SerializeField] private float releaseShakeMagnitude = 0.04f;
        [Tooltip("Duracion del shake (segundos).")]
        [SerializeField] private float releaseShakeDuration = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        private static FieldInfo s_relativeAngleField;
        private static FieldInfo s_constrainedRelativeAngleField;

        private int activeTier = -1;
        private float nextPulseTime;
        private bool wasGrabbing;

        void Awake()
        {
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            if (transformer == null) transformer = GetComponent<OneGrabRotateTransformer>();

            if (s_relativeAngleField == null)
            {
                var t = typeof(OneGrabRotateTransformer);
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                s_relativeAngleField = t.GetField("_relativeAngle", flags);
                s_constrainedRelativeAngleField = t.GetField("_constrainedRelativeAngle", flags);
            }
        }

        void Update()
        {
            if (grabbable == null || transformer == null) return;

            bool grabbing = grabbable.SelectingPointsCount > 0;

            // Reset si soltamos
            if (!grabbing)
            {
                if (wasGrabbing) Reset();
                wasGrabbing = false;
                return;
            }
            wasGrabbing = true;

            float overshoot = ReadOvershootDeg();
            int tierIdx = FindTier(overshoot);

            if (logDebug && Time.frameCount % 30 == 0)
                Debug.Log($"[GrabLimit:{name}] overshoot={overshoot:F1}° tier={tierIdx}");

            if (tierIdx < 0) { activeTier = -1; return; }

            // Nuevo tier — primer pulso inmediato
            if (tierIdx != activeTier)
            {
                activeTier = tierIdx;
                PulseTier(tiers[tierIdx]);
                nextPulseTime = Time.unscaledTime + Mathf.Max(tiers[tierIdx].intervalSeconds, tiers[tierIdx].duration);

                if (tiers[tierIdx].releaseOnEnter)
                {
                    if (CameraShake.Instance != null)
                        CameraShake.Instance.Shake(releaseShakeMagnitude, releaseShakeDuration);
                    ForceUnselect();
                }
                return;
            }

            // Mismo tier — pulsos intermitentes si interval > 0
            var current = tiers[tierIdx];
            if (current.intervalSeconds > 0f && Time.unscaledTime >= nextPulseTime)
            {
                PulseTier(current);
                nextPulseTime = Time.unscaledTime + current.intervalSeconds;
            }
        }

        private void Reset()
        {
            activeTier = -1;
            nextPulseTime = 0f;
        }

        private void PulseTier(Tier t)
        {
            GrabHaptics.PulseOnGrabbing(grabbable, t.amplitude, t.duration);
        }

        /// <summary>
        /// Magnitud del overshoot en grados — diferencia entre el angulo deseado (sin constraint)
        /// y el efectivo (con constraint). Si los fields privados no estan disponibles, devuelve 0.
        /// </summary>
        private float ReadOvershootDeg()
        {
            if (s_relativeAngleField == null || s_constrainedRelativeAngleField == null) return 0f;
            float relative = (float)s_relativeAngleField.GetValue(transformer);
            float constrained = (float)s_constrainedRelativeAngleField.GetValue(transformer);
            return Mathf.Abs(relative - constrained);
        }

        private int FindTier(float overshoot)
        {
            for (int i = 0; i < tiers.Length; i++)
            {
                var t = tiers[i];
                if (overshoot >= t.fromDeg && overshoot < t.toDeg) return i;
            }
            return -1;
        }

        /// <summary>
        /// Llama `Unselect()` en todos los Interactors que actualmente seleccionan el grabbable.
        /// Eso despega la mano del grabbable aunque el usuario siga apretando el trigger.
        /// </summary>
        private void ForceUnselect()
        {
            var go = grabbable.gameObject;

            foreach (var hgi in UnityEngine.Object.FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None))
            {
                if (hgi == null || hgi.State != InteractorState.Select) continue;
                if (hgi.SelectedInteractable != null && hgi.SelectedInteractable.gameObject == go)
                    hgi.Unselect();
            }

            foreach (var gi in UnityEngine.Object.FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None))
            {
                if (gi == null || gi.State != InteractorState.Select) continue;
                if (gi.SelectedInteractable != null && gi.SelectedInteractable.gameObject == go)
                    gi.Unselect();
            }

            if (logDebug) Debug.Log($"[GrabLimit:{name}] force-unselect", this);
        }
    }
}
