using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Instala el Eye Gaze BB de Meta XR + configura OVRManager para pedir permiso de eye
    /// tracking al startup. Solo funciona en Quest Pro (Quest 3 reporta EyeTrackingEnabled=false).
    ///
    /// Para que el GazeMirrorDetector dispare puntos, los espejos deben tener tag "Mirror" y
    /// un Collider. Este utility tambien hace ese setup si encuentra GameObjects llamados
    /// MirrorCenter / MirrorLeft / MirrorRight.
    /// </summary>
    public static class EyeGazeSetup
    {
        private const string EyeGazeId = "f92445a7-db19-452c-8d3d-96820f6c8972";

        [MenuItem("SafeDriver/Setup Eye Gaze")]
        public static async void Run()
        {
            try
            {
                EnsureMirrorTag();
                await InstallEyeGazeBB();
                ConfigureOVRManagerPermission();
                TagAndColliderizeMirrors();
                AddGazeMirrorDetector();

                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Eye Gaze setup completo. En Quest 3 va a quedar como codigo muerto (no hay HW eye tracking); en Quest Pro funciona.");
            }
            catch (Exception e)
            {
                Debug.LogError("Fallo Eye Gaze setup: " + e);
            }
        }

        private static async Task InstallEyeGazeBB()
        {
            var utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null) { Debug.LogError("No se encontro Meta.XR.BuildingBlocks.Editor.Utils"); return; }

            var getBlockData = utilsType.GetMethod("GetBlockData", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var blockData = getBlockData.Invoke(null, new object[] { EyeGazeId }) as ScriptableObject;
            if (blockData == null) { Debug.LogError("No se encontro EyeGaze BlockData"); return; }

            var isSingletonProp = blockData.GetType().GetProperty("IsSingletonAndAlreadyPresent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (isSingletonProp != null && (bool)isSingletonProp.GetValue(blockData))
            {
                Debug.Log("Eye Gaze BB ya estaba instalado, skip.");
                return;
            }

            var addToProject = blockData.GetType().GetMethod("AddToProject", BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task)addToProject.Invoke(blockData, new object[] { null, null });
            await task;
        }

        private static void ConfigureOVRManagerPermission()
        {
            var ovrManager = UnityEngine.Object.FindFirstObjectByType<OVRManager>();
            if (ovrManager == null) { Debug.LogWarning("No se encontro OVRManager"); return; }

            var so = new SerializedObject(ovrManager);
            var prop = so.FindProperty("requestEyeTrackingPermissionOnStartup");
            if (prop != null)
            {
                prop.boolValue = true;
                so.ApplyModifiedProperties();
            }
        }

        private static void EnsureMirrorTag()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Mirror") return;

            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Mirror";
            tagManager.ApplyModifiedProperties();
        }

        private static void TagAndColliderizeMirrors()
        {
            string[] names = { "MirrorCenter", "MirrorLeft", "MirrorRight" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                go.tag = "Mirror";
                if (go.GetComponent<Collider>() == null)
                {
                    var col = Undo.AddComponent<BoxCollider>(go);
                    col.isTrigger = true;
                }
            }
        }

        private static void AddGazeMirrorDetector()
        {
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye == null) { Debug.LogWarning("No se encontro CenterEyeAnchor"); return; }
            if (centerEye.GetComponent<SafeDriver.VR.GazeMirrorDetector>() != null) return;
            Undo.AddComponent<SafeDriver.VR.GazeMirrorDetector>(centerEye);
        }
    }
}
