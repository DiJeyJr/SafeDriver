namespace SafeDriver.Core
{
    /// <summary>
    /// Tipos de infraccion detectables en SafeDriver.
    /// Todas disparan OnInfractionDetected(type, message) via EventBus.
    /// </summary>
    public enum InfractionType
    {
        RanRedLight,          // Paso semaforo en rojo
        FailedToStopAtSign,   // No se detuvo en senal PARE
        PedestrianNotYielded, // No cedio paso a peaton
        Speeding,             // Excedio velocidad
        NoMirrorCheck,        // No chequeo espejos antes de girar
        DangerousManeuver,    // Maniobra peligrosa generica
    }
}
