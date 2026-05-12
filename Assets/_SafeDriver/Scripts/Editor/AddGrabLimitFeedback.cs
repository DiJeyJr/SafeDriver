using UnityEditor;
using UnityEngine;
using Oculus.Interaction;
using SafeDriver.VR;
using SafeDriver.Vehicle;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Wirea el sistema de vibracion de tope en el SteeringWheel y el GearShifter, y crea
    /// el CameraShake en el CenterEyeAnchor si no existe. Tiers configurados segun los
    /// requerimientos del proyecto.
    /// </summary>
    public static class AddGrabLimitFeedback
    {
        [MenuItem("SafeDriver/Setup Grab Limit Feedback")]
        public static void Run()
        {
            EnsureCameraShake();

            // -------- Volante --------
            var wheel = GameObject.Find("SteeringWheel");
            if (wheel != null)
            {
                var grabbable = wheel.GetComponent<Grabbable>();
                var transformer = wheel.GetComponent<OneGrabRotateTransformer>();
                if (grabbable != null && transformer != null)
                {
                    var feedback = wheel.GetComponent<GrabLimitFeedback>();
                    if (feedback == null) feedback = Undo.AddComponent<GrabLimitFeedback>(wheel);

                    // Tiers x2 sobre la version original — rangos mas largos y release mas tarde.
                    // El primer tier empieza en 1° (no 0°) para que el pulso unico se dispare
                    // recien al CRUZAR el tope, no al simplemente agarrar el volante.
                    var tiers = new[]
                    {
                        new GrabLimitFeedback.Tier { fromDeg = 1f,   toDeg = 20f,  amplitude = 0.8f, duration = 0.08f, intervalSeconds = 0f,    releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 20f,  toDeg = 50f,  amplitude = 0.3f, duration = 0.05f, intervalSeconds = 0.40f, releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 50f,  toDeg = 80f,  amplitude = 0.5f, duration = 0.05f, intervalSeconds = 0.25f, releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 80f,  toDeg = 110f, amplitude = 0.8f, duration = 0.05f, intervalSeconds = 0.15f, releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 110f, toDeg = Mathf.Infinity, amplitude = 1.0f, duration = 0.4f, intervalSeconds = 0f, releaseOnEnter = true },
                    };
                    AssignTiersAndRefs(feedback, grabbable, transformer, tiers, shakeMagnitude: 0.04f, shakeDuration: 0.3f);
                    EditorUtility.SetDirty(wheel);
                    Debug.Log("[GrabLimitFeedback] SteeringWheel wireado.", wheel);
                }
                else Debug.LogWarning("SteeringWheel sin Grabbable/OneGrabRotateTransformer.", wheel);
            }
            else Debug.LogWarning("No se encontro SteeringWheel.");

            // -------- Palanca --------
            var shifter = GameObject.Find("GearShifter");
            if (shifter != null)
            {
                var grabbable = shifter.GetComponent<Grabbable>();
                var transformer = shifter.GetComponent<OneGrabRotateTransformer>();
                if (grabbable != null && transformer != null)
                {
                    // Extender constraints a +-60° para abarcar zonas N/D/R completas
                    var soT = new SerializedObject(transformer);
                    var constraints = soT.FindProperty("_constraints");
                    constraints.FindPropertyRelative("MinAngle").FindPropertyRelative("Value").floatValue = -60f;
                    constraints.FindPropertyRelative("MaxAngle").FindPropertyRelative("Value").floatValue =  60f;
                    soT.ApplyModifiedProperties();

                    var feedback = shifter.GetComponent<GrabLimitFeedback>();
                    if (feedback == null) feedback = Undo.AddComponent<GrabLimitFeedback>(shifter);

                    // Tiers x1.5 sobre la version original; primer tier desde 1° para que el
                    // pulso unico aparezca al cruzar el tope, no al agarrar la palanca.
                    var tiers = new[]
                    {
                        new GrabLimitFeedback.Tier { fromDeg = 1f,  toDeg = 15f, amplitude = 0.8f, duration = 0.08f, intervalSeconds = 0f,    releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 15f, toDeg = 45f, amplitude = 0.5f, duration = 0.05f, intervalSeconds = 0.20f, releaseOnEnter = false },
                        new GrabLimitFeedback.Tier { fromDeg = 45f, toDeg = Mathf.Infinity, amplitude = 1.0f, duration = 0.3f, intervalSeconds = 0f, releaseOnEnter = true },
                    };
                    AssignTiersAndRefs(feedback, grabbable, transformer, tiers, shakeMagnitude: 0.03f, shakeDuration: 0.3f);

                    // Tunear el GearShifter para zonas N/D/R coherentes con +-60° de rango
                    var shifterComp = shifter.GetComponent<GearShifter>();
                    if (shifterComp != null)
                    {
                        var soS = new SerializedObject(shifterComp);
                        soS.FindProperty("neutralHalfRange").floatValue = 20f;
                        soS.FindProperty("snapAngle").floatValue = 40f;
                        soS.FindProperty("snapOnRelease").boolValue = true;
                        soS.FindProperty("lockWhenMoving").boolValue = true;
                        if (soS.FindProperty("grabbable").objectReferenceValue == null)
                            soS.FindProperty("grabbable").objectReferenceValue = grabbable;
                        soS.ApplyModifiedProperties();
                    }
                    EditorUtility.SetDirty(shifter);
                    Debug.Log("[GrabLimitFeedback] GearShifter wireado con +-60° y zonas N/D/R.", shifter);
                }
                else Debug.LogWarning("GearShifter sin Grabbable/OneGrabRotateTransformer.", shifter);
            }
            else Debug.LogWarning("No se encontro GearShifter.");

            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(active);
        }

        private static void EnsureCameraShake()
        {
            var existing = Object.FindFirstObjectByType<CameraShake>();
            if (existing != null) return;

            var center = GameObject.Find("CenterEyeAnchor");
            if (center == null) { Debug.LogWarning("No se encontro CenterEyeAnchor; CameraShake no se creo."); return; }

            Undo.AddComponent<CameraShake>(center);
            Debug.Log("[CameraShake] agregado al CenterEyeAnchor.", center);
        }

        private static void AssignTiersAndRefs(
            GrabLimitFeedback feedback,
            Grabbable grabbable,
            OneGrabRotateTransformer transformer,
            GrabLimitFeedback.Tier[] tiers,
            float shakeMagnitude,
            float shakeDuration)
        {
            var so = new SerializedObject(feedback);
            so.FindProperty("grabbable").objectReferenceValue = grabbable;
            so.FindProperty("transformer").objectReferenceValue = transformer;
            so.FindProperty("releaseShakeMagnitude").floatValue = shakeMagnitude;
            so.FindProperty("releaseShakeDuration").floatValue = shakeDuration;

            var arr = so.FindProperty("tiers");
            arr.arraySize = tiers.Length;
            for (int i = 0; i < tiers.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("fromDeg").floatValue = tiers[i].fromDeg;
                e.FindPropertyRelative("toDeg").floatValue = tiers[i].toDeg;
                e.FindPropertyRelative("amplitude").floatValue = tiers[i].amplitude;
                e.FindPropertyRelative("duration").floatValue = tiers[i].duration;
                e.FindPropertyRelative("intervalSeconds").floatValue = tiers[i].intervalSeconds;
                e.FindPropertyRelative("releaseOnEnter").boolValue = tiers[i].releaseOnEnter;
            }
            so.ApplyModifiedProperties();
        }
    }
}
