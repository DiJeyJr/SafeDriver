using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Detecta la colision REAL del auto con el peaton. Va en el collider fisico del
    /// peaton (no en la zona de la senda), por eso "chocar al peaton" deja de ser un
    /// evento de zona y pasa a ser una colision concreta. Dispara HitPedestrian
    /// (infraccion grave → SafeFail). Anti-spam: un solo disparo por contacto hasta
    /// que el auto se aleje.
    ///
    /// Setup: agregar al GameObject del peaton un Collider con isTrigger=true que
    /// cubra su cuerpo, mas este componente.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PedestrianHitbox : VehicleSensor
    {
        private bool hitThisContact;

        void Awake()
        {
            infractionType = InfractionType.HitPedestrian;
            pedagogicalMessage =
                "Atropellaste a un peaton. Reduci la velocidad cerca de cebras y cede el paso.";
        }

        void OnTriggerEnter(Collider other)
        {
            if (hitThisContact || !IsPlayer(other)) return;
            hitThisContact = true;
            infractionType = InfractionType.HitPedestrian;
            TriggerInfraction();
        }

        void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) hitThisContact = false;
        }
    }
}
