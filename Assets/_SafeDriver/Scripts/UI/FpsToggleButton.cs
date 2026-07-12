using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SafeDriver.UI
{
    /// <summary>
    /// Boton del menu que prende/apaga el indicador de FPS (FpsCounter.Visible, persistido).
    /// Se auto-cablea al Button del mismo GameObject en runtime; el label muestra la accion
    /// ("Ocultar FPS" / "Mostrar FPS").
    /// </summary>
    public class FpsToggleButton : MonoBehaviour
    {
        private TextMeshProUGUI label;

        void Awake()
        {
            label = GetComponentInChildren<TextMeshProUGUI>(true);
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(Toggle);
            RefreshLabel();
        }

        private void Toggle()
        {
            FpsCounter.Visible = !FpsCounter.Visible;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label != null)
                label.text = FpsCounter.Visible ? "Ocultar FPS" : "Mostrar FPS";
        }
    }
}
