using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Clase base abstracta para todos los detectores de infracciones.
    /// Cada tipo de infraccion tiene su propio MonoBehaviour que hereda de esta clase.
    /// Subclases solo necesitan definir la logica de deteccion (usando sensores, EventBus, triggers, etc.)
    /// y llamar TriggerInfraction() o TriggerCorrectAction() cuando corresponda.
    ///
    /// NOTA ARQUITECTURAL: el haptic feedback NO se llama directamente desde aca (Scoring no referencia VR).
    /// HapticsController en VR/ se auto-suscribe a OnInfractionDetected/OnCorrectActionPerformed del EventBus
    /// y reacciona solo. El efecto en runtime es identico al del spec original:
    ///   TriggerInfraction() → EventBus → HapticsController → vibrar.
    /// </summary>
    public abstract class InfractionDetector : MonoBehaviour
    {
        /// <summary>Mensaje pedagogico asociado (referencia a la Ley 24.449 o explicacion didactica).</summary>
        [TextArea(2, 4)]
        [SerializeField] protected string pedagogicalMessage = "";

        /// <summary>Tipo de infraccion que este detector maneja.</summary>
        [SerializeField] protected InfractionType infractionType;

        /// <summary>
        /// Llama cuando se detecta la infraccion. Publica al EventBus (HapticsController reacciona auto).
        /// </summary>
        protected void TriggerInfraction()
        {
            EventBus.Dispatch_Infraction(infractionType, pedagogicalMessage);
        }

        /// <summary>
        /// Llama cuando se detecta una accion correcta. Publica al EventBus con bonus de ActionPoints.
        /// </summary>
        protected void TriggerCorrectAction(ActionType action)
        {
            EventBus.Dispatch_CorrectAction(action, ActionPoints.GetBonus(action));
        }

        /// <summary>Overload si se quiere un bonus custom que no sea el default de ActionPoints.</summary>
        protected void TriggerCorrectAction(ActionType action, int customBonus)
        {
            EventBus.Dispatch_CorrectAction(action, customBonus);
        }
    }
}
