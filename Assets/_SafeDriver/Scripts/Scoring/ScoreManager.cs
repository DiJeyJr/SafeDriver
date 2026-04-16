using System.Collections.Generic;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Gestiona puntaje y progresion del nivel actual.
    /// - Arranca en startingScore (1000 default).
    /// - Resta penalidades segun tipo de infraccion (tabla interna).
    /// - Suma bonus por acciones correctas.
    /// - Triggerea SafeFail si la infraccion es grave (RanRedLight, PedestrianNotYielded).
    /// - Expone GetLevelResult() para que LevelEndPanel muestre el resumen.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Configuracion de nivel")]
        public int startingScore   = 1000;
        public int minimumPassScore = 600;

        public int CurrentScore { get; private set; }

        private readonly List<InfractionRecord> infractions = new List<InfractionRecord>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnInfractionDetected    += HandleInfraction;
            EventBus.OnCorrectActionPerformed += HandleCorrectAction;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected    -= HandleInfraction;
            EventBus.OnCorrectActionPerformed -= HandleCorrectAction;
        }

        void Start()
        {
            CurrentScore = startingScore;
            infractions.Clear();
            EventBus.Dispatch_ScoreChanged(CurrentScore);
        }

        // ============================================================
        //   Handlers del EventBus
        // ============================================================

        private void HandleInfraction(InfractionType type, string message)
        {
            int penalty = GetPenalty(type);
            CurrentScore = Mathf.Max(0, CurrentScore - penalty);
            infractions.Add(new InfractionRecord(type, message, Time.time));

            EventBus.Dispatch_ScoreDelta(-penalty);
            EventBus.Dispatch_ScoreChanged(CurrentScore);

            // Infraccion grave: SafeFail inmediato (el auto frena, se muestra pantalla pedagogica)
            if (IsGraveInfraction(type))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TransitionTo(GameState.SafeFail);
            }

            // Score llego a 0: nivel reprobado
            if (CurrentScore <= 0)
            {
                EventBus.Dispatch_LevelFailed();
            }
        }

        private void HandleCorrectAction(ActionType action, int points)
        {
            CurrentScore += points;
            EventBus.Dispatch_ScoreDelta(points);
            EventBus.Dispatch_ScoreChanged(CurrentScore);
        }

        // ============================================================
        //   Tabla de penalidades por tipo de infraccion
        // ============================================================

        private int GetPenalty(InfractionType t)
        {
            return t switch
            {
                InfractionType.RanRedLight          => 20,
                InfractionType.PedestrianNotYielded => 15,
                InfractionType.FailedToStopAtSign   => 12,
                InfractionType.Speeding             => 10,
                InfractionType.NoMirrorCheck        => 5,
                InfractionType.DangerousManeuver    => 8,
                _ => 5,
            };
        }

        /// <summary>
        /// Infracciones graves disparan SafeFail inmediato (el auto se detiene, pantalla pedagogica).
        /// Las no-graves solo restan puntos.
        /// </summary>
        private bool IsGraveInfraction(InfractionType t)
        {
            return t == InfractionType.RanRedLight
                || t == InfractionType.PedestrianNotYielded;
        }

        // ============================================================
        //   API para LevelEndPanel
        // ============================================================

        /// <summary>Genera el resultado final del nivel para la pantalla de resumen.</summary>
        public LevelResult GetLevelResult()
        {
            return new LevelResult
            {
                FinalScore  = CurrentScore,
                Passed      = CurrentScore >= minimumPassScore,
                Infractions = new List<InfractionRecord>(infractions),
            };
        }

        /// <summary>Lista de infracciones cometidas hasta ahora (readonly view).</summary>
        public IReadOnlyList<InfractionRecord> GetInfractions() => infractions;
    }
}
