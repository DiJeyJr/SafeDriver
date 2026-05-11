using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.Vehicle;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Construye una palanca de cambios D/N/R sobre el tablero del auto.
    /// Layout: GameObject "GearShifter" con BoxCollider + Grabbable + OneGrabRotateTransformer
    /// limitado a rotacion en X de -30 a +30 grados. Children visuales: Cylinder (cuerpo) +
    /// Sphere (pomo).
    ///
    /// Sigue el patron del volante: NO tiene Rigidbody propio, comparte el del auto.
    /// El GearShifter component detecta la rotacion local del root y notifica al
    /// VehicleController.SetGearFromShifter.
    /// </summary>
    public static class CreateGearShifter
    {
        [MenuItem("SafeDriver/Create Gear Shifter (palanca D/N/R)")]
        public static void Run()
        {
            var car = GameObject.Find("SafeDriver_Exterior_v1");
            if (car == null) { Debug.LogError("No se encontro SafeDriver_Exterior_v1."); return; }
            var carRb = car.GetComponent<Rigidbody>();
            if (carRb == null) { Debug.LogError("SafeDriver_Exterior_v1 sin Rigidbody."); return; }
            var interior = GameObject.Find("SafeDriver_Interior_v1");
            if (interior == null) { Debug.LogError("No se encontro SafeDriver_Interior_v1."); return; }
            var vehicle = car.GetComponent<VehicleController>();
            if (vehicle == null) { Debug.LogError("SafeDriver_Exterior_v1 sin VehicleController."); return; }

            // Si ya existe, abortar (no duplicar).
            var existing = GameObject.Find("GearShifter");
            if (existing != null)
            {
                Debug.LogWarning("GearShifter ya existe, seleccionando.", existing);
                Selection.activeGameObject = existing;
                return;
            }

            // Root pivot (sin mesh propio; el visual son children)
            var root = new GameObject("GearShifter");
            Undo.RegisterCreatedObjectUndo(root, "Create GearShifter");
            root.transform.SetParent(interior.transform, worldPositionStays: false);
            // Cerca del volante a la derecha del conductor, sobre el tablero
            root.transform.localPosition = new Vector3(-0.15f, 0.9f, 0.30f);
            root.transform.localRotation = Quaternion.identity;

            // Visual: cuerpo cilindrico
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>()); // colliders del primitive no sirven
            body.transform.SetParent(root.transform, worldPositionStays: false);
            body.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.022f, 0.06f, 0.022f);

            // Visual: pomo esferico
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Knob";
            Object.DestroyImmediate(knob.GetComponent<Collider>());
            knob.transform.SetParent(root.transform, worldPositionStays: false);
            knob.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            knob.transform.localRotation = Quaternion.identity;
            knob.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);

            // BoxCollider en el root: cubre solo la zona del pomo (la bola), no el cilindro.
            // Tamano generoso para que la mano pueda alcanzarlo facilmente; el proximity filter
            // limita el grab a esta zona.
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.14f, 0f);
            box.size = new Vector3(0.12f, 0.12f, 0.12f);
            box.isTrigger = false;

            // OneGrabRotateTransformer — rotacion sobre eje X, limites (-30, 30)
            var transformer = root.AddComponent<OneGrabRotateTransformer>();
            var soTransformer = new SerializedObject(transformer);
            soTransformer.FindProperty("_pivotTransform").objectReferenceValue = root.transform;
            soTransformer.FindProperty("_rotationAxis").enumValueIndex = 0; // 0 = Right (X)
            var constraints = soTransformer.FindProperty("_constraints");
            var minAngle = constraints.FindPropertyRelative("MinAngle");
            minAngle.FindPropertyRelative("Constrain").boolValue = true;
            minAngle.FindPropertyRelative("Value").floatValue = -30f;
            var maxAngle = constraints.FindPropertyRelative("MaxAngle");
            maxAngle.FindPropertyRelative("Constrain").boolValue = true;
            maxAngle.FindPropertyRelative("Value").floatValue = 30f;
            soTransformer.ApplyModifiedProperties();

            // Grabbable
            var grabbable = root.AddComponent<Grabbable>();
            var soGrabbable = new SerializedObject(grabbable);
            soGrabbable.FindProperty("_oneGrabTransformer").objectReferenceValue = transformer;
            soGrabbable.FindProperty("_maxGrabPoints").intValue = 1;
            soGrabbable.FindProperty("_kinematicWhileSelected").boolValue = true;
            soGrabbable.FindProperty("_throwWhenUnselected").boolValue = false;
            soGrabbable.ApplyModifiedProperties();

            // Proximity filter: limita el grab a la zona del BoxCollider (la bola).
            // Sin esto, ISDK auto-recolecta todos los colliders del rb del auto y se podria
            // agarrar la palanca desde cualquier lugar del auto.
            var proximityFilter = root.AddComponent<GrabProximityFilter>();
            var soFilter = new SerializedObject(proximityFilter);
            soFilter.FindProperty("hitbox").objectReferenceValue = box;
            soFilter.FindProperty("padding").floatValue = 0.06f;
            soFilter.ApplyModifiedProperties();

            // GrabInteractable — apunta al rigidbody del auto (compound, evita nested rb)
            var grabInteractable = root.AddComponent<GrabInteractable>();
            var soGI = new SerializedObject(grabInteractable);
            soGI.FindProperty("_rigidbody").objectReferenceValue = carRb;
            soGI.FindProperty("_pointableElement").objectReferenceValue = grabbable;
            var giFilters = soGI.FindProperty("_interactorFilters");
            giFilters.arraySize = 1;
            giFilters.GetArrayElementAtIndex(0).objectReferenceValue = proximityFilter;
            soGI.ApplyModifiedProperties();

            // HandGrabInteractable — para grab con manos. Mismas reglas que el SteeringWheel:
            // HandAlignment=AlignOnGrab (0), palm con SelectsWithOptionals=true, pinch con false.
            var handGrabInteractable = root.AddComponent<HandGrabInteractable>();
            var soHGI = new SerializedObject(handGrabInteractable);
            soHGI.FindProperty("_rigidbody").objectReferenceValue = carRb;
            soHGI.FindProperty("_pointableElement").objectReferenceValue = grabbable;
            soHGI.FindProperty("_supportedGrabTypes").intValue = 1; // Palm
            soHGI.FindProperty("_handAligment").intValue = 0; // AlignOnGrab
            var palmRules = soHGI.FindProperty("_palmGrabRules");
            palmRules.FindPropertyRelative("SelectsWithOptionals").boolValue = true;
            var pinchRules = soHGI.FindProperty("_pinchGrabRules");
            pinchRules.FindPropertyRelative("SelectsWithOptionals").boolValue = false;
            var hgiFilters = soHGI.FindProperty("_interactorFilters");
            hgiFilters.arraySize = 1;
            hgiFilters.GetArrayElementAtIndex(0).objectReferenceValue = proximityFilter;
            soHGI.ApplyModifiedProperties();

            // GearShifter component (lee rotacion local X y dispatcha al vehiculo)
            var shifter = root.AddComponent<GearShifter>();
            var soShifter = new SerializedObject(shifter);
            soShifter.FindProperty("vehicle").objectReferenceValue = vehicle;
            soShifter.FindProperty("pivot").objectReferenceValue = root.transform;
            soShifter.ApplyModifiedProperties();

            // Hand follower: pega la HandVisual a la palanca mientras se la agarra (igual
            // que el volante). Copia los interactor refs y HandVisuals del SteeringWheelHandFollower
            // existente, asi el setup queda sincronizado con el volante.
            var follower = root.AddComponent<GrabbableHandFollower>();
            var wheelGO = GameObject.Find("SteeringWheel");
            var wheelFollower = wheelGO != null ? wheelGO.GetComponent<SafeDriver.VR.SteeringWheelHandFollower>() : null;
            if (wheelFollower != null)
            {
                var soWheelFollower = new SerializedObject(wheelFollower);
                var soFollower = new SerializedObject(follower);
                soFollower.FindProperty("target").objectReferenceValue = root.transform;
                CopyArrayRefs(soWheelFollower, soFollower, "leftGrabInteractors");
                CopyArrayRefs(soWheelFollower, soFollower, "leftHandGrabInteractors");
                soFollower.FindProperty("leftHandVisual").objectReferenceValue = soWheelFollower.FindProperty("leftHandVisual").objectReferenceValue;
                CopyArrayRefs(soWheelFollower, soFollower, "rightGrabInteractors");
                CopyArrayRefs(soWheelFollower, soFollower, "rightHandGrabInteractors");
                soFollower.FindProperty("rightHandVisual").objectReferenceValue = soWheelFollower.FindProperty("rightHandVisual").objectReferenceValue;
                soFollower.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("No se encontro SteeringWheelHandFollower; el GrabbableHandFollower de la palanca quedo sin interactor refs. Wirealos a mano desde Inspector.");
            }

            // Activar el modo externo en el VehicleController
            var soVehicle = new SerializedObject(vehicle);
            var useExternal = soVehicle.FindProperty("useExternalShifter");
            if (useExternal != null) { useExternal.boolValue = true; soVehicle.ApplyModifiedProperties(); }

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("GearShifter creado en " + root.transform.position + ". Movelo si choca con otra cosa del tablero.", root);
        }

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
