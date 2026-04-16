using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.VR
{
    /// <summary>
    /// Controlador central de haptics para los controllers Quest.
    /// Expone Instance singleton y reacciona automaticamente a EventBus:
    ///   - OnInfractionDetected  → PlayInfractionPattern()
    ///   - OnCorrectActionPerformed → PlaySuccessPattern()
    ///
    /// Tambien se puede llamar directo desde scripts de la capa VR si se quiere
    /// un patron custom (ej: rumble de motor, impacto de bache, etc.)
    /// </summary>
    public class HapticsController : MonoBehaviour
    {
        public static HapticsController Instance { get; private set; }

        public enum HandSide { Left, Right, Both }

        [Header("Intensidad global (0-1)")]
        [Range(0f, 1f)]
        public float globalStrength = 1f;

        private OVRHapticsClip infractionClip;
        private OVRHapticsClip successClip;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            infractionClip = BuildInfractionClip();
            successClip    = BuildSuccessClip();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnInfractionDetected    += HandleInfraction;
            EventBus.OnCorrectActionPerformed += HandleCorrectAction;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected    -= HandleInfraction;
            EventBus.OnCorrectActionPerformed -= HandleCorrectAction;
        }

        // ============================================================
        //   API publica
        // ============================================================

        /// <summary>Patron de infraccion: doble pulso fuerte y corto. Alerta sin susto.</summary>
        public void PlayInfractionPattern()
        {
            Trigger(infractionClip, HandSide.Both, 0.55f, 0.30f);
        }

        /// <summary>Patron de exito: rampa ascendente suave. Sensacion de "bien hecho".</summary>
        public void PlaySuccessPattern()
        {
            Trigger(successClip, HandSide.Both, 0.45f, 0.25f);
        }

        /// <summary>Patron soft generico (para avisos leves, cambio de carril, etc.)</summary>
        public void PlaySoft(HandSide side)
        {
            Trigger(infractionClip, side, 0.25f, 0.15f);
        }

        /// <summary>Patron positivo generico.</summary>
        public void PlayPositive(HandSide side)
        {
            Trigger(successClip, side, 0.45f, 0.25f);
        }

        // ============================================================
        //   Event handlers (auto-reaccion al bus — capa VR sin que Scoring lo llame)
        // ============================================================

        private void HandleInfraction(InfractionType type, string message)
        {
            PlayInfractionPattern();
        }

        private void HandleCorrectAction(ActionType type, int bonus)
        {
            PlaySuccessPattern();
        }

        // ============================================================
        //   Internals
        // ============================================================

        private void Trigger(OVRHapticsClip clip, HandSide side, float amp, float seconds)
        {
            float s = Mathf.Clamp01(amp * globalStrength);
            if (side == HandSide.Left || side == HandSide.Both)
            {
                if (clip != null) OVRHaptics.LeftChannel.Preempt(clip);
                OVRInput.SetControllerVibration(1f, s, OVRInput.Controller.LTouch);
                CancelInvoke(nameof(StopLeft));
                Invoke(nameof(StopLeft), seconds);
            }
            if (side == HandSide.Right || side == HandSide.Both)
            {
                if (clip != null) OVRHaptics.RightChannel.Preempt(clip);
                OVRInput.SetControllerVibration(1f, s, OVRInput.Controller.RTouch);
                CancelInvoke(nameof(StopRight));
                Invoke(nameof(StopRight), seconds);
            }
        }

        private void StopLeft()  { OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch); }
        private void StopRight() { OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch); }

        /// <summary>Doble pulso: 60%-0%-80% sobre 96 samples (~300ms). Alerta sin susto.</summary>
        private OVRHapticsClip BuildInfractionClip()
        {
            const int samples = 96;
            OVRHapticsClip c = new OVRHapticsClip(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float env;
                if      (t < 0.20f) env = 0.60f;                // primer pulso
                else if (t < 0.35f) env = 0.0f;                 // silencio entre pulsos
                else if (t < 0.60f) env = 0.80f;                // segundo pulso (mas fuerte)
                else                env = Mathf.Lerp(0.80f, 0f, (t - 0.60f) / 0.40f); // decay
                byte level = (byte)Mathf.Clamp(env * 220f * globalStrength, 0f, 255f);
                c.WriteSample(level);
            }
            return c;
        }

        /// <summary>Rampa 25% -> 65% -> 95% sobre 80 samples (~250ms). Satisfaccion.</summary>
        private OVRHapticsClip BuildSuccessClip()
        {
            const int samples = 80;
            OVRHapticsClip c = new OVRHapticsClip(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float env = t < 0.7f
                    ? Mathf.Lerp(0.25f, 0.65f, t / 0.7f)
                    : Mathf.Lerp(0.65f, 0.95f, (t - 0.7f) / 0.3f);
                byte level = (byte)Mathf.Clamp(env * 230f * globalStrength, 0f, 255f);
                c.WriteSample(level);
            }
            return c;
        }
    }
}
