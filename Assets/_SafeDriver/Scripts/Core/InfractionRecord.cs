namespace SafeDriver.Core
{
    /// <summary>
    /// Registro historico de una infraccion cometida durante el nivel.
    /// Usado por ScoreManager para guardar el historial y por LevelEndPanel para mostrar el resumen.
    /// </summary>
    public readonly struct InfractionRecord
    {
        public readonly InfractionType Type;
        public readonly string Message;
        public readonly float TimeSeconds;

        public InfractionRecord(InfractionType type, string message, float timeSeconds)
        {
            Type = type;
            Message = message;
            TimeSeconds = timeSeconds;
        }
    }
}
