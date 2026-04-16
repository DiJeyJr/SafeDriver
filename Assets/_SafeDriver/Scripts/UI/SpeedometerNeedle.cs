using UnityEngine;

namespace SafeDriver.UI
{
    /// <summary>
    /// Rota la aguja del velocimetro diegetico (integrado en el tablero del auto).
    /// La aguja rota alrededor de su eje Z local, mapeando velocidad 0..maxSpeed
    /// al rango de angulos minAngle..maxAngle.
    ///
    /// Setup tipico:
    ///   - La aguja es un child de un fondo circular de velocimetro
    ///   - El pivot de la aguja esta en su base (donde "se clava" al centro del velocimetro)
    ///   - minAngle = 135 (posicion de "0 km/h", a las ~8 del reloj)
    ///   - maxAngle = -135 (posicion de "maxSpeed", a las ~4 del reloj)
    /// </summary>
    public class SpeedometerNeedle : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Velocidad maxima que muestra el velocimetro (km/h).")]
        [SerializeField] private float maxSpeed = 120f;

        [Tooltip("Angulo Z local de la aguja cuando la velocidad es 0. Tipico: 135 (8 del reloj).")]
        [SerializeField] private float minAngle = 135f;

        [Tooltip("Angulo Z local de la aguja cuando la velocidad es maxSpeed. Tipico: -135 (4 del reloj).")]
        [SerializeField] private float maxAngle = -135f;

        [Tooltip("Suavizado de la aguja (0 = instantaneo, mayor = mas suave). Recomendado: 5-10.")]
        [SerializeField] private float smoothSpeed = 8f;

        private float targetAngle;
        private float currentAngle;

        void Start()
        {
            currentAngle = minAngle;
            ApplyRotation(currentAngle);
        }

        /// <summary>Setea la velocidad objetivo. La aguja se mueve suavemente hacia ella.</summary>
        public void SetSpeed(float speedKmh)
        {
            float t = Mathf.Clamp01(speedKmh / maxSpeed);
            targetAngle = Mathf.Lerp(minAngle, maxAngle, t);
        }

        void Update()
        {
            if (!Mathf.Approximately(currentAngle, targetAngle))
            {
                currentAngle = smoothSpeed > 0f
                    ? Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed)
                    : targetAngle;
                ApplyRotation(currentAngle);
            }
        }

        private void ApplyRotation(float angle)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
