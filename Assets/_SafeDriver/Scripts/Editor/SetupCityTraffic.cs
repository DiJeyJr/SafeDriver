using UnityEditor;
using UnityEngine;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Monta trafico NPC circulando en la avenida principal de Level_01_City (la que recorre el
    /// player, x∈[-9,9], sentido +Z). Crea un loop cerrado de dos carriles:
    ///   - Carril derecho  (x=+4.5, +Z): trafico que va adelante del player (lo sigue).
    ///   - Carril izquierdo (x=-4.5, -Z): trafico que viene de frente (oncoming).
    /// y spawnea autos variados de SimplePoly recorriendolo. Ademas agrega dos zonas de detencion
    /// (TrafficLightStopZone) cerca del semaforo existente para que los NPC frenen en rojo.
    ///
    /// Velocidad/tamaño/cantidad salen de un TrafficProfile (ScriptableObject) — editalo para escalar
    /// dificultad. Los modelos SimplePoly vienen ~1.75x grandes; el profile los escala a tamaño real y
    /// el collider se ajusta al modelo (asi rozarlos registra el choque).
    ///
    /// NO toca el auto del player ni su deteccion (es aditivo). Idempotente: si _TrafficNPC ya existe,
    /// avisa y no duplica. Disparar con el menu y guardar la escena.
    /// </summary>
    public static class SetupCityTraffic
    {
        private const string RootName = "_TrafficNPC";
        private const string VehDir = "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/";
        private const string ConfigDir = "Assets/_SafeDriver/Config";
        private const string ProfilePath = "Assets/_SafeDriver/Config/DefaultTrafficProfile.asset";

        // Prefabs variados que se ciclan segun carCount del profile. Los startIndex apuntan a las
        // ESQUINAS RECTAS del oval (0,1,4,5), bien separadas → no spawnean amontonados (evita el
        // deadlock donde cada uno se detecta a otro adentro de su zona de frenado y todos se quedan).
        private static readonly (string prefab, int start)[] CarPool =
        {
            ("Vehicle_Car_color01.prefab",           0),  // (3,12)   sur-derecha
            ("Vehicle_SUV_color02.prefab",           1),  // (3,112)  norte-derecha
            ("Vehicle_Taxi.prefab",                  4),  // (-3,112) norte-izquierda
            ("Vehicle_Pick up Truck_color01.prefab", 5),  // (-3,12)  sur-izquierda
            ("Vehicle_Car_color03.prefab",           2),
            ("Vehicle_SUV_color01.prefab",           6),
        };

        [MenuItem("SafeDriver/Setup City Traffic (Level_01_City)")]
        public static void Run()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning("[CityTraffic] " + RootName + " ya existe. Borralo y volve a ejecutar para regenerar.");
                Selection.activeGameObject = GameObject.Find(RootName);
                return;
            }

            var profile = LoadOrCreateProfile();
            int trafficLayer = EnsureLayer("Traffic");

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Setup City Traffic");

            // Loop CONFINADO a la franja que el player maneja (x∈±3 bien dentro del asfalto, z 7→117,
            // antes del semaforo en z=127). Oval fino con giros cerrados; nunca llega a edificios/veredas.
            // Pasa POR el semaforo (z=127) para que los NPC lo respeten. Gira al norte en z=135 (pasando
            // el semaforo) y al sur en z=7. Carriles en x=±3 (dentro del asfalto).
            var path = CreatePath(root.transform, "AvenueLoop", closedLoop: true, new[]
            {
                new Vector3( 3f,   0f, 12f),    // carril derecho (sube +Z, adelante del player) — cruza el semaforo
                new Vector3( 3f,   0f, 130f),
                new Vector3( 1.5f, 0f, 135f),   // giro norte (arco cerrado, pasando el semaforo z=127)
                new Vector3(-1.5f, 0f, 135f),
                new Vector3(-3f,   0f, 130f),   // carril izquierdo (baja -Z, de frente / oncoming) — cruza el semaforo
                new Vector3(-3f,   0f, 12f),
                new Vector3(-1.5f, 0f, 7f),     // giro sur (arco cerrado)
                new Vector3( 1.5f, 0f, 7f),
            });

            int n = Mathf.Clamp(profile.carCount, 1, CarPool.Length);
            for (int i = 0; i < n; i++)
            {
                // Velocidad escalonada en [cruise-var, cruise+var] para que no vayan todos iguales.
                float t = n > 1 ? (i / (float)(n - 1)) * 2f - 1f : 0f; // -1..+1
                float speed = profile.cruiseSpeed + t * profile.speedVariation;
                SpawnCar(root.transform, path, VehDir + CarPool[i].prefab, speed, CarPool[i].start, profile.modelScale, trafficLayer);
            }

            SetupPlayerSolidHitbox(trafficLayer);

            // Zonas de detencion para que los NPC frenen en el semaforo del player (x=9, z=127).
            var tlc = Object.FindFirstObjectByType<TrafficLightController>();
            if (tlc != null)
            {
                // Linea de detencion del carril +Z (x=3): el NPC frena justo antes de la cebra sur
                // (z≈113) y, si ya la paso, termina de cruzar. Mover su z ajusta DONDE frena.
                // (El carril -Z arranca pegado a la cebra norte por la forma del loop, asi que ahi los
                //  oncoming pasan derecho — se puede refinar extendiendo la ruta si hace falta.)
                MakeStopLine(root.transform, "StopLine_NB", new Vector3(3f, 0f, 112f), Quaternion.identity, tlc);
            }
            else
            {
                Debug.LogWarning("[CityTraffic] No se encontro TrafficLightController; los NPC no frenaran en el semaforo.");
            }

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[CityTraffic] Listo: {n} autos NPC (escala {profile.modelScale}, vel {profile.cruiseSpeed}±{profile.speedVariation} m/s) " +
                      "+ zonas de detencion. Editar " + ProfilePath + " para ajustar dificultad. Guardar (Ctrl+S).", root);
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static TrafficProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TrafficProfile>(ProfilePath);
            if (profile == null)
            {
                if (!AssetDatabase.IsValidFolder(ConfigDir))
                    AssetDatabase.CreateFolder("Assets/_SafeDriver", "Config");
                profile = ScriptableObject.CreateInstance<TrafficProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[CityTraffic] Creado TrafficProfile por defecto en " + ProfilePath);
            }
            return profile;
        }

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

        private static void SpawnCar(Transform parent, TrafficWaypointPath path, string prefabPath, float cruiseSpeed, int startIndex, float modelScale, int trafficLayer)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning("[CityTraffic] Prefab no encontrado: " + prefabPath); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(prefabPath) + "_NPC";
            instance.transform.position = path.GetPosition(startIndex);
            instance.transform.localScale = Vector3.one * modelScale;

            // Orientar el auto hacia su PRIMER waypoint (evita que arranque en contramano).
            int next = path.Count > 0 ? (startIndex + 1) % path.Count : startIndex;
            Vector3 dir = path.GetPosition(next) - path.GetPosition(startIndex);
            dir.y = 0f;
            instance.transform.rotation = dir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(dir.normalized)
                : Quaternion.identity;

            // Collider que ENVUELVE el modelo (no un tamaño fijo): asi rozar el auto registra el choque.
            var col = instance.GetComponent<BoxCollider>();
            if (col == null) col = instance.AddComponent<BoxCollider>();
            var mr = instance.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                col.center = mr.localBounds.center;
                col.size = mr.localBounds.size;
            }
            else
            {
                col.center = new Vector3(0f, 0.5f, 0f);
                col.size = new Vector3(2f, 1.5f, 4f);
            }
            // Solido (no trigger) en la capa "Traffic" → choca con el hitbox solido del player.
            col.isTrigger = false;
            instance.layer = trafficLayer;

            var veh = instance.AddComponent<TrafficVehicle>();
            var so = new SerializedObject(veh);
            so.FindProperty("path").objectReferenceValue = path;
            so.FindProperty("startIndex").intValue = startIndex;
            so.FindProperty("cruiseSpeed").floatValue = cruiseSpeed;
            so.FindProperty("turnSpeed").floatValue = 540f;             // giros cerrados, sin overshoot a la vereda
            so.FindProperty("arriveThreshold").floatValue = 0.4f;       // sigue los waypoints mas pegado
            so.FindProperty("accel").floatValue = 12f;                  // frenado/arranque mas firme (city)
            so.FindProperty("forwardCheckDistance").floatValue = 8f;    // frena mas cerca (con accel 12 para a ~6m → gap ~2m)
            so.ApplyModifiedProperties();
        }

        private static void MakeStopLine(Transform parent, string name, Vector3 pos, Quaternion rot, TrafficLightController tlc)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var line = go.AddComponent<TrafficStopLine>();
            line.SetLight(tlc);
        }

        // Crea (si falta) la capa fisica indicada y devuelve su indice. Edita el TagManager del proyecto.
        private static int EnsureLayer(string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length == 0) { Debug.LogError("[CityTraffic] No se pudo abrir TagManager."); return 0; }
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[CityTraffic] Capa fisica '" + name + "' creada en el slot " + i + ".");
                    return i;
                }
            }
            Debug.LogWarning("[CityTraffic] Sin slots de capa libres; los NPC quedan en Default (no solidos).");
            return 0;
        }

        // Le agrega al auto del player un hitbox SOLIDO (capa Traffic) para que choque con los NPC
        // solidos, + el TrafficLayerIsolator (que aisla la capa Traffic de todo lo demas: no se engancha
        // con la calle/edificios). NO toca el collider trigger original del auto (deteccion de misiones).
        private static void SetupPlayerSolidHitbox(int trafficLayer)
        {
            var player = GameObject.Find("SafeDriver_Exterior_v1");
            if (player == null) { Debug.LogWarning("[CityTraffic] No se encontro el auto del player; sin hitbox solido."); return; }

            var t = player.transform.Find("SolidHitbox");
            GameObject hb = t != null ? t.gameObject : new GameObject("SolidHitbox");
            hb.transform.SetParent(player.transform, false);
            hb.transform.localPosition = Vector3.zero;
            hb.transform.localRotation = Quaternion.identity;
            hb.transform.localScale = Vector3.one;
            hb.layer = trafficLayer;
            var box = hb.GetComponent<BoxCollider>();
            if (box == null) box = hb.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = new Vector3(1.6f, 1.2f, 3.6f);

            if (player.GetComponent<TrafficLayerIsolator>() == null)
                player.AddComponent<TrafficLayerIsolator>();
        }
    }
}
