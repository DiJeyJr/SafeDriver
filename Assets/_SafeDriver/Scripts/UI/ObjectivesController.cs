using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Lista diegetica de objetivos del nivel. Se monta sobre un Canvas WorldSpace en el tablero
    /// del auto y se actualiza escuchando EventBus.OnCorrectActionPerformed.
    ///
    /// La UI se construye una sola vez via Inspector (right-click → "Rebuild UI") o via menu
    /// SafeDriver/Rebuild Objectives UI. Los children resultantes se serializan con la escena.
    /// En play mode no se reconstruye — solo se re-bindean las filas existentes a la lista.
    ///
    /// Cada fila es un TextMeshProUGUI con un check ASCII al inicio (□/✔) que se pinta de verde
    /// con tachado al completarse. Si requiredCount &gt; 1 el texto incluye contador "label (1/3)".
    ///
    /// Los objetivos del mismo ActionType se completan en orden — un solo evento incrementa solo
    /// el primer objetivo no terminado de ese tipo.
    /// </summary>
    public class ObjectivesController : MonoBehaviour
    {
        [Serializable]
        public struct ObjectiveSpec
        {
            [Tooltip("Texto que se muestra al jugador.")]
            public string label;

            [Tooltip("Accion del EventBus que cuenta para completar este objetivo.")]
            public ActionType triggerAction;

            [Tooltip("Cuantas veces hay que hacer la accion para completar.")]
            [Min(1)] public int requiredCount;
        }

        [Header("Lista de objetivos")]
        [SerializeField]
        private ObjectiveSpec[] objectives = new ObjectiveSpec[]
        {
            new ObjectiveSpec { label = "Detenerse en la senal PARE",   triggerAction = ActionType.StoppedAtPareSign,        requiredCount = 1 },
            new ObjectiveSpec { label = "Respetar el semaforo en rojo", triggerAction = ActionType.StoppedAtRedLight,        requiredCount = 1 },
            new ObjectiveSpec { label = "Ceder paso a peatones",        triggerAction = ActionType.YieldedToPedestrian,      requiredCount = 1 },
            new ObjectiveSpec { label = "Chequear espejos al girar",    triggerAction = ActionType.CheckedMirrorsBeforeTurn, requiredCount = 2 },
        };

        [Header("Estilo")]
        [Tooltip("Tamanio de fuente del titulo del panel.")]
        [SerializeField] private float titleFontSize = 22f;

        [Tooltip("Tamanio de fuente de cada item.")]
        [SerializeField] private float itemFontSize = 18f;

        [Tooltip("Color de objetivo pendiente.")]
        [SerializeField] private Color pendingColor = Color.white;

        [Tooltip("Color de objetivo completado.")]
        [SerializeField] private Color completedColor = new Color(0.55f, 0.85f, 0.55f);

        [Tooltip("Texto del titulo. Vacio = sin titulo.")]
        [SerializeField] private string titleText = "OBJETIVOS";

        [Tooltip("Padding interno del panel (left, right, top, bottom) en unidades de canvas.")]
        [SerializeField] private int paddingLeft = 20;
        [SerializeField] private int paddingRight = 20;
        [SerializeField] private int paddingTop = 20;
        [SerializeField] private int paddingBottom = 20;

        [Tooltip("Espaciado entre items.")]
        [SerializeField] private float itemSpacing = 8f;

        // Naming convention para encontrar los children al runtime.
        private const string TitleName = "Title";
        private const string ItemPrefix = "Item_";

        private struct Row { public ObjectiveSpec spec; public TextMeshProUGUI text; public int progress; }
        private readonly List<Row> rows = new List<Row>();

        void OnEnable()
        {
            if (!Application.isPlaying) return;

            LinkRowsFromChildren();
            EventBus.OnCorrectActionPerformed += HandleAction;
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            EventBus.OnCorrectActionPerformed -= HandleAction;
        }

        private void HandleAction(ActionType type, int bonus)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.spec.triggerAction != type || r.progress >= r.spec.requiredCount) continue;

                r.progress++;
                rows[i] = r;
                UpdateRow(i);

                if (AllCompleted())
                {
                    Debug.Log("[Objectives] Todos los objetivos completados.", this);
                }
                return;
            }
        }

        /// <summary>Devuelve true si todos los objetivos llegaron a su requiredCount.</summary>
        public bool AllCompleted()
        {
            foreach (var r in rows)
                if (r.progress < r.spec.requiredCount) return false;
            return rows.Count > 0;
        }

        // ============================================================
        //   Construccion de la UI (editor-time, llamado a mano)
        // ============================================================

        /// <summary>
        /// (Editor-time) Borra los children y reconstruye titulo + items desde el array de objetivos.
        /// Llamar via right-click en Inspector o desde una utility de editor.
        /// </summary>
        [ContextMenu("Rebuild UI")]
        public void BuildUI()
        {
            ClearChildren();
            EnsureLayoutComponents();

            if (!string.IsNullOrEmpty(titleText))
            {
                var title = CreateText(TitleName, titleText, titleFontSize, FontStyles.Bold);
                title.color = pendingColor;
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                var spec = objectives[i];
                var text = CreateText(ItemPrefix + i, FormatLabel(spec, 0), itemFontSize, FontStyles.Normal);
                text.color = pendingColor;
            }
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
            rows.Clear();
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

        private TextMeshProUGUI CreateText(string name, string content, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }

        // ============================================================
        //   Runtime: relink a children ya serializados en escena
        // ============================================================

        private void LinkRowsFromChildren()
        {
            rows.Clear();
            for (int i = 0; i < objectives.Length; i++)
            {
                var child = transform.Find(ItemPrefix + i);
                if (child == null)
                {
                    Debug.LogWarning("[Objectives] No se encontro child '" + ItemPrefix + i + "'. Reconstruir UI desde Inspector.", this);
                    continue;
                }
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;

                rows.Add(new Row { spec = objectives[i], text = tmp, progress = 0 });
                UpdateRow(rows.Count - 1);
            }
        }

        private void UpdateRow(int i)
        {
            var r = rows[i];
            bool done = r.progress >= r.spec.requiredCount;
            r.text.text = FormatLabel(r.spec, r.progress);
            r.text.color = done ? completedColor : pendingColor;
            r.text.fontStyle = done ? FontStyles.Strikethrough : FontStyles.Normal;
        }

        private static string FormatLabel(ObjectiveSpec spec, int progress)
        {
            string check = progress >= spec.requiredCount ? "<b>✔</b>" : "□";
            string body = spec.requiredCount > 1
                ? string.Format("{0}  ({1}/{2})", spec.label, progress, spec.requiredCount)
                : spec.label;
            return check + "  " + body;
        }
    }
}
