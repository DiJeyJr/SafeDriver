using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Testigo de freno de mano del tablero (al lado de la letra de marcha): rojo
    /// encendido con el freno puesto, gris tenue sin freno. Escucha
    /// EventBus.OnHandbrakeChanged (lo dispara VehicleController.SetHandbrake).
    /// </summary>
    public class HandbrakeLight : MonoBehaviour
    {
        [Tooltip("Grafico del testigo. Vacio = la Image de este GameObject.")]
        [SerializeField] private Graphic lamp;

        [Header("Colores")]
        [SerializeField] private Color onColor = new Color(0.945f, 0.38f, 0.38f);
        [SerializeField] private Color offColor = new Color(0.3f, 0.3f, 0.3f, 0.35f);

        void Awake()
        {
            if (lamp == null) lamp = GetComponent<Graphic>();
        }

        void OnEnable()
        {
            EventBus.OnHandbrakeChanged += Apply;
            Apply(false); // apagado hasta que el vehiculo avise el estado real
        }

        void OnDisable() => EventBus.OnHandbrakeChanged -= Apply;

        private void Apply(bool engaged)
        {
            if (lamp != null) lamp.color = engaged ? onColor : offColor;
        }
    }
}
