using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeDriver.UI
{
    /// <summary>
    /// Controla un menu de pausa diegetico: lo abre y cierra, congela el tiempo del juego,
    /// y posiciona el canvas frente al `head` (CenterEyeAnchor) cada vez que aparece — asi
    /// el menu siempre queda en el campo visual del jugador, sin importar a donde mire.
    ///
    /// Las acciones se exponen como metodos publicos para que Button.onClick las wiree desde
    /// Inspector. Se llama desde el `ControllerButtonsMapper` via `TogglePause()`.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Referencias")]
        [Tooltip("GameObject del canvas que se muestra/oculta. Si queda vacio, se usa este GameObject.")]
        [SerializeField] private GameObject menuRoot;

        [Tooltip("Transform de la cabeza (tipicamente CenterEyeAnchor). Si queda vacio se busca por nombre en Awake.")]
        [SerializeField] private Transform head;

        [Header("Posicionamiento")]
        [Tooltip("Distancia (m) entre la cabeza y el canvas cuando se abre el menu.")]
        [SerializeField] private float distance = 0.8f;

        [Tooltip("Offset vertical (m) respecto a la altura de los ojos.")]
        [SerializeField] private float verticalOffset = -0.05f;

        [Header("Escenas")]
        [Tooltip("Nombre de la escena del menu principal a cargar con 'Volver al menu'.")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public bool IsPaused { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (menuRoot == null) menuRoot = gameObject;
            if (head == null)
            {
                var center = GameObject.Find("CenterEyeAnchor");
                if (center != null) head = center.transform;
            }

            // Asegurar que arranca cerrado
            SetVisible(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // Garantizar que no quede el tiempo congelado si se destruye con pause activo
            Time.timeScale = 1f;
        }

        // ============================================================
        //   API publica (wireada desde Button.onClick y desde Mapper)
        // ============================================================

        /// <summary>Abre o cierra el menu segun su estado actual.</summary>
        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        /// <summary>Pausa el juego y muestra el menu frente al jugador.</summary>
        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            PositionInFrontOfHead();
            SetVisible(true);
        }

        /// <summary>Cierra el menu y reanuda el tiempo.</summary>
        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            SetVisible(false);
        }

        /// <summary>Recarga la escena actual.</summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            var s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.name);
        }

        /// <summary>Carga la escena del menu principal.</summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            if (string.IsNullOrEmpty(mainMenuSceneName)) return;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ============================================================
        //   Internos
        // ============================================================

        private void SetVisible(bool visible)
        {
            if (menuRoot == null) return;
            menuRoot.SetActive(visible);
        }

        /// <summary>
        /// Coloca el canvas a `distance` adelante de la cabeza, sobre el plano horizontal
        /// (proyectando el forward al plano XZ) para que no quede inclinado si el jugador
        /// mira al piso o al techo.
        /// </summary>
        private void PositionInFrontOfHead()
        {
            if (menuRoot == null || head == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            menuRoot.transform.position = head.position + forward * distance + Vector3.up * verticalOffset;
            menuRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
