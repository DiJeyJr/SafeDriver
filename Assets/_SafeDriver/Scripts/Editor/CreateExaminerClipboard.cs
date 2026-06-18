using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using SafeDriver.VR;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Convierte el panel de objetivos del tablero en una TABLILLA AGARRABLE estilo examinador.
    ///
    /// Clona el SteeringWheel (que tiene el stack de grab de Oculus ya correcto: Grabbable +
    /// GrabInteractable + HandGrabInteractable + filtro de proximidad), le quita lo especifico del
    /// volante, y cambia el OneGrabRotateTransformer (rotacion en el lugar) por un OneGrabFreeTransformer
    /// (movimiento libre 6DOF). Le agrega un board + clip visual, le reparenta la UI de objetivos y un
    /// ClipboardHolster que la devuelve sola a su lugar al soltarla.
    ///
    /// Es el MISMO patron probado de CloneWheelAsShifter (un grabbable compound mas, scopeado por su
    /// propio filtro: no rompe el grab del volante). Idempotente: re-ejecutar regenera la tablilla.
    /// </summary>
    public static class CreateExaminerClipboard
    {
        private const string DemoMatDir = "Assets/_SafeDriver/Materials/Demo/";

        [MenuItem("SafeDriver/VR/Crear Tablilla Agarrable (Objetivos)")]
        public static void Run()
        {
            var wheel = GameObject.Find("SteeringWheel");
            if (wheel == null) { Debug.LogError("[Tablilla] No se encontro SteeringWheel."); return; }
            var objectives = GameObject.Find("ObjectivesPanel");
            if (objectives == null) { Debug.LogError("[Tablilla] No se encontro ObjectivesPanel."); return; }

            var interior = objectives.transform.parent;
            Vector3 homeLocalPos = objectives.transform.localPosition;
            Quaternion homeLocalRot = objectives.transform.localRotation;

            // Limpiar tablilla previa (devolviendo el ObjectivesPanel al interior antes de borrarla).
            var prev = GameObject.Find("ExaminerClipboard");
            if (prev != null)
            {
                if (objectives.transform.IsChildOf(prev.transform))
                {
                    objectives.transform.SetParent(interior, false);
                    objectives.transform.localPosition = homeLocalPos;
                    objectives.transform.localRotation = homeLocalRot;
                }
                Object.DestroyImmediate(prev);
            }

            // 1. Clonar el volante en el interior, en la pose de reposo del panel.
            var clip = Object.Instantiate(wheel, interior);
            clip.name = "ExaminerClipboard";
            Undo.RegisterCreatedObjectUndo(clip, "Crear Tablilla Agarrable");
            clip.transform.localPosition = homeLocalPos;
            clip.transform.localRotation = homeLocalRot;
            clip.transform.localScale = Vector3.one;

            // 2. Quitar mesh y scripts especificos del volante.
            DestroyIfPresent(clip.GetComponent<MeshFilter>());
            DestroyIfPresent(clip.GetComponent<MeshRenderer>());
            DestroyIfPresent(clip.GetComponent<SteeringWheelController>());
            DestroyIfPresent(clip.GetComponent<SteeringWheelHandFollower>());
            DestroyIfPresent(clip.GetComponent<HandGrabRigidbodyTracker>());
            DestroyIfPresent(clip.GetComponent<GrabLimitFeedback>());

            // 3. Swap del transformer: rotate (en el lugar) -> free (6DOF).
            DestroyIfPresent(clip.GetComponent<OneGrabRotateTransformer>());
            var free = clip.AddComponent<OneGrabFreeTransformer>();
            var grab = clip.GetComponent<Grabbable>();
            SetRef(grab, "_oneGrabTransformer", free);

            // 4. Collider de agarre (cubre la tablilla) + re-apuntar el filtro de proximidad.
            var sphere = clip.GetComponent<SphereCollider>();
            if (sphere != null) { sphere.center = Vector3.zero; sphere.radius = 0.18f; }
            var filter = clip.GetComponent<WheelGrabProximityFilter>();
            if (filter != null && sphere != null)
            {
                var soF = new SerializedObject(filter);
                soF.FindProperty("hitbox").objectReferenceValue = sphere;
                soF.FindProperty("radiusMultiplier").floatValue = 1.6f;
                soF.ApplyModifiedProperties();
            }

            // 5. GrabbableHandFollower (mueve la mano visual con la tablilla), refs copiadas del volante.
            var follower = clip.AddComponent<GrabbableHandFollower>();
            var wheelFollower = wheel.GetComponent<SteeringWheelHandFollower>();
            if (wheelFollower != null)
            {
                var soWF = new SerializedObject(wheelFollower);
                var soF = new SerializedObject(follower);
                soF.FindProperty("target").objectReferenceValue = clip.transform;
                CopyArrayRefs(soWF, soF, "leftGrabInteractors");
                CopyArrayRefs(soWF, soF, "leftHandGrabInteractors");
                soF.FindProperty("leftHandVisual").objectReferenceValue = soWF.FindProperty("leftHandVisual").objectReferenceValue;
                CopyArrayRefs(soWF, soF, "rightGrabInteractors");
                CopyArrayRefs(soWF, soF, "rightHandGrabInteractors");
                soF.FindProperty("rightHandVisual").objectReferenceValue = soWF.FindProperty("rightHandVisual").objectReferenceValue;
                soF.ApplyModifiedProperties();
            }

            // 6. Visual: board (backing oscuro) + clip (barra clara arriba).
            var dark = LoadMat("DimDark");
            var light = LoadMat("SignWhite");
            // El canvas mira hacia +Z local (hacia el jugador); el board va detras (-Z).
            MakeVisual(PrimitiveType.Cube, clip.transform, "Board",   new Vector3(0f, 0f, 0.007f),  new Vector3(0.30f, 0.40f, 0.012f), dark);
            MakeVisual(PrimitiveType.Cube, clip.transform, "ClipBar", new Vector3(0f, 0.18f, -0.004f), new Vector3(0.12f, 0.03f, 0.02f), light);

            // 7. Reparentar la UI de objetivos sobre la tablilla (queda al frente del board).
            objectives.transform.SetParent(clip.transform, worldPositionStays: false);
            objectives.transform.localPosition = Vector3.zero;
            objectives.transform.localRotation = Quaternion.identity;
            // (su localScale 0.001 se mantiene)

            // 8. Holster: vuelve sola a la pose de reposo al soltar.
            var holster = clip.AddComponent<ClipboardHolster>();
            SetRef(holster, "grabbable", grab);

            Selection.activeGameObject = clip;
            EditorUtility.SetDirty(clip);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(clip.scene);
            Debug.Log("[Tablilla] Lista: ObjectivesPanel montado en una tablilla agarrable (grab libre + retorno). " +
                      "Validar en VR el agarre y afinar pose/tamaño. Guardar (Ctrl+S).", clip);
        }

        // ============================================================
        //   Helpers
        // ============================================================

        private static void DestroyIfPresent(Object o) { if (o != null) Object.DestroyImmediate(o); }

        private static GameObject MakeVisual(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
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

        private static Material LoadMat(string fileName)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DemoMatDir + fileName + ".mat");
            if (mat == null) Debug.LogWarning("[Tablilla] Material no encontrado: " + DemoMatDir + fileName + ".mat");
            return mat;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); }
            else Debug.LogWarning("[Tablilla] Campo '" + field + "' no encontrado en " + target.GetType().Name);
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
