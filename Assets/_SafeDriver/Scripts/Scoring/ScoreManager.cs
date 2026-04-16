using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Lleva el puntaje del nivel. Escucha infracciones/aciertos del EventBus y
    /// reemite el nuevo total y el delta para que la UI reaccione.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] private int startingScore = 100;
        [SerializeField] private int penaltyPerInfraction = 10;

        public int CurrentScore { get; private set; }

        void Awake() { CurrentScore = startingScore; }

        void OnEnable()
        {
            EventBus.OnInfractionDetected += HandleInfraction;
            EventBus.OnCorrectActionPerformed += HandleCorrectAction;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected -= HandleInfraction;
            EventBus.OnCorrectActionPerformed -= HandleCorrectAction;
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            CurrentScore += amount;
            EventBus.Dispatch_ScoreDelta(amount);
            EventBus.Dispatch_ScoreChanged(CurrentScore);
        }

        public void SubtractPoints(int amount)
        {
            if (amount <= 0) return;
            CurrentScore = Mathf.Max(0, CurrentScore - amount);
            EventBus.Dispatch_ScoreDelta(-amount);
            EventBus.Dispatch_ScoreChanged(CurrentScore);

            if (CurrentScore <= 0)
                EventBus.Dispatch_LevelFailed();
        }

        private void HandleInfraction(InfractionType type, string message)
        {
            SubtractPoints(penaltyPerInfraction);
        }

        private void HandleCorrectAction(ActionType type, int bonus)
        {
            AddPoints(bonus);
        }
    }
}
