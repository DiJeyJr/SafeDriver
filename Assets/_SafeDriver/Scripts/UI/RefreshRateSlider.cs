using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.VR;

namespace SafeDriver.UI
{
    /// <summary>
    /// Slider de opciones que cambia el limite de refresco del visor. El Quest soporta
    /// frecuencias discretas (72/80/90/120), asi que el slider snapea entre esas posiciones
    /// (wholeNumbers, 4 pasos). Se auto-cablea al Slider del mismo GameObject y persiste
    /// via DisplayRefreshRate.Target.
    /// </summary>
    public class RefreshRateSlider : MonoBehaviour
    {
        private static readonly float[] Rates = { 72f, 80f, 90f, 120f };

        [Tooltip("Label que muestra el valor elegido (ej. '90 Hz'). Si queda vacio se busca en hijos.")]
        [SerializeField] private TextMeshProUGUI valueLabel;

        private Slider slider;

        void Awake()
        {
            slider = GetComponent<Slider>();
            if (valueLabel == null) valueLabel = GetComponentInChildren<TextMeshProUGUI>(true);
            if (slider == null) return;

            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = Rates.Length - 1;
            slider.SetValueWithoutNotify(IndexFor(DisplayRefreshRate.Target));
            slider.onValueChanged.AddListener(OnChanged);
            RefreshLabel();
        }

        private void OnChanged(float index)
        {
            DisplayRefreshRate.Target = Rates[Mathf.Clamp((int)index, 0, Rates.Length - 1)];
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (valueLabel != null && slider != null)
                valueLabel.text = Rates[(int)slider.value] + " Hz";
        }

        private static int IndexFor(float hz)
        {
            // El indice cuyo rate quede mas cerca del target guardado.
            int mejor = 0;
            for (int i = 1; i < Rates.Length; i++)
                if (Mathf.Abs(Rates[i] - hz) < Mathf.Abs(Rates[mejor] - hz)) mejor = i;
            return mejor;
        }
    }
}
