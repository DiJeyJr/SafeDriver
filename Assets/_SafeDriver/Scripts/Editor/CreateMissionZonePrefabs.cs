using UnityEditor;
using UnityEngine;
using SafeDriver.Scoring;
using SafeDriver.Traffic;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Genera prefabs drag-and-drop de zonas de mision en Assets/_SafeDriver/Prefabs/MissionZones/.
    /// Cada prefab es AUTOCONTENIDO: trae sus detectores ya wireados (incluido el semaforo con su
    /// propio TrafficLightController), colliders trigger configurados y visuales. El disenador solo
    /// lo arrastra a la escena, lo rota hacia el sentido de circulacion (+Z local = avance legal) y
    /// lo ubica sobre la calle.
    ///
    /// El sentido local de todos los prefabs es +Z = direccion de avance del auto. Los detectores se
    /// disparan cuando el auto del jugador (tag PlayerVehicle) entra a los triggers; las misiones que
    /// escuchen el ActionType correspondiente avanzan solas (acoplamiento debil via EventBus).
    ///
    /// Idempotente: re-ejecutar sobreescribe los prefabs.
    /// </summary>
    public static class CreateMissionZonePrefabs
    {
        private const string FolderRoot   = "Assets/_SafeDriver/Prefabs";
        private const string FolderZones  = "Assets/_SafeDriver/Prefabs/MissionZones";
        private const string DemoMatDir   = "Assets/_SafeDriver/Materials/Demo/";
        private const string StopSignPrefab  = "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Sign_stop.prefab";
        private const string SpeedSignPrefab = "Assets/SimplePoly City - Low Poly Assets/Prefab/Props/Props_Traffic Sign_speed limit.prefab";

        [MenuItem("SafeDriver/Prefabs/Crear Mission Zone Prefabs")]
        public static void BuildAll()
        {
            EnsureFolders();

            BuildTrafficLight();
            BuildStopSign();
            BuildSpeedLimit();
            BuildDirection();
            BuildCrosswalk();
            BuildPedestrian();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MissionZones] 6 prefabs creados/actualizados en " + FolderZones +
                      " (TrafficLight, StopSign, SpeedLimit, Direction, Crosswalk, Pedestrian).");
        }

        // ============================================================
        //   Prefabs
        // ============================================================

        // Semaforo autocontenido: poste + 3 focos + TrafficLightController wireado + zonas
        // (RedLightStopZone, TrafficLightCrossLine, TrafficLightStopZone) referenciando ese controller.
        private static void BuildTrafficLight()
        {
            var dim = LoadMat("DimDark");
            var root = new GameObject("MissionZone_TrafficLight");

            // Visual del semaforo, al costado del carril (x=-10.5, justo afuera del trigger de 20m de ancho).
            var signal = new GameObject("Signal");
            signal.transform.SetParent(root.transform, false);
            signal.transform.localPosition = new Vector3(-10.5f, 0f, 0f);

            Visual(PrimitiveType.Cylinder, signal.transform, "Pole",    new Vector3(0f, 1.5f, 0f),   new Vector3(0.12f, 1.5f, 0.12f), dim);
            Visual(PrimitiveType.Cube,     signal.transform, "Housing", new Vector3(0f, 3.35f, 0f),  new Vector3(0.45f, 1.15f, 0.3f), dim);
            // Focos mirando hacia -Z (hacia el auto que se aproxima). Rojo arriba, verde abajo.
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

            // Zona de aproximacion: premia frenar en rojo (nunca penaliza). 7m antes de la linea.
            var stop = ZoneChild(root.transform, "StopZone", new Vector3(0f, 0f, -7f), new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 14f));
            SetRef(stop.AddComponent<RedLightStopZone>(), "trafficLight", tlc);

            // Linea de detencion: cruzar en rojo = infraccion, en verde = premio.
            var cross = ZoneChild(root.transform, "CrossLine", new Vector3(0f, 0f, 1f), new Vector3(0f, 2f, 0f), new Vector3(20f, 4f, 6f));
            SetRef(cross.AddComponent<TrafficLightCrossLine>(), "trafficLight", tlc);

            // Zona de detencion para autos NPC (TrafficVehicle consulta ShouldStop).
            var npc = ZoneChild(root.transform, "NpcStopZone", new Vector3(0f, 0f, -3f), new Vector3(0f, 1f, 0f), new Vector3(8f, 2f, 6f));
            SetRef(npc.AddComponent<TrafficLightStopZone>(), "trafficLight", tlc);

            SavePrefab(root, "MissionZone_TrafficLight");
        }

        // Senal de PARE: dispara StoppedAtPareSign al detenerse / FailedToStopAtSign al salir sin parar.
        private static void BuildStopSign()
        {
            var root = new GameObject("MissionZone_StopSign");
            InstantiateUnpacked(StopSignPrefab, root.transform, "Sign", new Vector3(-3.5f, 0f, 0f), new Vector3(0f, 180f, 0f));
            var line = ZoneChild(root.transform, "StopLine", Vector3.zero, new Vector3(0f, 1.5f, 0f), new Vector3(8f, 3f, 4f));
            line.AddComponent<StopSignDetector>();
            SavePrefab(root, "MissionZone_StopSign");
        }

        // Limite de velocidad (40 por defecto): notifica el limite al HUD y premia/penaliza segun velocidad.
        private static void BuildSpeedLimit()
        {
            var root = new GameObject("MissionZone_SpeedLimit");
            InstantiateUnpacked(SpeedSignPrefab, root.transform, "Sign", new Vector3(-3.5f, 0f, 0f), new Vector3(0f, 180f, 0f));
            var zone = ZoneChild(root.transform, "Zone", new Vector3(0f, 0f, 10f), new Vector3(0f, 2f, 0f), new Vector3(12f, 4f, 40f));
            zone.AddComponent<SpeedLimitZone>(); // speedLimitKmH default = 40
            SavePrefab(root, "MissionZone_SpeedLimit");
        }

        // Contramano: dispara WrongWay si el auto circula en sentido opuesto a +Z local.
        private static void BuildDirection()
        {
            var root = new GameObject("MissionZone_Direction");
            // Flecha-marcador sobre el asfalto indicando el sentido legal (+Z).
            Visual(PrimitiveType.Cube, root.transform, "ArrowMarker", new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.04f, 3f), LoadMat("LitGreen"));
            var zone = ZoneChild(root.transform, "Zone", Vector3.zero, new Vector3(0f, 2f, 0f), new Vector3(8f, 4f, 20f));
            zone.AddComponent<DirectionZone>(); // allowedLocalDirection default = +Z
            SavePrefab(root, "MissionZone_Direction");
        }

        // Senda peatonal: PedestrianCrossingDetector. Para que premie "ceder paso" hay que wirearle
        // un Pedestrian_Crossing como crossingNotifierRef (o un NPCPedestrianAI). Solo: detecta cruce.
        private static void BuildCrosswalk()
        {
            var root = new GameObject("MissionZone_Crosswalk");
            var white = LoadMat("SignWhite");
            var zebra = new GameObject("Zebra");
            zebra.transform.SetParent(root.transform, false);
            for (int i = 0; i < 6; i++)
                Visual(PrimitiveType.Cube, zebra.transform, "Stripe_" + i, new Vector3(-3f + i * 1.2f, 0.02f, 0f), new Vector3(0.5f, 0.04f, 5f), white);

            var zone = ZoneChild(root.transform, "Zone", Vector3.zero, new Vector3(0f, 2f, 0f), new Vector3(10f, 4f, 6f));
            zone.AddComponent<PedestrianCrossingDetector>();
            SavePrefab(root, "MissionZone_Crosswalk");
        }

        // Peaton: capsula con PedestrianHitbox (atropello = HitPedestrian, grave). Para hacerlo caminar
        // y que notifique a una senda, agregarle un TrafficWaypointPath + TrafficPedestrian al instanciar.
        private static void BuildPedestrian()
        {
            var root = new GameObject("Pedestrian_Crossing");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            var mr = body.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = LoadMat("LitRed");

            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(0.8f, 2f, 0.8f);
            root.AddComponent<PedestrianHitbox>();

            SavePrefab(root, "Pedestrian_Crossing");
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(FolderRoot))
                AssetDatabase.CreateFolder("Assets/_SafeDriver", "Prefabs");
            if (!AssetDatabase.IsValidFolder(FolderZones))
                AssetDatabase.CreateFolder(FolderRoot, "MissionZones");
        }

        // Primitiva visual SIN collider (la quitamos para que no bloquee fisicamente).
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

        // Instancia un prefab visual (ej. señal de SimplePoly) y lo desempaqueta para que el prefab
        // de zona quede autocontenido (sin dependencia al pack externo). Si no se encuentra, usa un cubo.
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
                Debug.LogWarning("[MissionZones] Visual no encontrado, usando cubo placeholder: " + prefabPath);
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

        // Hijo con BoxCollider trigger ya configurado (la zona de deteccion).
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
            if (mat == null) Debug.LogWarning("[MissionZones] Material no encontrado: " + DemoMatDir + fileName + ".mat");
            return mat;
        }

        // Setea un campo serializado por referencia (para los [SerializeField] private como 'trafficLight').
        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); }
            else Debug.LogWarning("[MissionZones] Campo '" + field + "' no encontrado en " + target.GetType().Name);
        }

        private static void SavePrefab(GameObject root, string name)
        {
            string path = FolderZones + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
