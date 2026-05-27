using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>Runtime de mision en secuencia: exige las acciones en orden estricto.</summary>
    public class SequenceMissionRuntime : MissionRuntime
    {
        private readonly SequenceMissionDefinition def;
        private int index;

        public SequenceMissionRuntime(SequenceMissionDefinition definition) : base(definition)
        {
            def = definition;
        }

        private int StepCount => def.steps != null ? def.steps.Length : 0;

        public override float Progress01 => StepCount <= 0 ? 1f : (float)index / StepCount;

        public override string ProgressLabel => StepCount > 1 ? $"{index}/{StepCount}" : string.Empty;

        public override void Activate() => EventBus.OnCorrectActionPerformed += HandleAction;
        public override void Deactivate() => EventBus.OnCorrectActionPerformed -= HandleAction;

        private void HandleAction(ActionType type, int bonus)
        {
            if (Status == MissionStatus.Completed) return;
            if (index >= StepCount) return;

            // Solo avanza si la accion coincide con el paso esperado. Fuera de orden se ignora.
            if (type != def.steps[index]) return;

            index++;
            Status = index >= StepCount ? MissionStatus.Completed : MissionStatus.InProgress;
            RaiseChanged();
        }
    }
}
