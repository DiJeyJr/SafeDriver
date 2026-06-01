using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Notificacion breve de "accion bien hecha" que aparece apenas arriba del volante y
    /// se desvanece sola en unos segundos. Escucha EventBus.OnCorrectActionPerformed.
    /// Solo visual (no requiere interaccion). Reemplaza al viejo popup del HUD, ahora
    /// dedicado solo a aciertos, con estilo lindo y mensajes amigables.
    /// </summary>
    public class SuccessToast : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Tiempos (segundos)")]
        [SerializeField] private float fadeInSeconds = 0.18f;
        [SerializeField] private float holdSeconds = 2.2f;
        [SerializeField] private float fadeOutSeconds = 0.5f;

        private static readonly Dictionary<ActionType, string> Messages = new()
        {
            { ActionType.StoppedAtRedLight,        "Frenaste en el rojo" },
            { ActionType.PassedGreenLight,         "Cruzaste en verde" },
            { ActionType.StoppedAtPareSign,        "Parada completa en PARE" },
            { ActionType.YieldedToPedestrian,      "Cediste el paso al peaton" },
            { ActionType.PedestrianNotPresent,     "Cruce despejado" },
            { ActionType.CheckedMirrorsBeforeTurn, "Chequeaste los espejos" },
            { ActionType.MaintainedLegalSpeed,     "Velocidad correcta" },
        };

        private Coroutine active;

        void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        void OnEnable()  => EventBus.OnCorrectActionPerformed += Show;
        void OnDisable() => EventBus.OnCorrectActionPerformed -= Show;

        private void Show(ActionType type, int bonus)
        {
            string body = Messages.TryGetValue(type, out var m) ? m : "Bien hecho";
            if (messageText != null)
                messageText.text = $"<b>¡Muy bien!</b>\n{body}   <b>+{bonus}</b>";

            if (active != null) StopCoroutine(active);
            active = StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            yield return Fade(0f, 1f, fadeInSeconds);
            float t = 0f;
            while (t < holdSeconds) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Fade(1f, 0f, fadeOutSeconds);
            active = null;
        }

        private IEnumerator Fade(float from, float to, float dur)
        {
            if (canvasGroup == null) yield break;
            if (dur <= 0f) { canvasGroup.alpha = to; yield break; }
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
