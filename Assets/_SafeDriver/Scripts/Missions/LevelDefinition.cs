using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Definicion de un nivel: que escena carga, que misiones tiene y como se encadena
    /// con el siguiente. Editable como asset (Assets > Create > SafeDriver > Nivel).
    /// </summary>
    [CreateAssetMenu(menuName = "SafeDriver/Nivel", fileName = "Level_00")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Id unico del nivel. Clave de progreso/guardado.")]
        public string levelId;

        [Tooltip("Nombre que se muestra en el menu de seleccion.")]
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        [Header("Escena")]
        [Tooltip("Nombre de la escena a cargar (debe estar en Build Settings).")]
        public string sceneName;

        [Header("Misiones")]
        [Tooltip("Misiones del nivel. Las no-opcionales deben completarse para pasar.")]
        public MissionDefinition[] missions;

        [Header("Progresion")]
        [Tooltip("Nivel que se desbloquea al completar este. Vacio = es el ultimo de la campania.")]
        public LevelDefinition nextLevel;

        [Tooltip("Modo libre: sin condicion de fin por misiones, solo suma puntos manejando.")]
        public bool isFreeRoam;
    }
}
