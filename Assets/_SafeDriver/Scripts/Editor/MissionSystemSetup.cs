using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Missions;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Genera el sistema de misiones para Level_01_City: crea los assets de
    /// CountableMissionDefinition, un LevelDefinition, y monta MissionManager +
    /// LevelManager en la escena activa. Reemplaza al viejo ObjectivesController
    /// con objetivos fijos en Inspector.
    ///
    /// Disparar con: SafeDriver/Setup Mission System (Level_01_City)
    /// </summary>
    public static class MissionSystemSetup
    {
        private const string MissionsDir = "Assets/_SafeDriver/Missions/Level01City";
        private const string LevelsDir   = "Assets/_SafeDriver/Missions/Levels";

        [MenuItem("SafeDriver/Setup Mission System (Level_01_City)")]
        public static void Setup()
        {
            EnsureFolder("Assets/_SafeDriver/Missions");
            EnsureFolder(MissionsDir);
            EnsureFolder(LevelsDir);

            // 1. Crear las misiones (mismas que tenia el ObjectivesController viejo).
            var missions = new List<MissionDefinition>
            {
                CreateCountable("m_pare",    "Detenerse en la senal PARE",   ActionType.StoppedAtPareSign,        1, 8),
                CreateCountable("m_verde",   "Pasar el semaforo en verde",   ActionType.PassedGreenLight,         1, 3),
                CreateCountable("m_rojo",    "Detenerse en semaforo rojo",   ActionType.StoppedAtRedLight,        1, 10),
                CreateCountable("m_peaton",  "Ceder paso a peatones",        ActionType.YieldedToPedestrian,      1, 15),
                CreateCountable("m_senda",   "Cruzar senda sin peatones",    ActionType.PedestrianNotPresent,     1, 3),
                CreateCountable("m_espejos", "Chequear espejos al girar",    ActionType.CheckedMirrorsBeforeTurn, 2, 5),
            };

            // 2. Crear el LevelDefinition.
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelsDir + "/Level_01_City.asset");
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, LevelsDir + "/Level_01_City.asset");
            }
            level.levelId = "level_01_city";
            level.displayName = "Ciudad - Basico";
            level.description = "Recorre la ciudad respetando senales, semaforos y peatones.";
            level.sceneName = "Level_01_City";
            level.missions = missions.ToArray();
            level.isFreeRoam = false;
            EditorUtility.SetDirty(level);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Montar los managers en la escena activa.
            var managersGo = GameObject.Find("MissionSystem");
            if (managersGo == null) managersGo = new GameObject("MissionSystem");

            var mm = managersGo.GetComponent<MissionManager>();
            if (mm == null) mm = managersGo.AddComponent<MissionManager>();

            var lm = managersGo.GetComponent<LevelManager>();
            if (lm == null) lm = managersGo.AddComponent<LevelManager>();

            // MissionManager: que NO auto-cargue (el LevelManager inyecta desde el LevelDefinition).
            var mmSo = new SerializedObject(mm);
            mmSo.FindProperty("autoLoadInspectorMissions").boolValue = false;
            mmSo.ApplyModifiedProperties();

            // LevelManager: asignar el LevelDefinition.
            var lmSo = new SerializedObject(lm);
            lmSo.FindProperty("level").objectReferenceValue = level;
            lmSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(managersGo);
            EditorSceneManager.MarkSceneDirty(managersGo.scene);

            Debug.Log($"[MissionSystemSetup] Listo. {missions.Count} misiones creadas en {MissionsDir}, " +
                      $"LevelDefinition en {LevelsDir}, MissionSystem montado en la escena. " +
                      "Guardar la escena (Ctrl+S) para persistir.");
        }

        private static CountableMissionDefinition CreateCountable(
            string id, string title, ActionType action, int count, int points)
        {
            string path = $"{MissionsDir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CountableMissionDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CountableMissionDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.missionId = id;
            def.title = title;
            def.action = action;
            def.requiredCount = count;
            def.points = points;
            def.isOptional = false;
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string leaf = path.Substring(lastSlash + 1);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
