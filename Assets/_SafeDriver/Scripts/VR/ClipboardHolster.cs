using UnityEngine;
using Oculus.Interaction;

namespace SafeDriver.VR
{
    /// <summary>
    /// Hace que la tablilla de objetivos vuelva sola a su pose de reposo en el tablero cuando el
    /// jugador la suelta. Mientras esta agarrada, el OneGrabFreeTransformer la mueve libre (6DOF);
    /// al soltarla, tras un pequeño delay, vuelve suavemente a su lugar (asi siempre queda legible).
    ///
    /// La pose "home" se captura en Awake (localPosition/localRotation iniciales), asi que basta con
    /// dejar la tablilla colocada en su lugar de reposo en la escena.
    /// </summary>
    public class ClipboardHolster : MonoBehaviour
    {
        [Header("Grab")]
        [Tooltip("Grabbable de la tablilla (para saber cuando esta agarrada).")]
        [SerializeField] private Grabbable grabbable;

        [Header("Retorno")]
        [Tooltip("Segundos a esperar tras soltar antes de empezar a volver.")]
        [SerializeField] private float returnDelay = 0.35f;

        [Tooltip("Velocidad de interpolacion al volver (mayor = mas rapido).")]
        [SerializeField] private float returnLerp = 7f;

        private Vector3 homeLocalPos;
        private Quaternion homeLocalRot;
        private int selectCount;
        private float releaseTime;

        void Awake()
        {
            homeLocalPos = transform.localPosition;
            homeLocalRot = transform.localRotation;
        }

        void OnEnable()
        {
            if (grabbable != null)
                ((IPointableElement)grabbable).WhenPointerEventRaised += HandlePointerEvent;
        }

        void OnDisable()
        {
            if (grabbable != null)
                ((IPointableElement)grabbable).WhenPointerEventRaised -= HandlePointerEvent;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                selectCount++;
            }
            else if (evt.Type == PointerEventType.Unselect)
            {
                selectCount = Mathf.Max(0, selectCount - 1);
                if (selectCount == 0) releaseTime = Time.time;
            }
        }

        void LateUpdate()
        {
            if (selectCount > 0) return;                       // agarrada: manda el transformer
            if (Time.time - releaseTime < returnDelay) return; // recien soltada: esperar el delay

            transform.localPosition = Vector3.Lerp(transform.localPosition, homeLocalPos, Time.deltaTime * returnLerp);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, homeLocalRot, Time.deltaTime * returnLerp);
        }

        /// <summary>Permite re-definir la pose de reposo desde un editor utility tras posicionar la tablilla.</summary>
        public void CaptureHome()
        {
            homeLocalPos = transform.localPosition;
            homeLocalRot = transform.localRotation;
        }
    }
}
