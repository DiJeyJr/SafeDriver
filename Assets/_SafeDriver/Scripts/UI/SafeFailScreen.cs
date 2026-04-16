using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Pantalla de retroalimentacion pedagogica.
    /// Reemplaza COMPLETAMENTE la visualizacion de accidentes — SafeDriver NUNCA muestra choques.
    /// Muestra titulo, descripcion didactica y referencia a la Ley 24.449.
    /// Fade in suave, botones Reintentar / Menu Principal.
    /// </summary>
    public class SafeFailScreen : MonoBehaviour
    {
        [Header("UI")]
        public Canvas safeFailCanvas;
        public TextMeshPro titleText;
        public TextMeshPro descriptionText;
        public TextMeshPro lawReferenceText;
        public Button retryButton;
        public Button mainMenuButton;

        [Header("Fade")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        // Textos pedagogicos por tipo de infraccion (Ley Nacional de Transito 24.449)
        private static readonly Dictionary<InfractionType, (string title, string desc, string law)>
            messages = new Dictionary<InfractionType, (string, string, string)>
        {
            [InfractionType.RanRedLight] = (
                "Semaforo en Rojo",
                "Pasaste el semaforo cuando estaba en rojo. "
                + "Debes detenerte completamente en la linea de detencion y esperar la luz verde.",
                "Ley 24.449 — Art. 43: Senales semaforicas"
            ),
            [InfractionType.PedestrianNotYielded] = (
                "Prioridad Peatonal",
                "El peaton tiene prioridad absoluta en la senda peatonal. "
                + "Debes frenar y esperar que cruce completamente antes de avanzar.",
                "Ley 24.449 — Art. 41: Derechos y obligaciones del peaton"
            ),
            [InfractionType.FailedToStopAtSign] = (
                "Senal de PARE",
                "No te detuviste completamente ante la senal de PARE. "
                + "Debes parar, verificar que la via este despejada y recien entonces avanzar.",
                "Ley 24.449 — Art. 44: Senales de transito"
            ),
            [InfractionType.Speeding] = (
                "Exceso de Velocidad",
                "Superaste el limite de velocidad de esta zona. "
                + "Respetar los limites protege tu vida y la de los demas.",
                "Ley 24.449 — Art. 51: Limites de velocidad"
            ),
            [InfractionType.NoMirrorCheck] = (
                "Espejos No Chequeados",
                "Iniciaste una maniobra de giro sin verificar los espejos retrovisores. "
                + "Siempre chequea tus espejos antes de cambiar de carril o girar.",
                "Ley 24.449 — Art. 39: Requisitos para girar"
            ),
            [InfractionType.DangerousManeuver] = (
                "Maniobra Peligrosa",
                "Realizaste una maniobra que pone en riesgo a otros participantes del transito. "
                + "Conduce de forma previsible y segura.",
                "Ley 24.449 — Art. 48: Prohibiciones al conductor"
            ),
        };

        private static readonly (string title, string desc, string law) fallbackMessage = (
            "Infraccion",
            "Se detecto una infraccion de transito. Revisa las normas de conduccion segura.",
            "Ley 24.449 — Ley Nacional de Transito"
        );

        void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryPressed);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuPressed);

            Hide();
        }

        /// <summary>Muestra la pantalla pedagogica para el tipo de infraccion dado. Fade in suave.</summary>
        public void Show(InfractionType type)
        {
            var (title, desc, law) = messages.ContainsKey(type) ? messages[type] : fallbackMessage;

            if (titleText != null)        titleText.text        = title;
            if (descriptionText != null)  descriptionText.text  = desc;
            if (lawReferenceText != null) lawReferenceText.text = law;

            if (safeFailCanvas != null)   safeFailCanvas.enabled = true;

            StartCoroutine(FadeIn());
        }

        /// <summary>Overload legacy por si algun caller todavia pasa string.</summary>
        public void Show(string reason)
        {
            // Intentar matchear por contenido del mensaje (fallback graceful)
            if (titleText != null)       titleText.text        = "Infraccion";
            if (descriptionText != null) descriptionText.text  = reason;
            if (lawReferenceText != null) lawReferenceText.text = "";

            if (safeFailCanvas != null)  safeFailCanvas.enabled = true;
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (safeFailCanvas != null) safeFailCanvas.enabled = false;
        }

        // ============================================================
        //   Botones
        // ============================================================

        private void OnRetryPressed()
        {
            Hide();
            if (GameManager.Instance != null)
                GameManager.Instance.TransitionTo(GameState.Driving);
        }

        private void OnMainMenuPressed()
        {
            Hide();
            if (GameManager.Instance != null)
                GameManager.Instance.TransitionTo(GameState.MainMenu);
        }

        // ============================================================
        //   Fade
        // ============================================================

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime; // unscaled porque TimeScale puede ser 0 en SafeFail
                canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }
}
