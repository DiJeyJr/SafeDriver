using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Oculus.Interaction.GrabAPI;

namespace SafeDriver.VR
{
    /// <summary>
    /// Diagnostico exhaustivo del estado de grab del volante. Se ejecuta cada 1s y reporta:
    /// - Estado del GrabInteractable y HandGrabInteractable
    /// - Existencia y estado del InteractableTriggerBroadcaster
    /// - Posiciones de los interactors y del hitbox
    /// - HandGrabAPI: que dedos estan detectados como grabbing
    /// </summary>
    public class WheelGrabDiagnostic : MonoBehaviour
    {
        [SerializeField] private float logInterval = 1f;
        [SerializeField] private SphereCollider hitbox;

        private float lastLog;

        void Awake()
        {
            if (hitbox == null) hitbox = GetComponent<SphereCollider>();
        }

        void Update()
        {
            if (Time.time - lastLog < logInterval) return;
            lastLog = Time.time;

            DumpAll();
        }

        private void DumpAll()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== [WheelDiag] =====");

            // 1. Wheel interactables
            var grabInteractable = GetComponent<GrabInteractable>();
            var handGrabInteractable = GetComponent<HandGrabInteractable>();

            if (grabInteractable != null)
            {
                sb.AppendLine($" GrabInteractable: State={grabInteractable.State} " +
                    $"InteractorsCount={CountEnumerable(grabInteractable.InteractorViews)} " +
                    $"SelectingCount={CountEnumerable(grabInteractable.SelectingInteractorViews)} " +
                    $"Rigidbody={(grabInteractable.Rigidbody != null ? grabInteractable.Rigidbody.name : "null")}");
            }
            else sb.AppendLine(" GrabInteractable: NOT PRESENT");

            if (handGrabInteractable != null)
            {
                sb.AppendLine($" HandGrabInteractable: State={handGrabInteractable.State} " +
                    $"InteractorsCount={CountEnumerable(handGrabInteractable.InteractorViews)} " +
                    $"SelectingCount={CountEnumerable(handGrabInteractable.SelectingInteractorViews)} " +
                    $"Rigidbody={(handGrabInteractable.Rigidbody != null ? handGrabInteractable.Rigidbody.name : "null")} " +
                    $"SupportedTypes={handGrabInteractable.SupportedGrabTypes}");
            }
            else sb.AppendLine(" HandGrabInteractable: NOT PRESENT");

            // 2. InteractableTriggerBroadcasters in scene
            var broadcasters = FindObjectsByType<InteractableTriggerBroadcaster>(FindObjectsSortMode.None);
            sb.AppendLine($" Broadcasters in scene: {broadcasters.Length}");
            foreach (var b in broadcasters)
            {
                sb.AppendLine($"  - on '{b.gameObject.name}' (path: {GetPath(b.transform)})");
            }

            // 3. Wheel hitbox position vs controllers
            if (hitbox != null)
            {
                Vector3 wheelCenter = hitbox.transform.TransformPoint(hitbox.center);
                float wheelRadius = hitbox.radius * Mathf.Max(
                    hitbox.transform.lossyScale.x, hitbox.transform.lossyScale.y, hitbox.transform.lossyScale.z);
                sb.AppendLine($" Wheel hitbox: center={wheelCenter} radius={wheelRadius:F3}");
            }

            // 4. Find all GrabInteractor + HandGrabInteractor in scene and report state
            var grabInteractors = FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None);
            sb.AppendLine($" GrabInteractor count: {grabInteractors.Length}");
            foreach (var gi in grabInteractors)
            {
                Vector3 pos = gi.transform.position;
                float dist = hitbox != null ? Vector3.Distance(pos, hitbox.transform.TransformPoint(hitbox.center)) : -1f;
                sb.AppendLine($"  - '{gi.gameObject.name}' State={gi.State} HasCandidate={gi.HasCandidate} " +
                    $"HasInteractable={gi.HasInteractable} pos={pos} distToWheel={dist:F3}");
            }

            var handGrabInteractors = FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
            sb.AppendLine($" HandGrabInteractor count: {handGrabInteractors.Length}");
            Vector3 wheelCenterW = hitbox != null ? hitbox.transform.TransformPoint(hitbox.center) : Vector3.zero;
            foreach (var hgi in handGrabInteractors)
            {
                Vector3 goPos = hgi.transform.position;
                Vector3 wristPos = hgi.WristPoint != null ? hgi.WristPoint.position : Vector3.zero;
                Vector3 palmPos = hgi.PalmPoint != null ? hgi.PalmPoint.position : Vector3.zero;
                Vector3 pinchPos = hgi.PinchPoint != null ? hgi.PinchPoint.position : Vector3.zero;
                Vector3 rbPos = hgi.Rigidbody != null ? hgi.Rigidbody.position : Vector3.zero;

                Vector3 handRootPos = Vector3.zero;
                bool handPoseValid = false;
                try
                {
                    if (hgi.HandGrabApi != null && hgi.HandGrabApi.Hand != null)
                    {
                        handPoseValid = hgi.HandGrabApi.Hand.GetRootPose(out var rootPose);
                        if (handPoseValid) handRootPos = rootPose.position;
                    }
                }
                catch { }

                float distGo = Vector3.Distance(goPos, wheelCenterW);
                float distWrist = Vector3.Distance(wristPos, wheelCenterW);
                float distRb = Vector3.Distance(rbPos, wheelCenterW);

                string fingerStates = "?";
                try
                {
                    var api = hgi.HandGrabApi;
                    if (api != null)
                    {
                        var pinchFlags = api.HandPinchGrabbingFingers();
                        var palmFlags = api.HandPalmGrabbingFingers();
                        fingerStates = $"Pinch={pinchFlags} Palm={palmFlags}";
                    }
                }
                catch (System.Exception e) { fingerStates = $"err:{e.Message}"; }

                sb.AppendLine($"  - '{hgi.gameObject.name}' parent='{(hgi.transform.parent != null ? hgi.transform.parent.name : "<root>")}' State={hgi.State} HasCandidate={hgi.HasCandidate}");
                sb.AppendLine($"      goPos={goPos} dist={distGo:F3}");
                sb.AppendLine($"      wristPos={wristPos} dist={distWrist:F3}");
                sb.AppendLine($"      rbPos={rbPos} dist={distRb:F3}");
                sb.AppendLine($"      palmPos={palmPos} pinchPos={pinchPos}");
                sb.AppendLine($"      handRootValid={handPoseValid} handRootPos={handRootPos}");
                sb.AppendLine($"      fingers=[{fingerStates}] FingersStrength={hgi.FingersStrength:F2}");
            }

            Debug.Log(sb.ToString());
        }

        private static int CountEnumerable(System.Collections.IEnumerable e)
        {
            if (e == null) return 0;
            int n = 0; foreach (var _ in e) n++; return n;
        }

        private static string GetPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            var p = t.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }
    }
}
