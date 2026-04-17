#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SafeDriver.Vehicle;

public static class _FixVehicleTemp
{
    [MenuItem("SafeDriver/One-Shot Fix Vehicle")]
    public static void Run()
    {
        var ext = GameObject.Find("SafeDriver_Exterior_v1");
        if (ext == null) { Debug.LogError("No ext"); return; }
        Undo.RegisterFullObjectHierarchyUndo(ext, "Fix Vehicle");

        // --- 1. Rigidbody: quitar COM override, dejar que Unity calcule natural ---
        var rb = ext.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.ResetCenterOfMass();
            rb.ResetInertiaTensor();
        }

        // --- 2. Anchors para wheel meshes (fix SyncWheel override de rotacion) ---
        string[] meshNames = { "Wheel_FrontLeft", "Wheel_FrontRight", "Wheel_RearLeft", "Wheel_RearRight" };
        string[] anchorNames = { "WheelMeshAnchor_FL", "WheelMeshAnchor_FR", "WheelMeshAnchor_RL", "WheelMeshAnchor_RR" };
        Transform[] anchors = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var mesh = ext.transform.Find(meshNames[i]);
            if (mesh == null) { Debug.LogError($"Mesh {meshNames[i]} missing"); continue; }

            var anchor = ext.transform.Find(anchorNames[i]);
            if (anchor == null)
            {
                var go = new GameObject(anchorNames[i]);
                Undo.RegisterCreatedObjectUndo(go, "Create Anchor");
                go.transform.SetParent(ext.transform, false);
                go.transform.position = mesh.position;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                anchor = go.transform;
            }

            // Reparent mesh under anchor preserving world transform
            Undo.SetTransformParent(mesh, anchor, "Reparent wheel mesh");
            anchors[i] = anchor;
        }

        // --- 3. Subir WheelColliders 0.08m para evitar que arranquen bajo el piso ---
        string[] wcNames = { "WheelCollider_FL", "WheelCollider_FR", "WheelCollider_RL", "WheelCollider_RR" };
        WheelCollider[] wcs = new WheelCollider[4];
        for (int i = 0; i < 4; i++)
        {
            var t = ext.transform.Find(wcNames[i]);
            if (t == null) continue;
            var pos = t.localPosition;
            pos.y = 0.40f; // wheel radius 0.33 + targetPos offset; queda como resting point sano
            t.localPosition = pos;
            wcs[i] = t.GetComponent<WheelCollider>();
        }

        // --- 4. VehicleController: update mesh refs a anchors ---
        var vc = ext.GetComponent<VehicleController>();
        if (vc != null)
        {
            vc.wheelMeshFL = anchors[0];
            vc.wheelMeshFR = anchors[1];
            vc.wheelMeshRL = anchors[2];
            vc.wheelMeshRR = anchors[3];
            EditorUtility.SetDirty(vc);
        }

        // --- 5. SteeringWheel's SphereCollider = trigger (no colisiona con body) ---
        var wheelGo = GameObject.Find("SteeringWheel");
        if (wheelGo != null)
        {
            var sc = wheelGo.GetComponent<SphereCollider>();
            if (sc != null) { sc.isTrigger = true; EditorUtility.SetDirty(sc); }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ext.scene);
        Debug.Log("[FIX] COM reset, mesh anchors created/reused, WCs raised, wheel collider trigger set.");
    }
}
#endif
