using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

/// <summary>
/// Arma la escena GroundPlane (AR sin tracker): ARCamera + Plane Finder + Ground Plane Stage con 3 objetos,
/// selector de objeto en pantalla y botones para cambiar de modo. Es idempotente: si la escena ya tiene
/// el Stage solo limpia residuos de Vuforia (ver RemoveVuforiaLeftovers); borrala a mano para regenerarla.
/// </summary>
public static class GroundPlaneSceneBuilder
{
    const string ScenePath = "Assets/Scenes/GroundPlane.unity";
    const string ImageTargetScene = "SampleScene";
    const string StageName = "Ground Plane Stage";

    struct Option
    {
        public string label, partPrefix;
        public float targetSize;     // en metros
        public bool bySize;          // true = lado mas largo, false = altura
    }

    // Las piezas salen de la ciudad completa: los FBX sueltos (Car_Blue.fbx, Pedestrian_Blue.fbx,
    // TrafficLight.fbx) vienen mal exportados (piezas rotadas y desplazadas, eje Z arriba).
    const string CityModelPath = "Assets/Models/MiniCity_Complete.fbx";

    // Tamaños de mesa: la ciudad completa es una maqueta, estos objetos van sueltos sobre el plano
    static readonly Option[] Options =
    {
        new Option { label = "Auto",     partPrefix = "Car_Blue_",           targetSize = 0.25f, bySize = true  },
        new Option { label = "Peaton",   partPrefix = "Pedestrian_1_",       targetSize = 0.20f, bySize = false },
        new Option { label = "Semaforo", partPrefix = "TrafficLight_North_", targetSize = 0.30f, bySize = false },
    };

    [MenuItem("Tools/SafeDriver/Build Ground Plane Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(ScenePath))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        if (GameObject.Find(StageName) != null)
        {
            // Escena ya armada: solo se sacan residuos que dejaron versiones anteriores del builder
            if (RemoveVuforiaLeftovers())
            {
                EnableDeviceTracking();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                Debug.Log("[GroundPlane] Escena limpiada y guardada en " + ScenePath);
            }
            else
                Debug.Log("[GroundPlane] La escena ya tiene el Ground Plane Stage, no se regenera (borrala a mano para recrearla)");
            return;
        }

        CreateLight();
        CreateVuforiaObjects(out PlaneFinderBehaviour planeFinder, out AnchorBehaviour stage);
        var optionObjects = CreateOptions(stage.transform);
        CreateUI(planeFinder, optionObjects);
        RemoveVuforiaLeftovers();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AddToBuildSettings();
        Debug.Log("[GroundPlane] Escena armada y guardada en " + ScenePath);
    }

    /// <summary>
    /// Ground Plane necesita el tracking del dispositivo (la pose del celular en el mundo). Es el "Track Device
    /// Pose" del inspector del ARCamera, que vive en VuforiaConfiguration: con eso activo Vuforia crea su propio
    /// DevicePoseBehaviour en runtime (VuforiaBehaviour.Awake) y es el unico que recibe el observer de pose.
    /// </summary>
    static void EnableDeviceTracking()
    {
        var config = VuforiaConfiguration.Instance;
        if (config.DeviceTracker.AutoInitAndStartTracker) return;
        config.DeviceTracker.AutoInitAndStartTracker = true;
        EditorUtility.SetDirty(config);
        Debug.Log("[GroundPlane] Track Device Pose activado en VuforiaConfiguration");
    }

    /// <summary>
    /// Saca lo que no tiene que estar en la escena: DevicePoseBehaviours agregados a mano (Vuforia crea el suyo
    /// en runtime; el de la escena queda huerfano, nunca sale de NO_POSE y encima resetea el tracking al
    /// arrancar) y el UnityLifeCycleListener que Vuforia crea al tocar sus menus desde el editor (es un
    /// singleton interno de runtime, no va serializado). El listener se barre al final porque destruir un
    /// DevicePoseBehaviour en el editor puede volver a crearlo.
    /// </summary>
    static bool RemoveVuforiaLeftovers()
    {
        bool changed = false;
        foreach (var pose in Object.FindObjectsByType<DevicePoseBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log("[GroundPlane] Saco DevicePoseBehaviour huerfano de " + pose.gameObject.name);
            Object.DestroyImmediate(pose);
            changed = true;
        }
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb.GetType().FullName != "Vuforia.Internal.Core.UnityLifeCycleListener") continue;
            Debug.Log("[GroundPlane] Saco UnityLifeCycleListener residual (" + mb.gameObject.name + ")");
            Object.DestroyImmediate(mb.gameObject);
            changed = true;
        }
        return changed;
    }

    static void CreateLight()
    {
        if (Object.FindFirstObjectByType<Light>() != null) return;
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Undo.RegisterCreatedObjectUndo(go, "Crear luz");
    }

    /// <summary>Usa los menus oficiales de Vuforia para que los componentes queden como espera el paquete.</summary>
    static void CreateVuforiaObjects(out PlaneFinderBehaviour planeFinder, out AnchorBehaviour stage)
    {
        if (Object.FindFirstObjectByType<VuforiaBehaviour>() == null)
        {
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera");
        }

        // Sin seleccion, el menu crea los objetos en la raiz (con seleccion los cuelga de ella)
        Selection.activeObject = null;
        EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/Ground Plane/Plane Finder");
        Selection.activeObject = null;
        EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/Ground Plane/Ground Plane Stage");

        // Sin tracking del dispositivo el Plane Finder no devuelve hit tests ni se pueden crear anchors.
        // No se agrega ningun DevicePoseBehaviour a la escena: ver EnableDeviceTracking.
        EnableDeviceTracking();

        planeFinder = Object.FindFirstObjectByType<PlaneFinderBehaviour>();
        stage = Object.FindFirstObjectByType<AnchorBehaviour>();
        if (planeFinder == null || stage == null)
            throw new System.Exception("[GroundPlane] No se crearon el Plane Finder / Stage con los menus de Vuforia");

        // Por si el menu los dejo colgando de algo
        planeFinder.transform.SetParent(null, true);
        stage.transform.SetParent(null, true);
        stage.gameObject.name = StageName;

        // Automatico: busca planos en el centro de la pantalla y muestra el indicador. El toque lo maneja
        // GroundPlacementController (coloca sobre el ultimo plano detectado).
        planeFinder.HitTestMode = HitTestMode.AUTOMATIC;

        var positioning = planeFinder.GetComponent<ContentPositioningBehaviour>();
        positioning.AnchorStage = stage;
        positioning.DuplicateStage = true;

        // El input lo maneja GroundPlacementController (para ignorar los toques sobre la UI),
        // asi que se saca el listener de input del Plane Finder.
        var listener = planeFinder.GetComponent<AnchorInputListenerBehaviour>();
        if (listener != null) Object.DestroyImmediate(listener);

        EditorUtility.SetDirty(planeFinder);
        EditorUtility.SetDirty(positioning);
    }

    static GameObject[] CreateOptions(Transform stage)
    {
        var city = AssetDatabase.LoadAssetAtPath<GameObject>(CityModelPath);
        if (city == null)
            throw new System.Exception("[GroundPlane] No se encontro el modelo " + CityModelPath);

        var result = new GameObject[Options.Length];
        for (int i = 0; i < Options.Length; i++)
        {
            var opt = Options[i];
            var go = new GameObject(opt.label);
            go.transform.SetParent(stage, false);

            // Copia las piezas de ese objeto (sin las del auto opuesto "_Opp"), con su pose dentro de la ciudad
            int copied = 0;
            foreach (Transform part in city.transform)
            {
                if (!part.name.StartsWith(opt.partPrefix) || part.name.Contains("_Opp")) continue;
                var copy = Object.Instantiate(part.gameObject, go.transform, false);
                copy.name = part.name;
                copied++;
            }
            if (copied == 0)
                throw new System.Exception("[GroundPlane] No hay piezas " + opt.partPrefix + " en " + CityModelPath);

            FitToSize(go, opt);
            result[i] = go;
            go.SetActive(i == 0);
        }
        return result;
    }

    /// <summary>Escala el modelo a un tamaño real y lo apoya en y = 0 centrado en el stage.</summary>
    static void FitToSize(GameObject go, Option opt)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogWarning("[GroundPlane] " + go.name + " no tiene Renderers"); return; }

        Bounds b = Combine(renderers);
        float current = opt.bySize ? Mathf.Max(b.size.x, b.size.y, b.size.z) : b.size.y;
        if (current > 0.0001f)
            go.transform.localScale = Vector3.one * (opt.targetSize / current);

        b = Combine(renderers);
        go.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);

        foreach (var r in renderers)
            foreach (var m in r.sharedMaterials)
                if (m != null && m.shader != null)
                    Debug.Log("[GroundPlane] " + go.name + " material " + m.name + " -> " + m.shader.name);
    }

    static Bounds Combine(Renderer[] renderers)
    {
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // ---------- UI ----------

    static void CreateUI(PlaneFinderBehaviour planeFinder, GameObject[] optionObjects)
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var canvasObj = new GameObject("GroundPlaneUI");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f; // vertical: se ancla al ancho
        canvasObj.AddComponent<GraphicRaycaster>();

        // Todo dentro del safe area para que no lo tape el notch
        var safe = NewRect("SafeArea", canvasObj.transform);
        safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        safe.gameObject.AddComponent<SafeAreaFitter>();

        var controllerObj = new GameObject("GroundPlacement");
        var controller = controllerObj.AddComponent<GroundPlacementController>();

        // Selector de objeto (abajo al centro)
        var panel = NewRect("OptionsPanel", safe);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0, 30);
        panel.sizeDelta = new Vector2(960, 150);
        var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        // Sin childControl los hijos se quedan en su tamaño por defecto (100x100) y el layout solo los separa
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var images = new Image[optionObjects.Length];
        for (int i = 0; i < optionObjects.Length; i++)
        {
            var btn = NewButton(Options[i].label, panel, 40, out Image img);
            images[i] = img;
            UnityEventTools.AddIntPersistentListener(btn.onClick, controller.Select, i);
        }

        // Borrar lo colocado (arriba a la derecha)
        var clear = NewButton("Borrar todo", safe, 32, out _);
        PlaceCorner(clear.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(300, 100));
        UnityEventTools.AddPersistentListener(clear.onClick, controller.ClearPlaced);

        // Cambio de modo (arriba a la izquierda)
        var switcher = canvasObj.AddComponent<SceneSwitcher>();
        var toImage = NewButton("Modo Image Target", safe, 30, out _);
        PlaceCorner(toImage.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(20, -20), new Vector2(380, 100));
        UnityEventTools.AddStringPersistentListener(toImage.onClick, switcher.Load, ImageTargetScene);

        // Estado: dice si hay superficie y deja contadores para diagnosticar en el celular
        var status = NewRect("Status", safe);
        status.anchorMin = status.anchorMax = new Vector2(0.5f, 0f);
        status.pivot = new Vector2(0.5f, 0f);
        status.anchoredPosition = new Vector2(0, 200);
        status.sizeDelta = new Vector2(1000, 220);
        var statusText = status.gameObject.AddComponent<TextMeshProUGUI>();
        statusText.text = "Buscando superficie...";
        statusText.fontSize = 36;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        statusText.raycastTarget = false;

        var so = new SerializedObject(controller);
        so.FindProperty("planeFinder").objectReferenceValue = planeFinder;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        var optProp = so.FindProperty("options");
        optProp.arraySize = optionObjects.Length;
        var imgProp = so.FindProperty("optionButtonImages");
        imgProp.arraySize = images.Length;
        for (int i = 0; i < optionObjects.Length; i++)
        {
            optProp.GetArrayElementAtIndex(i).objectReferenceValue = optionObjects[i];
            imgProp.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void PlaceCorner(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Button NewButton(string label, Transform parent, int fontSize, out Image image)
    {
        var rt = NewRect(label, parent);
        image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        var btn = rt.gameObject.AddComponent<Button>();

        var textRt = NewRect("Text", rt);
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return btn;
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        // El cambio queda en memoria hasta guardar el proyecto
        EditorApplication.ExecuteMenuItem("File/Save Project");
    }

    /// <summary>
    /// Agrega a la escena del Image Target un boton para pasar a Ground Plane. Va en un canvas aparte:
    /// la UI del Image Target se oculta hasta detectar el target (TrackingUIController) y el boton
    /// tiene que estar siempre visible.
    /// </summary>
    [MenuItem("Tools/SafeDriver/Add Mode Switch To Image Target Scene")]
    public static void AddSwitchToImageTargetScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/" + ImageTargetScene + ".unity", OpenSceneMode.Single);

        if (GameObject.Find("ModeSwitchUI") != null)
        {
            Debug.Log("[GroundPlane] ModeSwitchUI ya existe en " + ImageTargetScene + ", no se regenera");
            return;
        }

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var canvasObj = new GameObject("ModeSwitchUI");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f; // vertical: se ancla al ancho
        canvasObj.AddComponent<GraphicRaycaster>();

        var safe = NewRect("SafeArea", canvasObj.transform);
        safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        safe.gameObject.AddComponent<SafeAreaFitter>();

        var switcher = canvasObj.AddComponent<SceneSwitcher>();
        var btn = NewButton("Modo Ground Plane", safe, 30, out _);
        PlaceCorner(btn.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(20, -20), new Vector2(400, 100));
        UnityEventTools.AddStringPersistentListener(btn.onClick, switcher.Load, "GroundPlane");

        AddToBuildSettings();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[GroundPlane] Boton de cambio de modo agregado a " + ImageTargetScene);
    }
}
