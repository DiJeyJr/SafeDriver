using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Missions;

namespace SafeDriver.UI
{
    /// <summary>
    /// Lista diegetica de objetivos del nivel, montada sobre un Canvas WorldSpace en el
    /// tablero del auto. Lee las misiones del MissionManager y se actualiza con sus eventos.
    ///
    /// A diferencia de la version anterior (objetivos fijos en Inspector), ahora las filas
    /// se generan en runtime: una por mision activa, cuando MissionManager dispara
    /// MissionsLoaded. Cada fila es un TextMeshProUGUI con check al inicio (caja / tilde /
    /// cruz) que se pinta segun el estado de la mision.
    /// </summary>
    public class ObjectivesController : MonoBehaviour
    {
        [Header("Estilo")]
        [SerializeField] private float titleFontSize = 22f;
        [SerializeField] private float itemFontSize = 18f;
        [SerializeField] private Color pendingColor = Color.white;
        [SerializeField] private Color completedColor = new Color(0.55f, 0.85f, 0.55f);
        [SerializeField] private Color failedColor = new Color(0.90f, 0.45f, 0.45f);

        [Tooltip("Texto del titulo. Vacio = sin titulo.")]
        [SerializeField] private string titleText = "OBJETIVOS";

        [Tooltip("Padding interno del panel (left, right, top, bottom) en unidades de canvas.")]
        [SerializeField] private int paddingLeft = 20;
        [SerializeField] private int paddingRight = 20;
        [SerializeField] private int paddingTop = 20;
        [SerializeField] private int paddingBottom = 20;

        [Tooltip("Espaciado entre items.")]
        [SerializeField] private float itemSpacing = 8f;

        [Tooltip("Fuente para titulo e items. Si queda vacia usa la default de TMP.")]
        [SerializeField] private TMP_FontAsset font;

        private const string TitleName = "Title";
        private const string ItemPrefix = "Item_";

        private readonly Dictionary<MissionRuntime, TextMeshProUGUI> rows = new();

        void OnEnable()
        {
            if (!Application.isPlaying) return;

            var mm = MissionManager.Instance;
            if (mm != null)
            {
                mm.MissionsLoaded += Rebuild;
                mm.MissionChanged += UpdateRow;
                if (mm.ActiveMissions.Count > 0) Rebuild();
            }
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;

            var mm = MissionManager.Instance;
            if (mm != null)
            {
                mm.MissionsLoaded -= Rebuild;
                mm.MissionChanged -= UpdateRow;
            }
        }

        // ============================================================
        //   Construccion / actualizacion de filas
        // ============================================================

        private void Rebuild()
        {
            ClearChildren();
            rows.Clear();
            EnsureLayoutComponents();

            if (!string.IsNullOrEmpty(titleText))
            {
                var title = CreateText(TitleName, titleText, titleFontSize, FontStyles.Bold);
                title.color = pendingColor;
            }

            var mm = MissionManager.Instance;
            if (mm == null) return;

            int i = 0;
            foreach (var mission in mm.ActiveMissions)
            {
                var tmp = CreateText(ItemPrefix + i, string.Empty, itemFontSize, FontStyles.Normal);
                rows[mission] = tmp;
                Paint(mission, tmp);
                i++;
            }
        }

        private void UpdateRow(MissionRuntime mission)
        {
            if (rows.TryGetValue(mission, out var tmp))
                Paint(mission, tmp);
        }

        private void Paint(MissionRuntime mission, TextMeshProUGUI tmp)
        {
            string check = mission.Status switch
            {
                MissionStatus.Completed => "<b>✔</b>",  // ✔
                MissionStatus.Failed    => "<b>✘</b>",  // ✘
                _                       => "□",          // □
            };

            string progress = mission.ProgressLabel;
            string body = string.IsNullOrEmpty(progress)
                ? mission.Definition.title
                : $"{mission.Definition.title}  ({progress})";

            tmp.text = check + "  " + body;
            tmp.color = mission.Status switch
            {
                MissionStatus.Completed => completedColor,
                MissionStatus.Failed    => failedColor,
                _                       => pendingColor,
            };
            tmp.fontStyle = mission.Status == MissionStatus.Completed
                ? FontStyles.Strikethrough
                : FontStyles.Normal;
        }

        // ============================================================
        //   Helpers de layout / creacion de texto
        // ============================================================

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }

        private void EnsureLayoutComponents()
        {
            var layout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            layout.spacing = itemSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private TextMeshProUGUI CreateText(string objectName, string content, float size, FontStyles style)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = content;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }
    }
}
