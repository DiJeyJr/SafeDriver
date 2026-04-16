using UnityEngine;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Wrapper de AudioSource 3D con utilidades: fade in/out, distance attenuation presets.
    /// Util para sirenas, bocinas de NPC, sonidos ambientales espacializados.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpatialAudioEmitter : MonoBehaviour
    {
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 25f;

        private AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

        public void PlayFadeIn(float duration)  { /* TODO */ }
        public void StopFadeOut(float duration) { /* TODO */ }
    }
}
