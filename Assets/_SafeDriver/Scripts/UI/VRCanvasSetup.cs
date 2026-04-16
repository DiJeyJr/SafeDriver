using UnityEngine;

namespace SafeDriver.UI
{
    /// <summary>
    /// Configura un Canvas para VR segun las reglas obligatorias:
    /// - Render Mode: World Space (SIEMPRE — Screen Space causa problemas en VR)
    /// - Scale: 0.001 a 0.002 (texto en metros, no pixeles)
    /// - Layer: UI
    /// - Distancia al jugador: 0.8 a 1.2m
    /// - Font size minimo: 0.06 Unity units (aprox 60pt a 1m)
    /// - Posicion relativa al OVRCameraRig (se mueve con el jugador)
    ///
    /// Attach este componente al Canvas y llamar Setup() en Awake, o usar los
    /// metodos estaticos CreateDiegeticCanvas / CreateNotificationCanvas para
    /// generar uno completo por codigo.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VRCanvasSetup : MonoBehaviour
    {
        [Header("Reglas VR obligatorias")]
        [Tooltip("Escala del canvas. 0.001 = 1 pixel del canvas = 1mm en world. Rango: 0.001-0.002.")]
        [SerializeField] private float canvasScale = 0.001f;

        [Tooltip("Distancia al jugador en metros (zona de confort visual VR: 0.8-1.2m).")]
        [SerializeField] private float distanceFromPlayer = 1.0f;

        private Canvas canvas;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            Apply();
        }

        /// <summary>Aplica todas las reglas VR obligatorias al Canvas.</summary>
        public void Apply()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();

            // 1. Render mode: SIEMPRE World Space
            canvas.renderMode = RenderMode.WorldSpace;

            // 2. Layer: UI
            gameObject.layer = LayerMask.NameToLayer("UI");

            // 3. Scale: metros, no pixeles
            transform.localScale = Vector3.one * canvasScale;

            // 4. No event camera para world space VR (el OVR rig maneja raycasts)
            canvas.worldCamera = null;
        }
    }
}
