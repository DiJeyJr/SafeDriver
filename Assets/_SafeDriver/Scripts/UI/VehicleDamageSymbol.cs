using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.Scoring;

namespace SafeDriver.UI
{
    /// <summary>
    /// Muestra la integridad del auto: un simbolo top-down con 8 zonas (carroceria + 4 ruedas) que van
    /// de verde a rojo segun el daño, mas un % total. Se actualiza al recibir OnChanged de
    /// VehicleIntegrity. Se usa igual en el tablero (live) y en la pantalla de fin de nivel.
    /// </summary>
    public class VehicleDamageSymbol : MonoBehaviour
    {
        [Header("Zonas (orden EXACTO de IntegrityZone)")]
        [Tooltip("8 imagenes: Front, Rear, LeftSide, RightSide, WheelFL, WheelFR, WheelRL, WheelRR.")]
        [SerializeField] private Image[] zoneImages = new Image[8];

        [Header("Total")]
        [SerializeField] private TMP_Text percentText;

        [Header("Colores")]
        [SerializeField] private Color fullColor  = new Color(0.30f, 0.78f, 0.36f); // verde (100%)
        [SerializeField] private Color emptyColor = new Color(0.85f, 0.22f, 0.22f); // rojo (0%)

        private VehicleIntegrity tracked;

        void OnEnable()
        {
            TryHook();
            Refresh();
        }

        void OnDisable()
        {
            if (tracked != null) tracked.OnChanged -= Refresh;
        }

        void Update()
        {
            // VehicleIntegrity.Instance puede aparecer despues (orden de Awake entre escenas); reintenta.
            if (tracked == null) { TryHook(); if (tracked != null) Refresh(); }
        }

        private void TryHook()
        {
            if (tracked != null) return;
            tracked = VehicleIntegrity.Instance;
            if (tracked != null) tracked.OnChanged += Refresh;
        }

        /// <summary>Repinta las 8 zonas y el % desde el estado actual de VehicleIntegrity.</summary>
        public void Refresh()
        {
            if (tracked == null) return;
            for (int i = 0; i < zoneImages.Length && i < 8; i++)
            {
                if (zoneImages[i] == null) continue;
                float v = tracked.GetZone01((IntegrityZone)i);
                zoneImages[i].color = Color.Lerp(emptyColor, fullColor, v);
            }
            if (percentText != null) percentText.text = tracked.OverallPercent + "%";
        }
    }
}
