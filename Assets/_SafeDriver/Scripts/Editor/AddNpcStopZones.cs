using UnityEditor;
using UnityEngine;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea zonas de detencion para autos NPC alineadas con el TrafficLight existente.
    /// Cada zona referencia el mismo TrafficLightController; cuando este en rojo (o amarillo),
    /// los TrafficVehicle que la atraviesen frenan.
    ///
    /// Layout (asume el demo creado con Setup Traffic Demo: NPCs en X=20 yendo Z200->Z-10
    /// y X=40 yendo Z-10->Z200, y TrafficLight en X=9 Z=120):
    ///   - StopZone_X20: en (20, 0.5, 130) — justo antes del cruce para los autos que bajan
    ///   - StopZone_X40: en (40, 0.5, 110) — justo antes del cruce para los autos que suben
    /// </summary>
    public static class AddNpcStopZones
    {
        [MenuItem("SafeDriver/Setup NPC Stop Zones")]
        public static void Run()
        {
            var lightGO = GameObject.Find("TrafficLight");
            if (lightGO == null) { Debug.LogError("No se encontro 'TrafficLight' en la escena."); return; }
            var light = lightGO.GetComponent<TrafficLightController>();
            if (light == null) { Debug.LogError("'TrafficLight' no tiene TrafficLightController."); return; }

            var trafficRoot = GameObject.Find("_TrafficDemo");
            if (trafficRoot == null)
            {
                Debug.LogError("_TrafficDemo no encontrado. Ejecuta primero 'SafeDriver/Setup Traffic Demo'.");
                return;
            }

            // Limpiar zonas previas si existen
            var existing = trafficRoot.transform.Find("StopZones_NPC");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var rootGO = new GameObject("StopZones_NPC");
            Undo.RegisterCreatedObjectUndo(rootGO, "Setup NPC Stop Zones");
            rootGO.transform.SetParent(trafficRoot.transform, worldPositionStays: false);

            CreateZone(rootGO.transform, "StopZone_X20", new Vector3(20f, 0.5f, 130f), new Vector3(8f, 2f, 4f), light);
            CreateZone(rootGO.transform, "StopZone_X40", new Vector3(40f, 0.5f, 110f), new Vector3(8f, 2f, 4f), light);

            EditorUtility.SetDirty(rootGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rootGO.scene);
            Selection.activeGameObject = rootGO;
            Debug.Log("[NPC Stop Zones] creadas 2 zonas referenciando el TrafficLight.", rootGO);
        }

        private static void CreateZone(Transform parent, string name, Vector3 worldPos, Vector3 size, TrafficLightController light)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPos;

            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;

            var zone = go.AddComponent<TrafficLightStopZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("trafficLight").objectReferenceValue = light;
            so.ApplyModifiedProperties();
        }
    }
}
