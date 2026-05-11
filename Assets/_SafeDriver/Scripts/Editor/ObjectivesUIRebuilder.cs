using UnityEditor;
using UnityEngine;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Reconstruye en editor la UI de todos los ObjectivesController de la escena activa.
    /// El controller no construye en Awake (es runtime-only) asi que esta utility lo dispara
    /// manualmente cuando se cambia el array de objetivos o el estilo.
    /// </summary>
    public static class ObjectivesUIRebuilder
    {
        [MenuItem("SafeDriver/Rebuild Objectives UI")]
        public static void Rebuild()
        {
            var controllers = Object.FindObjectsByType<ObjectivesController>(FindObjectsSortMode.None);
            if (controllers.Length == 0)
            {
                Debug.LogWarning("No se encontraron ObjectivesController en la escena.");
                return;
            }

            foreach (var c in controllers)
            {
                Undo.RegisterFullObjectHierarchyUndo(c.gameObject, "Rebuild Objectives UI");
                c.BuildUI();
                EditorUtility.SetDirty(c);
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Rebuild Objectives UI: {controllers.Length} controller(s) reconstruido(s).");
        }
    }
}
