using UnityEngine;
using SafeDriver.Core;
#if FMOD_AVAILABLE
using FMODUnity;
using FMOD.Studio;
#endif

namespace SafeDriver.Audio
{
    /// <summary>
    /// Emitter de audio 3D espacializado via FMOD.
    /// Adjuntar a: autos NPC, grupos de peatones, semaforos con sonido.
    /// FMOD maneja el spatializing automaticamente — NO configurar nada en Unity AudioSource.
    ///
    /// AttachInstanceToGameObject() hace que FMOD actualice posicion y velocidad (doppler)
    /// cada frame internamente. Esto reemplaza completamente:
    ///   - AudioSource.spatialBlend
    ///   - AudioSource.minDistance / maxDistance
    ///   - AudioSource.dopplerLevel
    ///
    /// Las distancias min/max y curvas de atenuacion se configuran en FMOD Studio, no aca.
    /// Los campos minDistance/maxDistance son solo informativos para el diseñador en Inspector.
    ///
    /// Parametros runtime: llamar SetParameter("Speed", value) desde NPCVehicleAI
    /// para modular el sonido segun el estado del NPC.
    /// </summary>
    public class SpatialAudioEmitter : MonoBehaviour
    {
#if FMOD_AVAILABLE
        [Header("FMOD Event")]
        [SerializeField] private EventReference loopEvent;
#endif

        [Header("FMOD maneja el 3D — solo configurar en Studio")]
        [Tooltip("Solo a modo informativo — la distancia real se setea en FMOD Studio.")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 30f;

        [Header("Config")]
        [SerializeField] private bool playOnStart = true;

        private bool isPlaying;

#if FMOD_AVAILABLE
        private EventInstance instance;
#endif

        void Start()
        {
            if (playOnStart) Play();
        }

        /// <summary>Inicia la reproduccion del loop 3D, attached a este transform.</summary>
        public void Play()
        {
            if (isPlaying) return;
            isPlaying = true;

#if FMOD_AVAILABLE
            instance = RuntimeManager.CreateInstance(loopEvent);

            // Attach al GameObject — FMOD actualiza posicion y velocidad automaticamente.
            // Si no hay Rigidbody (objeto estatico), pasar null — FMOD lo maneja.
            RuntimeManager.AttachInstanceToGameObject(
                instance,
                transform,
                GetComponent<Rigidbody>()
            );

            instance.start();
#else
            Debug.Log("[SpatialAudio] Play (FMOD not installed) on " + gameObject.name);
#endif
        }

        /// <summary>Detiene el loop con fade out.</summary>
        public void Stop()
        {
            if (!isPlaying) return;
            isPlaying = false;

#if FMOD_AVAILABLE
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
#else
            Debug.Log("[SpatialAudio] Stop (FMOD not installed) on " + gameObject.name);
#endif
        }

        /// <summary>
        /// Setea un parametro FMOD por nombre en runtime.
        /// Llamar desde NPCVehicleAI para modular el sonido segun el estado del NPC.
        /// Ejemplo: SetParameter("Speed", currentSpeedKmh) para variar el pitch del motor NPC.
        /// </summary>
        public void SetParameter(string paramName, float value)
        {
#if FMOD_AVAILABLE
            if (isPlaying) instance.setParameterByName(paramName, value);
#endif
        }

        void OnDestroy()
        {
#if FMOD_AVAILABLE
            if (isPlaying)
            {
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instance.release();
                isPlaying = false;
            }
#endif
        }
    }
}
