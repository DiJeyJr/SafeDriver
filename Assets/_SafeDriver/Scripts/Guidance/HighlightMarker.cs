using System;
using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Esfera translucida pulsante que resalta un elemento (control de la cabina,
    /// cartel, peaton...). Envuelve al target y "respira" (se agranda/achica un poco
    /// mientras titila el alpha) para llamar la atencion sin distraer.
    ///
    /// Se apaga con Dismiss(): lo llama un DismissOnGrab (al agarrar el elemento), un
    /// HighlightOnApproach (al pasar de largo), el timer dismissAfterSeconds, o una
    /// HighlightSequence que ademas encadena el siguiente resalte.
    /// </summary>
    public class HighlightMarker : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Material de la esfera (unlit transparente; lo genera el menu SafeDriver/Guidance).")]
        [SerializeField] private Material haloMaterial;

        [Tooltip("Transform a resaltar. Vacio = este mismo GameObject.")]
        [SerializeField] private Transform target;

        [Tooltip("Diametro base de la esfera en metros (que envuelva al elemento).")]
        [SerializeField] private float size = 0.35f;

        [Tooltip("Offset local respecto al target (ej. levantarlo sobre un cartel).")]
        [SerializeField] private Vector3 offset = Vector3.zero;

        [Header("Comportamiento")]
        [Tooltip("Mostrar apenas arranca la escena. Desactivar si lo maneja una HighlightSequence.")]
        [SerializeField] private bool showOnStart = true;

        [Tooltip("Se apaga solo despues de estos segundos visibles. 0 = no se apaga por tiempo.")]
        [SerializeField] private float dismissAfterSeconds = 0f;

        /// <summary>Se dispara al apagarse (lo escucha la HighlightSequence para encadenar).</summary>
        public event Action Dismissed;

        /// <summary>True mientras el halo esta visible.</summary>
        public bool IsShowing { get; private set; }

        private Transform sphere;
        private MeshRenderer sphereRenderer;
        private MaterialPropertyBlock mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private Color baseColor = Color.white;
        private float shownAt;
        private bool dismissed;

        void Start()
        {
            if (showOnStart) Show();
        }

        /// <summary>Prende la esfera (si no fue ya descartada).</summary>
        public void Show()
        {
            if (dismissed) return;
            EnsureSphere();
            IsShowing = true;
            shownAt = Time.time;
            sphere.gameObject.SetActive(true);
        }

        /// <summary>Apaga el resalte definitivamente y avisa a quien escuche.</summary>
        public void Dismiss()
        {
            if (dismissed) return;
            dismissed = true;
            IsShowing = false;
            if (sphere != null) sphere.gameObject.SetActive(false);
            Dismissed?.Invoke();
        }

        void LateUpdate()
        {
            if (!IsShowing || sphere == null) return;

            if (dismissAfterSeconds > 0f && Time.time - shownAt >= dismissAfterSeconds)
            {
                Dismiss();
                return;
            }

            var t = target != null ? target : transform;
            sphere.position = t.position + t.TransformVector(offset);

            // "Respira": pulso suave de escala + titileo de alpha.
            float pulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f; // 0..1
            sphere.localScale = Vector3.one * size * (1f + 0.15f * pulse);
            if (sphereRenderer != null)
            {
                var c = baseColor;
                c.a = baseColor.a * (0.5f + 0.5f * pulse);
                mpb.SetColor(BaseColorId, c);
                sphereRenderer.SetPropertyBlock(mpb);
            }
        }

        private void EnsureSphere()
        {
            if (sphere != null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "_HighlightSphere";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(null, true); // suelta en el mundo: sigue al target por codigo
            sphere = go.transform;
            sphereRenderer = go.GetComponent<MeshRenderer>();
            if (haloMaterial != null)
            {
                sphereRenderer.sharedMaterial = haloMaterial;
                baseColor = haloMaterial.HasProperty(BaseColorId)
                    ? haloMaterial.GetColor(BaseColorId) : Color.white;
            }
            mpb = new MaterialPropertyBlock();
            go.SetActive(false);
        }

        void OnDestroy()
        {
            if (sphere != null) Destroy(sphere.gameObject);
        }
    }
}
