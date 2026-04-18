using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Snappea la HandVisual de cada controller al volante mientras lo agarra
    /// y la hace seguir su rotacion. Solo controllers, sin HandGrabPose.
    ///
    /// Soporta dos familias de interactors porque el rig de Meta puede rutear
    /// el grab via GrabInteractor (Controller puro) o via HandGrabInteractor
    /// (Controller + Hand hybrid). Comparamos SelectedInteractable.gameObject
    /// contra el GO de la rueda para detectar el grab sin importar el tipo.
    ///
    /// Al entrar en grab:
    ///   - Snapshot de handVisual.Root en espacio local de wheel.
    ///   - HandVisual._updateRootPose = false (via InjectOptionalUpdateRootPose)
    ///     para que HandVisual.UpdateSkeleton deje de reescribir Root.
    /// Cada LateUpdate forzamos handVisual.Root al pose snapshoteado transformado
    /// por la rotacion actual del volante. Al soltar, restauramos _updateRootPose.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SteeringWheelHandFollower : MonoBehaviour
    {
        [Header("Volante")]
        [Tooltip("Transform que rota. Si queda vacio usa este GameObject.")]
        [SerializeField] private Transform wheel;

        [Header("Left — cualquier interactor que pueda agarrar desde el lado izquierdo")]
        [SerializeField] private GrabInteractor[] leftGrabInteractors;
        [SerializeField] private HandGrabInteractor[] leftHandGrabInteractors;
        [SerializeField] private HandVisual leftHandVisual;

        [Header("Right — cualquier interactor que pueda agarrar desde el lado derecho")]
        [SerializeField] private GrabInteractor[] rightGrabInteractors;
        [SerializeField] private HandGrabInteractor[] rightHandGrabInteractors;
        [SerializeField] private HandVisual rightHandVisual;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        private HandState left;
        private HandState right;

        private struct HandState
        {
            public bool wasGrabbing;
            public Vector3 localPos;
            public Quaternion localRot;
        }

        void Awake()
        {
            if (wheel == null) wheel = transform;
        }

        void LateUpdate()
        {
            Process("L", leftGrabInteractors, leftHandGrabInteractors, leftHandVisual, ref left);
            Process("R", rightGrabInteractors, rightHandGrabInteractors, rightHandVisual, ref right);
        }

        private void Process(
            string tag,
            GrabInteractor[] grabInteractors,
            HandGrabInteractor[] handGrabInteractors,
            HandVisual handVisual,
            ref HandState s)
        {
            if (handVisual == null || handVisual.Root == null) return;

            bool isGrabbing = AnySelectsWheel(grabInteractors) || AnySelectsWheel(handGrabInteractors);

            Transform handRoot = handVisual.Root;

            if (isGrabbing && !s.wasGrabbing)
            {
                s.localPos = wheel.InverseTransformPoint(handRoot.position);
                s.localRot = Quaternion.Inverse(wheel.rotation) * handRoot.rotation;
                handVisual.InjectOptionalUpdateRootPose(false);
                if (logDebug) Debug.Log($"[WheelFollower] {tag} GRAB start. localPos={s.localPos}");
            }
            else if (!isGrabbing && s.wasGrabbing)
            {
                handVisual.InjectOptionalUpdateRootPose(true);
                if (logDebug) Debug.Log($"[WheelFollower] {tag} RELEASE.");
            }

            if (isGrabbing)
            {
                handRoot.SetPositionAndRotation(
                    wheel.TransformPoint(s.localPos),
                    wheel.rotation * s.localRot);
            }

            s.wasGrabbing = isGrabbing;
        }

        private bool AnySelectsWheel(GrabInteractor[] interactors)
        {
            if (interactors == null) return false;
            for (int i = 0; i < interactors.Length; i++)
            {
                var it = interactors[i];
                if (it == null) continue;
                if (it.State != InteractorState.Select) continue;
                var selected = it.SelectedInteractable;
                if (selected != null && selected.gameObject == wheel.gameObject) return true;
            }
            return false;
        }

        private bool AnySelectsWheel(HandGrabInteractor[] interactors)
        {
            if (interactors == null) return false;
            for (int i = 0; i < interactors.Length; i++)
            {
                var it = interactors[i];
                if (it == null) continue;
                if (it.State != InteractorState.Select) continue;
                var selected = it.SelectedInteractable;
                if (selected != null && selected.gameObject == wheel.gameObject) return true;
            }
            return false;
        }

        void OnDisable()
        {
            if (left.wasGrabbing && leftHandVisual != null)
            {
                leftHandVisual.InjectOptionalUpdateRootPose(true);
                left.wasGrabbing = false;
            }
            if (right.wasGrabbing && rightHandVisual != null)
            {
                rightHandVisual.InjectOptionalUpdateRootPose(true);
                right.wasGrabbing = false;
            }
        }
    }
}
