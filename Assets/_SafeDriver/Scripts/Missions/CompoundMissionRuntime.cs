using System.Collections.Generic;

namespace SafeDriver.Missions
{
    /// <summary>Runtime de mision compuesta: agrega el estado de sus sub-misiones runtime.</summary>
    public class CompoundMissionRuntime : MissionRuntime
    {
        private readonly CompoundMissionDefinition def;
        private readonly List<MissionRuntime> subs = new List<MissionRuntime>();

        public CompoundMissionRuntime(CompoundMissionDefinition definition) : base(definition)
        {
            def = definition;
            if (def.subMissions != null)
            {
                foreach (var sub in def.subMissions)
                    if (sub != null) subs.Add(sub.CreateRuntime());
            }
        }

        public IReadOnlyList<MissionRuntime> SubMissions => subs;

        public override float Progress01
        {
            get
            {
                if (subs.Count == 0) return 1f;
                float sum = 0f;
                foreach (var s in subs) sum += s.Progress01;
                return sum / subs.Count;
            }
        }

        public override string ProgressLabel
        {
            get
            {
                int done = 0;
                foreach (var s in subs) if (s.Status == MissionStatus.Completed) done++;
                return subs.Count > 1 ? $"{done}/{subs.Count}" : string.Empty;
            }
        }

        public override void Activate()
        {
            foreach (var s in subs)
            {
                s.Changed += HandleSubChanged;
                s.Activate();
            }
        }

        public override void Deactivate()
        {
            foreach (var s in subs)
            {
                s.Changed -= HandleSubChanged;
                s.Deactivate();
            }
        }

        public override void Tick(float deltaTime)
        {
            foreach (var s in subs) s.Tick(deltaTime);
        }

        private void HandleSubChanged(MissionRuntime sub)
        {
            if (Status == MissionStatus.Failed || Status == MissionStatus.Completed)
            {
                RaiseChanged();
                return;
            }

            if (def.failOnAnySubFailure)
            {
                foreach (var s in subs)
                {
                    if (s.Status == MissionStatus.Failed)
                    {
                        Status = MissionStatus.Failed;
                        RaiseChanged();
                        return;
                    }
                }
            }

            bool allDone = true;
            bool anyProgress = false;
            foreach (var s in subs)
            {
                if (s.Status != MissionStatus.Completed) allDone = false;
                if (s.Status != MissionStatus.Pending) anyProgress = true;
            }

            Status = allDone ? MissionStatus.Completed
                   : anyProgress ? MissionStatus.InProgress
                   : MissionStatus.Pending;
            RaiseChanged();
        }
    }
}
