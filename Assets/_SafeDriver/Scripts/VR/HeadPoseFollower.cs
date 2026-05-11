using UnityEngine;
using UnityEngine.XR;

namespace SafeDriver.VR
{
    /// <summary>
    /// Aplica la pose del HMD (XRNode.Head) al transform local en LateUpdate.
    /// Workaround para cuando el rig padre (OVRCameraRig u otro) no propaga las
    /// poses al CenterEyeAnchor — leemos directamente del XR device estandar.
    ///
    /// Pegar al CenterEyeAnchor (o equivalente) del rig de camara.
    /// </summary>
    public class HeadPoseFollower : MonoBehaviour
    {
        [Tooltip("Aplicar posicion del HMD.")]
        [SerializeField] private bool applyPosition = true;

        [Tooltip("Aplicar rotacion del HMD.")]
        [SerializeField] private bool applyRotation = true;

        void LateUpdate()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!head.isValid) return;

            if (applyPosition && head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                transform.localPosition = pos;

            if (applyRotation && head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                transform.localRotation = rot;
        }
    }
}
