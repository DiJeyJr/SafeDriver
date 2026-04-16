using System;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;
#if FMOD_AVAILABLE
using FMODUnity;
using FMOD.Studio;
#endif

namespace SafeDriver.Audio
{
    /// <summary>
    /// Controlador central de audio via FMOD.
    /// Reemplaza AudioManager + EngineAudioController en un solo componente.
    /// FMOD maneja el 3D internamente — NO usar AudioSource en ningun objeto del entorno.
    ///
    /// Responsabilidades:
    /// - Engine loop 3D attached al vehiculo del jugador (parametros RPM + Load)
    /// - City ambience 2D loop (fondo urbano)
    /// - Feedback one-shots: infraccion, success, level complete, horn
    /// - Todo suscripto a EventBus (desacoplado de otras capas)
    ///
    /// Compila con o sin FMOD gracias a #if FMOD_AVAILABLE (auto-definido por asmdef versionDefines).
    /// </summary>
    public class FMODAudioManager : MonoBehaviour
    {
        public static FMODAudioManager Instance { get; private set; }

#if FMOD_AVAILABLE
        [Header("FMOD Event References (asignar desde Inspector)")]
        [SerializeField] private EventReference engineEvent;
        [SerializeField] private EventReference hornEvent;
        [SerializeField] private EventReference infractionEvent;
        [SerializeField] private EventReference successEvent;
        [SerializeField] private EventReference cityAmbienceEvent;
        [SerializeField] private EventReference levelCompleteEvent;

        // Instancias persistentes (loops)
        private EventInstance engineInstance;
        private EventInstance cityAmbienceInstance;
#endif

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
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ReleaseInstances();
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
        //   Motor (loop 3D attached al vehiculo)
        // ============================================================

        private void StartEngineLoop()
        {
#if FMOD_AVAILABLE
            if (VehicleController.Instance == null) return;
            engineInstance = RuntimeManager.CreateInstance(engineEvent);
            RuntimeManager.AttachInstanceToGameObject(
                engineInstance,
                VehicleController.Instance.transform,
                VehicleController.Instance.GetComponent<Rigidbody>());
            engineInstance.start();
#else
            Debug.Log("[FMODAudioManager] Engine loop started (FMOD not installed)");
#endif
        }

        private void UpdateEngineAudio(float speedKmh)
        {
#if FMOD_AVAILABLE
            // RPM: 0 km/h = 800 idle, 120 km/h = 7200 redline
            float rpm = Mathf.Lerp(800f, 7200f, Mathf.InverseLerp(0f, 120f, speedKmh));
            engineInstance.setParameterByName(FMODEvents.PARAM_RPM, rpm);

            // Load: cuanto esta acelerando (0 = coasting, 1 = full throttle)
            float load = VehicleInput.Instance != null ? VehicleInput.Instance.ThrottleInput : 0f;
            engineInstance.setParameterByName(FMODEvents.PARAM_LOAD, load);
#endif
        }

        // ============================================================
        //   Bocina (one-shot 3D)
        // ============================================================

        /// <summary>Toca la bocina. Llamar desde VehicleInput al presionar el boton.</summary>
        public void PlayHorn()
        {
#if FMOD_AVAILABLE
            if (VehicleController.Instance != null)
                RuntimeManager.PlayOneShotAttached(hornEvent, VehicleController.Instance.gameObject);
#else
            Debug.Log("[FMODAudioManager] Horn (FMOD not installed)");
#endif
        }

        // ============================================================
        //   Feedback 2D (sin posicion — directo al jugador)
        // ============================================================

        private void PlayInfraction()
        {
#if FMOD_AVAILABLE
            RuntimeManager.PlayOneShot(infractionEvent);
#else
            Debug.Log("[FMODAudioManager] Infraction SFX (FMOD not installed)");
#endif
        }

        private void PlaySuccess()
        {
#if FMOD_AVAILABLE
            RuntimeManager.PlayOneShot(successEvent);
#else
            Debug.Log("[FMODAudioManager] Success SFX (FMOD not installed)");
#endif
        }

        private void PlayLevelComplete()
        {
#if FMOD_AVAILABLE
            RuntimeManager.PlayOneShot(levelCompleteEvent);
            cityAmbienceInstance.setPaused(true);
#else
            Debug.Log("[FMODAudioManager] Level Complete SFX (FMOD not installed)");
#endif
        }

        // ============================================================
        //   Ambience (loop 2D)
        // ============================================================

        private void StartCityAmbience()
        {
#if FMOD_AVAILABLE
            cityAmbienceInstance = RuntimeManager.CreateInstance(cityAmbienceEvent);
            cityAmbienceInstance.start();
#else
            Debug.Log("[FMODAudioManager] City ambience started (FMOD not installed)");
#endif
        }

        /// <summary>Reanuda el city ambience (ej: al volver a Driving desde LevelEnd).</summary>
        public void ResumeCityAmbience()
        {
#if FMOD_AVAILABLE
            cityAmbienceInstance.setPaused(false);
#endif
        }

        // ============================================================
        //   Cleanup
        // ============================================================

        private void ReleaseInstances()
        {
#if FMOD_AVAILABLE
            engineInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            engineInstance.release();
            cityAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            cityAmbienceInstance.release();
#endif
        }
    }
}
