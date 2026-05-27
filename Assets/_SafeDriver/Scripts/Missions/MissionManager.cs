using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Orquesta las misiones del nivel actual: crea los runtimes desde las definitions,
    /// los activa, los avanza (Tick) y reexpone su progreso via eventos para la UI.
    /// Detecta cuando todas las misiones obligatorias se completaron.
    ///
    /// Carga las misiones de dos formas:
    ///   - autoLoadInspectorMissions = true: usa el array del Inspector en Start
    ///     (para escenas standalone sin LevelManager).
    ///   - LevelManager.LoadMissions(...): un LevelManager las inyecta (override del Inspector).
    ///
    /// La UI (ObjectivesController) se suscribe a MissionsLoaded + MissionChanged.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Carga")]
        [Tooltip("Si true, carga las misiones del array de abajo en Start. Desactivar si un LevelManager las inyecta.")]
        [SerializeField] private bool autoLoadInspectorMissions = true;

        [Tooltip("Misiones del nivel (solo se usan si autoLoadInspectorMissions).")]
        [SerializeField] private MissionDefinition[] inspectorMissions;

        private readonly List<MissionRuntime> active = new List<MissionRuntime>();
        private bool requiredCompletedDispatched;

        /// <summary>Misiones activas en el nivel actual.</summary>
        public IReadOnlyList<MissionRuntime> ActiveMissions => active;

        /// <summary>Se dispara cuando se (re)cargan las misiones. La UI reconstruye su lista aca.</summary>
        public event Action MissionsLoaded;

        /// <summary>Se dispara cuando una mision cambia estado o progreso.</summary>
        public event Action<MissionRuntime> MissionChanged;

        /// <summary>Se dispara una sola vez cuando todas las misiones obligatorias estan completas.</summary>
        public event Action AllRequiredCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (autoLoadInspectorMissions && active.Count == 0 && inspectorMissions != null)
                LoadMissions(inspectorMissions);
        }

        void OnDestroy()
        {
            ClearMissions();
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < active.Count; i++)
                active[i].Tick(dt);
        }

        /// <summary>Carga y activa una nueva lista de misiones, descartando las anteriores.</summary>
        public void LoadMissions(IEnumerable<MissionDefinition> definitions)
        {
            ClearMissions();
            requiredCompletedDispatched = false;

            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def == null) continue;
                    var runtime = def.CreateRuntime();
                    runtime.Changed += HandleMissionChanged;
                    active.Add(runtime);
                }
            }

            foreach (var m in active) m.Activate();
            MissionsLoaded?.Invoke();
        }

        /// <summary>Desactiva y limpia todas las misiones activas.</summary>
        public void ClearMissions()
        {
            foreach (var m in active)
            {
                m.Changed -= HandleMissionChanged;
                m.Deactivate();
            }
            active.Clear();
        }

        private void HandleMissionChanged(MissionRuntime mission)
        {
            MissionChanged?.Invoke(mission);
            CheckAllRequiredCompleted();
        }

        private void CheckAllRequiredCompleted()
        {
            if (requiredCompletedDispatched) return;

            bool hasRequired = false;
            foreach (var m in active)
            {
                if (m.Definition.isOptional) continue;
                hasRequired = true;
                if (m.Status != MissionStatus.Completed) return;
            }

            // Sin misiones obligatorias no hay condicion de fin por misiones (ej. modo libre).
            if (!hasRequired) return;

            requiredCompletedDispatched = true;
            AllRequiredCompleted?.Invoke();
        }
    }
}
