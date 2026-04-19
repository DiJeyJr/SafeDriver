using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// Rendereo manual de una camara para espejo retrovisor.
    ///
    /// La camara se deshabilita para que no rendere en el loop normal; llamamos a
    /// Render() cada renderEveryNFrames frames. A ~36 FPS efectivos (render cada 2 frames
    /// en un target de 72Hz) el trafico lejano se ve fluido y ahorramos ~50% del costo
    /// vs. render cada frame, critico en Quest.
    ///
    /// Setup esperado:
    ///   - Camera hijo con targetTexture = RT del espejo, targetEye = None (monoscopico).
    ///   - Culling mask acotado (excluir UI, manos VR, interior del auto, volante).
    ///   - Far clip ~40-60m, FOV ~35-45°.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class MirrorCamera : MonoBehaviour
    {
        [Tooltip("Camera a rendear. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private Camera mirrorCamera;

        [Tooltip("Rendereo cada cuantos frames. 1 = cada frame, 2 = mitad, 3 = un tercio.")]
        [Range(1, 4)]
        [SerializeField] private int renderEveryNFrames = 2;

        private int frameCounter;

        void Awake()
        {
            if (mirrorCamera == null) mirrorCamera = GetComponentInChildren<Camera>(true);
            if (mirrorCamera != null) mirrorCamera.enabled = false;
        }

        void LateUpdate()
        {
            if (mirrorCamera == null) return;

            frameCounter++;
            if (frameCounter < renderEveryNFrames) return;
            frameCounter = 0;

            mirrorCamera.Render();
        }
    }
}
