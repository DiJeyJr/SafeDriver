using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeDriver.UI
{
    /// <summary>
    /// Manejo del menu principal. Wireado a los OnClick de los botones via Inspector.
    ///
    /// - Jugar: carga la escena de gameplay configurada en gameplaySceneName.
    /// - Salir: cierra la aplicacion (en editor, sale de Play Mode).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Escenas")]
        [Tooltip("Nombre de la escena de gameplay a cargar al presionar Jugar. Debe estar en Build Settings.")]
        [SerializeField] private string gameplaySceneName = "Level_01_Basics";

        public void OnPlayPressed()
        {
            if (string.IsNullOrEmpty(gameplaySceneName))
            {
                Debug.LogError("[MainMenuController] gameplaySceneName vacio. Configurar en Inspector.", this);
                return;
            }
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
