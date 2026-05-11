using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace SafeDriver.VR
{
    /// <summary>
    /// Loguea el estado del XR runtime al iniciar — para debug cuando el head tracking no funciona.
    /// Dropealo en cualquier GameObject de la escena que estes testeando.
    /// </summary>
    public class XRDiagnostic : MonoBehaviour
    {
        void Start()
        {
            DumpStatus("Start");
            Invoke(nameof(DumpAfter1s), 1f);
        }

        void DumpAfter1s() => DumpStatus("Start+1s");

        private void DumpStatus(string label)
        {
            Debug.Log("=== XR Diagnostic [" + label + "] ===");
            Debug.Log("XRSettings.enabled = " + XRSettings.enabled);
            Debug.Log("XRSettings.loadedDeviceName = '" + XRSettings.loadedDeviceName + "'");
            Debug.Log("XRSettings.isDeviceActive = " + XRSettings.isDeviceActive);
            Debug.Log("XRSettings.eyeTextureWidth = " + XRSettings.eyeTextureWidth + " x " + XRSettings.eyeTextureHeight);
            Debug.Log("XRSettings.stereoRenderingMode = " + XRSettings.stereoRenderingMode);

            var settings = XRGeneralSettings.Instance;
            if (settings == null) { Debug.LogWarning("XRGeneralSettings.Instance is null"); return; }
            var manager = settings.Manager;
            if (manager == null) { Debug.LogWarning("XRManager is null"); return; }

            Debug.Log("XRManager.isInitializationComplete = " + manager.isInitializationComplete);
            Debug.Log("XRManager.activeLoader = " + (manager.activeLoader != null ? manager.activeLoader.name : "<null>"));
            Debug.Log("XRManager.activeLoaders count = " + manager.activeLoaders.Count);
            for (int i = 0; i < manager.activeLoaders.Count; i++)
            {
                Debug.Log("  loader[" + i + "] = " + manager.activeLoaders[i].name);
            }

            // Check head subsystem
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            Debug.Log("Head device: name='" + headDevice.name + "' isValid=" + headDevice.isValid + " characteristics=" + headDevice.characteristics);

            if (headDevice.isValid)
            {
                if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                    Debug.Log("Head position from XR: " + pos);
                else
                    Debug.LogWarning("Head device has no devicePosition feature.");

                if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                    Debug.Log("Head rotation from XR: " + rot.eulerAngles);
                else
                    Debug.LogWarning("Head device has no deviceRotation feature.");
            }
        }
    }
}
