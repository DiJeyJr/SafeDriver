using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// El juego es solo joystick: si el usuario suelta los controles, el OS de Quest pasa
    /// solo a hand tracking optico y aparecen manos trackeadas que toman input. Este
    /// componente apaga la rama OVRHands del rig ISDK cuando el input activo deja de ser
    /// Touch, matando visuales e input de manos trackeadas de una. Con los joysticks en
    /// mano la rama queda activa: la necesitan las manos sinteticas controller-driven
    /// (controllerDrivenHandPosesType = Natural, que usa el mismo pipeline).
    ///
    /// No se puede resolver por config: handTrackingSupport = ControllersOnly tambien
    /// romperia las manos sinteticas, y el SDK v85 no tiene kill-switch runtime.
    ///
    /// Se auto-instancia en todas las escenas (no requiere wiring).
    /// </summary>
    public class ControllerOnlyHands : MonoBehaviour
    {
        private GameObject handsBranch; // GO "OVRHands" del OVRInteractionComprehensive de la escena

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ControllerOnlyHands>() != null) return;
            var go = new GameObject("ControllerOnlyHands");
            DontDestroyOnLoad(go);
            go.AddComponent<ControllerOnlyHands>();
        }

        void LateUpdate()
        {
            // La referencia muere con cada cambio de escena (el rig es por-escena): re-buscar.
            if (handsBranch == null)
            {
                handsBranch = GameObject.Find("OVRHands");
                if (handsBranch == null) return; // escena sin rig ISDK
            }

            // Touch = LTouch|RTouch. Si el OS paso a Hands (o None), los joysticks no estan en mano.
            bool controllersHeld = (OVRInput.GetActiveController() & OVRInput.Controller.Touch) != 0;
            if (handsBranch.activeSelf != controllersHeld)
                handsBranch.SetActive(controllersHeld);
        }
    }
}
