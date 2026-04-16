namespace SafeDriver.Core
{
    /// <summary>
    /// Acciones correctas que se premian con puntos.
    /// Disparan OnCorrectActionPerformed(type, bonus) via EventBus.
    /// Los valores default de bonus viven en ActionPoints.
    /// </summary>
    public enum ActionType
    {
        StoppedAtRedLight,         // +10: parar cuando corresponde en luz roja
        YieldedToPedestrian,       // +15: ceder paso a peaton cruzando
        CheckedMirrorsBeforeTurn,  // +5:  chequear espejo(s) antes de girar
        MaintainedLegalSpeed,      // +2:  mantener velocidad dentro del limite por tramo
        StoppedAtPareSign,         // +8:  detenerse completamente en senal PARE
    }
}
