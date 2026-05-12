using UnityEditor;
using UnityEngine;
using SafeDriver.Traffic;
using SafeDriver.Scoring;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea 2 peatones que cruzan la cebra del CrosswalkDetector (Z=60) en sentidos opuestos.
    /// Usa el path "CrosswalkPedPath" con 4 waypoints (vereda izq -> entra cebra -> sale cebra
    /// -> vereda der) en modo rebote. Cada peaton notifica al detector usando su instanceId
    /// (`NotifyByInstance`) — multiples peatones coexisten sin pisarse el bool.
    ///
    /// Tambien desactiva el `DemoPedestrianFaker` viejo si esta en escena para no duplicar
    /// notificaciones.
    /// </summary>
    public static class SetupCrossingPedestrians
    {
        private const string RootName  = "_TrafficDemo";
        private const string LitRedMat = "Assets/_SafeDriver/Materials/Demo/LitRed.mat";
        private const string LitYelMat = "Assets/_SafeDriver/Materials/Demo/LitYellow.mat";

        [MenuItem("SafeDriver/Setup Crossing Pedestrians")]
        public static void Run()
        {
            var trafficRoot = GameObject.Find(RootName);
            if (trafficRoot == null)
            {
                Debug.LogError("_TrafficDemo no encontrado. Ejecuta primero 'SafeDriver/Setup Traffic Demo'.");
                return;
            }

            var detectorGO = GameObject.Find("CrosswalkDetector");
            if (detectorGO == null)
            {
                Debug.LogError("CrosswalkDetector no encontrado en la escena.");
                return;
            }
            var detector = detectorGO.GetComponent<PedestrianCrossingDetector>();
            if (detector == null)
            {
                Debug.LogError("CrosswalkDetector no tiene PedestrianCrossingDetector.");
                return;
            }

            // Desactivar el faker viejo para no duplicar notificaciones
            var faker = Object.FindFirstObjectByType<DemoPedestrianFaker>();
            if (faker != null && faker.isActiveAndEnabled)
            {
                faker.enabled = false;
                EditorUtility.SetDirty(faker);
                Debug.Log("DemoPedestrianFaker desactivado (no destruido).", faker);
            }

            // Limpiar si ya existe el path/peatones de un Run previo
            var existingPath = trafficRoot.transform.Find("CrosswalkPedPath");
            if (existingPath != null) Object.DestroyImmediate(existingPath.gameObject);
            var existingA = trafficRoot.transform.Find("Ped_Cross_A");
            if (existingA != null) Object.DestroyImmediate(existingA.gameObject);
            var existingB = trafficRoot.transform.Find("Ped_Cross_B");
            if (existingB != null) Object.DestroyImmediate(existingB.gameObject);

            // Path: vereda izq (-12) → cebra (-9..9) → vereda der (+12) en Z=60. Rebote.
            // Indices 1 y 2 son los que estan DENTRO de la cebra.
            var pathGo = new GameObject("CrosswalkPedPath");
            pathGo.transform.SetParent(trafficRoot.transform, worldPositionStays: false);
            var path = pathGo.AddComponent<TrafficWaypointPath>();
            path.closedLoop = false;
            AddWaypoint(pathGo.transform, "WP_0_VerdaIzq",  new Vector3(-12f, 0f, 60f));
            AddWaypoint(pathGo.transform, "WP_1_EntraCebra", new Vector3(-9f,  0f, 60f));
            AddWaypoint(pathGo.transform, "WP_2_SaleCebra",  new Vector3( 9f,  0f, 60f));
            AddWaypoint(pathGo.transform, "WP_3_VerdaDer",  new Vector3( 12f, 0f, 60f));

            SpawnPedestrian(trafficRoot.transform, path, detector, "Ped_Cross_A", startIndex: 0, speed: 1.25f, materialPath: LitRedMat);
            SpawnPedestrian(trafficRoot.transform, path, detector, "Ped_Cross_B", startIndex: 3, speed: 1.40f, materialPath: LitYelMat);

            EditorUtility.SetDirty(trafficRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trafficRoot.scene);
            Selection.activeGameObject = pathGo;
            Debug.Log("[Crossing Pedestrians] 2 peatones cruzando wireados al CrosswalkDetector.", pathGo);
        }

        private static void AddWaypoint(Transform parent, string name, Vector3 worldPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPos;
        }

        private static void SpawnPedestrian(Transform parent, TrafficWaypointPath path,
            PedestrianCrossingDetector detector, string name, int startIndex, float speed, string materialPath)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.SetParent(parent, worldPositionStays: false);
            capsule.transform.position = path.GetPosition(startIndex) + Vector3.up * 1f;
            capsule.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var renderer = capsule.GetComponent<MeshRenderer>();
            if (mat != null && renderer != null) renderer.sharedMaterial = mat;

            var col = capsule.GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;

            var ped = capsule.AddComponent<TrafficPedestrian>();
            var so = new SerializedObject(ped);
            so.FindProperty("path").objectReferenceValue = path;
            so.FindProperty("startIndex").intValue = startIndex;
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("crosswalkMinIndex").intValue = 1;
            so.FindProperty("crosswalkMaxIndex").intValue = 2;
            so.FindProperty("crossingNotifierRef").objectReferenceValue = detector;
            so.ApplyModifiedProperties();
        }
    }
}
