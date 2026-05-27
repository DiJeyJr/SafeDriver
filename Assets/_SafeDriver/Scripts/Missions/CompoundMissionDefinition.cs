using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Mision compuesta: se completa cuando todas sus sub-misiones se completan.
    /// Si failOnAnySubFailure y alguna sub-mision falla, la compuesta falla.
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/Misiones/Compuesta", fileName = "Mission_Compound")]
    public class CompoundMissionDefinition : MissionDefinition
    {
        [Header("Compuesta")]
        [Tooltip("Sub-misiones que deben completarse. Pueden ser de cualquier tipo, incluso otras compuestas.")]
        public MissionDefinition[] subMissions;

        [Tooltip("Si alguna sub-mision falla, la compuesta falla inmediatamente.")]
        public bool failOnAnySubFailure = true;

        public override MissionRuntime CreateRuntime() => new CompoundMissionRuntime(this);
    }
}
