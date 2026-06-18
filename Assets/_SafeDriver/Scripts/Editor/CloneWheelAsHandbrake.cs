using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.Vehicle;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Clona el SteeringWheel y lo convierte en la palanca de FRENO DE MANO. Preserva el stack de grab
    /// que SI funciona y solo cambia: eje del transformer (Y→X), constraints (0..48, solo tira para
    /// arriba), radio del SphereCollider, scripts custom y la mesh visual (shaft + grip).
    ///
    /// La palanca al tirarla arriba (HandbrakeController) pone el freno de mano en el VehicleController.
    /// Idempotente: si ya existe "Handbrake", se borra y se regenera (preservando su posicion).
    /// </summary>
    public static class CloneWheelAsHandbrake
    {
        [MenuItem("SafeDriver/VR/Clonar Volante como Freno de Mano")]
        public static void Run()
        {
            var wheel = GameObject.Find("SteeringWheel");
            if (wheel == null) { Debug.LogError("[Handbrake] No se encontro SteeringWheel."); return; }

            // Preservar posicion si ya existe.
            var existing = GameObject.Find("Handbrake");
            Vector3 preservedLocalPos = new Vector3(0.02f, 0.9f, 0.27f);
            Transform parent = existing != null ? existing.transform.parent : wheel.transform.parent;
            if (existing != null)
            {
                preservedLocalPos = existing.transform.localPosition;
                Object.DestroyImmediate(existing);
            }

            var clone = Object.Instantiate(wheel, parent);
            clone.name = "Handbrake";
            Undo.RegisterCreatedObjectUndo(clone, "Clone wheel as handbrake");

            clone.transform.localPosition = preservedLocalPos;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            // Quitar mesh del root (no queremos la rueda).
            DestroyIfPresent(clone.GetComponent<MeshFilter>());
            DestroyIfPresent(clone.GetComponent<MeshRenderer>());
            // Quitar scripts especificos del volante.
            DestroyIfPresent(clone.GetComponent<SteeringWheelController>());
            DestroyIfPresent(clone.GetComponent<SteeringWheelHandFollower>());
            DestroyIfPresent(clone.GetComponent<HandGrabRigidbodyTracker>());

            // SphereCollider mas chico (el del volante es 0.22m).
            var sphere = clone.GetComponent<SphereCollider>();
            if (sphere != null) { sphere.center = new Vector3(0f, 0.12f, 0f); sphere.radius = 0.10f; }

            // Re-apuntar el filtro de proximidad al nuevo hitbox.
            var filter = clone.GetComponent<WheelGrabProximityFilter>();
            if (filter != null && sphere != null)
            {
                var soFilter = new SerializedObject(filter);
                soFilter.FindProperty("hitbox").objectReferenceValue = sphere;
                soFilter.FindProperty("radiusMultiplier").floatValue = 1.5f;
                soFilter.ApplyModifiedProperties();
            }

            // OneGrabRotateTransformer: eje Y(1) → X(0), y constraints 0..48 (la palanca solo tira arriba).
            var transformer = clone.GetComponent<OneGrabRotateTransformer>();
            if (transformer != null)
            {
                var soT = new SerializedObject(transformer);
                soT.FindProperty("_rotationAxis").enumValueIndex = 0; // Right = X
                var constraints = soT.FindProperty("_constraints");
                var min = constraints.FindPropertyRelative("MinAngle");
                min.FindPropertyRelative("Constrain").boolValue = true;
                min.FindPropertyRelative("Value").floatValue = -2f;
                var max = constraints.FindPropertyRelative("MaxAngle");
                max.FindPropertyRelative("Constrain").boolValue = true;
                max.FindPropertyRelative("Value").floatValue = 48f;
                soT.ApplyModifiedProperties();
            }

            // Visual: shaft (cilindro) + grip (esfera) al final.
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(clone.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            shaft.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // levemente inclinado hacia atras
            shaft.transform.localScale = new Vector3(0.02f, 0.08f, 0.02f);

            var grip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grip.name = "Grip";
            Object.DestroyImmediate(grip.GetComponent<Collider>());
            grip.transform.SetParent(clone.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0.16f, -0.04f);
            grip.transform.localScale = new Vector3(0.045f, 0.045f, 0.045f);

            // Vehicle ref.
            var carGo = GameObject.Find("SafeDriver_Exterior_v1");
            var vehicle = carGo != null ? carGo.GetComponent<VehicleController>() : null;

            // HandbrakeController.
            var hb = clone.AddComponent<HandbrakeController>();
            var soHb = new SerializedObject(hb);
            soHb.FindProperty("vehicle").objectReferenceValue = vehicle;
            soHb.FindProperty("pivot").objectReferenceValue = clone.transform;
            soHb.ApplyModifiedProperties();

            // GrabbableHandFollower con refs copiadas del SteeringWheelHandFollower del wheel.
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

            Selection.activeGameObject = clone;
            EditorUtility.SetDirty(clone);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(clone.scene);
            Debug.Log("[Handbrake] Palanca de freno de mano creada (clon del volante) en " + clone.transform.localPosition +
                      ". Tirar arriba = freno puesto. Reubicar/ajustar y guardar (Ctrl+S).", clone);
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
