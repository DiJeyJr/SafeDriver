using UnityEngine;
using Oculus.Interaction;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Apaga el HighlightMarker cuando el jugador agarra el elemento (Grabbable con al
    /// menos un punto de seleccion). Va en el mismo GameObject que el marker; el
    /// Grabbable se busca en el padre si no se asigna.
    /// </summary>
    [RequireComponent(typeof(HighlightMarker))]
    public class DismissOnGrab : MonoBehaviour
    {
        [Tooltip("Grabbable a observar. Vacio = se busca en este GameObject o sus padres.")]
        [SerializeField] private Grabbable grabbable;

        private HighlightMarker marker;

        void Start()
        {
            marker = GetComponent<HighlightMarker>();
            if (grabbable == null) grabbable = GetComponentInParent<Grabbable>();
            if (grabbable == null)
            {
                Debug.LogWarning("[DismissOnGrab] No se encontro Grabbable — el resalte no se va a apagar al agarrar.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (marker.IsShowing && grabbable.SelectingPointsCount > 0)
            {
                marker.Dismiss();
                enabled = false;
            }
        }
    }
}
