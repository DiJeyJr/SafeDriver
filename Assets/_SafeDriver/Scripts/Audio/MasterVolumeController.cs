using UnityEngine;
using UnityEngine.Audio;

namespace SafeDriver.Audio
{
    /// <summary>
    /// Controla el volumen master del juego via AudioMixer.
    ///
    /// - Convierte valor lineal 0..1 a dB (via log10), que es lo que AudioMixer espera.
    /// - Persiste el ultimo valor en PlayerPrefs para que el setting sobreviva al cerrar el juego.
    /// - Wirear a un UI Slider con Slider.onValueChanged -> SetMasterVolume(float).
    ///
    /// Setup:
    ///   1. El AudioMixer debe tener el parametro "MasterVolume" expuesto (apuntando al Volume del Master group).
    ///   2. Todas las AudioSources del juego deben rutearse al Master group (o a un hijo suyo).
    ///   3. Asignar el mixer en Inspector y wirear Slider.onValueChanged -> SetMasterVolume.
    /// </summary>
    public class MasterVolumeController : MonoBehaviour
    {
        public static MasterVolumeController Instance { get; private set; }

        [Header("Mixer")]
        [Tooltip("AudioMixer con el parametro 'MasterVolume' expuesto.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("Nombre del parametro expuesto (default: MasterVolume).")]
        [SerializeField] private string exposedParam = "MasterVolume";

        [Header("Curva y persistencia")]
        [Tooltip("Volumen inicial si no hay valor guardado (0 = mudo, 1 = 0 dB).")]
        [Range(0f, 1f)] [SerializeField] private float defaultVolume = 0.75f;

        [Tooltip("Volumen minimo en dB cuando slider=0. -80 dB = silencio efectivo.")]
        [SerializeField] private float minDb = -80f;

        [Tooltip("Volumen maximo en dB cuando slider=1. 0 dB = sin atenuacion.")]
        [SerializeField] private float maxDb = 0f;

        [Tooltip("Clave PlayerPrefs para persistir el valor.")]
        [SerializeField] private string prefsKey = "SafeDriver.MasterVolume";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            float saved = PlayerPrefs.HasKey(prefsKey) ? PlayerPrefs.GetFloat(prefsKey) : defaultVolume;
            ApplyToMixer(saved);
        }

        /// <summary>
        /// Setea el volumen master. Input lineal 0..1; se convierte a dB internamente.
        /// Persiste en PlayerPrefs.
        /// </summary>
        public void SetMasterVolume(float linear01)
        {
            linear01 = Mathf.Clamp01(linear01);
            ApplyToMixer(linear01);
            PlayerPrefs.SetFloat(prefsKey, linear01);
        }

        /// <summary>Devuelve el volumen persistido (linear 0..1) o el default si no hay.</summary>
        public float GetCurrentVolume()
        {
            return PlayerPrefs.HasKey(prefsKey) ? PlayerPrefs.GetFloat(prefsKey) : defaultVolume;
        }

        private void ApplyToMixer(float linear01)
        {
            if (mixer == null) return;

            float db;
            if (linear01 <= 0.0001f)
            {
                db = minDb;
            }
            else
            {
                // Conversion perceptual: lineal 0..1 -> dB via log10.
                // linear = 1 -> 0 dB, linear = 0.5 -> -6 dB, linear = 0.1 -> -20 dB
                db = Mathf.Lerp(minDb, maxDb, Mathf.Log10(1f + 9f * linear01));
            }

            mixer.SetFloat(exposedParam, db);
        }
    }
}
