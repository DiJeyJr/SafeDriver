using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>Runtime de mision con tiempo: contable + timer que la hace fallar si se agota.</summary>
    public class TimedMissionRuntime : MissionRuntime
    {
        private readonly TimedMissionDefinition def;
        private int count;
        private float elapsed;
        private bool timerRunning;

        public TimedMissionRuntime(TimedMissionDefinition definition) : base(definition)
        {
            def = definition;
        }

        public override float Progress01 =>
            def.requiredCount <= 0 ? 1f : (float)count / def.requiredCount;

        public override string ProgressLabel
        {
            get
            {
                float remaining = Mathf.Max(0f, def.timeLimitSeconds - elapsed);
                string countPart = def.requiredCount > 1 ? $"{count}/{def.requiredCount}  " : string.Empty;
                return $"{countPart}{remaining:0}s";
            }
        }

        public override void Activate()
        {
            EventBus.OnCorrectActionPerformed += HandleAction;
            if (def.startOnLevelBegin) timerRunning = true;
        }

        public override void Deactivate() => EventBus.OnCorrectActionPerformed -= HandleAction;

        public override void Tick(float deltaTime)
        {
            if (!timerRunning) return;
            if (Status == MissionStatus.Completed || Status == MissionStatus.Failed) return;

            elapsed += deltaTime;
            if (elapsed >= def.timeLimitSeconds)
            {
                elapsed = def.timeLimitSeconds;
                Status = MissionStatus.Failed;
            }
            RaiseChanged();
        }

        private void HandleAction(ActionType type, int bonus)
        {
            if (Status == MissionStatus.Completed || Status == MissionStatus.Failed) return;
            if (type != def.action) return;

            // Si no arrancaba con el nivel, el timer empieza al primer progreso.
            timerRunning = true;

            count++;
            if (count >= def.requiredCount)
            {
                count = def.requiredCount;
                Status = MissionStatus.Completed;
            }
            else
            {
                Status = MissionStatus.InProgress;
            }
            RaiseChanged();
        }
    }
}
