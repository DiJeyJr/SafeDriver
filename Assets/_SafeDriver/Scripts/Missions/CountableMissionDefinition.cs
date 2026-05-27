using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Mision que se completa al realizar una accion N veces.
    /// Ej: "Detenerse en semaforo rojo x1", "Chequear espejos x2".
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/Misiones/Contable", fileName = "Mission_Countable")]
    public class CountableMissionDefinition : MissionDefinition
    {
        [Header("Contable")]
        [Tooltip("Accion del EventBus que cuenta para esta mision.")]
        public ActionType action;

        [Min(1)]
        [Tooltip("Cuantas veces hay que hacer la accion para completar.")]
        public int requiredCount = 1;

        public override MissionRuntime CreateRuntime() => new CountableMissionRuntime(this);
    }
}
