using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Core;
using SafeDriver.UI;
using SafeDriver.Scoring;
using SafeDriver.Traffic;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// One-shot setup para Level_01_City.unity (copia de SimplePoly demo).
    /// Crea: tag/collider del auto, _Bootstrap con managers, HUD diegético, paneles SafeFail/LevelEnd,
    /// ObjectivesPanel, detectores y zonas. Idempotente: skip si ya existen.
    /// </summary>
    public static class Level01CitySetup
    {
        [MenuItem("SafeDriver/Setup Level_01_City")]
        public static void Run()
        {
            var car = GameObject.Find("SafeDriver_Exterior_v1");
            if (car == null) { Debug.LogError("No se encontro SafeDriver_Exterior_v1"); return; }
            var interior = GameObject.Find("SafeDriver_Interior_v1");
            var cameraRig = FindRig(car);

            EnsureTag("PlayerVehicle");
            car.tag = "PlayerVehicle";

            EnsureBoxColliderTrigger(car, new Vector3(0, 0.6f, 0), new Vector3(1.6f, 1.2f, 3.6f));

            var bootstrap = GetOrCreate("_Bootstrap");
            EnsureComponent<GameManager>(bootstrap);
            var scoreMgr = EnsureComponent<ScoreManager>(bootstrap);
            var uiMgr = EnsureComponent<UIManager>(bootstrap);
            var hud = EnsureComponent<HUDController>(bootstrap);
            EnsureComponent<LevelTimer>(bootstrap);

            // HUD diegético en el dashboard
            if (interior != null)
            {
                var scoreTmp = CreateOrUpdateTMP3D(interior, "ScoreDisplay",
                    new Vector3(0.155f, 0.921f, 0.371f), new Vector3(15, 0, 0),
                    new Vector2(0.3f, 0.08f), "1000", 0.5f, new Color(1f, 0.85f, 0.4f), FontStyles.Bold);

                var limitTmp = CreateOrUpdateTMP3D(interior, "SpeedLimitSign",
                    new Vector3(0, 0.96f, 0.40f), new Vector3(15, 0, 0),
                    new Vector2(0.2f, 0.08f), "40", 0.6f, Color.white, FontStyles.Bold);

                var speedTmp = CreateOrUpdateTMP3D(interior, "CurrentSpeedDisplay",
                    new Vector3(-0.30f, 0.92f, 0.40f), new Vector3(15, 0, 0),
                    new Vector2(0.25f, 0.10f), "0", 0.7f, new Color(0.8f, 1f, 0.8f), FontStyles.Bold);

                var notifPanel = GetOrCreate("NotificationPanel", interior.transform);
                var notifTmp = CreateOrUpdateTMP3D(notifPanel.transform, "Text",
                    new Vector3(0, 0, 0), Vector3.zero,
                    new Vector2(0.6f, 0.15f), "", 0.6f, Color.white, FontStyles.Bold);
                notifPanel.transform.localPosition = new Vector3(0, 1.4f, 1.0f);

                hud.scoreDisplay = scoreTmp;
                hud.speedLimitSign = limitTmp;
                hud.currentSpeedDisplay = speedTmp;
                hud.notificationPanel = notifPanel;
                hud.notificationText = notifTmp;
                LevelTimer timer = bootstrap.GetComponent<LevelTimer>();
                var so = new SerializedObject(timer);
                var dispProp = so.FindProperty("display");
                if (dispProp != null)
                {
                    var timerTmp = CreateOrUpdateTMP3D(interior, "TimerDisplay",
                        new Vector3(0.30f, 0.92f, 0.40f), new Vector3(15, 0, 0),
                        new Vector2(0.25f, 0.10f), "02:00", 0.55f, Color.white, FontStyles.Bold);
                    dispProp.objectReferenceValue = timerTmp;
                    so.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(hud);
            }

            // Paneles SafeFail + LevelEnd parented al rig (visible para el conductor)
            Transform panelParent = cameraRig != null ? cameraRig.transform : car.transform;
            var safeFailCanvas = BuildSafeFailCanvas(panelParent);
            uiMgr.GetType().GetField("safeFailScreen",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(uiMgr, safeFailCanvas.GetComponent<SafeFailScreen>());

            var levelEndCanvas = BuildLevelEndCanvas(panelParent);
            uiMgr.GetType().GetField("levelEndPanel",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(uiMgr, levelEndCanvas.GetComponent<LevelEndPanel>());

            uiMgr.GetType().GetField("hud",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(uiMgr, hud);
            EditorUtility.SetDirty(uiMgr);

            // ObjectivesPanel en el tablero
            if (interior != null) BuildObjectivesPanel(interior.transform);

            // HeadTrackingDetector + GazeMirrorDetector en CenterEyeAnchor
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                EnsureComponent<HeadTrackingDetector>(centerEye);
                EnsureComponent<GazeMirrorDetector>(centerEye);
            }

            // Detectores en el mundo (z+ adelante del auto)
            BuildDetectors();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Level_01_City setup completo. Falta: SafeDriver/Rebuild Objectives UI + SafeDriver/Setup Eye Gaze.");
        }

        // ===== Helpers =====

        private static GameObject FindRig(GameObject car)
        {
            foreach (Transform child in car.transform)
                if (child.name.Contains("Camera Rig")) return child.gameObject;
            return null;
        }

        private static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }

        private static void EnsureBoxColliderTrigger(GameObject go, Vector3 center, Vector3 size)
        {
            var bc = go.GetComponent<BoxCollider>();
            if (bc == null) bc = Undo.AddComponent<BoxCollider>(go);
            bc.isTrigger = true;
            bc.center = center;
            bc.size = size;
        }

        private static GameObject GetOrCreate(string name, Transform parent = null)
        {
            GameObject go = parent != null ? FindChildByName(parent, name) : GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                if (parent != null) go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            return go;
        }

        private static GameObject FindChildByName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject;
            return null;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        private static TextMeshPro CreateOrUpdateTMP3D(GameObject parent, string name, Vector3 localPos, Vector3 localEuler, Vector2 size, string text, float fontSize, Color color, FontStyles style)
        {
            return CreateOrUpdateTMP3D(parent.transform, name, localPos, localEuler, size, text, fontSize, color, style);
        }

        private static TextMeshPro CreateOrUpdateTMP3D(Transform parent, string name, Vector3 localPos, Vector3 localEuler, Vector2 size, string text, float fontSize, Color color, FontStyles style)
        {
            var go = FindChildByName(parent, name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = Undo.AddComponent<RectTransform>(go);
            rt.localPosition = localPos;
            rt.localEulerAngles = localEuler;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = Undo.AddComponent<TextMeshPro>(go);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = style;
            return tmp;
        }

        private static GameObject BuildSafeFailCanvas(Transform parent)
        {
            var canvas = FindChildByName(parent, "SafeFailCanvas") ?? CreateCanvas("SafeFailCanvas", parent, new Vector3(0, 0.05f, 1.0f), 1000, 700);
            var img = canvas.GetComponent<Image>() ?? Undo.AddComponent<Image>(canvas);
            img.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            EnsureComponent<CanvasGroup>(canvas);
            EnsureComponent<Oculus.Interaction.PointableCanvas>(canvas);
            var script = EnsureComponent<SafeFailScreen>(canvas);
            // Children: TitleText, DescriptionText, LawText, RetryButton, MainMenuButton — el script ya
            // los expone para wirear manualmente; lo dejamos vacio en este pase.
            return canvas;
        }

        private static GameObject BuildLevelEndCanvas(Transform parent)
        {
            var canvas = FindChildByName(parent, "LevelEndCanvas") ?? CreateCanvas("LevelEndCanvas", parent, new Vector3(0, 0.05f, 1.0f), 1000, 700);
            var img = canvas.GetComponent<Image>() ?? Undo.AddComponent<Image>(canvas);
            img.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            EnsureComponent<LevelEndPanel>(canvas);
            canvas.SetActive(false);
            return canvas;
        }

        private static GameObject CreateCanvas(string name, Transform parent, Vector3 localPos, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var rt = Undo.AddComponent<RectTransform>(go);
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var canvas = Undo.AddComponent<Canvas>(go);
            canvas.renderMode = RenderMode.WorldSpace;
            Undo.AddComponent<CanvasScaler>(go);
            Undo.AddComponent<GraphicRaycaster>(go);
            var cam = GameObject.Find("CenterEyeAnchor")?.GetComponent<Camera>();
            if (cam != null) canvas.worldCamera = cam;
            return go;
        }

        private static void BuildObjectivesPanel(Transform parent)
        {
            var panel = FindChildByName(parent, "ObjectivesPanel") ?? CreateCanvas("ObjectivesPanel", parent, new Vector3(0.35f, 1.30f, 0.45f), 280, 250);
            panel.transform.localEulerAngles = new Vector3(20, -15, 0);
            var img = panel.GetComponent<Image>() ?? Undo.AddComponent<Image>(panel);
            img.color = new Color(0.05f, 0.07f, 0.10f, 0.85f);
            var list = FindChildByName(panel.transform, "List");
            if (list == null)
            {
                list = new GameObject("List");
                list.transform.SetParent(panel.transform, false);
                Undo.RegisterCreatedObjectUndo(list, "Create List");
                var rt = Undo.AddComponent<RectTransform>(list);
                rt.localPosition = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            EnsureComponent<ObjectivesController>(list);
        }

        private static void BuildDetectors()
        {
            var demo = GameObject.Find("_DemoElements") ?? new GameObject("_DemoElements");
            Undo.RegisterCreatedObjectUndo(demo, "Create _DemoElements");

            // SpeedLimitZone en (0, 0, 15)
            BuildZone(demo.transform, "SpeedLimitZone_40", new Vector3(0, 0, 15), new Vector3(8, 4, 10), z => {
                var s = z.GetComponent<SpeedLimitZone>() ?? Undo.AddComponent<SpeedLimitZone>(z);
                s.speedLimitKmH = 40;
            });

            // StopSignDetector en (0, 0, 30)
            BuildZone(demo.transform, "StopSignDetector", new Vector3(0, 0, 30), new Vector3(8, 3, 1.2f), z => {
                EnsureComponent<StopSignDetector>(z);
            });

            // CrosswalkDetector en (0, 0, 50)
            BuildZone(demo.transform, "CrosswalkDetector", new Vector3(0, 0, 50), new Vector3(8, 3, 4), z => {
                EnsureComponent<PedestrianCrossingDetector>(z);
            });

            // TrafficLightDetector en (0, 0, 65)
            BuildZone(demo.transform, "TrafficLightDetector", new Vector3(0, 0, 65), new Vector3(8, 3, 1.2f), z => {
                EnsureComponent<TrafficLightDetector>(z);
            });
        }

        private static void BuildZone(Transform parent, string name, Vector3 pos, Vector3 size, System.Action<GameObject> configure)
        {
            var z = FindChildByName(parent, name);
            if (z == null)
            {
                z = new GameObject(name);
                z.transform.SetParent(parent, false);
                Undo.RegisterCreatedObjectUndo(z, "Create " + name);
            }
            z.transform.localPosition = pos;
            var bc = z.GetComponent<BoxCollider>() ?? Undo.AddComponent<BoxCollider>(z);
            bc.isTrigger = true;
            bc.size = size;
            bc.center = new Vector3(0, size.y / 2 - 0.5f, 0);
            configure?.Invoke(z);
        }
    }
}
