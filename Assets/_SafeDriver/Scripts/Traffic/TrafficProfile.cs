using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Configuracion de trafico NPC para un nivel. Permite ajustar velocidad, tamaño y cantidad de
    /// autos sin tocar la escena — ideal para escalar dificultad nivel a nivel (mas rapido = mas dificil).
    ///
    /// Crear assets via Create > SafeDriver > Traffic Profile y referenciar uno por nivel. El editor
    /// utility "SafeDriver/Setup City Traffic" lo lee al generar el trafico.
    /// </summary>
    [CreateAssetMenu(fileName = "TrafficProfile", menuName = "SafeDriver/Traffic Profile")]
    public class TrafficProfile : ScriptableObject
    {
        [Header("Velocidad")]
        [Tooltip("Velocidad de crucero base de los NPC en m/s (4 ≈ 14 km/h, tranqui para VR). Subir por nivel para mas dificultad.")]
        public float cruiseSpeed = 4f;

        [Tooltip("Variacion +/- de velocidad entre autos (m/s), para que no vayan todos iguales.")]
        public float speedVariation = 1f;

        [Header("Tamaño")]
        [Tooltip("Escala uniforme de los modelos NPC. Los SimplePoly vienen ~1.75x grandes; 0.70 los deja un poco mas grandes que el auto del player.")]
        public float modelScale = 0.70f;

        [Header("Cantidad")]
        [Tooltip("Cantidad de autos a spawnear (se ciclan los prefabs disponibles).")]
        [Range(1, 12)]
        public int carCount = 4;
    }
}
