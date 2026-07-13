using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Resalta un elemento del mundo (cartel de PARE, semaforo, peaton) la primera vez
    /// que el jugador se le acerca: al entrar a la zona trigger se prende la esfera del
    /// HighlightMarker, y al salir (ya paso el obstaculo) se apaga para siempre.
    ///
    /// Uso: GameObject con collider trigger cubriendo la zona de aproximacion +
    /// HighlightMarker (con su target apuntando al elemento) + este componente.
    /// Ponerlo SOLO en el primer encuentro del nivel con ese tipo de elemento;
    /// en los siguientes no va (eso decide el level designer).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(HighlightMarker))]
    public class HighlightOnApproach : MonoBehaviour
    {
        [Tooltip("Tag del vehiculo del jugador.")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        private HighlightMarker marker;

        void Awake()
        {
            marker = GetComponent<HighlightMarker>();
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag)) marker.Show();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag)) marker.Dismiss();
        }
    }
}
