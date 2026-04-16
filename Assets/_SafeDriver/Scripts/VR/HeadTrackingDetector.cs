using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.VR
{
    /// <summary>
    /// Detecta si el jugador giro la cabeza para chequear espejos.
    /// Se adjunta al CenterEyeAnchor del OVRCameraRig — el Update lee localEulerAngles,
    /// que representa la rotacion de la cabeza respecto a la TrackingSpace del rig.
    ///
    /// Publica EventBus.Dispatch_CorrectAction(MirrorChecked, bonus) en el edge rising
    /// (primera vez que el jugador mira un espejo desde el ultimo ResetMirrorChecks).
    /// </summary>
    public class HeadTrackingDetector : MonoBehaviour
    {
        [Header("Umbrales de deteccion (grados)")]
        [Tooltip("Yaw minimo (negativo) para contar mirada al espejo lateral izquierdo.")]
        public float leftMirrorAngle  = -45f;

        [Tooltip("Yaw minimo (positivo) para contar mirada al espejo lateral derecho.")]
        public float rightMirrorAngle =  45f;

        [Tooltip("Cuantos grados el driver tiene que mirar HACIA ARRIBA para contar retrovisor.")]
        public float rearMirrorAngle  =  15f;

        [Header("Recompensas")]
        [Tooltip("Bonus points por chequear un espejo (rising edge). Si es <= 0 se usa ActionPoints.CheckedMirrorsBeforeTurn.")]
        [SerializeField] private int checkBonusOverride = 0;

        private bool checkedLeftMirror;
        private bool checkedRightMirror;
        private bool checkedRearMirror;

        public bool CheckedLeftMirror  => checkedLeftMirror;
        public bool CheckedRightMirror => checkedRightMirror;
        public bool CheckedRearMirror  => checkedRearMirror;

        /// <summary>Reinicia los flags — llamar al empezar una nueva maniobra (ej: al detectar intencion de giro).</summary>
        public void ResetMirrorChecks()
        {
            checkedLeftMirror  = false;
            checkedRightMirror = false;
            checkedRearMirror  = false;
        }

        /// <summary>Devuelve true si el requerimiento correspondiente ya fue cumplido.</summary>
        public bool HasCheckedRequiredMirrors(MirrorCheckRequirement req)
        {
            return req switch
            {
                MirrorCheckRequirement.TurnLeft  => checkedLeftMirror,
                MirrorCheckRequirement.TurnRight => checkedRightMirror,
                MirrorCheckRequirement.Reverse   => checkedRearMirror,
                _ => true
            };
        }

        void Update()
        {
            // localEulerAngles de CenterEyeAnchor = orientacion de la cabeza respecto a la TrackingSpace.
            Vector3 e = transform.localEulerAngles;
            float yaw   = NormalizeAngle(e.y);
            float pitch = NormalizeAngle(e.x);

            // Unity: pitch positivo = mirar hacia abajo, negativo = mirar hacia arriba.
            // Internamente flipeo para que rearMirrorAngle (positivo) signifique "mirar arriba".
            float pitchUp = -pitch;

            // Rising-edge: solo dispara evento la primera vez de cada ciclo.
            if (!checkedLeftMirror && yaw < leftMirrorAngle)
            {
                checkedLeftMirror = true;
                DispatchChecked(MirrorType.LeftSide);
            }

            if (!checkedRightMirror && yaw > rightMirrorAngle)
            {
                checkedRightMirror = true;
                DispatchChecked(MirrorType.RightSide);
            }

            if (!checkedRearMirror && pitchUp > rearMirrorAngle)
            {
                checkedRearMirror = true;
                DispatchChecked(MirrorType.Rearview);
            }
        }

        private void DispatchChecked(MirrorType mirror)
        {
            int bonus = checkBonusOverride > 0
                ? checkBonusOverride
                : ActionPoints.CheckedMirrorsBeforeTurn;
            EventBus.Dispatch_CorrectAction(ActionType.CheckedMirrorsBeforeTurn, bonus);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a >  180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}
