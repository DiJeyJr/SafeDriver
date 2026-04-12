using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script que ensambla la escena completa de la tarjeta AR SafeDriver.
/// Ejecutar desde: Tools > SafeDriver > Build AR Scene
/// </summary>
public class SceneBuilder : EditorWindow
{
    [MenuItem("Tools/SafeDriver/Build AR Scene")]
    public static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Build AR Scene",
            "Esto va a configurar la escena con la mini ciudad AR.\n\n" +
            "Asegurate de tener la escena SampleScene abierta.\n" +
            "¿Continuar?",
            "Sí", "Cancelar"))
            return;

        DisableMainCamera();
        CreateMaterials();
        SetupMiniCity();
        CreateUI();
        CreateManagers();
        SetupLayers();

        EditorUtility.DisplayDialog("Done", "Escena AR armada. Configurá el Image Target manualmente desde Vuforia.", "OK");
    }

    // ============================================================
    // LAYERS
    // ============================================================
    static void SetupLayers()
    {
        // Try to set layer 6 as "Interactable"
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Layer 6
        SerializedProperty layer6 = layers.GetArrayElementAtIndex(6);
        if (string.IsNullOrEmpty(layer6.stringValue))
        {
            layer6.stringValue = "Interactable";
            tagManager.ApplyModifiedProperties();
            Debug.Log("[SceneBuilder] Layer 6 set to 'Interactable'");
        }
    }

    // ============================================================
    // MAIN CAMERA
    // ============================================================
    static void DisableMainCamera()
    {
        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null)
        {
            mainCam.SetActive(false);
            Debug.Log("[SceneBuilder] Main Camera disabled");
        }
    }

    // ============================================================
    // MATERIALS
    // ============================================================
    static string matFolder = "Assets/Materials";

    static Material CreateOrGetMaterial(string name, Color color, bool emission = false, float emissionStrength = 1f)
    {
        string path = $"{matFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        // Create URP Lit material
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        mat.name = name;
        mat.SetColor("_BaseColor", color);

        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionStrength);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (!AssetDatabase.IsValidFolder(matFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void CreateMaterials()
    {
        // Traffic light materials
        CreateOrGetMaterial("Light_Red_On", new Color(1f, 0.1f, 0.1f), true, 5f);
        CreateOrGetMaterial("Light_Red_Off", new Color(0.3f, 0.08f, 0.08f));
        CreateOrGetMaterial("Light_Yellow_On", new Color(1f, 0.85f, 0.1f), true, 5f);
        CreateOrGetMaterial("Light_Yellow_Off", new Color(0.3f, 0.25f, 0.05f));
        CreateOrGetMaterial("Light_Green_On", new Color(0.1f, 1f, 0.2f), true, 5f);
        CreateOrGetMaterial("Light_Green_Off", new Color(0.05f, 0.25f, 0.08f));

        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] Materials created");
    }

    // ============================================================
    // MINI CITY SETUP
    // ============================================================
    static void SetupMiniCity()
    {
        // Load the complete city FBX
        string cityPath = "Assets/Models/MiniCity_Complete.fbx";
        GameObject cityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cityPath);
        if (cityPrefab == null)
        {
            Debug.LogError($"[SceneBuilder] Could not find city model at {cityPath}");
            return;
        }

        // Create ImageTarget placeholder (user will replace with real Vuforia ImageTarget)
        GameObject imageTarget = new GameObject("ImageTarget_SafeDriver");
        imageTarget.transform.position = Vector3.zero;

        // Instantiate city as child
        GameObject miniCity = (GameObject)PrefabUtility.InstantiatePrefab(cityPrefab);
        miniCity.name = "MiniCity";
        miniCity.transform.SetParent(imageTarget.transform);
        miniCity.transform.localPosition = Vector3.zero;
        miniCity.transform.localRotation = Quaternion.identity;
        miniCity.transform.localScale = Vector3.one * 0.05f; // Scale down to fit on card

        // Add colliders to traffic light parts for interaction
        AddTrafficLightColliders(miniCity);

        // Create waypoints for cars
        CreateCarWaypoints(imageTarget.transform);

        // Create pedestrian crossing points
        CreatePedestrianPoints(imageTarget.transform);

        Debug.Log("[SceneBuilder] Mini City assembled under ImageTarget_SafeDriver");
    }

    static void AddTrafficLightColliders(GameObject city)
    {
        string[] lightNames = {
            "TrafficLight_North", "TrafficLight_South",
            "TrafficLight_East", "TrafficLight_West"
        };

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer < 0) interactableLayer = 6;

        foreach (string name in lightNames)
        {
            // Find the housing part of each traffic light
            Transform housing = FindDeep(city.transform, $"{name}_Housing");
            if (housing != null)
            {
                BoxCollider col = housing.gameObject.AddComponent<BoxCollider>();
                col.size = Vector3.one * 3f; // Larger for easier tapping
                housing.gameObject.layer = interactableLayer;

                // Add TrafficLightController to a parent group
                Transform pole = FindDeep(city.transform, $"{name}_Pole");
                if (pole != null)
                {
                    // Create a parent for the traffic light group
                    GameObject tlGroup = new GameObject(name);
                    tlGroup.transform.SetParent(city.transform);
                    tlGroup.transform.localPosition = pole.localPosition;
                    tlGroup.transform.localScale = Vector3.one;

                    var controller = tlGroup.AddComponent<TrafficLightController>();

                    // Assign light renderers
                    Transform redLight = FindDeep(city.transform, $"{name}_Red");
                    Transform yellowLight = FindDeep(city.transform, $"{name}_Yellow");
                    Transform greenLight = FindDeep(city.transform, $"{name}_Green");

                    if (redLight) SetSerializedField(controller, "redLight", redLight.GetComponent<Renderer>());
                    if (yellowLight) SetSerializedField(controller, "yellowLight", yellowLight.GetComponent<Renderer>());
                    if (greenLight) SetSerializedField(controller, "greenLight", greenLight.GetComponent<Renderer>());

                    // Assign materials
                    SetSerializedField(controller, "matRedOn", LoadMat("Light_Red_On"));
                    SetSerializedField(controller, "matRedOff", LoadMat("Light_Red_Off"));
                    SetSerializedField(controller, "matYellowOn", LoadMat("Light_Yellow_On"));
                    SetSerializedField(controller, "matYellowOff", LoadMat("Light_Yellow_Off"));
                    SetSerializedField(controller, "matGreenOn", LoadMat("Light_Green_On"));
                    SetSerializedField(controller, "matGreenOff", LoadMat("Light_Green_Off"));

                    // Set collider layer on all children
                    SetLayerRecursive(housing.gameObject, interactableLayer);
                }
            }
        }
    }

    static void CreateCarWaypoints(Transform parent)
    {
        // N-S car waypoints (car drives from south to north)
        GameObject nsWaypoints = new GameObject("Waypoints_NS");
        nsWaypoints.transform.SetParent(parent);
        nsWaypoints.transform.localPosition = Vector3.zero;
        nsWaypoints.transform.localScale = Vector3.one;

        Vector3[] nsPositions = {
            new Vector3(0.015f, 0, -0.25f),  // Start south
            new Vector3(0.015f, 0, -0.07f),  // Before intersection
            new Vector3(0.015f, 0, 0),        // Center
            new Vector3(0.015f, 0, 0.07f),   // After intersection
            new Vector3(0.015f, 0, 0.25f),   // End north
        };

        Transform[] nsWPs = new Transform[nsPositions.Length];
        for (int i = 0; i < nsPositions.Length; i++)
        {
            GameObject wp = new GameObject($"WP_NS_{i}");
            wp.transform.SetParent(nsWaypoints.transform);
            wp.transform.localPosition = nsPositions[i];
            nsWPs[i] = wp.transform;
        }

        // Stop line for NS
        GameObject stopLineNS = new GameObject("StopLine_NS");
        stopLineNS.transform.SetParent(nsWaypoints.transform);
        stopLineNS.transform.localPosition = new Vector3(0.015f, 0, -0.065f);

        // E-W car waypoints
        GameObject ewWaypoints = new GameObject("Waypoints_EW");
        ewWaypoints.transform.SetParent(parent);
        ewWaypoints.transform.localPosition = Vector3.zero;
        ewWaypoints.transform.localScale = Vector3.one;

        Vector3[] ewPositions = {
            new Vector3(0.25f, 0, -0.015f),
            new Vector3(0.07f, 0, -0.015f),
            new Vector3(0, 0, -0.015f),
            new Vector3(-0.07f, 0, -0.015f),
            new Vector3(-0.25f, 0, -0.015f),
        };

        Transform[] ewWPs = new Transform[ewPositions.Length];
        for (int i = 0; i < ewPositions.Length; i++)
        {
            GameObject wp = new GameObject($"WP_EW_{i}");
            wp.transform.SetParent(ewWaypoints.transform);
            wp.transform.localPosition = ewPositions[i];
            ewWPs[i] = wp.transform;
        }

        GameObject stopLineEW = new GameObject("StopLine_EW");
        stopLineEW.transform.SetParent(ewWaypoints.transform);
        stopLineEW.transform.localPosition = new Vector3(0.065f, 0, -0.015f);

        Debug.Log("[SceneBuilder] Car waypoints created");
    }

    static void CreatePedestrianPoints(Transform parent)
    {
        // Crossing points for pedestrians (start/end of each crosswalk)
        string[,] crossings = {
            { "CrossingN_Start", "CrossingN_End", "-0.08,0,0.065", "0.08,0,0.065" },
            { "CrossingS_Start", "CrossingS_End", "-0.08,0,-0.065", "0.08,0,-0.065" },
            { "CrossingE_Start", "CrossingE_End", "0.065,0,-0.08", "0.065,0,0.08" },
            { "CrossingW_Start", "CrossingW_End", "-0.065,0,-0.08", "-0.065,0,0.08" },
        };

        GameObject crossingPoints = new GameObject("CrossingPoints");
        crossingPoints.transform.SetParent(parent);
        crossingPoints.transform.localPosition = Vector3.zero;
        crossingPoints.transform.localScale = Vector3.one;

        for (int i = 0; i < crossings.GetLength(0); i++)
        {
            GameObject start = new GameObject(crossings[i, 0]);
            start.transform.SetParent(crossingPoints.transform);
            start.transform.localPosition = ParseVector3(crossings[i, 2]);

            GameObject end = new GameObject(crossings[i, 1]);
            end.transform.SetParent(crossingPoints.transform);
            end.transform.localPosition = ParseVector3(crossings[i, 3]);
        }

        Debug.Log("[SceneBuilder] Pedestrian crossing points created");
    }

    // ============================================================
    // UI
    // ============================================================
    static void CreateUI()
    {
        GameObject imageTarget = GameObject.Find("ImageTarget_SafeDriver");
        if (imageTarget == null) return;

        // --- Info Panel ---
        GameObject infoPanel = new GameObject("InfoPanel");
        infoPanel.transform.SetParent(imageTarget.transform);
        infoPanel.transform.localPosition = new Vector3(0, 0.15f, 0);
        infoPanel.transform.localScale = Vector3.one * 0.02f;

        Canvas infoCanvas = infoPanel.AddComponent<Canvas>();
        infoCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(400, 250);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(infoPanel.transform);
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.1f, 0.15f, 0.3f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(infoPanel.transform);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SafeDriver VR";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.65f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, 0);

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(infoPanel.transform);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Simulador de manejo seguro\nen Realidad Virtual\n\nTocá los semáforos para interactuar";
        descText.fontSize = 22;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.8f, 0.9f, 1f);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.05f);
        descRect.anchorMax = new Vector2(1, 0.6f);
        descRect.offsetMin = new Vector2(15, 0);
        descRect.offsetMax = new Vector2(-15, 0);

        infoPanel.AddComponent<InfoPanelController>();

        // --- Score Panel ---
        GameObject scorePanel = new GameObject("ScorePanel");
        scorePanel.transform.SetParent(imageTarget.transform);
        scorePanel.transform.localPosition = new Vector3(0.2f, 0.08f, 0);
        scorePanel.transform.localScale = Vector3.one * 0.015f;

        Canvas scoreCanvas = scorePanel.AddComponent<Canvas>();
        scoreCanvas.renderMode = RenderMode.WorldSpace;
        RectTransform scoreRect = scorePanel.GetComponent<RectTransform>();
        scoreRect.sizeDelta = new Vector2(250, 60);

        // Score BG
        GameObject scoreBg = new GameObject("ScoreBG");
        scoreBg.transform.SetParent(scorePanel.transform);
        var scoreBgImg = scoreBg.AddComponent<UnityEngine.UI.Image>();
        scoreBgImg.color = new Color(0, 0, 0, 0.6f);
        RectTransform scoreBgRect = scoreBg.GetComponent<RectTransform>();
        scoreBgRect.anchorMin = Vector2.zero;
        scoreBgRect.anchorMax = Vector2.one;
        scoreBgRect.offsetMin = Vector2.zero;
        scoreBgRect.offsetMax = Vector2.zero;

        // Score Text (using TextMeshPro for world space)
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(scorePanel.transform);
        var scoreTMP = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = "Puntaje: 0";
        scoreTMP.fontSize = 36;
        scoreTMP.fontStyle = FontStyles.Bold;
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreTMP.color = Color.white;
        RectTransform scoreTxtRect = scoreTextObj.GetComponent<RectTransform>();
        scoreTxtRect.anchorMin = Vector2.zero;
        scoreTxtRect.anchorMax = Vector2.one;
        scoreTxtRect.offsetMin = Vector2.zero;
        scoreTxtRect.offsetMax = Vector2.zero;

        // --- Tooltip ---
        GameObject tooltip = new GameObject("Tooltip");
        tooltip.transform.SetParent(imageTarget.transform);
        tooltip.transform.localPosition = new Vector3(0, 0.06f, 0.15f);
        tooltip.transform.localScale = Vector3.one * 0.01f;

        Canvas tipCanvas = tooltip.AddComponent<Canvas>();
        tipCanvas.renderMode = RenderMode.WorldSpace;
        CanvasGroup tipCG = tooltip.AddComponent<CanvasGroup>();
        RectTransform tipRect = tooltip.GetComponent<RectTransform>();
        tipRect.sizeDelta = new Vector2(500, 50);

        GameObject tipTextObj = new GameObject("TipText");
        tipTextObj.transform.SetParent(tooltip.transform);
        var tipText = tipTextObj.AddComponent<TextMeshProUGUI>();
        tipText.text = "Tocá los semáforos para cambiar la luz";
        tipText.fontSize = 30;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.color = Color.yellow;
        RectTransform tipTxtRect = tipTextObj.GetComponent<RectTransform>();
        tipTxtRect.anchorMin = Vector2.zero;
        tipTxtRect.anchorMax = Vector2.one;
        tipTxtRect.offsetMin = Vector2.zero;
        tipTxtRect.offsetMax = Vector2.zero;

        tooltip.AddComponent<TooltipController>();

        Debug.Log("[SceneBuilder] UI panels created");
    }

    // ============================================================
    // MANAGERS
    // ============================================================
    static void CreateManagers()
    {
        GameObject imageTarget = GameObject.Find("ImageTarget_SafeDriver");
        if (imageTarget == null) return;

        // Intersection Manager
        GameObject intersectionMgr = new GameObject("IntersectionManager");
        intersectionMgr.transform.SetParent(imageTarget.transform);
        intersectionMgr.AddComponent<IntersectionManager>();

        // AR Touch Interaction
        GameObject touchMgr = new GameObject("ARInteractionManager");
        touchMgr.transform.SetParent(imageTarget.transform);
        var touchScript = touchMgr.AddComponent<ARTouchInteraction>();

        // Try to assign AR Camera
        GameObject arCam = GameObject.Find("ARCamera");
        if (arCam != null)
        {
            Camera cam = arCam.GetComponent<Camera>();
            if (cam != null)
                SetSerializedField(touchScript, "arCamera", cam);
        }

        // Set interactable layer mask
        SetSerializedField(touchScript, "interactableLayer", LayerMask.GetMask("Interactable"));

        // Score Manager
        GameObject scorePanelObj = GameObject.Find("ScorePanel");
        if (scorePanelObj != null)
        {
            var scoreMgr = scorePanelObj.AddComponent<ScoreManager>();
            // Find the score text
            var scoreText = scorePanelObj.GetComponentInChildren<TextMeshProUGUI>();
            // ScoreManager uses TextMeshPro (not UGUI), but for now this works in world space canvas
        }

        Debug.Log("[SceneBuilder] Managers created");
    }

    // ============================================================
    // HELPERS
    // ============================================================
    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }

    static Material LoadMat(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Material>($"{matFolder}/{name}.mat");
    }

    static void SetSerializedField(Component component, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedField(Component component, string fieldName, int value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static Vector3 ParseVector3(string s)
    {
        string[] parts = s.Split(',');
        return new Vector3(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2])
        );
    }
}
