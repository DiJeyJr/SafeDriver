using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Borra los '[BuildingBlock] Hand Tracking left/right' de la escena abierta.
    /// El juego es solo joystick: esos BBs estaban inactivos y sin uso en MainMenu
    /// (el nivel jugable no los tiene). Las manos que se ven con joystick vienen del
    /// rig ISDK (controllerDrivenHandPosesType = Natural), no de estos bloques.
    /// </summary>
    public static class RemoveHandTrackingBlocks
    {
        [MenuItem("SafeDriver/VR/Quitar Hand Tracking BBs (escena abierta)")]
        public static void Run()
        {
            int borrados = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (t.name == "[BuildingBlock] Hand Tracking left" || t.name == "[BuildingBlock] Hand Tracking right")
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    borrados++;
                }
            }

            if (borrados > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[HandTracking] Building Blocks de hand tracking borrados: " + borrados + ".");
        }
    }
}
