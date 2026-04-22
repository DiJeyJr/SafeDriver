using UnityEngine;

namespace SafeDriver.UI
{
    /// <summary>
    /// Configura un Canvas para VR segun las reglas obligatorias:
    /// - Render Mode: World Space (SIEMPRE — Screen Space causa problemas en VR)
    /// - Scale: 0.001 a 0.002 (texto en metros, no pixeles)
    /// - Layer: UI
    /// - Font size minimo: 0.06 Unity units (aprox 60pt a 1m)
    ///
    /// La distancia al jugador se maneja por posicionamiento del Canvas en escena
    /// (parentado al Camera Rig a 0.8-1.2m del CenterEyeAnchor, zona de confort VR).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VRCanvasSetup : MonoBehaviour
    {
        [Header("Reglas VR obligatorias")]
        [Tooltip("Escala del canvas. 0.001 = 1 pixel del canvas = 1mm en world. Rango: 0.001-0.002.")]
        [SerializeField] private float canvasScale = 0.001f;

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

            // 4. No event camera para world space VR (el ray/poke interactor de ISDK maneja raycasts)
            canvas.worldCamera = null;
        }
    }
}
