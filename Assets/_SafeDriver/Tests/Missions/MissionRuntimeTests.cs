using NUnit.Framework;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Missions.Tests
{
    /// <summary>
    /// Tests EditMode de la logica de misiones. Validan que cada MissionRuntime
    /// reacciona bien a las acciones del EventBus y a los ticks de tiempo, sin
    /// necesitar la escena ni VR. Limpian el EventBus entre tests.
    /// </summary>
    public class MissionRuntimeTests
    {
        [TearDown]
        public void Cleanup() => EventBus.Clear();

        // ============================================================
        //   Contable
        // ============================================================

        [Test]
        public void Countable_CompletaAlLlegarAlCount()
        {
            var def = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            def.action = ActionType.StoppedAtRedLight;
            def.requiredCount = 2;

            var rt = def.CreateRuntime();
            rt.Activate();

            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtRedLight, 10);
            Assert.AreEqual(MissionStatus.InProgress, rt.Status, "Tras 1 de 2 deberia estar en progreso.");

            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtRedLight, 10);
            Assert.AreEqual(MissionStatus.Completed, rt.Status, "Tras 2 de 2 deberia completarse.");

            rt.Deactivate();
        }

        [Test]
        public void Countable_IgnoraOtrasAcciones()
        {
            var def = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            def.action = ActionType.StoppedAtRedLight;
            def.requiredCount = 1;

            var rt = def.CreateRuntime();
            rt.Activate();

            EventBus.Dispatch_CorrectAction(ActionType.YieldedToPedestrian, 15);
            Assert.AreEqual(MissionStatus.Pending, rt.Status, "Otra accion no deberia avanzar la mision.");

            rt.Deactivate();
        }

        // ============================================================
        //   Secuencia
        // ============================================================

        [Test]
        public void Secuencia_AvanzaEnOrden()
        {
            var def = ScriptableObject.CreateInstance<SequenceMissionDefinition>();
            def.steps = new[] { ActionType.PassedGreenLight, ActionType.StoppedAtPareSign };

            var rt = def.CreateRuntime();
            rt.Activate();

            EventBus.Dispatch_CorrectAction(ActionType.PassedGreenLight, 3);
            Assert.AreEqual(MissionStatus.InProgress, rt.Status);

            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtPareSign, 8);
            Assert.AreEqual(MissionStatus.Completed, rt.Status);

            rt.Deactivate();
        }

        [Test]
        public void Secuencia_IgnoraFueraDeOrden()
        {
            var def = ScriptableObject.CreateInstance<SequenceMissionDefinition>();
            def.steps = new[] { ActionType.PassedGreenLight, ActionType.StoppedAtPareSign };

            var rt = def.CreateRuntime();
            rt.Activate();

            // El segundo paso llega primero: debe ignorarse.
            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtPareSign, 8);
            Assert.AreEqual(MissionStatus.Pending, rt.Status);

            rt.Deactivate();
        }

        // ============================================================
        //   Con tiempo
        // ============================================================

        [Test]
        public void Timed_FallaAlAgotarseElTiempo()
        {
            var def = ScriptableObject.CreateInstance<TimedMissionDefinition>();
            def.action = ActionType.StoppedAtPareSign;
            def.requiredCount = 1;
            def.timeLimitSeconds = 5f;
            def.startOnLevelBegin = true;

            var rt = def.CreateRuntime();
            rt.Activate();

            rt.Tick(6f); // supera el limite
            Assert.AreEqual(MissionStatus.Failed, rt.Status);

            rt.Deactivate();
        }

        [Test]
        public void Timed_CompletaAntesDelLimite()
        {
            var def = ScriptableObject.CreateInstance<TimedMissionDefinition>();
            def.action = ActionType.StoppedAtPareSign;
            def.requiredCount = 1;
            def.timeLimitSeconds = 5f;
            def.startOnLevelBegin = true;

            var rt = def.CreateRuntime();
            rt.Activate();

            rt.Tick(2f);
            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtPareSign, 8);
            Assert.AreEqual(MissionStatus.Completed, rt.Status);

            rt.Tick(10f); // ya completada: el tiempo no la hace fallar
            Assert.AreEqual(MissionStatus.Completed, rt.Status);

            rt.Deactivate();
        }

        // ============================================================
        //   Compuesta
        // ============================================================

        [Test]
        public void Compuesta_CompletaCuandoTodasLasSubs()
        {
            var sub1 = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            sub1.action = ActionType.StoppedAtRedLight; sub1.requiredCount = 1;
            var sub2 = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            sub2.action = ActionType.YieldedToPedestrian; sub2.requiredCount = 1;

            var def = ScriptableObject.CreateInstance<CompoundMissionDefinition>();
            def.subMissions = new MissionDefinition[] { sub1, sub2 };
            def.failOnAnySubFailure = true;

            var rt = def.CreateRuntime();
            rt.Activate();

            EventBus.Dispatch_CorrectAction(ActionType.StoppedAtRedLight, 10);
            Assert.AreEqual(MissionStatus.InProgress, rt.Status, "Una sub completa: en progreso.");

            EventBus.Dispatch_CorrectAction(ActionType.YieldedToPedestrian, 15);
            Assert.AreEqual(MissionStatus.Completed, rt.Status, "Todas las subs: completada.");

            rt.Deactivate();
        }

        [Test]
        public void Compuesta_FallaSiUnaSubFalla()
        {
            var subTimed = ScriptableObject.CreateInstance<TimedMissionDefinition>();
            subTimed.action = ActionType.StoppedAtPareSign;
            subTimed.requiredCount = 1;
            subTimed.timeLimitSeconds = 1f;
            subTimed.startOnLevelBegin = true;

            var def = ScriptableObject.CreateInstance<CompoundMissionDefinition>();
            def.subMissions = new MissionDefinition[] { subTimed };
            def.failOnAnySubFailure = true;

            var rt = def.CreateRuntime();
            rt.Activate();

            rt.Tick(2f); // la sub con tiempo falla
            Assert.AreEqual(MissionStatus.Failed, rt.Status);

            rt.Deactivate();
        }
    }
}
