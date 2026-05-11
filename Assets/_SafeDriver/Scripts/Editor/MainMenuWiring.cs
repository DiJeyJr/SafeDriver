using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Utilidad one-shot para wirear los botones del menu principal a MainMenuController
    /// y agregar EventSystem con PointableCanvasModule. Se ejecuta desde el menu SafeDriver.
    /// </summary>
    public static class MainMenuWiring
    {
        [MenuItem("SafeDriver/Wire Main Menu")]
        public static void Run()
        {
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null) { Debug.LogError("No se encontro MainMenuController en la escena."); return; }

            WireButton("PlayButton", nameof(MainMenuController.OnPlayPressed), controller);
            WireButton("QuitButton", nameof(MainMenuController.OnQuitPressed), controller);

            CreateEventSystemIfMissing();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Main Menu wireado.");
        }

        private static void WireButton(string buttonName, string method, MainMenuController controller)
        {
            var go = GameObject.Find(buttonName);
            if (go == null) { Debug.LogError("No se encontro " + buttonName); return; }

            var btn = go.GetComponent<Button>();
            if (btn == null) { Debug.LogError(buttonName + " no tiene Button"); return; }

            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(btn.onClick, i);

            var call = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), controller, method);
            UnityEventTools.AddPersistentListener(btn.onClick, call);
        }

        private static void CreateEventSystemIfMissing()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<Oculus.Interaction.PointableCanvasModule>();
        }
    }
}
