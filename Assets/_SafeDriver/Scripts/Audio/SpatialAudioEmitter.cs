using UnityEngine;
using UnityEngine.Audio;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Emitter de audio 3D espacializado usando AudioSource nativo de Unity.
    /// Adjuntar a: autos NPC, grupos de peatones, semaforos con sonido.
    ///
    /// En VR con Meta XR, el Audio Spatializer plugin (si esta activo) toma este AudioSource
    /// y lo espacializa con HRTF. No hace falta configuracion especial por instancia —
    /// alcanza con spatialBlend = 1 (full 3D).
    ///
    /// Para modular el sonido en runtime (ej: pitch del motor NPC segun velocidad),
    /// usar SetPitch(float) o SetVolume(float). Cualquier logica mas compleja (crossfades,
    /// layering) se hace con multiples emitters o con AudioMixer snapshots.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpatialAudioEmitter : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip loopClip;

        [Tooltip("Mixer group para routing (SFX/Traffic/etc). Opcional.")]
        [SerializeField] private AudioMixerGroup mixerGroup;

        [Header("3D curve")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        [Tooltip("0 = sin doppler, 1 = doppler fisico. Valores bajos son mas confortables en VR.")]
        [Range(0f, 5f)] [SerializeField] private float dopplerLevel = 0.2f;

        [Header("Config")]
        [SerializeField] private bool playOnStart = true;

        private AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = rolloff;
            source.dopplerLevel = dopplerLevel;
            source.outputAudioMixerGroup = mixerGroup;
            if (loopClip != null) source.clip = loopClip;
        }

        void Start()
        {
            if (playOnStart) Play();
        }

        /// <summary>Inicia el loop 3D.</summary>
        public void Play()
        {
            if (source == null || source.clip == null) return;
            if (!source.isPlaying) source.Play();
        }

        /// <summary>Detiene el loop.</summary>
        public void Stop()
        {
            if (source != null && source.isPlaying) source.Stop();
        }

        /// <summary>Setea el pitch (ej: para modular el motor de un NPC segun velocidad).</summary>
        public void SetPitch(float pitch) { if (source != null) source.pitch = pitch; }

        /// <summary>Setea el volumen (0-1).</summary>
        public void SetVolume(float volume) { if (source != null) source.volume = Mathf.Clamp01(volume); }
    }
}
