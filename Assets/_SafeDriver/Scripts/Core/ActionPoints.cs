namespace SafeDriver.Core
{
    /// <summary>
    /// Tabla central de puntos para cada ActionType.
    /// Mantiene los valores en un solo lugar — publishers, subscribers, UI y debug leen de aca
    /// para evitar que los numeros queden dispersos en la codebase.
    /// </summary>
    public static class ActionPoints
    {
        public const int StoppedAtRedLight        = 10;
        public const int YieldedToPedestrian      = 15;
        public const int CheckedMirrorsBeforeTurn = 5;
        public const int MaintainedLegalSpeed     = 2;
        public const int StoppedAtPareSign        = 8;

        /// <summary>Devuelve el bonus default para una ActionType.</summary>
        public static int GetBonus(ActionType action)
        {
            return action switch
            {
                ActionType.StoppedAtRedLight        => StoppedAtRedLight,
                ActionType.YieldedToPedestrian      => YieldedToPedestrian,
                ActionType.CheckedMirrorsBeforeTurn => CheckedMirrorsBeforeTurn,
                ActionType.MaintainedLegalSpeed    => MaintainedLegalSpeed,
                ActionType.StoppedAtPareSign        => StoppedAtPareSign,
                _ => 0,
            };
        }
    }
}
