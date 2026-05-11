using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;
using Meta.XR.BuildingBlocks;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea el menu de pausa en la escena activa:
    ///   - GameObject `_PauseMenu` con `PauseMenuController`
    ///   - Child `PauseMenuCanvas` (WorldSpace, GraphicRaycaster, PointableCanvas)
    ///   - Background panel + Title + 3 botones (Continuar / Reiniciar / Menu principal)
    ///   - Wirea Button.onClick via UnityEventTools (UnityEvent persistente)
    ///   - Instala el BB "Pointable Item" sobre el canvas (Ray Interaction)
    ///   - Wirea un ButtonClickAction en el ControllerButtonsMapper para que el boton secundario
    ///     (B en mano derecha, Y en mano izquierda) abra/cierre el menu via TogglePause()
    ///
    /// El menu arranca oculto y se posiciona frente a la cabeza cuando se abre.
    /// </summary>
    public static class CreatePauseMenu
    {
        private const string RootName   = "_PauseMenu";
        private const string CanvasName = "PauseMenuCanvas";
        private const string PointableItemId = "76a013d1-7c16-4a60-9c1b-79b3691c0438";

        [MenuItem("SafeDriver/Create Pause Menu")]
        public static async void Run()
        {
            try
            {
                var existing = GameObject.Find(RootName);
                if (existing != null)
                {
                    Selection.activeGameObject = existing;
                    Debug.LogWarning(RootName + " ya existe; seleccionando.", existing);
                    return;
                }

                var centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye == null) { Debug.LogError("No se encontro CenterEyeAnchor."); return; }
                var centerEyeCam = centerEye.GetComponent<Camera>();

                var root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create Pause Menu");
                var controller = root.AddComponent<PauseMenuController>();

                var canvasGo = BuildCanvas(root.transform, centerEyeCam);

                BuildBackground(canvasGo.transform);
                BuildTitle(canvasGo.transform);
                var continueBtn = BuildButton(canvasGo.transform, "ContinueButton",  "Continuar",       y:  90f, bgColor: new Color(0.18f, 0.55f, 0.28f));
                var restartBtn  = BuildButton(canvasGo.transform, "RestartButton",   "Reiniciar nivel", y:  20f, bgColor: new Color(0.30f, 0.40f, 0.65f));
                var menuBtn     = BuildButton(canvasGo.transform, "MainMenuButton",  "Menu principal",  y: -50f, bgColor: new Color(0.55f, 0.30f, 0.30f));

                WireOnClick(continueBtn, controller, nameof(PauseMenuController.Resume));
                WireOnClick(restartBtn,  controller, nameof(PauseMenuController.RestartLevel));
                WireOnClick(menuBtn,     controller, nameof(PauseMenuController.GoToMainMenu));

                // Asignar refs en el controller. IMPORTANTE: menuRoot apunta al canvas (child),
                // no al root, porque el root tiene el controller y no queremos desactivarlo.
                var soController = new SerializedObject(controller);
                soController.FindProperty("menuRoot").objectReferenceValue = canvasGo;
                soController.FindProperty("head").objectReferenceValue = centerEye.transform;
                soController.ApplyModifiedProperties();

                // Instalar BB Pointable Item sobre el canvas para que el ray detecte hits
                await InstallBlockOnCanvas(PointableItemId, canvasGo);

                // Asegurar EventSystem con PointableCanvasModule
                EnsureEventSystem();

                // Wirear el toggle al boton secundario del controller
                WireToggleOnControllerMapper(controller);

                // El menu arranca oculto
                root.SetActive(true);
                // El controller en Awake desactiva el canvas, no toda la jerarquia. Mantenemos root activo.

                EditorUtility.SetDirty(root);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
                Selection.activeGameObject = root;
                Debug.Log("[PauseMenu] creado y wireado.", root);
            }
            catch (Exception e)
            {
                Debug.LogError("Fallo creando Pause Menu: " + e);
            }
        }

        // ============================================================
        //   Canvas + UI
        // ============================================================

        private static GameObject BuildCanvas(Transform parent, Camera centerEyeCam)
        {
            var go = new GameObject(CanvasName, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (centerEyeCam != null)
            {
                // En el Inspector el campo serializado es m_Camera; asignamos via SerializedObject
                var so = new SerializedObject(canvas);
                so.FindProperty("m_Camera").objectReferenceValue = centerEyeCam;
                so.ApplyModifiedProperties();
            }

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 320f);
            rt.localScale = Vector3.one * 0.001f;

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<Oculus.Interaction.PointableCanvas>();
            return go;
        }

        private static void BuildBackground(Transform parent)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.10f, 0.92f);
        }

        private static void BuildTitle(Transform parent)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(360f, 60f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "PAUSA";
            tmp.fontSize = 42f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static Button BuildButton(Transform parent, string name, string label, float y, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(320f, 56f);

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.15f);
            btn.colors = colors;
            btn.targetGraphic = img;

            // Label
            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(go.transform, worldPositionStays: false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static void WireOnClick(Button btn, PauseMenuController target, string method)
        {
            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(btn.onClick, i);

            var call = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), target, method);
            UnityEventTools.AddPersistentListener(btn.onClick, call);
        }

        // ============================================================
        //   EventSystem
        // ============================================================

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<Oculus.Interaction.PointableCanvasModule>();
        }

        // ============================================================
        //   Building Block install
        // ============================================================

        private static async Task InstallBlockOnCanvas(string blockId, GameObject canvasGo)
        {
            var utilsType = Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null) { Debug.LogWarning("Meta.XR.BuildingBlocks.Editor.Utils no encontrado; skip Pointable Item install."); return; }

            var getBlockData = utilsType.GetMethod("GetBlockData", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            var blockData = getBlockData?.Invoke(null, new object[] { blockId }) as ScriptableObject;
            if (blockData == null) { Debug.LogWarning("BlockData no encontrado: " + blockId); return; }

            var addToProject = blockData.GetType().GetMethod("AddToProject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (addToProject == null) { Debug.LogWarning("Metodo AddToProject no encontrado en BlockData."); return; }

            var task = (Task)addToProject.Invoke(blockData, new object[] { canvasGo, null });
            await task;
        }

        // ============================================================
        //   Wirear el ControllerButtonsMapper para abrir/cerrar el menu
        // ============================================================

        private static void WireToggleOnControllerMapper(PauseMenuController controller)
        {
            var mapper = UnityEngine.Object.FindFirstObjectByType<ControllerButtonsMapper>();
            if (mapper == null)
            {
                Debug.LogWarning("ControllerButtonsMapper no encontrado en la escena; el toggle del menu por boton no quedo wireado.");
                return;
            }

            var so = new SerializedObject(mapper);
            var actions = so.FindProperty("_buttonClickActions");

            // Buscar si ya hay una accion con titulo conocido para no duplicar
            const string actionTitle = "Toggle Pause Menu";
            for (int i = 0; i < actions.arraySize; i++)
            {
                var titleProp = actions.GetArrayElementAtIndex(i).FindPropertyRelative("Title");
                if (titleProp != null && titleProp.stringValue == actionTitle)
                {
                    Debug.Log("Toggle Pause Menu ya estaba mapeado.", mapper);
                    return;
                }
            }

            int newIdx = actions.arraySize;
            actions.arraySize = newIdx + 1;
            so.ApplyModifiedProperties();

            // OVRInput.Button.Two = boton secundario (B mano derecha, Y mano izquierda)
            // ButtonClickMode.OnButtonDown
            var elem = actions.GetArrayElementAtIndex(newIdx);
            elem.FindPropertyRelative("Title").stringValue = actionTitle;
            elem.FindPropertyRelative("Button").intValue = (int)OVRInput.Button.Two;
            elem.FindPropertyRelative("ButtonMode").enumValueIndex = (int)ControllerButtonsMapper.ButtonClickAction.ButtonClickMode.OnButtonDown;
            so.ApplyModifiedProperties();

            // Persist listener al Callback (UnityEvent)
            var callbackProp = elem.FindPropertyRelative("Callback");
            so.ApplyModifiedProperties();

            // Para agregar un PersistentListener, usar UnityEventTools sobre el UnityEvent real
            // (obtenido via reflection del field _buttonClickActions[idx].Callback).
            var mapperType = mapper.GetType();
            var fld = mapperType.GetField("_buttonClickActions", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = fld?.GetValue(mapper) as System.Collections.IList;
            if (list == null || newIdx >= list.Count) return;

            var actionBoxed = list[newIdx];
            var callbackField = actionBoxed.GetType().GetField("Callback");
            var callback = callbackField?.GetValue(actionBoxed) as UnityEvent;
            if (callback == null)
            {
                callback = new UnityEvent();
                callbackField?.SetValue(actionBoxed, callback);
                list[newIdx] = actionBoxed;
            }

            var call = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), controller, nameof(PauseMenuController.TogglePause));
            UnityEventTools.AddPersistentListener(callback, call);

            EditorUtility.SetDirty(mapper);
            Debug.Log("ControllerButtonsMapper: boton B/Y (OVRInput.Button.Two) wireado a PauseMenuController.TogglePause.", mapper);
        }
    }
}
