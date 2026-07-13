using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Testigo de freno de mano estilo luz de giro: un bloquecito con la "P" en el
    /// DashboardCluster (junto a LIMITE/TIEMPO/PUNTOS) que se pone ROJO con el freno
    /// puesto y queda gris tenue al soltarlo. Re-entrante (borra versiones previas).
    /// </summary>
    public static class CreateHandbrakeLamp
    {
        [MenuItem("SafeDriver/UI/Agregar luz de freno de mano (escena abierta)")]
        public static void Run()
        {
            // Borrar cualquier version previa del testigo (incluida la vieja junto a la letra).
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name == "HandbrakeLamp") { Object.DestroyImmediate(t.gameObject); }

            var cluster = GameObject.Find("DashboardCluster");
            if (cluster == null) { Debug.LogError("[HandbrakeLamp] No se encontro DashboardCluster en la escena."); return; }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_SafeDriver/UI/RoundedRect.png");

            // Fila nueva en el cluster (el VerticalLayoutGroup la acomoda solo).
            var row = new GameObject("HandbrakeLamp", typeof(RectTransform));
            row.transform.SetParent(cluster.transform, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;

            // Bloquecito centrado con la "P" adentro.
            var block = new GameObject("Block", typeof(RectTransform), typeof(CanvasRenderer));
            block.transform.SetParent(row.transform, false);
            var brt = block.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(38f, 38f);
            var img = block.AddComponent<Image>();
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            img.raycastTarget = false;

            var pGo = new GameObject("P", typeof(RectTransform), typeof(CanvasRenderer));
            pGo.transform.SetParent(block.transform, false);
            var prt = pGo.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var tmp = pGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "P";
            tmp.fontSize = 26f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var lightComp = block.AddComponent<HandbrakeLight>();
            var so = new SerializedObject(lightComp);
            so.FindProperty("lamp").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[HandbrakeLamp] Testigo 'P' agregado al DashboardCluster.");
        }
    }
}
