using UnityEngine;

namespace SafeDriver.Vehicle
{
    public enum TurnSignal { Off, Left, Right }

    /// <summary>
    /// Luces direccionales (guiño) del auto. Hace parpadear el par correspondiente a ~1.4 Hz:
    /// izquierda = luces delantera-izq + trasera-izq (+ indicador del tablero), derecha = las de la
    /// derecha. La palanca (TurnSignalStalk) llama SetSignal(Left/Right/Off).
    ///
    /// Cada "luz" es un Renderer al que se le swapea el material entre litMat (encendida) y dimMat
    /// (apagada). Va en el auto del player.
    /// </summary>
    public class TurnSignalController : MonoBehaviour
    {
        public static TurnSignalController Instance { get; private set; }

        [Header("Luces (Renderers)")]
        [Tooltip("Luces del lado izquierdo: delantera-izq, trasera-izq, indicador tablero izq.")]
        [SerializeField] private Renderer[] leftLights;
        [Tooltip("Luces del lado derecho: delantera-der, trasera-der, indicador tablero der.")]
        [SerializeField] private Renderer[] rightLights;

        [Header("Materiales")]
        [SerializeField] private Material litMat;
        [SerializeField] private Material dimMat;

        [Header("Parpadeo")]
        [Tooltip("Segundos por medio ciclo (on u off). 0.4 ≈ 1.25 Hz.")]
        [SerializeField] private float blinkInterval = 0.4f;

        private TurnSignal state = TurnSignal.Off;
        private float blinkTimer;
        private bool blinkOn = true;

        public TurnSignal State => state;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start() => ApplyLights();

        /// <summary>Cambia el guiño activo. Off apaga todo.</summary>
        public void SetSignal(TurnSignal s)
        {
            if (s == state) return;
            state = s;
            blinkTimer = 0f;
            blinkOn = true;     // arranca encendido al accionar
            ApplyLights();
        }

        void Update()
        {
            if (state == TurnSignal.Off) return;
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                blinkOn = !blinkOn;
                ApplyLights();
            }
        }

        private void ApplyLights()
        {
            SetMat(leftLights,  state == TurnSignal.Left  && blinkOn);
            SetMat(rightLights, state == TurnSignal.Right && blinkOn);
        }

        private void SetMat(Renderer[] lights, bool on)
        {
            if (lights == null) return;
            var m = on ? litMat : dimMat;
            if (m == null) return;
            foreach (var r in lights) if (r != null) r.sharedMaterial = m;
        }
    }
}
