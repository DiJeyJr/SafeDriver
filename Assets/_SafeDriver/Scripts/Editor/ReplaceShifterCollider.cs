using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Reemplaza el setup del GearShifter para usar EXACTAMENTE el mismo grab que el volante:
    ///   - BoxCollider + GrabProximityFilter → SphereCollider + WheelGrabProximityFilter
    /// Mantiene el resto (Grabbable, OneGrabRotateTransformer, Hand{Grab}Interactable, GearShifter,
    /// GrabbableHandFollower). El SteeringWheel funciona con SphereCollider + WheelGrabProximityFilter,
    /// asi que clonar exactamente ese patron garantiza que el grab funcione.
    /// </summary>
    public static class ReplaceShifterCollider
    {
        [MenuItem("SafeDriver/Replace Shifter Collider (Sphere + WheelFilter)")]
        public static void Run()
        {
            var shifter = GameObject.Find("GearShifter");
            if (shifter == null) { Debug.LogError("No se encontro GearShifter."); return; }

            // Quitar componentes viejos
            var oldBox = shifter.GetComponent<BoxCollider>();
            if (oldBox != null) Object.DestroyImmediate(oldBox);

            var oldFilter = shifter.GetComponent<GrabProximityFilter>();
            if (oldFilter != null) Object.DestroyImmediate(oldFilter);

            // SphereCollider — radius generoso para que la mano alcance facil; el filter limita
            // donde realmente se permite el grab.
            var sphere = shifter.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0f, 0.14f, 0f);
            sphere.radius = 0.10f;
            sphere.isTrigger = false;

            // WheelGrabProximityFilter — script existente que funciona para el volante.
            // Funciona generico para cualquier SphereCollider.
            var filter = shifter.AddComponent<WheelGrabProximityFilter>();
            var soFilter = new SerializedObject(filter);
            soFilter.FindProperty("hitbox").objectReferenceValue = sphere;
            soFilter.FindProperty("radiusMultiplier").floatValue = 1.5f; // un poco mas generoso
            soFilter.ApplyModifiedProperties();

            // Re-wirear _interactorFilters del GrabInteractable
            var grabInteractable = shifter.GetComponent<GrabInteractable>();
            if (grabInteractable != null)
            {
                var soGI = new SerializedObject(grabInteractable);
                var giFilters = soGI.FindProperty("_interactorFilters");
                giFilters.arraySize = 1;
                giFilters.GetArrayElementAtIndex(0).objectReferenceValue = filter;
                soGI.ApplyModifiedProperties();
            }

            // Re-wirear _interactorFilters del HandGrabInteractable + reglas igual al wheel
            var handGrabInteractable = shifter.GetComponent<HandGrabInteractable>();
            if (handGrabInteractable != null)
            {
                var soHGI = new SerializedObject(handGrabInteractable);
                var hgiFilters = soHGI.FindProperty("_interactorFilters");
                hgiFilters.arraySize = 1;
                hgiFilters.GetArrayElementAtIndex(0).objectReferenceValue = filter;

                // Forzar reglas IGUAL al wheel
                var pinchRules = soHGI.FindProperty("_pinchGrabRules");
                pinchRules.FindPropertyRelative("SelectsWithOptionals").boolValue = false;
                var palmRules = soHGI.FindProperty("_palmGrabRules");
                palmRules.FindPropertyRelative("SelectsWithOptionals").boolValue = true;
                soHGI.FindProperty("_handAligment").intValue = 0;

                soHGI.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(shifter);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shifter.scene);
            Selection.activeGameObject = shifter;
            Debug.Log("GearShifter ahora usa SphereCollider + WheelGrabProximityFilter, igual al volante.", shifter);
        }
    }
}
