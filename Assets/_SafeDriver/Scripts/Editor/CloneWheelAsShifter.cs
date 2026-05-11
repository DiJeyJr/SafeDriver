using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.Vehicle;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Clona el SteeringWheel y lo convierte en GearShifter. Preserva el setup completo
    /// del wheel (que SI funciona) y solo cambia: axis del transformer (Y -> X), constraints
    /// (±180 -> ±30), radius del SphereCollider, scripts custom, mesh visual.
    ///
    /// El GearShifter anterior se borra; su posicion local se preserva en el clon.
    /// </summary>
    public static class CloneWheelAsShifter
    {
        [MenuItem("SafeDriver/Clone Wheel as Gear Shifter")]
        public static void Run()
        {
            var wheel = GameObject.Find("SteeringWheel");
            if (wheel == null) { Debug.LogError("No se encontro SteeringWheel."); return; }

            // Capturar la posicion local del GearShifter actual (si existe) para preservarla.
            var existing = GameObject.Find("GearShifter");
            Vector3 preservedLocalPos = new Vector3(-0.15f, 0.9f, 0.30f);
            Transform parent = existing != null ? existing.transform.parent : null;
            if (existing != null)
            {
                preservedLocalPos = existing.transform.localPosition;
                Object.DestroyImmediate(existing);
            }

            // Si no habia GearShifter anterior, usar el parent del wheel
            if (parent == null) parent = wheel.transform.parent;

            // Duplicar el wheel
            var clone = Object.Instantiate(wheel, parent);
            clone.name = "GearShifter";
            Undo.RegisterCreatedObjectUndo(clone, "Clone wheel as gear shifter");

            // Reset rotation y aplicar posicion preservada
            clone.transform.localPosition = preservedLocalPos;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            // Quitar mesh del root (no queremos rueda)
            var mf = clone.GetComponent<MeshFilter>();
            if (mf != null) Object.DestroyImmediate(mf);
            var mr = clone.GetComponent<MeshRenderer>();
            if (mr != null) Object.DestroyImmediate(mr);

            // Quitar scripts especificos del wheel
            var swc = clone.GetComponent<SafeDriver.VR.SteeringWheelController>();
            if (swc != null) Object.DestroyImmediate(swc);
            var swhf = clone.GetComponent<SafeDriver.VR.SteeringWheelHandFollower>();
            if (swhf != null) Object.DestroyImmediate(swhf);
            // El HandGrabRigidbodyTracker es global (FindObjectsByType); el del wheel ya cubre todos
            var hgrt = clone.GetComponent<SafeDriver.VR.HandGrabRigidbodyTracker>();
            if (hgrt != null) Object.DestroyImmediate(hgrt);

            // Achicar SphereCollider (el del wheel es 0.22m)
            var sphere = clone.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.center = new Vector3(0f, 0.14f, 0f);
                sphere.radius = 0.10f;
            }

            // Cambiar el WheelGrabProximityFilter para apuntar al nuevo hitbox (se mantiene
            // su tipo, solo re-wirear el hitbox)
            var filter = clone.GetComponent<SafeDriver.VR.WheelGrabProximityFilter>();
            if (filter != null && sphere != null)
            {
                var soFilter = new SerializedObject(filter);
                soFilter.FindProperty("hitbox").objectReferenceValue = sphere;
                soFilter.FindProperty("radiusMultiplier").floatValue = 1.5f;
                soFilter.ApplyModifiedProperties();
            }

            // Cambiar OneGrabRotateTransformer: axis Y(1) -> X(0), constraints ±180 -> ±30
            var transformer = clone.GetComponent<OneGrabRotateTransformer>();
            if (transformer != null)
            {
                var soT = new SerializedObject(transformer);
                soT.FindProperty("_rotationAxis").enumValueIndex = 0; // Right = X
                var constraints = soT.FindProperty("_constraints");
                var min = constraints.FindPropertyRelative("MinAngle");
                min.FindPropertyRelative("Constrain").boolValue = true;
                min.FindPropertyRelative("Value").floatValue = -30f;
                var max = constraints.FindPropertyRelative("MaxAngle");
                max.FindPropertyRelative("Constrain").boolValue = true;
                max.FindPropertyRelative("Value").floatValue = 30f;
                soT.ApplyModifiedProperties();
            }

            // Agregar children visuales (cilindro vertical + bola arriba)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(clone.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            body.transform.localScale = new Vector3(0.022f, 0.06f, 0.022f);

            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Knob";
            Object.DestroyImmediate(knob.GetComponent<Collider>());
            knob.transform.SetParent(clone.transform, false);
            knob.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            knob.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);

            // Vehicle ref
            var carGo = GameObject.Find("SafeDriver_Exterior_v1");
            var vehicle = carGo != null ? carGo.GetComponent<VehicleController>() : null;

            // Agregar GearShifter component
            var shifter = clone.AddComponent<GearShifter>();
            var soShifter = new SerializedObject(shifter);
            soShifter.FindProperty("vehicle").objectReferenceValue = vehicle;
            soShifter.FindProperty("pivot").objectReferenceValue = clone.transform;
            soShifter.ApplyModifiedProperties();

            // Agregar GrabbableHandFollower con refs copiadas del SteeringWheelHandFollower del wheel
            var follower = clone.AddComponent<GrabbableHandFollower>();
            var wheelFollower = wheel.GetComponent<SafeDriver.VR.SteeringWheelHandFollower>();
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

            // Forzar useExternalShifter=true en el VehicleController
            if (vehicle != null)
            {
                var soV = new SerializedObject(vehicle);
                var useExt = soV.FindProperty("useExternalShifter");
                if (useExt != null) { useExt.boolValue = true; soV.ApplyModifiedProperties(); }
            }

            Selection.activeGameObject = clone;
            EditorUtility.SetDirty(clone);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(clone.scene);
            Debug.Log("GearShifter clonado del SteeringWheel. Setup identico al volante en pos " + clone.transform.localPosition, clone);
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
