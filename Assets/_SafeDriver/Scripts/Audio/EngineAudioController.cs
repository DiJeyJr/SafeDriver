using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Controla el sonido del motor: pitch y volumen segun velocidad simulada.
    /// Se suscribe a EventBus.OnSpeedChanged — sin dependencias hard a Vehicle.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EngineAudioController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve pitchBySpeed  = AnimationCurve.Linear(0f, 0.7f, 60f, 1.4f);
        [SerializeField] private AnimationCurve volumeBySpeed = AnimationCurve.Linear(0f, 0.3f, 60f, 1.0f);

        private AudioSource source;
        private float currentSpeedKmh;

        void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            EventBus.OnSpeedChanged += HandleSpeedChanged;
            EventBus.OnEngineStarted += HandleEngineStarted;
            EventBus.OnEngineStopped += HandleEngineStopped;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged -= HandleSpeedChanged;
            EventBus.OnEngineStarted -= HandleEngineStarted;
            EventBus.OnEngineStopped -= HandleEngineStopped;
        }

        private void HandleSpeedChanged(float speedKmh) { currentSpeedKmh = speedKmh; }
        private void HandleEngineStarted()              { if (source != null && !source.isPlaying) source.Play(); }
        private void HandleEngineStopped()              { if (source != null &&  source.isPlaying) source.Stop(); }

        void Update()
        {
            if (source == null) return;
            source.pitch  = pitchBySpeed.Evaluate(currentSpeedKmh);
            source.volume = volumeBySpeed.Evaluate(currentSpeedKmh);
        }
    }
}
