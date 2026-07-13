using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Apaga el HighlightMarker cuando el jugador MIRA el elemento: head-gaze (la camara
    /// apuntando dentro de un cono hacia el target, sostenido un instante). Para espejos
    /// y elementos que no se agarran. Se usa head-gaze y no eye-tracking para que funcione
    /// siempre, sin depender del permiso de ojos del visor.
    /// </summary>
    [RequireComponent(typeof(HighlightMarker))]
    public class DismissOnLook : MonoBehaviour
    {
        [Tooltip("Angulo maximo (grados) entre la mirada y el elemento para contar como 'lo esta mirando'.")]
        [SerializeField] private float angleThreshold = 14f;

        [Tooltip("Segundos sostenidos mirando para dar por chequeado el elemento.")]
        [SerializeField] private float dwellSeconds = 0.5f;

        private HighlightMarker marker;
        private float lookTimer;

        void Start() => marker = GetComponent<HighlightMarker>();

        void Update()
        {
            if (marker == null || !marker.IsShowing) { lookTimer = 0f; return; }

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 to = transform.position - cam.transform.position;
            if (Vector3.Angle(cam.transform.forward, to) <= angleThreshold)
            {
                lookTimer += Time.deltaTime;
                if (lookTimer >= dwellSeconds)
                {
                    marker.Dismiss();
                    enabled = false;
                }
            }
            else lookTimer = 0f;
        }
    }
}
