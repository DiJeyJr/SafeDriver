using TMPro;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Indicador de marcha (D / N / R) en texto. Se suscribe a EventBus.OnGearChanged.
    ///
    /// En ReverseArming (timer de cambio de marcha), muestra "R" parpadeando para avisar
    /// al conductor que se esta por activar la reversa. En el resto de los estados el
    /// texto es fijo con el color correspondiente.
    /// </summary>
    public class GearIndicator : MonoBehaviour
    {
        [Tooltip("TextMeshPro que muestra la letra. Si queda vacio se busca en hijos.")]
        [SerializeField] private TMP_Text label;

        [Header("Colores")]
        [SerializeField] private Color driveColor = new Color(0.4f, 1f, 0.4f);
        [SerializeField] private Color reverseColor = new Color(1f, 0.5f, 0.3f);

        [Header("Blink")]
        [Tooltip("Hz del parpadeo durante ReverseArming.")]
        [SerializeField] private float blinkHz = 3f;

        private GearState currentGear = GearState.Drive;

        void Awake()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        }

        void OnEnable()
        {
            EventBus.OnGearChanged += HandleGearChanged;
            RefreshStatic();
        }

        void OnDisable()
        {
            EventBus.OnGearChanged -= HandleGearChanged;
        }

        void Update()
        {
            if (label == null) return;

            // Parpadeo on/off via alpha durante cualquier arming. Muestra la letra DESTINO.
            bool armingToReverse = currentGear == GearState.ReverseArming;
            bool armingToDrive = currentGear == GearState.DriveArming;
            if (!armingToReverse && !armingToDrive) return;

            bool on = Mathf.Repeat(Time.unscaledTime * blinkHz, 1f) < 0.5f;
            Color c = armingToReverse ? reverseColor : driveColor;
            c.a = on ? 1f : 0f;
            label.color = c;
            label.text = armingToReverse ? "R" : "D";
        }

        private void HandleGearChanged(GearState gear)
        {
            currentGear = gear;
            RefreshStatic();
        }

        private void RefreshStatic()
        {
            if (label == null) return;
            switch (currentGear)
            {
                case GearState.Drive:
                    label.text = "D";
                    label.color = driveColor;
                    break;
                case GearState.Reverse:
                    label.text = "R";
                    label.color = reverseColor;
                    break;
                case GearState.ReverseArming:
                    label.text = "R";
                    label.color = reverseColor;
                    break;
                case GearState.DriveArming:
                    label.text = "D";
                    label.color = driveColor;
                    break;
            }
        }
    }
}
