using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// Controlador central de haptics.
    /// - PlaySoft: buzz suave para infracciones.
    /// - PlayPositive: patron ascendente para aciertos.
    /// Combina OVRHaptics (clip sample-based, alta fidelidad) + OVRInput.SetControllerVibration (fallback universal).
    /// </summary>
    public class HapticsController : MonoBehaviour
    {
        public enum HandSide { Left, Right, Both }

        [Header("Intensidad global (0-1)")]
        [Range(0f, 1f)]
        public float globalStrength = 1f;

        private OVRHapticsClip softClip;
        private OVRHapticsClip positiveClip;

        void Awake()
        {
            softClip = MakeSoftPattern();
            positiveClip = MakeRisingPattern();
        }

        public void PlaySoft(HandSide side)
        {
            Trigger(softClip, side, 0.35f, 0.20f);
        }

        public void PlayPositive(HandSide side)
        {
            Trigger(positiveClip, side, 0.70f, 0.25f);
        }

        public void PlaySoftBoth()     { PlaySoft(HandSide.Both); }
        public void PlayPositiveBoth() { PlayPositive(HandSide.Both); }

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

        private OVRHapticsClip MakeSoftPattern()
        {
            const int samples = 64;
            byte level = (byte)Mathf.Clamp(90f * globalStrength, 0f, 255f);
            OVRHapticsClip c = new OVRHapticsClip(samples);
            for (int i = 0; i < samples; i++) c.WriteSample(level);
            return c;
        }

        private OVRHapticsClip MakeRisingPattern()
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
