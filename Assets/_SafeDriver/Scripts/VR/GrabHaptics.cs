using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

namespace SafeDriver.VR
{
    /// <summary>
    /// Helper para disparar pulsos hapticos en el controller que esta agarrando un grabbable.
    /// Usa el API estandar de Unity XR (`InputDevice.SendHapticImpulse`), evita OVR classic.
    ///
    /// Resolucion de handedness: itera los HandGrabInteractor y GrabInteractor en estado Select
    /// cuyo `SelectedInteractable.gameObject` coincida con el grabbable, y sube por la jerarquia
    /// buscando HandRef o ControllerRef para identificar el lado.
    /// </summary>
    public static class GrabHaptics
    {
        /// <summary>Dispara un pulso haptic en el controller del handedness indicado.</summary>
        public static void Pulse(Handedness hand, float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            XRNode node = hand == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }

        /// <summary>Dispara un pulso en cada handedness que actualmente este agarrando el grabbable.</summary>
        public static void PulseOnGrabbing(Grabbable grabbable, float amplitude, float duration)
        {
            if (grabbable == null) return;
            foreach (var h in ResolveSelectingHands(grabbable))
                Pulse(h, amplitude, duration);
        }

        /// <summary>Dispara un pulso en ambos controllers (fallback cuando no se puede resolver).</summary>
        public static void PulseBoth(float amplitude, float duration)
        {
            Pulse(Handedness.Left, amplitude, duration);
            Pulse(Handedness.Right, amplitude, duration);
        }

        /// <summary>Devuelve las Handedness de los controllers que ahora seleccionan el grabbable.</summary>
        public static IEnumerable<Handedness> ResolveSelectingHands(Grabbable grabbable)
        {
            if (grabbable == null) yield break;
            var targetGO = grabbable.gameObject;

            foreach (var hgi in Object.FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None))
            {
                if (hgi == null || hgi.State != InteractorState.Select) continue;
                var sel = hgi.SelectedInteractable;
                if (sel == null || sel.gameObject != targetGO) continue;
                if (TryResolveHandedness(hgi.gameObject, out var h)) yield return h;
            }

            foreach (var gi in Object.FindObjectsByType<GrabInteractor>(FindObjectsSortMode.None))
            {
                if (gi == null || gi.State != InteractorState.Select) continue;
                var sel = gi.SelectedInteractable;
                if (sel == null || sel.gameObject != targetGO) continue;
                if (TryResolveHandedness(gi.gameObject, out var h)) yield return h;
            }
        }

        private static bool TryResolveHandedness(GameObject go, out Handedness h)
        {
            var handRef = go.GetComponentInParent<HandRef>();
            if (handRef != null && handRef.Hand != null) { h = handRef.Handedness; return true; }
            var controllerRef = go.GetComponentInParent<ControllerRef>();
            if (controllerRef != null) { h = controllerRef.Handedness; return true; }
            // Fallback: buscar en el nombre del ancestro mas cercano
            var t = go.transform;
            while (t != null)
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("left"))  { h = Handedness.Left;  return true; }
                if (n.Contains("right")) { h = Handedness.Right; return true; }
                t = t.parent;
            }
            h = default;
            return false;
        }
    }
}
