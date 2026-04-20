using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Workaround para el rig de Building Blocks "Controller and Hand": el Rigidbody que
    /// usa ISDK para detectar overlap (HandGrabInteractor.Rigidbody) queda fijo en la
    /// posicion del GameObject padre y no se mueve con el tracking de la mano. Pero los
    /// childs (WristPoint, PalmPoint, PinchPoint) si reciben la pose tracked.
    ///
    /// Este script sincroniza el Rigidbody con el WristPoint cada LateUpdate, asi el
    /// broadcaster de ISDK detecta el overlap correctamente cuando la mano alcanza
    /// algun grabbable.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public class HandGrabRigidbodyTracker : MonoBehaviour
    {
        private struct Pair { public HandGrabInteractor Interactor; public Rigidbody Rb; public Transform Source; }
        private readonly List<Pair> _pairs = new List<Pair>();
        private float _lastScan;

        void Start() => Rescan();

        void LateUpdate()
        {
            if (Time.time - _lastScan > 2f) Rescan();
            for (int i = 0; i < _pairs.Count; i++)
            {
                var p = _pairs[i];
                if (p.Rb == null || p.Source == null) continue;
                p.Rb.position = p.Source.position;
                p.Rb.rotation = p.Source.rotation;
            }
        }

        private void Rescan()
        {
            _lastScan = Time.time;
            _pairs.Clear();
            var interactors = FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
            foreach (var hgi in interactors)
            {
                if (hgi == null || hgi.Rigidbody == null || hgi.WristPoint == null) continue;
                _pairs.Add(new Pair { Interactor = hgi, Rb = hgi.Rigidbody, Source = hgi.WristPoint });
            }
        }
    }
}
