using System;
using UnityEngine;
using UnityEngine.Audio;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Controlador central de audio usando AudioSource/AudioMixer de Unity.
    ///
    /// Responsabilidades:
    /// - Engine loop 3D: AudioSource parentado al vehiculo, pitch/volume modulados por velocidad + throttle
    /// - City ambience 2D loop
    /// - Feedback one-shots: infraccion, success, level complete (2D) y horn (3D attached)
    /// - Todo suscripto a EventBus (desacoplado de otras capas)
    ///
    /// Las AudioSources se crean por codigo en Awake/Start; no hay que configurarlas en Inspector.
    /// Solo asignar los AudioClips y, opcionalmente, un AudioMixerGroup.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("AudioClips")]
        [SerializeField] private AudioClip engineClip;
        [SerializeField] private AudioClip hornClip;
        [SerializeField] private AudioClip infractionClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip cityAmbienceClip;
        [SerializeField] private AudioClip levelCompleteClip;

        [Header("Mixer")]
        [Tooltip("Grupo Master del AudioMixer. Todo el audio del juego se rutea aca (lo que permite control global con MasterVolumeController).")]
        [SerializeField] private AudioMixerGroup masterGroup;

        [Header("Engine config")]
        [Tooltip("Pitch minimo del loop del motor (idle, 0 km/h).")]
        [Range(0.1f, 2f)] [SerializeField] private float engineMinPitch = 0.6f;

        [Tooltip("Pitch maximo del loop del motor (maxSpeedForPitch).")]
        [Range(0.5f, 3f)] [SerializeField] private float engineMaxPitch = 1.8f;

        [Tooltip("Volumen del motor en idle.")]
        [Range(0f, 1f)] [SerializeField] private float engineIdleVolume = 0.35f;

        [Tooltip("Volumen del motor a full throttle.")]
        [Range(0f, 1f)] [SerializeField] private float engineMaxVolume = 1.0f;

        [Tooltip("Velocidad en km/h a la que el pitch alcanza su maximo.")]
        [SerializeField] private float maxSpeedForPitch = 120f;

        [Header("Engine 3D")]
        [SerializeField] private float engineMinDistance = 1.5f;
        [SerializeField] private float engineMaxDistance = 25f;

        // Private sources (creadas por codigo)
        private AudioSource engineSource;       // 3D, parented al vehiculo
        private AudioSource cityAmbienceSource; // 2D, en este GO
        private AudioSource sfxSource;          // 2D, para one-shots de feedback

        // Cached delegates para poder desuscribir (NO usar lambdas anonimos en OnEnable/OnDisable)
        private Action<float> cachedUpdateEngine;
        private Action<InfractionType, string> cachedInfraction;
        private Action<ActionType, int> cachedSuccess;
        private Action cachedLevelComplete;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            cachedUpdateEngine  = UpdateEngineAudio;
            cachedInfraction    = (_, __) => PlayInfraction();
            cachedSuccess       = (_, __) => PlaySuccess();
            cachedLevelComplete = PlayLevelComplete;

            cityAmbienceSource = CreateLocalSource("CityAmbience", spatial: false, loop: true, group: masterGroup);
            sfxSource          = CreateLocalSource("SFX2D",        spatial: false, loop: false, group: masterGroup);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnSpeedChanged            += cachedUpdateEngine;
            EventBus.OnInfractionDetected      += cachedInfraction;
            EventBus.OnCorrectActionPerformed  += cachedSuccess;
            EventBus.OnLevelComplete           += cachedLevelComplete;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged            -= cachedUpdateEngine;
            EventBus.OnInfractionDetected      -= cachedInfraction;
            EventBus.OnCorrectActionPerformed  -= cachedSuccess;
            EventBus.OnLevelComplete           -= cachedLevelComplete;
        }

        void Start()
        {
            StartEngineLoop();
            StartCityAmbience();
        }

        // ============================================================
        //   Motor (loop 3D parentado al vehiculo)
        // ============================================================

        private void StartEngineLoop()
        {
            if (engineClip == null || VehicleController.Instance == null) return;

            var host = new GameObject("EngineAudio");
            host.transform.SetParent(VehicleController.Instance.transform, worldPositionStays: false);

            engineSource = host.AddComponent<AudioSource>();
            engineSource.clip = engineClip;
            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.spatialBlend = 1f; // 3D
            engineSource.minDistance = engineMinDistance;
            engineSource.maxDistance = engineMaxDistance;
            engineSource.rolloffMode = AudioRolloffMode.Logarithmic;
            engineSource.dopplerLevel = 0.2f;
            engineSource.outputAudioMixerGroup = masterGroup;
            engineSource.volume = engineIdleVolume;
            engineSource.pitch = engineMinPitch;
            engineSource.Play();
        }

        private void UpdateEngineAudio(float speedKmh)
        {
            if (engineSource == null || !engineSource.isPlaying) return;

            float speedT = Mathf.Clamp01(speedKmh / maxSpeedForPitch);
            engineSource.pitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, speedT);

            float load = VehicleInput.Instance != null ? VehicleInput.Instance.ThrottleInput : 0f;
            // Volume: piso por velocidad + boost por throttle, clampeado entre idle y max
            float volT = Mathf.Max(speedT * 0.7f, load);
            engineSource.volume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, volT);
        }

        // ============================================================
        //   Bocina (one-shot 3D en la posicion del auto)
        // ============================================================

        /// <summary>Toca la bocina. Llamar desde VehicleInput al presionar el boton.</summary>
        public void PlayHorn()
        {
            if (hornClip == null || VehicleController.Instance == null) return;
            AudioSource.PlayClipAtPoint(hornClip, VehicleController.Instance.transform.position);
        }

        // ============================================================
        //   Feedback 2D (sin posicion — directo al jugador)
        // ============================================================

        private void PlayInfraction()
        {
            if (infractionClip != null) sfxSource.PlayOneShot(infractionClip);
        }

        private void PlaySuccess()
        {
            if (successClip != null) sfxSource.PlayOneShot(successClip);
        }

        private void PlayLevelComplete()
        {
            if (levelCompleteClip != null) sfxSource.PlayOneShot(levelCompleteClip);
            if (cityAmbienceSource != null && cityAmbienceSource.isPlaying) cityAmbienceSource.Pause();
        }

        // ============================================================
        //   Ambience (loop 2D)
        // ============================================================

        private void StartCityAmbience()
        {
            if (cityAmbienceClip == null) return;
            cityAmbienceSource.clip = cityAmbienceClip;
            cityAmbienceSource.Play();
        }

        /// <summary>Reanuda el city ambience (ej: al volver a Driving desde LevelEnd).</summary>
        public void ResumeCityAmbience()
        {
            if (cityAmbienceSource != null && !cityAmbienceSource.isPlaying) cityAmbienceSource.UnPause();
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private AudioSource CreateLocalSource(string name, bool spatial, bool loop, AudioMixerGroup group)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = spatial ? 1f : 0f;
            src.outputAudioMixerGroup = group;
            return src;
        }
    }
}
