using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Definicion base de una mision (data editable como asset ScriptableObject).
    /// Cada tipo concreto (contable, secuencia, con tiempo, compuesta) hereda y
    /// produce su MissionRuntime correspondiente via CreateRuntime().
    ///
    /// Para crear misiones nuevas: Assets > Create > SafeDriver > Misiones > ...
    /// </summary>
    public abstract class MissionDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Id unico de la mision dentro del nivel. Sirve para tracking y guardado de progreso.")]
        public string missionId;

        [Tooltip("Titulo corto que se muestra en el panel de objetivos.")]
        public string title;

        [TextArea(2, 4)]
        [Tooltip("Descripcion larga / explicacion didactica. Opcional.")]
        public string description;

        [Header("Puntaje")]
        [Tooltip("Puntos que otorga al completarse.")]
        public int points = 10;

        [Tooltip("Si es opcional, no bloquea la finalizacion del nivel.")]
        public bool isOptional;

        /// <summary>Crea la instancia runtime que trackea el estado de esta mision durante el juego.</summary>
        public abstract MissionRuntime CreateRuntime();
    }
}
