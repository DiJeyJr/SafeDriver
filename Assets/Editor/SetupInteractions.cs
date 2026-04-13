using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Configura colliders, layers y scripts de interacción sobre la MiniCity ya importada.
/// Ejecutar: Tools > SafeDriver > Setup Interactions
/// </summary>
public class SetupInteractions
{
    [MenuItem("Tools/SafeDriver/Setup Interactions")]
    public static void Setup()
    {
        // Ensure Interactable layer exists
        SetupLayer();

        GameObject miniCity = GameObject.Find("MiniCity");
        if (miniCity == null)
        {
            Debug.LogError("[Setup] MiniCity not found!");
            return;
        }

        // Setup traffic light colliders and controllers
        SetupTrafficLights(miniCity);

        // Setup car controllers
        SetupCars();

        // Setup pedestrian controllers
        SetupPedestrians();

        // Setup IntersectionManager wiring
        SetupIntersectionManager();

        // Disable ARTouchInteraction (replaced by UI button)
        DisableTouchInteraction();

        // Setup UI button and indicators
        SetupTrafficLightUI();

        // Setup ScoreManager
        SetupScoreManager();

        EditorUtility.SetDirty(miniCity);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Setup] All interactions configured!");
    }

    static void SetupLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SerializedProperty layer6 = layers.GetArrayElementAtIndex(6);
        if (string.IsNullOrEmpty(layer6.stringValue))
        {
            layer6.stringValue = "Interactable";
            tagManager.ApplyModifiedProperties();
        }
    }

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

    static void SetupTrafficLights(GameObject miniCity)
    {
        string[] names = { "TrafficLight_North", "TrafficLight_South", "TrafficLight_East", "TrafficLight_West" };
        int interactableLayer = 6;

        Material redOn = LoadOrCreateEmissiveMat("Light_Red_On", new Color(1, 0.1f, 0.1f), 5f);
        Material redOff = LoadOrCreateEmissiveMat("Light_Red_Off", new Color(0.3f, 0.08f, 0.08f), 0f);
        Material yelOn = LoadOrCreateEmissiveMat("Light_Yellow_On", new Color(1, 0.85f, 0.1f), 5f);
        Material yelOff = LoadOrCreateEmissiveMat("Light_Yellow_Off", new Color(0.3f, 0.25f, 0.05f), 0f);
        Material grnOn = LoadOrCreateEmissiveMat("Light_Green_On", new Color(0.1f, 1, 0.2f), 5f);
        Material grnOff = LoadOrCreateEmissiveMat("Light_Green_Off", new Color(0.05f, 0.25f, 0.08f), 0f);

        foreach (string tlName in names)
        {
            Transform housing = FindDeep(miniCity.transform, $"{tlName}_Housing");
            if (housing == null) { Debug.LogWarning($"[Setup] {tlName}_Housing not found"); continue; }

            // Add collider to housing for tap detection
            if (housing.GetComponent<BoxCollider>() == null)
            {
                BoxCollider col = housing.gameObject.AddComponent<BoxCollider>();
                col.size = Vector3.one * 4f; // Large for easy tapping on small AR objects
            }
            SetLayerRecursive(housing.gameObject, interactableLayer);

            // Add TrafficLightController to housing
            TrafficLightController controller = housing.GetComponent<TrafficLightController>();
            if (controller == null)
                controller = housing.gameObject.AddComponent<TrafficLightController>();

            // Find light renderers
            Transform redT = FindDeep(miniCity.transform, $"{tlName}_Red");
            Transform yelT = FindDeep(miniCity.transform, $"{tlName}_Yellow");
            Transform grnT = FindDeep(miniCity.transform, $"{tlName}_Green");

            SerializedObject so = new SerializedObject(controller);
            if (redT) so.FindProperty("redLight").objectReferenceValue = redT.GetComponent<Renderer>();
            if (yelT) so.FindProperty("yellowLight").objectReferenceValue = yelT.GetComponent<Renderer>();
            if (grnT) so.FindProperty("greenLight").objectReferenceValue = grnT.GetComponent<Renderer>();
            so.FindProperty("matRedOn").objectReferenceValue = redOn;
            so.FindProperty("matRedOff").objectReferenceValue = redOff;
            so.FindProperty("matYellowOn").objectReferenceValue = yelOn;
            so.FindProperty("matYellowOff").objectReferenceValue = yelOff;
            so.FindProperty("matGreenOn").objectReferenceValue = grnOn;
            so.FindProperty("matGreenOff").objectReferenceValue = grnOff;
            so.ApplyModifiedProperties();

            // Also set layer on lights for raycast
            if (redT) redT.gameObject.layer = interactableLayer;
            if (yelT) yelT.gameObject.layer = interactableLayer;
            if (grnT) grnT.gameObject.layer = interactableLayer;

            // Set layer on pole too
            Transform pole = FindDeep(miniCity.transform, $"{tlName}_Pole");
            if (pole) pole.gameObject.layer = interactableLayer;

            Debug.Log($"[Setup] {tlName} configured");
        }
    }

    static void SetupCars()
    {
        GameObject miniCity = GameObject.Find("MiniCity");
        if (miniCity == null) return;

        // Delete old waypoints from ImageTarget (wrong coordinate space)
        GameObject imageTarget = GameObject.Find("ImageTarget_SafeDriver");
        if (imageTarget != null)
        {
            foreach (string old in new[] { "Waypoints_NS", "Waypoints_EW", "CrossingPoints" })
            {
                Transform t = imageTarget.transform.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        // Car_Blue_Body localPos=(-0.25, 0.13, 2.50), faces -Z
        SetupSingleCar("Car_Blue", miniCity.transform, new Vector3[] {
            new Vector3(-0.25f, 0.13f, 2.50f),
            new Vector3(-0.25f, 0.13f, 1.20f),
            new Vector3(-0.25f, 0.13f, 0f),
            new Vector3(-0.25f, 0.13f, -1.20f),
            new Vector3(-0.25f, 0.13f, -2.50f),
        }, new Vector3(-0.25f, 0.13f, 1.20f));

        // Car_Red_Body localPos=(-2.50, 0.13, -0.25), faces +X
        SetupSingleCar("Car_Red", miniCity.transform, new Vector3[] {
            new Vector3(-2.50f, 0.13f, -0.25f),
            new Vector3(-1.20f, 0.13f, -0.25f),
            new Vector3(0f, 0.13f, -0.25f),
            new Vector3(1.20f, 0.13f, -0.25f),
            new Vector3(2.50f, 0.13f, -0.25f),
        }, new Vector3(-1.20f, 0.13f, -0.25f));
    }

    static void SetupSingleCar(string carPrefix, Transform miniCity,
                                 Vector3[] waypointPositions, Vector3 stopLinePos)
    {
        // Put CarController on the Body, link other parts
        Transform carBody = FindDeep(miniCity, $"{carPrefix}_Body");
        if (carBody == null) { Debug.LogWarning($"[Setup] {carPrefix}_Body not found"); return; }

        CarController carCtrl = carBody.GetComponent<CarController>();
        if (carCtrl == null) carCtrl = carBody.gameObject.AddComponent<CarController>();

        // Find all other parts of this car to link
        var linked = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in miniCity)
        {
            if (child.name.StartsWith(carPrefix + "_") && child != carBody)
                linked.Add(child);
        }

        // Create waypoints as children of MiniCity (same coordinate space as car)
        string wpParentName = $"Waypoints_{carPrefix}";
        Transform existingWP = miniCity.Find(wpParentName);
        if (existingWP != null) Object.DestroyImmediate(existingWP.gameObject);

        GameObject wpContainer = new GameObject(wpParentName);
        wpContainer.transform.SetParent(miniCity);
        wpContainer.transform.localPosition = Vector3.zero;
        wpContainer.transform.localScale = Vector3.one;

        Transform[] wps = new Transform[waypointPositions.Length];
        for (int i = 0; i < waypointPositions.Length; i++)
        {
            GameObject wp = new GameObject($"WP_{i}");
            wp.transform.SetParent(wpContainer.transform);
            wp.transform.localPosition = waypointPositions[i];
            wps[i] = wp.transform;
        }

        GameObject stopLineObj = new GameObject("StopLine");
        stopLineObj.transform.SetParent(wpContainer.transform);
        stopLineObj.transform.localPosition = stopLinePos;

        // Wire waypoints and linked parts
        SerializedObject so = new SerializedObject(carCtrl);
        SerializedProperty wpProp = so.FindProperty("waypoints");
        wpProp.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++)
            wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        so.FindProperty("stopLine").objectReferenceValue = stopLineObj.transform;

        // Wire linked parts
        SerializedProperty linkProp = so.FindProperty("linkedParts");
        if (linkProp != null)
        {
            linkProp.arraySize = linked.Count;
            for (int i = 0; i < linked.Count; i++)
                linkProp.GetArrayElementAtIndex(i).objectReferenceValue = linked[i];
        }

        so.ApplyModifiedProperties();

        Debug.Log($"[Setup] {carPrefix} on Body, {linked.Count} linked, {wps.Length} waypoints");
    }

    static void SetupPedestrians()
    {
        GameObject miniCity = GameObject.Find("MiniCity");
        if (miniCity == null) return;

        // Create crossing points inside MiniCity (Blender coords)
        string crossingsName = "CrossingPoints";
        Transform existingCrossings = miniCity.transform.Find(crossingsName);
        if (existingCrossings != null) Object.DestroyImmediate(existingCrossings.gameObject);

        GameObject crossingsObj = new GameObject(crossingsName);
        crossingsObj.transform.SetParent(miniCity.transform);
        crossingsObj.transform.localPosition = Vector3.zero;
        crossingsObj.transform.localScale = Vector3.one;

        // Crossing point positions in Blender coords (crosswalks at ±0.95)
        string[] pedNames = { "Pedestrian_1", "Pedestrian_2", "Pedestrian_3", "Pedestrian_4" };
        // Positions matching actual torso localPositions in Unity
        Vector3[] startPositions = {
            new Vector3(1.20f, 0.44f, -0.95f),   // Ped1 torso pos
            new Vector3(-1.20f, 0.44f, 0.95f),   // Ped2 torso pos
            new Vector3(0.95f, 0.44f, 1.20f),    // Ped3 torso pos
            new Vector3(-0.95f, 0.44f, -1.20f),  // Ped4 torso pos
        };
        Vector3[] endPositions = {
            new Vector3(-1.20f, 0.44f, -0.95f),  // Ped1 crosses to opposite X
            new Vector3(1.20f, 0.44f, 0.95f),    // Ped2
            new Vector3(0.95f, 0.44f, -1.20f),   // Ped3 crosses to opposite Z
            new Vector3(-0.95f, 0.44f, 1.20f),   // Ped4
        };

        for (int i = 0; i < pedNames.Length; i++)
        {
            string pedName = pedNames[i];

            // Controller on Torso, link head/legs
            Transform pedTorsoT = FindDeep(miniCity.transform, $"{pedName}_Torso");
            if (pedTorsoT == null) { Debug.LogWarning($"[Setup] {pedName}_Torso not found"); continue; }

            PedestrianController pedCtrl = pedTorsoT.GetComponent<PedestrianController>();
            if (pedCtrl == null) pedCtrl = pedTorsoT.gameObject.AddComponent<PedestrianController>();

            var pedLinked = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in miniCity.transform)
            {
                if (child.name.StartsWith(pedName + "_") && child != pedTorsoT)
                    pedLinked.Add(child);
            }

            GameObject startPt = new GameObject($"Cross_{pedName}_Start");
            startPt.transform.SetParent(crossingsObj.transform);
            startPt.transform.localPosition = startPositions[i];

            GameObject endPt = new GameObject($"Cross_{pedName}_End");
            endPt.transform.SetParent(crossingsObj.transform);
            endPt.transform.localPosition = endPositions[i];

            SerializedObject so = new SerializedObject(pedCtrl);
            so.FindProperty("startPoint").objectReferenceValue = startPt.transform;
            so.FindProperty("endPoint").objectReferenceValue = endPt.transform;

            SerializedProperty linkProp = so.FindProperty("linkedParts");
            linkProp.arraySize = pedLinked.Count;
            for (int j = 0; j < pedLinked.Count; j++)
                linkProp.GetArrayElementAtIndex(j).objectReferenceValue = pedLinked[j];

            so.ApplyModifiedProperties();

            Debug.Log($"[Setup] {pedName} configured with crossings inside MiniCity");
        }
    }

    static void SetupIntersectionManager()
    {
        GameObject mgrObj = GameObject.Find("IntersectionManager");
        if (mgrObj == null) return;

        IntersectionManager mgr = mgrObj.GetComponent<IntersectionManager>();
        if (mgr == null) return;

        GameObject miniCity = GameObject.Find("MiniCity");
        if (miniCity == null) return;

        SerializedObject so = new SerializedObject(mgr);

        // Find traffic light controllers
        Transform nHousing = FindDeep(miniCity.transform, "TrafficLight_North_Housing");
        Transform sHousing = FindDeep(miniCity.transform, "TrafficLight_South_Housing");
        Transform eHousing = FindDeep(miniCity.transform, "TrafficLight_East_Housing");
        Transform wHousing = FindDeep(miniCity.transform, "TrafficLight_West_Housing");

        if (nHousing) so.FindProperty("northLight").objectReferenceValue = nHousing.GetComponent<TrafficLightController>();
        if (sHousing) so.FindProperty("southLight").objectReferenceValue = sHousing.GetComponent<TrafficLightController>();
        if (eHousing) so.FindProperty("eastLight").objectReferenceValue = eHousing.GetComponent<TrafficLightController>();
        if (wHousing) so.FindProperty("westLight").objectReferenceValue = wHousing.GetComponent<TrafficLightController>();

        // Cars - controller is on Body
        GameObject miniCityObj = GameObject.Find("MiniCity");
        Transform carBlue = FindDeep(miniCityObj.transform, "Car_Blue_Body");
        Transform carRed = FindDeep(miniCityObj.transform, "Car_Red_Body");

        SerializedProperty nsCars = so.FindProperty("northSouthCars");
        if (carBlue != null && nsCars != null)
        {
            nsCars.arraySize = 1;
            nsCars.GetArrayElementAtIndex(0).objectReferenceValue = carBlue.GetComponent<CarController>();
        }

        SerializedProperty ewCars = so.FindProperty("eastWestCars");
        if (carRed != null && ewCars != null)
        {
            ewCars.arraySize = 1;
            ewCars.GetArrayElementAtIndex(0).objectReferenceValue = carRed.GetComponent<CarController>();
        }

        // Pedestrians - wire to IntersectionManager
        // Ped 1 & 2 cross N-S crosswalks (affected by N-S traffic)
        // Ped 3 & 4 cross E-W crosswalks (affected by E-W traffic)
        Transform ped1 = FindDeep(miniCityObj.transform, "Pedestrian_1_Torso");
        Transform ped2 = FindDeep(miniCityObj.transform, "Pedestrian_2_Torso");
        Transform ped3 = FindDeep(miniCityObj.transform, "Pedestrian_3_Torso");
        Transform ped4 = FindDeep(miniCityObj.transform, "Pedestrian_4_Torso");

        SerializedProperty nsPeds = so.FindProperty("northSouthPedestrians");
        if (nsPeds != null)
        {
            nsPeds.arraySize = 2;
            if (ped1) nsPeds.GetArrayElementAtIndex(0).objectReferenceValue = ped1.GetComponent<PedestrianController>();
            if (ped2) nsPeds.GetArrayElementAtIndex(1).objectReferenceValue = ped2.GetComponent<PedestrianController>();
        }

        SerializedProperty ewPeds = so.FindProperty("eastWestPedestrians");
        if (ewPeds != null)
        {
            ewPeds.arraySize = 2;
            if (ped3) ewPeds.GetArrayElementAtIndex(0).objectReferenceValue = ped3.GetComponent<PedestrianController>();
            if (ped4) ewPeds.GetArrayElementAtIndex(1).objectReferenceValue = ped4.GetComponent<PedestrianController>();
        }

        // Score Manager
        ScoreManager scoreMgr = Object.FindFirstObjectByType<ScoreManager>();
        if (scoreMgr) so.FindProperty("scoreManager").objectReferenceValue = scoreMgr;

        so.ApplyModifiedProperties();
        Debug.Log("[Setup] IntersectionManager wired (cars + pedestrians)");
    }

    static void SetupTouchInteraction()
    {
        GameObject touchObj = GameObject.Find("ARInteractionManager");
        if (touchObj == null) return;

        ARTouchInteraction touch = touchObj.GetComponent<ARTouchInteraction>();
        if (touch == null) return;

        SerializedObject so = new SerializedObject(touch);

        // AR Camera
        GameObject arCam = GameObject.Find("ARCamera");
        if (arCam) so.FindProperty("arCamera").objectReferenceValue = arCam.GetComponent<Camera>();

        // Interactable layer mask (layer 6)
        so.FindProperty("interactableLayer").intValue = 1 << 6;
        so.ApplyModifiedProperties();

        Debug.Log("[Setup] ARTouchInteraction configured");
    }

    static void SetupScoreManager()
    {
        ScoreManager scoreMgr = Object.FindFirstObjectByType<ScoreManager>();
        if (scoreMgr == null) return;

        // Find score text
        TextMeshProUGUI scoreTxt = scoreMgr.GetComponentInChildren<TextMeshProUGUI>();
        if (scoreTxt != null)
        {
            // ScoreManager uses TextMeshPro (world space) - need to check type
            SerializedObject so = new SerializedObject(scoreMgr);
            SerializedProperty txtProp = so.FindProperty("scoreText");
            if (txtProp != null)
            {
                // The component might need TextMeshPro instead of TextMeshProUGUI for world canvas
                txtProp.objectReferenceValue = scoreTxt;
                so.ApplyModifiedProperties();
            }
        }
        Debug.Log("[Setup] ScoreManager configured");
    }

    static Material LoadOrCreateEmissiveMat(string name, Color color, float emission)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        m = new Material(shader);
        m.name = name;
        m.SetColor("_BaseColor", color);
        if (emission > 0)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static void DisableTouchInteraction()
    {
        GameObject touchObj = GameObject.Find("ARInteractionManager");
        if (touchObj != null)
        {
            var touch = touchObj.GetComponent<ARTouchInteraction>();
            if (touch != null) touch.enabled = false;
        }
        Debug.Log("[Setup] ARTouchInteraction disabled (replaced by UI)");
    }

    static void SetupTrafficLightUI()
    {
        var oldUI = GameObject.Find("TrafficLightUI");
        if (oldUI != null) Object.DestroyImmediate(oldUI);

        // EventSystem
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Screen Space Canvas
        GameObject canvasObj = new GameObject("TrafficLightUI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel (bottom center)
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 40);
        panelRect.sizeDelta = new Vector2(400, 200);

        UnityEngine.UI.Image panelBg = panel.AddComponent<UnityEngine.UI.Image>();
        panelBg.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);

        // Outline
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(panel.transform, false);
        RectTransform outlineRect = outline.AddComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = new Vector2(-2, -2);
        outlineRect.offsetMax = new Vector2(2, 2);
        outline.transform.SetAsFirstSibling();
        outline.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.7f, 0.9f, 0.8f);

        // Indicator row
        GameObject indicatorRow = new GameObject("IndicatorRow");
        indicatorRow.transform.SetParent(panel.transform, false);
        RectTransform indRowRect = indicatorRow.AddComponent<RectTransform>();
        indRowRect.anchorMin = new Vector2(0, 0.55f);
        indRowRect.anchorMax = new Vector2(1, 0.95f);
        indRowRect.offsetMin = new Vector2(10, 0);
        indRowRect.offsetMax = new Vector2(-10, 0);
        var hlg = indicatorRow.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject miniCity = GameObject.Find("MiniCity");
        string[] dirs = { "N", "S", "E", "W" };
        string[] tlNames = {
            "TrafficLight_North_Housing", "TrafficLight_South_Housing",
            "TrafficLight_East_Housing", "TrafficLight_West_Housing"
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject indObj = new GameObject($"Indicator_{dirs[i]}");
            indObj.transform.SetParent(indicatorRow.transform, false);
            indObj.AddComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
            indObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.2f);

            GameObject light = new GameObject("Light");
            light.transform.SetParent(indObj.transform, false);
            RectTransform lr = light.AddComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.15f, 0.15f);
            lr.anchorMax = new Vector2(0.85f, 0.85f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var lightImg = light.AddComponent<UnityEngine.UI.Image>();
            lightImg.color = new Color(0.9f, 0.15f, 0.15f);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(indObj.transform, false);
            RectTransform labRect = label.AddComponent<RectTransform>();
            labRect.anchorMin = new Vector2(0, -0.1f);
            labRect.anchorMax = new Vector2(1, 0.15f);
            labRect.offsetMin = labRect.offsetMax = Vector2.zero;
            var labText = label.AddComponent<TMPro.TextMeshProUGUI>();
            labText.text = dirs[i]; labText.fontSize = 16;
            labText.alignment = TMPro.TextAlignmentOptions.Center;
            labText.color = new Color(0.7f, 0.85f, 1f);

            var indicator = indObj.AddComponent<TrafficLightIndicator>();
            var indSO = new SerializedObject(indicator);
            indSO.FindProperty("indicatorImage").objectReferenceValue = lightImg;
            indSO.FindProperty("directionLabel").stringValue = dirs[i];
            Transform housing = FindDeep(miniCity.transform, tlNames[i]);
            if (housing != null)
            {
                var ctrl = housing.GetComponent<TrafficLightController>();
                if (ctrl != null) indSO.FindProperty("targetLight").objectReferenceValue = ctrl;
            }
            indSO.ApplyModifiedProperties();
        }

        // Button
        GameObject btnObj = new GameObject("ChangeButton");
        btnObj.transform.SetParent(panel.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0.05f);
        btnRect.anchorMax = new Vector2(0.9f, 0.48f);
        btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;
        btnObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.5f, 0.75f);
        var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.5f, 0.75f);
        colors.highlightedColor = new Color(0.2f, 0.6f, 0.85f);
        colors.pressedColor = new Color(0.1f, 0.35f, 0.55f);
        btn.colors = colors;

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btr = btnTextObj.AddComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        btnText.text = "Cambiar Semaforo"; btnText.fontSize = 28;
        btnText.fontStyle = TMPro.FontStyles.Bold;
        btnText.alignment = TMPro.TextAlignmentOptions.Center;
        btnText.color = Color.white;

        var tlBtn = btnObj.AddComponent<TrafficLightButton>();
        var btnSO = new SerializedObject(tlBtn);
        // Wire to IntersectionManager
        GameObject mgrObj = GameObject.Find("IntersectionManager");
        if (mgrObj != null)
        {
            var mgr = mgrObj.GetComponent<IntersectionManager>();
            if (mgr != null) btnSO.FindProperty("intersectionManager").objectReferenceValue = mgr;
        }
        btnSO.ApplyModifiedProperties();

        Debug.Log("[Setup] Traffic Light UI created (button + 4 indicators)");
    }
}
