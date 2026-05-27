namespace SafeDriver.Missions
{
    /// <summary>Estado de una mision durante el juego.</summary>
    public enum MissionStatus
    {
        Pending,     // todavia no hubo progreso
        InProgress,  // progreso parcial
        Completed,   // cumplida
        Failed,      // fallada (tiempo agotado, sub-mision fallada, etc.)
    }
}
