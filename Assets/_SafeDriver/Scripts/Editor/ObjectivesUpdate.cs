using UnityEditor;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Resetea la lista de objetivos del ObjectivesController de la escena activa al set actualizado
    /// (incluye Pasar el semaforo en verde y Cruzar paso peatonal sin peatones, y reconstruye la UI).
    /// </summary>
    public static class ObjectivesUpdate
    {
        [MenuItem("SafeDriver/Reset Objectives to Default")]
        public static void Run()
        {
            var oc = Object.FindFirstObjectByType<ObjectivesController>();
            if (oc == null) { Debug.LogError("No se encontro ObjectivesController en la escena."); return; }

            var so = new SerializedObject(oc);
            var arr = so.FindProperty("objectives");
            arr.arraySize = 0;
            arr.arraySize = 6;

            SetSpec(arr.GetArrayElementAtIndex(0), "Detenerse en la senal PARE",        ActionType.StoppedAtPareSign,        1);
            SetSpec(arr.GetArrayElementAtIndex(1), "Pasar el semaforo en verde",        ActionType.PassedGreenLight,         1);
            SetSpec(arr.GetArrayElementAtIndex(2), "Detenerse en semaforo rojo",        ActionType.StoppedAtRedLight,        1);
            SetSpec(arr.GetArrayElementAtIndex(3), "Ceder paso a peatones",             ActionType.YieldedToPedestrian,      1);
            SetSpec(arr.GetArrayElementAtIndex(4), "Cruzar senda sin peatones",         ActionType.PedestrianNotPresent,     1);
            SetSpec(arr.GetArrayElementAtIndex(5), "Chequear espejos al girar",         ActionType.CheckedMirrorsBeforeTurn, 2);

            so.ApplyModifiedProperties();
            oc.BuildUI();
            EditorUtility.SetDirty(oc);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(oc.gameObject.scene);
            Debug.Log("Objetivos reseteados a default (6 items) y UI reconstruida.");
        }

        private static void SetSpec(SerializedProperty elem, string label, ActionType action, int count)
        {
            elem.FindPropertyRelative("label").stringValue = label;
            elem.FindPropertyRelative("triggerAction").enumValueIndex = (int)action;
            elem.FindPropertyRelative("requiredCount").intValue = count;
        }
    }
}
