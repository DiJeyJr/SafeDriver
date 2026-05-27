using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Mision que exige realizar acciones EN ORDEN. Una accion fuera de orden
    /// se ignora (no avanza ni falla). Ej: recorrer una ruta A -> B -> C.
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/Misiones/Secuencia", fileName = "Mission_Sequence")]
    public class SequenceMissionDefinition : MissionDefinition
    {
        [Header("Secuencia")]
        [Tooltip("Acciones en el orden requerido. Se completan de a una en orden.")]
        public ActionType[] steps;

        public override MissionRuntime CreateRuntime() => new SequenceMissionRuntime(this);
    }
}
