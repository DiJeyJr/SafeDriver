using UnityEngine;
using TMPro;

namespace SafeDriver.UI
{
    /// <summary>
    /// Tema visual centralizado de la UI: paleta, tipografia y metricas.
    ///
    /// Direccion "manual del conductor + senaletica vial": fondos crema claros,
    /// acentos de senal vial (verde avanzar, rojo detener, ambar precaucion, azul
    /// info), esquinas redondeadas y tipografia con presencia, legible a distancia
    /// en VR. Un solo asset (EduTheme) es la fuente de verdad; todo restyle lo lee.
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Superficies (crema de manual)")]
        public Color backgroundBase = new Color(0.969f, 0.957f, 0.925f); // #F7F4EC
        public Color surface        = new Color(1.000f, 1.000f, 1.000f); // #FFFFFF
        public Color surfaceRaised  = new Color(0.988f, 0.984f, 0.972f); // crema muy claro

        [Header("Texto")]
        public Color textPrimary   = new Color(0.165f, 0.192f, 0.259f);  // #2A3142
        public Color textSecondary = new Color(0.361f, 0.400f, 0.467f);  // #5C6677
        public Color textMuted     = new Color(0.600f, 0.620f, 0.660f);
        public Color textOnAccent  = new Color(1.000f, 1.000f, 1.000f);  // texto sobre botones de color

        [Header("Acentos (senaletica vial)")]
        public Color brand   = new Color(0.176f, 0.490f, 0.824f);  // #2D7DD2 azul info / marca
        public Color success = new Color(0.239f, 0.682f, 0.353f);  // #3DAE5A verde semaforo
        public Color danger  = new Color(0.839f, 0.271f, 0.314f);  // #D64550 rojo senal
        public Color warning = new Color(0.949f, 0.663f, 0.231f);  // #F2A93B ambar

        [Header("Bordes / lineas")]
        public Color border = new Color(0.886f, 0.863f, 0.808f);   // #E2DCCE calido

        [Header("Sprites")]
        [Tooltip("Sprite 9-sliced redondeado para paneles y botones. El editor asigna el UISprite built-in de Unity.")]
        public Sprite roundedSprite;

        [Header("Tipografia")]
        [Tooltip("Fuente de titulos (presencia). Recomendado: Oswald Bold SDF.")]
        public TMP_FontAsset titleFont;
        [Tooltip("Fuente de cuerpo/botones (legible). Recomendado: LiberationSans SDF.")]
        public TMP_FontAsset bodyFont;
        public float titleSize   = 42f;
        public float headingSize = 30f;
        public float bodySize    = 22f;
        public float buttonSize  = 24f;

        [Header("Metricas")]
        public float spacingBase = 8f;     // unidad base; usar multiplos
        public float radiusSmall = 12f;    // botones / inputs
        public float radiusCard  = 18f;    // paneles
        public float radiusModal = 28f;    // modales grandes

        [Header("Sombra (depth approachable)")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.18f);
        public Vector2 shadowDistance = new Vector2(0f, -3f);
    }
}
