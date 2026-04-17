#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SafeDriver.Vehicle;

public static class _InspectVehicleTemp
{
    [MenuItem("SafeDriver/One-Shot Inspect Vehicle State")]
    public static void Run()
    {
        var ext = GameObject.Find("SafeDriver_Exterior_v1");
        Debug.Log($"[EXT] pos={ext.transform.position} rot={ext.transform.rotation.eulerAngles} localScale={ext.transform.localScale} lossyScale={ext.transform.lossyScale}");
        var rb = ext.GetComponent<Rigidbody>();
        if (rb != null) Debug.Log($"[RB] mass={rb.mass} com={rb.centerOfMass} worldCOM={rb.worldCenterOfMass} isKin={rb.isKinematic} useGrav={rb.useGravity}");

        var body = ext.transform.Find("Body");
        if (body != null)
        {
            Debug.Log($"[BODY] localPos={body.localPosition} localRot={body.localRotation.eulerAngles} localScale={body.localScale}");
            var bc = body.GetComponent<BoxCollider>();
            if (bc != null) Debug.Log($"[BODY_BC] center={bc.center} size={bc.size} worldBounds={bc.bounds.size} worldCenter={bc.bounds.center}");
        }

        string[] wheels = { "Wheel_FrontLeft", "Wheel_FrontRight", "Wheel_RearLeft", "Wheel_RearRight" };
        foreach (var w in wheels)
        {
            var t = ext.transform.Find(w);
            if (t != null) Debug.Log($"[WM] {w} localPos={t.localPosition} localRot={t.localRotation.eulerAngles} localScale={t.localScale}");
        }
        string[] wcNames = { "WheelCollider_FL", "WheelCollider_FR", "WheelCollider_RL", "WheelCollider_RR" };
        foreach (var n in wcNames)
        {
            var t = ext.transform.Find(n);
            if (t == null) { Debug.Log($"[WC MISSING] {n}"); continue; }
            var wc = t.GetComponent<WheelCollider>();
            Debug.Log($"[WC] {n} localPos={t.localPosition} localRot={t.localRotation.eulerAngles} radius={wc.radius} suspDist={wc.suspensionDistance} spring={wc.suspensionSpring.spring} target={wc.suspensionSpring.targetPosition}");
        }
    }
}
#endif
