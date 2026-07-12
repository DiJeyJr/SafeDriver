using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeDriver.UI
{
    /// <summary>
    /// Manejo del menu principal. Wireado a los OnClick de los botones via Inspector.
    ///
    /// - Jugar: abre el panel de seleccion de niveles (o, si no hay panel asignado,
    ///   carga directo la escena de gameplaySceneName como fallback legacy).
    /// - Salir: cierra la aplicacion (en editor, sale de Play Mode).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Seleccion de niveles")]
        [Tooltip("Panel de seleccion de niveles. Si esta asignado, Jugar lo abre en vez de cargar una escena directa.")]
        [SerializeField] private LevelSelectPanel levelSelectPanel;

        [Header("Escenas (fallback si no hay panel)")]
        [Tooltip("Nombre de la escena de gameplay a cargar al presionar Jugar si no hay panel de niveles. Debe estar en Build Settings.")]
        [SerializeField] private string gameplaySceneName = "Level_01_Basics";

        public void OnPlayPressed()
        {
            if (levelSelectPanel != null)
            {
                levelSelectPanel.Show();
                return;
            }

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
