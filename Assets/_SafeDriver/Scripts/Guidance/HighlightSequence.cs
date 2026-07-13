using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Tutorial de elementos en orden: resalta los markers de la lista de a UNO. Cuando
    /// el actual se apaga (el jugador agarro la palanca, paso el timer, etc.), se prende
    /// el siguiente. Ej: espejos -> freno de mano -> palanca de cambios -> guinie.
    ///
    /// Los markers de la lista deben tener showOnStart desactivado (los prende esta
    /// secuencia). El armado automatico lo hace el menu SafeDriver/Guidance.
    /// </summary>
    public class HighlightSequence : MonoBehaviour
    {
        [Tooltip("Markers en orden de tutorial. Cada uno se prende cuando se apaga el anterior.")]
        [SerializeField] private HighlightMarker[] pasos;

        [Tooltip("Cartel 'Chequeo de elementos en curso' visible mientras corre la secuencia (opcional).")]
        [SerializeField] private GameObject cartel;

        private int actual = -1;

        void Start()
        {
            if (cartel != null) cartel.SetActive(true);
            Next();
        }

        private void Next()
        {
            // Desuscribir el anterior
            if (actual >= 0 && actual < pasos.Length && pasos[actual] != null)
                pasos[actual].Dismissed -= Next;

            actual++;
            // Saltear huecos nulos
            while (actual < pasos.Length && pasos[actual] == null) actual++;
            if (actual >= pasos.Length)
            {
                // Tutorial completo: chau cartel.
                if (cartel != null) cartel.SetActive(false);
                return;
            }

            pasos[actual].Dismissed += Next;
            pasos[actual].Show();
        }

        void OnDestroy()
        {
            if (actual >= 0 && actual < pasos.Length && pasos[actual] != null)
                pasos[actual].Dismissed -= Next;
        }
    }
}
