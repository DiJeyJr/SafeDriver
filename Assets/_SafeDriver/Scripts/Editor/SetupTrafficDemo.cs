using UnityEditor;
using UnityEngine;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea un demo de trafico ambiental en la escena activa:
    ///   - 1 path para autos NPC en una calle paralela
    ///   - 2 autos siguiendo ese path con offset de waypoint inicial
    ///   - 2 paths de peatones (vereda izquierda y derecha)
    ///   - 4 peatones (2 por vereda) recorriendo en loop
    ///
    /// Reusa los prefabs de SimplePoly para los autos. Los peatones son Capsules con
    /// material rojo para que se distingan claramente del entorno.
    /// </summary>
    public static class SetupTrafficDemo
    {
        private const string RootName = "_TrafficDemo";

        private const string CarPrefab1 = "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/Vehicle_Car_color01.prefab";
        private const string CarPrefab2 = "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/Vehicle_Car_color02.prefab";
        private const string LitRedMat  = "Assets/_SafeDriver/Materials/Demo/LitRed.mat";

        [MenuItem("SafeDriver/Setup Traffic Demo")]
        public static void Run()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.LogWarning(RootName + " ya existe; seleccionando. Borralo y volve a ejecutar para regenerar.", existing);
                return;
            }

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Setup Traffic Demo");

            // ----- Path autos: calle paralela en X=20, vuelta cerrada Z=-10 ↔ 200 -----
            var carPath = CreatePath(root.transform, "CarPath", closedLoop: true, new[]
            {
                new Vector3(20f, 0f, 200f),
                new Vector3(20f, 0f, -10f),
                new Vector3(40f, 0f, -10f),
                new Vector3(40f, 0f, 200f),
            });

            SpawnCar(root.transform, carPath, CarPrefab1, "Car_NPC_01", startIndex: 0, cruiseSpeed: 7f);
            SpawnCar(root.transform, carPath, CarPrefab2, "Car_NPC_02", startIndex: 2, cruiseSpeed: 6f);

            // ----- Path peatones vereda derecha (X=+9), abierto con rebote -----
            var rightSidewalk = CreatePath(root.transform, "RightSidewalkPath", closedLoop: false, new[]
            {
                new Vector3(9f, 0f, 5f),
                new Vector3(9f, 0f, 50f),
                new Vector3(9f, 0f, 100f),
                new Vector3(9f, 0f, 150f),
            });

            SpawnPedestrian(root.transform, rightSidewalk, "Ped_Right_01", startIndex: 0, speed: 1.3f);
            SpawnPedestrian(root.transform, rightSidewalk, "Ped_Right_02", startIndex: 2, speed: 1.1f);

            // ----- Path peatones vereda izquierda (X=-9), abierto con rebote -----
            var leftSidewalk = CreatePath(root.transform, "LeftSidewalkPath", closedLoop: false, new[]
            {
                new Vector3(-9f, 0f, 10f),
                new Vector3(-9f, 0f, 75f),
                new Vector3(-9f, 0f, 140f),
            });

            SpawnPedestrian(root.transform, leftSidewalk, "Ped_Left_01", startIndex: 0, speed: 1.25f);
            SpawnPedestrian(root.transform, leftSidewalk, "Ped_Left_02", startIndex: 2, speed: 1.5f);

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[Traffic Demo] creado: 2 autos NPC en calle paralela + 4 peatones por veredas.", root);
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static TrafficWaypointPath CreatePath(Transform parent, string name, bool closedLoop, Vector3[] points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var path = go.AddComponent<TrafficWaypointPath>();
            path.closedLoop = closedLoop;

            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject("WP_" + i);
                wp.transform.SetParent(go.transform, worldPositionStays: false);
                wp.transform.position = points[i];
            }
            return path;
        }

        private static void SpawnCar(Transform parent, TrafficWaypointPath path, string prefabPath, string name, int startIndex, float cruiseSpeed)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning("Prefab no encontrado: " + prefabPath); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = path.GetPosition(startIndex);
            instance.transform.rotation = Quaternion.identity;

            // BoxCollider trigger para detectar colision logica
            var col = instance.GetComponent<BoxCollider>();
            if (col == null) col = instance.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(2f, 1.5f, 4f);
            col.isTrigger = true;

            var veh = instance.AddComponent<TrafficVehicle>();
            var so = new SerializedObject(veh);
            so.FindProperty("path").objectReferenceValue = path;
            so.FindProperty("startIndex").intValue = startIndex;
            so.FindProperty("cruiseSpeed").floatValue = cruiseSpeed;
            so.ApplyModifiedProperties();
        }

        private static void SpawnPedestrian(Transform parent, TrafficWaypointPath path, string name, int startIndex, float speed)
        {
            // Visual: Capsule rojo (no hay prefab de persona en SimplePoly)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.SetParent(parent, worldPositionStays: false);
            capsule.transform.position = path.GetPosition(startIndex) + Vector3.up * 1f;
            capsule.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);

            // Material rojo
            var mat = AssetDatabase.LoadAssetAtPath<Material>(LitRedMat);
            var renderer = capsule.GetComponent<MeshRenderer>();
            if (mat != null && renderer != null) renderer.sharedMaterial = mat;

            // Configurar collider como trigger para no chocar fisicamente con el auto
            var col = capsule.GetComponent<CapsuleCollider>();
            if (col != null) col.isTrigger = true;

            var ped = capsule.AddComponent<TrafficPedestrian>();
            var so = new SerializedObject(ped);
            so.FindProperty("path").objectReferenceValue = path;
            so.FindProperty("startIndex").intValue = startIndex;
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedProperties();
        }
    }
}
