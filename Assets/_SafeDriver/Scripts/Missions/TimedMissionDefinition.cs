using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Mision contable con limite de tiempo. Si se agota el tiempo antes de
    /// llegar al requiredCount, la mision falla.
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/Misiones/Con tiempo", fileName = "Mission_Timed")]
    public class TimedMissionDefinition : MissionDefinition
    {
        [Header("Contable")]
        public ActionType action;
        [Min(1)] public int requiredCount = 1;

        [Header("Tiempo")]
        [Tooltip("Segundos disponibles para completar.")]
        public float timeLimitSeconds = 60f;

        [Tooltip("Si true el timer arranca al iniciar el nivel; si false, al primer progreso.")]
        public bool startOnLevelBegin = true;

        public override MissionRuntime CreateRuntime() => new TimedMissionRuntime(this);
    }
}
