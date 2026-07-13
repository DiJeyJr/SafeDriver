using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Notificacion breve que aparece apenas arriba del volante y se desvanece sola.
    /// Escucha EventBus.OnCorrectActionPerformed (acierto, verde) y tambien
    /// OnInfractionDetected (advertencia, rojo) — salvo las infracciones graves, que ya
    /// tienen su pantalla SafeFail y no necesitan toast.
    /// Solo visual (no requiere interaccion).
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
            { ActionType.ReachedGoal,              "Llegaste a la meta" },
        };

        // Advertencias cortas por infraccion (el mensaje pedagogico completo queda para
        // el resumen de fin de nivel / SafeFail).
        private static readonly Dictionary<InfractionType, string> Warnings = new()
        {
            { InfractionType.FailedToStopAtSign, "No paraste en el PARE" },
            { InfractionType.Speeding,           "Exceso de velocidad" },
            { InfractionType.NoMirrorCheck,      "No chequeaste los espejos" },
            { InfractionType.DangerousManeuver,  "Maniobra peligrosa" },
            { InfractionType.WrongWay,           "Vas en contramano" },
        };

        // Estas ya muestran la pantalla SafeFail completa: sin toast para no duplicar.
        private static readonly HashSet<InfractionType> Graves = new()
        {
            InfractionType.RanRedLight,
            InfractionType.PedestrianNotYielded,
            InfractionType.HitPedestrian,
            InfractionType.SevereCollision,
        };

        private Coroutine active;

        void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        void OnEnable()
        {
            EventBus.OnCorrectActionPerformed += Show;
            EventBus.OnInfractionDetected += ShowWarning;
        }

        void OnDisable()
        {
            EventBus.OnCorrectActionPerformed -= Show;
            EventBus.OnInfractionDetected -= ShowWarning;
        }

        private void ShowWarning(InfractionType type, string message)
        {
            if (Graves.Contains(type)) return;

            string body = Warnings.TryGetValue(type, out var w) ? w : "Infraccion";
            if (messageText != null)
                messageText.text = $"<color=#F16161><b>¡Ojo!</b>\n{body}</color>";

            if (active != null) StopCoroutine(active);
            active = StartCoroutine(Routine());
        }

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
