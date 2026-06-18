using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.Vehicle;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Monta las luces direccionales (guiños) del auto + la palanca que las acciona:
    ///   - 4 luces en las esquinas (delantera/trasera izq y der) + 2 indicadores en el tablero,
    ///     controladas por un TurnSignalController en el auto (parpadean en ambar).
    ///   - Una palanca grabbable (clon del volante, eje X, 3 posiciones) con TurnSignalStalk:
    ///     arriba = derecha, centro = apagado, abajo = izquierda.
    ///
    /// Idempotente: regenera si ya existe. Materiales ambar/dim reusados de los Demo.
    /// </summary>
    public static class SetupTurnSignals
    {
        private const string DemoMatDir = "Assets/_SafeDriver/Materials/Demo/";

        [MenuItem("SafeDriver/VR/Crear Luces Direccionales + Palanca")]
        public static void Run()
        {
            var car = GameObject.Find("SafeDriver_Exterior_v1");
            if (car == null) { Debug.LogError("[TurnSignal] No se encontro el auto del player."); return; }
            var interior = GameObject.Find("SafeDriver_Exterior_v1/SafeDriver_Interior_v1");

            var litMat = LoadMat("LitYellow");
            var dimMat = LoadMat("DimDark");

            // Limpiar previo.
            DestroyChild(car.transform, "TurnSignalLights");
            if (interior != null) DestroyChild(interior.transform, "TurnSignalDash");

            // --- Luces de las esquinas (hijas del auto) ---
            var lightsRoot = new GameObject("TurnSignalLights");
            lightsRoot.transform.SetParent(car.transform, false);
            var fl = MakeLight(lightsRoot.transform, "Light_FL", new Vector3(-0.75f, 0.5f,  1.7f), dimMat);
            var fr = MakeLight(lightsRoot.transform, "Light_FR", new Vector3( 0.75f, 0.5f,  1.7f), dimMat);
            var rl = MakeLight(lightsRoot.transform, "Light_RL", new Vector3(-0.75f, 0.5f, -1.7f), dimMat);
            var rr = MakeLight(lightsRoot.transform, "Light_RR", new Vector3( 0.75f, 0.5f, -1.7f), dimMat);

            // --- Indicadores del tablero (hijos del interior, parpadean con el guiño) ---
            Renderer dashL = null, dashR = null;
            if (interior != null)
            {
                var dashRoot = new GameObject("TurnSignalDash");
                dashRoot.transform.SetParent(interior.transform, false);
                dashRoot.transform.localPosition = Vector3.zero;
                dashL = MakeDashArrow(dashRoot.transform, "Dash_L", new Vector3(-0.05f, 0.99f, 0.40f), dimMat);
                dashR = MakeDashArrow(dashRoot.transform, "Dash_R", new Vector3( 0.05f, 0.99f, 0.40f), dimMat);
            }

            // --- Controller en el auto ---
            var ctrl = car.GetComponent<TurnSignalController>();
            if (ctrl == null) ctrl = car.AddComponent<TurnSignalController>();
            var so = new SerializedObject(ctrl);
            SetRendererArray(so, "leftLights",  Filter(fl, rl, dashL));
            SetRendererArray(so, "rightLights", Filter(fr, rr, dashR));
            so.FindProperty("litMat").objectReferenceValue = litMat;
            so.FindProperty("dimMat").objectReferenceValue = dimMat;
            so.ApplyModifiedProperties();

            // --- Palanca (clon del volante) ---
            CreateStalk(ctrl);

            EditorUtility.SetDirty(car);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(car.scene);
            Debug.Log("[TurnSignal] Luces direccionales + palanca creadas. Palanca: arriba=derecha, " +
                      "centro=apagado, abajo=izquierda. Reubicar/ajustar y guardar (Ctrl+S).", car);
        }

        // ============================================================
        //   Palanca (clon del volante, igual que el freno de mano)
        // ============================================================

        private static void CreateStalk(TurnSignalController ctrl)
        {
            var wheel = GameObject.Find("SteeringWheel");
            if (wheel == null) { Debug.LogWarning("[TurnSignal] Sin SteeringWheel; no se creo la palanca."); return; }

            var existing = GameObject.Find("TurnSignalStalk");
            Vector3 pos = new Vector3(-0.62f, 0.95f, 0.30f);
            Transform parent = existing != null ? existing.transform.parent : wheel.transform.parent;
            if (existing != null) { pos = existing.transform.localPosition; Object.DestroyImmediate(existing); }

            var clone = Object.Instantiate(wheel, parent);
            clone.name = "TurnSignalStalk";
            clone.transform.localPosition = pos;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            DestroyIfPresent(clone.GetComponent<MeshFilter>());
            DestroyIfPresent(clone.GetComponent<MeshRenderer>());
            DestroyIfPresent(clone.GetComponent<SteeringWheelController>());
            DestroyIfPresent(clone.GetComponent<SteeringWheelHandFollower>());
            DestroyIfPresent(clone.GetComponent<HandGrabRigidbodyTracker>());

            var sphere = clone.GetComponent<SphereCollider>();
            if (sphere != null) { sphere.center = new Vector3(0f, 0.06f, -0.06f); sphere.radius = 0.08f; }

            var filter = clone.GetComponent<WheelGrabProximityFilter>();
            if (filter != null && sphere != null)
            {
                var soF = new SerializedObject(filter);
                soF.FindProperty("hitbox").objectReferenceValue = sphere;
                soF.FindProperty("radiusMultiplier").floatValue = 1.5f;
                soF.ApplyModifiedProperties();
            }

            // Transformer: eje X, 3 zonas (-30..30).
            var transformer = clone.GetComponent<OneGrabRotateTransformer>();
            if (transformer != null)
            {
                var soT = new SerializedObject(transformer);
                soT.FindProperty("_rotationAxis").enumValueIndex = 0; // X
                var c = soT.FindProperty("_constraints");
                var min = c.FindPropertyRelative("MinAngle");
                min.FindPropertyRelative("Constrain").boolValue = true;
                min.FindPropertyRelative("Value").floatValue = -30f;
                var max = c.FindPropertyRelative("MaxAngle");
                max.FindPropertyRelative("Constrain").boolValue = true;
                max.FindPropertyRelative("Value").floatValue = 30f;
                soT.ApplyModifiedProperties();
            }

            // Visual: brazo apuntando a la izquierda (sale de la columna) + punta.
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "Arm";
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(clone.transform, false);
            arm.transform.localPosition = new Vector3(0.04f, 0.0f, -0.05f);
            arm.transform.localRotation = Quaternion.Euler(75f, 0f, 0f); // casi horizontal, hacia atras
            arm.transform.localScale = new Vector3(0.013f, 0.06f, 0.013f);
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Tip";
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            tip.transform.SetParent(clone.transform, false);
            tip.transform.localPosition = new Vector3(0.04f, 0.0f, -0.11f);
            tip.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

            var stalk = clone.AddComponent<TurnSignalStalk>();
            var soS = new SerializedObject(stalk);
            soS.FindProperty("controller").objectReferenceValue = ctrl;
            soS.FindProperty("pivot").objectReferenceValue = clone.transform;
            soS.ApplyModifiedProperties();

            var follower = clone.AddComponent<GrabbableHandFollower>();
            var wheelFollower = wheel.GetComponent<SteeringWheelHandFollower>();
            if (wheelFollower != null)
            {
                var soWF = new SerializedObject(wheelFollower);
                var soF = new SerializedObject(follower);
                soF.FindProperty("target").objectReferenceValue = clone.transform;
                CopyArrayRefs(soWF, soF, "leftGrabInteractors");
                CopyArrayRefs(soWF, soF, "leftHandGrabInteractors");
                soF.FindProperty("leftHandVisual").objectReferenceValue = soWF.FindProperty("leftHandVisual").objectReferenceValue;
                CopyArrayRefs(soWF, soF, "rightGrabInteractors");
                CopyArrayRefs(soWF, soF, "rightHandGrabInteractors");
                soF.FindProperty("rightHandVisual").objectReferenceValue = soWF.FindProperty("rightHandVisual").objectReferenceValue;
                soF.ApplyModifiedProperties();
            }
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static Renderer MakeLight(Transform parent, string name, Vector3 localPos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.14f;
            var r = go.GetComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            return r;
        }

        // Indicador del tablero: un cubo chato (placeholder de flecha) que mira al conductor.
        private static Renderer MakeDashArrow(Transform parent, string name, Vector3 localPos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            go.transform.localScale = new Vector3(0.035f, 0.025f, 0.008f);
            var r = go.GetComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            return r;
        }

        private static Renderer[] Filter(params Renderer[] rs)
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in rs) if (r != null) list.Add(r);
            return list.ToArray();
        }

        private static void SetRendererArray(SerializedObject so, string prop, Renderer[] rs)
        {
            var arr = so.FindProperty(prop);
            arr.arraySize = rs.Length;
            for (int i = 0; i < rs.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rs[i];
        }

        private static Material LoadMat(string fileName)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(DemoMatDir + fileName + ".mat");
            if (m == null) Debug.LogWarning("[TurnSignal] Material no encontrado: " + fileName);
            return m;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static void DestroyIfPresent(Object o) { if (o != null) Object.DestroyImmediate(o); }

        private static void CopyArrayRefs(SerializedObject src, SerializedObject dst, string propertyName)
        {
            var sp = src.FindProperty(propertyName);
            var dp = dst.FindProperty(propertyName);
            if (sp == null || dp == null) return;
            dp.arraySize = sp.arraySize;
            for (int i = 0; i < sp.arraySize; i++)
                dp.GetArrayElementAtIndex(i).objectReferenceValue = sp.GetArrayElementAtIndex(i).objectReferenceValue;
        }
    }
}
