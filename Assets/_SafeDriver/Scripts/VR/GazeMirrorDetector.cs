using System.Collections.Generic;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.VR
{
    /// <summary>
    /// Deteccion de espejos via Eye Gaze (Meta XR Eye Gaze BB).
    /// Hace raycast desde la pose de cada OVREyeGaze hacia delante; cuando el ray hit un
    /// Collider con tag = mirrorTag, dispara CheckedMirrorsBeforeTurn (rising edge por mirror).
    ///
    /// Si el hardware NO soporta eye tracking (Quest 3, etc.), OVREyeGaze.EyeTrackingEnabled
    /// es false y el detector no hace nada. En ese caso el HeadTrackingDetector (basado en
    /// head pose) sigue funcionando como fallback — ambos pueden coexistir.
    ///
    /// Marcar los espejos (MirrorCenter, MirrorLeft, MirrorRight) con tag "Mirror" y un
    /// Collider para que el raycast los detecte. Reusable: cualquier GameObject con ese
    /// tag suma puntos al ser mirado.
    /// </summary>
    public class GazeMirrorDetector : MonoBehaviour
    {
        [Tooltip("Tag de los espejos a detectar.")]
        [SerializeField] private string mirrorTag = "Mirror";

        [Tooltip("Distancia maxima del raycast desde el ojo.")]
        [SerializeField] private float maxDistance = 5f;

        [Tooltip("Layers a considerar en el raycast.")]
        [SerializeField] private LayerMask layerMask = ~0;

        private OVREyeGaze[] eyeGazes;
        private readonly HashSet<int> checkedMirrors = new HashSet<int>();
        private bool eyeTrackingAvailable;

        void Start()
        {
            eyeGazes = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None);
            RecheckAvailability();
        }

        void Update()
        {
            if (!eyeTrackingAvailable)
            {
                // Re-chequear de vez en cuando — el permiso puede otorgarse despues del Start
                if (Time.frameCount % 120 == 0) RecheckAvailability();
                return;
            }

            for (int i = 0; i < eyeGazes.Length; i++)
            {
                var gaze = eyeGazes[i];
                if (gaze == null || !gaze.enabled) continue;

                if (Physics.Raycast(gaze.transform.position, gaze.transform.forward,
                                    out RaycastHit hit, maxDistance, layerMask))
                {
                    if (hit.collider.CompareTag(mirrorTag))
                    {
                        int id = hit.collider.gameObject.GetInstanceID();
                        if (checkedMirrors.Add(id))
                        {
                            EventBus.Dispatch_CorrectAction(
                                ActionType.CheckedMirrorsBeforeTurn,
                                ActionPoints.CheckedMirrorsBeforeTurn);
                        }
                    }
                }
            }
        }

        private void RecheckAvailability()
        {
            eyeTrackingAvailable = false;
            if (eyeGazes == null || eyeGazes.Length == 0)
            {
                eyeGazes = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None);
            }
            foreach (var gaze in eyeGazes)
            {
                if (gaze != null && gaze.EyeTrackingEnabled)
                {
                    eyeTrackingAvailable = true;
                    break;
                }
            }
        }

        /// <summary>Limpia los espejos chequeados — llamar al iniciar una nueva maniobra.</summary>
        public void ResetMirrorChecks() => checkedMirrors.Clear();
    }
}
