using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Instala los Building Blocks de Meta XR All-in-One requeridos para el menu principal:
    /// Camera Rig, Controller Tracking, Hand Tracking, Interactions Rig.
    ///
    /// La API de Building Blocks (BlockData.AddToProject) es internal, asi que usamos reflection.
    /// Despues de instalar, posiciona el [BuildingBlock] Camera Rig en la esquina SO mirando al cruce.
    /// </summary>
    public static class MainMenuBuildingBlocks
    {
        private const string CameraRigId      = "e47682b9-c270-40b1-b16d-90b627a5ce1b";
        private const string CtrlTrackingId   = "5817f7c0-f2a5-45f9-a5ca-64264e0166e8";
        private const string HandTrackingId   = "8b26b298-7bf4-490e-b245-a039c0184303";
        private const string InteractionsRigId = "81f55626-5fad-45e9-a1df-184f330da7ba";
        private const string PointableItemId  = "76a013d1-7c16-4a60-9c1b-79b3691c0438";
        private const string PokeableItemId   = "5838e892-31a4-4f65-a415-dc108aefa14c";

        private const string CanvasName = "MainMenuCanvas";

        private static readonly Vector3 PlayerSpawnPos = new Vector3(-13f, 0f, -13f);
        private static readonly Vector3 PlayerSpawnEuler = new Vector3(0f, 45f, 0f);

        [MenuItem("SafeDriver/Setup Main Menu Building Blocks")]
        public static async void Run()
        {
            try
            {
                await Install(CameraRigId);
                await Install(InteractionsRigId);
                await Install(CtrlTrackingId);
                await Install(HandTrackingId);

                PositionCameraRig();

                await InstallOnCanvas(PointableItemId, CanvasName);
                // NOTA: PokeableItemId NO sirve para Canvas UI — crea un boton fisico 3D separado,
                // no convierte el canvas en pokeable. Para Poke sobre Canvas hay que agregar
                // PokeInteractable + ClippedPlaneSurface manualmente. Por ahora solo Ray.
                // await InstallOnCanvas(PokeableItemId, CanvasName);

                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Building Blocks instalados y rig posicionado.");
            }
            catch (Exception e)
            {
                Debug.LogError("Fallo instalando Building Blocks: " + e);
            }
        }

        private static async Task Install(string blockId)
        {
            var utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null) { Debug.LogError("No se encontro Meta.XR.BuildingBlocks.Editor.Utils"); return; }

            var getBlockData = utilsType.GetMethod("GetBlockData", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var blockData = getBlockData.Invoke(null, new object[] { blockId }) as ScriptableObject;
            if (blockData == null) { Debug.LogError("No se encontro BlockData con id " + blockId); return; }

            var isInstalledProp = blockData.GetType().GetProperty("IsSingletonAndAlreadyPresent", BindingFlags.Instance | BindingFlags.NonPublic);
            if (isInstalledProp != null && (bool)isInstalledProp.GetValue(blockData)) return;

            var addToProject = blockData.GetType().GetMethod("AddToProject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (addToProject == null) { Debug.LogError("No se encontro metodo AddToProject en " + blockData.GetType().Name); return; }

            var task = (Task)addToProject.Invoke(blockData, new object[] { null, null });
            await task;
        }

        private static async Task InstallOnCanvas(string blockId, string canvasName)
        {
            var canvasGo = GameObject.Find(canvasName);
            if (canvasGo == null) { Debug.LogWarning("No se encontro canvas " + canvasName); return; }

            var utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            var getBlockData = utilsType.GetMethod("GetBlockData", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var blockData = getBlockData.Invoke(null, new object[] { blockId }) as ScriptableObject;
            if (blockData == null) { Debug.LogError("No se encontro BlockData con id " + blockId); return; }

            var addToProject = blockData.GetType().GetMethod("AddToProject", BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task)addToProject.Invoke(blockData, new object[] { canvasGo, null });
            await task;
        }

        private static void PositionCameraRig()
        {
            var go = GameObject.Find("[BuildingBlock] Camera Rig");
            if (go == null) { Debug.LogWarning("No se encontro [BuildingBlock] Camera Rig para posicionar."); return; }

            Undo.RecordObject(go.transform, "Position Camera Rig");
            go.transform.position = PlayerSpawnPos;
            go.transform.eulerAngles = PlayerSpawnEuler;
        }
    }
}
