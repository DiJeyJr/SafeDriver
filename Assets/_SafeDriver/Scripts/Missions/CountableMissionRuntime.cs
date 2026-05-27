using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>Runtime de mision contable: cuenta ocurrencias de una ActionType.</summary>
    public class CountableMissionRuntime : MissionRuntime
    {
        private readonly CountableMissionDefinition def;
        private int count;

        public CountableMissionRuntime(CountableMissionDefinition definition) : base(definition)
        {
            def = definition;
        }

        public override float Progress01 =>
            def.requiredCount <= 0 ? 1f : (float)count / def.requiredCount;

        public override string ProgressLabel =>
            def.requiredCount > 1 ? $"{count}/{def.requiredCount}" : string.Empty;

        public override void Activate() => EventBus.OnCorrectActionPerformed += HandleAction;
        public override void Deactivate() => EventBus.OnCorrectActionPerformed -= HandleAction;

        private void HandleAction(ActionType type, int bonus)
        {
            if (Status == MissionStatus.Completed) return;
            if (type != def.action) return;

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
