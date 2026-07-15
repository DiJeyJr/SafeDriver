using UnityEditor;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Missions;
using SafeDriver.Scoring;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Genera los Mission Kits: 1 prefab = 1 mision 100% funcional al arrastrarla a la escena.
    /// Cada kit trae TODO lo que la mision necesita (zona de deteccion, visuales, NPC con
    /// waypoints si corresponde) + un MissionKit que auto-registra la mision al arrancar.
    /// El disenador solo arrastra el kit, lo apoya sobre la calle con +Z local = sentido de
    /// circulacion, y listo. Los counts/titulos se pueden ajustar por instancia en el Inspector.
    ///
    /// Tambien crea los assets template de mision en Missions/Templates (solo si no existen,
    /// no pisa ajustes hechos a mano).
    ///
    /// Idempotente: re-ejecutar regenera los prefabs.
    /// </summary>
    public static class CreateMissionKits
    {
        private const string FolderKits = "Assets/_SafeDriver/Prefabs/MissionKits";
        private const string FolderTemplates = "Assets/_SafeDriver/Missions/Templates";
        private const string DemoMatDir = "Assets/_SafeDriver/Materials/Demo/";
        private const string StopSignPrefab  = "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Sign_stop.prefab";
        private const string SpeedSignPrefab = "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Sign_speed limit.prefab";

        [MenuItem("SafeDriver/Prefabs/Crear Mission Kits (1 kit = 1 mision)")]
        public static void BuildAll()
        {
            EnsureFolders();

            var tplSemaforo  = Template("Kit_Semaforo",  "kit_semaforo",  "Detenerse en el semaforo en rojo",   ActionType.StoppedAtRedLight,        1, 10);
            var tplPare      = Template("Kit_Pare",      "kit_pare",      "Detenerse en la senal de PARE",      ActionType.StoppedAtPareSign,        1, 10);
            var tplVelocidad = Template("Kit_Velocidad", "kit_velocidad", "Respetar el limite de velocidad",    ActionType.MaintainedLegalSpeed,     1, 10);
            var tplPeaton    = Template("Kit_Peaton",    "kit_peaton",    "Ceder el paso al peaton",            ActionType.YieldedToPedestrian,      1, 15);
            var tplEspejos   = Template("Kit_Espejos",   "kit_espejos",   "Chequear los espejos antes de girar", ActionType.CheckedMirrorsBeforeTurn, 2, 10);

            BuildSemaforo(tplSemaforo);
            BuildPare(tplPare);
            BuildVelocidad(tplVelocidad);
            BuildPeaton(tplPeaton);
            BuildEspejos(tplEspejos);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MissionKits] 5 kits creados en " + FolderKits +
                      " (Semaforo, Pare, Velocidad, Peaton, Espejos). Arrastrar a la escena con +Z = sentido de circulacion.");
        }

        // ============================================================
        //   Kits
        // ============================================================

        // Semaforo completo: visual + TrafficLightController + 3 zonas + mision "parar en rojo".
        private static void BuildSemaforo(MissionDefinition tpl)
        {
            var dim = LoadMat("DimDark");
            var root = new GameObject("MissionKit_Semaforo");

            var signal = new GameObject("Signal");
            signal.transform.SetParent(root.transform, false);
            signal.transform.localPosition = new Vector3(-10.5f, 0f, 0f);

            Visual(PrimitiveType.Cylinder, signal.transform, "Pole",    new Vector3(0f, 1.5f, 0f),  new Vector3(0.12f, 1.5f, 0.12f), dim);
            Visual(PrimitiveType.Cube,     signal.transform, "Housing", new Vector3(0f, 3.35f, 0f), new Vector3(0.45f, 1.15f, 0.3f), dim);
            var red    = Visual(PrimitiveType.Sphere, signal.transform, "BulbRed",    new Vector3(0f, 3.75f, -0.16f), Vector3.one * 0.26f, dim);
            var yellow = Visual(PrimitiveType.Sphere, signal.transform, "BulbYellow", new Vector3(0f, 3.35f, -0.16f), Vector3.one * 0.26f, dim);
            var green  = Visual(PrimitiveType.Sphere, signal.transform, "BulbGreen",  new Vector3(0f, 2.95f, -0.16f), Vector3.one * 0.26f, dim);

            var tlc = root.AddComponent<TrafficLightController>();
            tlc.redLight    = red.GetComponent<MeshRenderer>();
            tlc.yellowLight = yellow.GetComponent<MeshRenderer>();
            tlc.greenLight  = green.GetComponent<MeshRenderer>();
            tlc.litRed    = LoadMat("LitRed");
            tlc.litYellow = LoadMat("LitYellow");
            tlc.litGreen  = LoadMat("LitGreen");
            tlc.dimMat    = dim;

            var stop = ZoneChild(root.transform, "StopZone", new Vector3(0f, 0f, -7f), new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 14f));
            SetRef(stop.AddComponent<RedLightStopZone>(), "trafficLight", tlc);
            var cross = ZoneChild(root.transform, "CrossLine", new Vector3(0f, 0f, 1f), new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 6f));
            SetRef(cross.AddComponent<TrafficLightCrossLine>(), "trafficLight", tlc);
            var npc = ZoneChild(root.transform, "NpcStopZone", new Vector3(0f, 0f, -3f), new Vector3(0f, 1f, 0f), new Vector3(8f, 2f, 6f));
            SetRef(npc.AddComponent<TrafficLightStopZone>(), "trafficLight", tlc);

            // Linea de stop para NPCs: el punto que TrafficVehicle consulta para frenar en rojo
            // (sin esto los autos NPC cruzan el semaforo del kit como si nada).
            var stopLine = new GameObject("NpcStopLine");
            stopLine.transform.SetParent(root.transform, false);
            stopLine.transform.localPosition = new Vector3(0f, 0f, -3f);
            var tsl = stopLine.AddComponent<TrafficStopLine>();
            SetRef(tsl, "trafficLight", tlc);

            AddKit(root, tpl);
            SavePrefab(root, "MissionKit_Semaforo");
        }

        private static void BuildPare(MissionDefinition tpl)
        {
            var root = new GameObject("MissionKit_Pare");
            InstantiateUnpacked(StopSignPrefab, root.transform, "Sign", new Vector3(-3.5f, 0f, 0f), new Vector3(0f, 180f, 0f));
            var line = ZoneChild(root.transform, "StopLine", Vector3.zero, new Vector3(0f, 1.5f, 0f), new Vector3(8f, 3f, 4f));
            line.AddComponent<StopSignDetector>();

            AddKit(root, tpl);
            SavePrefab(root, "MissionKit_Pare");
        }

        private static void BuildVelocidad(MissionDefinition tpl)
        {
            var root = new GameObject("MissionKit_Velocidad");
            InstantiateUnpacked(SpeedSignPrefab, root.transform, "Sign", new Vector3(-3.5f, 0f, 0f), new Vector3(0f, 180f, 0f));
            var zone = ZoneChild(root.transform, "Zone", new Vector3(0f, 0f, 10f), new Vector3(0f, 2f, 0f), new Vector3(12f, 4f, 40f));
            zone.AddComponent<SpeedLimitZone>(); // speedLimitKmH default = 40

            AddKit(root, tpl);
            SavePrefab(root, "MissionKit_Velocidad");
        }

        // El kit estrella: cebra + detector + peaton que camina waypoints cruzando la senda.
        // El peaton notifica presencia al detector (indices 1..2 del path = dentro de la cebra),
        // el detector premia ceder el paso o castiga pasar con peatones; el hitbox del peaton
        // (en un hijo separado, para no duplicar triggers) dispara el atropello grave.
        private static void BuildPeaton(MissionDefinition tpl)
        {
            var root = new GameObject("MissionKit_Peaton");
            var white = LoadMat("SignWhite");

            // Cebra visual
            var zebra = new GameObject("Zebra");
            zebra.transform.SetParent(root.transform, false);
            for (int i = 0; i < 6; i++)
                Visual(PrimitiveType.Cube, zebra.transform, "Stripe_" + i, new Vector3(-3f + i * 1.2f, 0.02f, 0f), new Vector3(0.5f, 0.04f, 5f), white);

            // Zona de control sobre la senda
            var zone = ZoneChild(root.transform, "Zone", Vector3.zero, new Vector3(0f, 2f, 0f), new Vector3(10f, 4f, 6f));
            var detector = zone.AddComponent<PedestrianCrossingDetector>();

            // Zona de ESPERA antes de la cebra: donde el auto se detiene a ceder el paso.
            // (El detector de arriba solo evalua sobre las rayas — nadie frena ahi arriba.)
            var espera = ZoneChild(root.transform, "YieldZone", new Vector3(0f, 0f, -8f), new Vector3(0f, 2f, 0f), new Vector3(12f, 4f, 12f));
            var yieldZone = espera.AddComponent<YieldZone>();
            SetRef(yieldZone, "detector", detector);

            // Waypoints: vereda A -> borde cebra -> borde cebra -> vereda B (rebote, ida y vuelta).
            // Los indices 1 y 2 caen dentro de la senda (rango que notifica presencia).
            var wpRoot = new GameObject("Waypoints");
            wpRoot.transform.SetParent(root.transform, false);
            var path = wpRoot.AddComponent<TrafficWaypointPath>();
            path.closedLoop = false;
            Waypoint(wpRoot.transform, "WP_0_VeredaA", new Vector3(-7f, 0f, 0f));
            Waypoint(wpRoot.transform, "WP_1_Cebra",   new Vector3(-4.5f, 0f, 0f));
            Waypoint(wpRoot.transform, "WP_2_Cebra",   new Vector3(4.5f, 0f, 0f));
            Waypoint(wpRoot.transform, "WP_3_VeredaB", new Vector3(7f, 0f, 0f));

            // Peaton: root con el caminante (sin collider), visual e hitbox en hijos separados.
            var peaton = new GameObject("Peaton");
            peaton.transform.SetParent(root.transform, false);
            peaton.transform.localPosition = new Vector3(-7f, 0f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            body.transform.SetParent(peaton.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            var mr = body.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = LoadMat("LitRed");

            var hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(peaton.transform, false);
            hitbox.tag = "Pedestrian"; // los TrafficVehicle frenan por este tag (SphereCast)
            var box = hitbox.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(0.8f, 2f, 0.8f);
            hitbox.AddComponent<PedestrianHitbox>();

            // CrossingPedestrian = ciclo del diseño original (espera en vereda -> cruza ->
            // espera -> vuelve), sin NavMesh. Reemplaza al TrafficPedestrian de loop continuo.
            var walker = peaton.AddComponent<CrossingPedestrian>();
            SetRef(walker, "path", path);
            SetRef(walker, "crossingNotifierRef", detector);
            SetInt(walker, "crosswalkMinIndex", 1);
            SetInt(walker, "crosswalkMaxIndex", 2);

            AddKit(root, tpl);
            SavePrefab(root, "MissionKit_Peaton");
        }

        // Kit sin zona: la mision cuenta chequeos de espejos (los detecta el auto via eye gaze).
        private static void BuildEspejos(MissionDefinition tpl)
        {
            var root = new GameObject("MissionKit_Espejos");
            AddKit(root, tpl);
            SavePrefab(root, "MissionKit_Espejos");
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_SafeDriver/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_SafeDriver", "Prefabs");
            if (!AssetDatabase.IsValidFolder(FolderKits))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Prefabs", "MissionKits");
            if (!AssetDatabase.IsValidFolder(FolderTemplates))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Missions", "Templates");
        }

        // Crea el asset template solo si no existe (no pisa ajustes hechos a mano).
        private static CountableMissionDefinition Template(string file, string id, string title, ActionType action, int count, int points)
        {
            string path = FolderTemplates + "/" + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<CountableMissionDefinition>(path);
            if (existing != null) return existing;

            var def = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            def.missionId = id;
            def.title = title;
            def.action = action;
            def.requiredCount = count;
            def.points = points;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static void AddKit(GameObject root, MissionDefinition tpl)
        {
            var kit = root.AddComponent<MissionKit>();
            SetRef(kit, "missionTemplate", tpl);
        }

        private static void Waypoint(Transform parent, string name, Vector3 localPos)
        {
            var wp = new GameObject(name);
            wp.transform.SetParent(parent, false);
            wp.transform.localPosition = localPos;
        }

        private static GameObject Visual(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            if (mat != null && mr != null) mr.sharedMaterial = mat;
            return go;
        }

        private static GameObject InstantiateUnpacked(string prefabPath, Transform parent, string name, Vector3 localPos, Vector3 localEuler)
        {
            GameObject go;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                Debug.LogWarning("[MissionKits] Visual no encontrado, usando cubo placeholder: " + prefabPath);
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = localEuler;
            return go;
        }

        private static GameObject ZoneChild(Transform parent, string name, Vector3 localPos, Vector3 boxCenter, Vector3 boxSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = boxCenter;
            box.size = boxSize;
            return go;
        }

        private static Material LoadMat(string fileName)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DemoMatDir + fileName + ".mat");
            if (mat == null) Debug.LogWarning("[MissionKits] Material no encontrado: " + DemoMatDir + fileName + ".mat");
            return mat;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); }
            else Debug.LogWarning("[MissionKits] Campo '" + field + "' no encontrado en " + target.GetType().Name);
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.intValue = value; so.ApplyModifiedProperties(); }
            else Debug.LogWarning("[MissionKits] Campo '" + field + "' no encontrado en " + target.GetType().Name);
        }

        private static void SavePrefab(GameObject root, string name)
        {
            string path = FolderKits + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
