using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Convierte un prefab en una mision autocontenida: al arrancar la escena clona su
    /// MissionDefinition template, aplica los overrides del Inspector y la registra en el
    /// MissionManager. Arrastrar el kit a la escena = mision funcionando. No hace falta
    /// tocar el LevelDefinition ni crear assets de mision a mano.
    ///
    /// Cada instancia del kit genera su propia mision (id unico), asi se pueden poner
    /// varios kits iguales en un nivel. Ojo: las misiones contables cuentan acciones
    /// GLOBALES del EventBus — dos kits de PARE con requiredCount 1 se completan ambos
    /// con la primera parada; para exigir N paradas usar un solo kit con requiredCount N.
    /// </summary>
    public class MissionKit : MonoBehaviour
    {
        [Header("Mision del kit")]
        [Tooltip("Definicion base (asset template). Se clona por instancia, el asset no se toca.")]
        [SerializeField] private MissionDefinition missionTemplate;

        [Header("Overrides (opcionales)")]
        [Tooltip("Si no esta vacio, reemplaza el titulo del template en el panel de objetivos.")]
        [SerializeField] private string customTitle;

        [Tooltip("Solo misiones contables: cuantas veces hay que hacer la accion. 0 = usar el valor del template.")]
        [Min(0)]
        [SerializeField] private int requiredCountOverride = 0;

        [Tooltip("Si esta marcada, la mision no bloquea la finalizacion del nivel.")]
        [SerializeField] private bool isOptional;

        void Start()
        {
            if (missionTemplate == null)
            {
                Debug.LogWarning("[MissionKit] Sin missionTemplate asignada, el kit no registra nada.", this);
                return;
            }
            if (MissionManager.Instance == null)
            {
                Debug.LogWarning("[MissionKit] No hay MissionManager en la escena — el kit necesita el stack de gameplay (GameManager/MissionManager/ScoreManager).", this);
                return;
            }

            // Clon por instancia: cada kit es una mision independiente con id unico.
            var def = Instantiate(missionTemplate);
            def.missionId = missionTemplate.missionId + "_" + GetInstanceID();
            if (!string.IsNullOrEmpty(customTitle)) def.title = customTitle;
            def.isOptional = isOptional;
            if (requiredCountOverride > 0 && def is CountableMissionDefinition contable)
                contable.requiredCount = requiredCountOverride;

            MissionManager.Instance.RegisterMission(def);
        }
    }
}
