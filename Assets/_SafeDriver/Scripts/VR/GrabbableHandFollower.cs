using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Version generica del SteeringWheelHandFollower: hace que la HandVisual del controller
    /// quede snappeada al `target` mientras se lo agarra, siguiendo su rotacion.
    /// Soporta GrabInteractor y HandGrabInteractor. Comparamos SelectedInteractable.gameObject
    /// contra el GO del target para detectar el grab sin importar el tipo de interactor.
    ///
    /// Layout:
    ///   - Snapshot de handVisual.Root en espacio local del target al iniciar grab.
    ///   - InjectOptionalUpdateRootPose(false) para que HandVisual deje de reescribir Root.
    ///   - LateUpdate fuerza handVisual.Root al pose snapshoteado * rotacion actual del target.
    ///   - Al soltar, restaura InjectOptionalUpdateRootPose(true).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class GrabbableHandFollower : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform que rota (el grabbable). Si queda vacio usa este GameObject.")]
        [SerializeField] private Transform target;

        [Header("Left — interactors del lado izquierdo")]
        [SerializeField] private GrabInteractor[] leftGrabInteractors;
        [SerializeField] private HandGrabInteractor[] leftHandGrabInteractors;
        [SerializeField] private HandVisual leftHandVisual;

        [Header("Right — interactors del lado derecho")]
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
            if (target == null) target = transform;
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

            bool isGrabbing = AnySelectsTarget(grabInteractors) || AnySelectsTarget(handGrabInteractors);

            Transform handRoot = handVisual.Root;

            if (isGrabbing && !s.wasGrabbing)
            {
                s.localPos = target.InverseTransformPoint(handRoot.position);
                s.localRot = Quaternion.Inverse(target.rotation) * handRoot.rotation;
                handVisual.InjectOptionalUpdateRootPose(false);
                if (logDebug) Debug.Log($"[Follower:{name}] {tag} GRAB start. localPos={s.localPos}");
            }
            else if (!isGrabbing && s.wasGrabbing)
            {
                handVisual.InjectOptionalUpdateRootPose(true);
                if (logDebug) Debug.Log($"[Follower:{name}] {tag} RELEASE.");
            }

            if (isGrabbing)
            {
                handRoot.SetPositionAndRotation(
                    target.TransformPoint(s.localPos),
                    target.rotation * s.localRot);
            }

            s.wasGrabbing = isGrabbing;
        }

        private bool AnySelectsTarget(GrabInteractor[] interactors)
        {
            if (interactors == null) return false;
            for (int i = 0; i < interactors.Length; i++)
            {
                var it = interactors[i];
                if (it == null) continue;
                if (it.State != InteractorState.Select) continue;
                var selected = it.SelectedInteractable;
                if (selected != null && selected.gameObject == target.gameObject) return true;
            }
            return false;
        }

        private bool AnySelectsTarget(HandGrabInteractor[] interactors)
        {
            if (interactors == null) return false;
            for (int i = 0; i < interactors.Length; i++)
            {
                var it = interactors[i];
                if (it == null) continue;
                if (it.State != InteractorState.Select) continue;
                var selected = it.SelectedInteractable;
                if (selected != null && selected.gameObject == target.gameObject) return true;
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
