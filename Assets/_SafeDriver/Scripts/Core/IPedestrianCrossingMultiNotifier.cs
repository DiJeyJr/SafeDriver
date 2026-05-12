namespace SafeDriver.Core
{
    /// <summary>
    /// Extension de <see cref="IPedestrianCrossingNotifier"/> que soporta multiples agentes
    /// notificadores identificados por su instanceId. Permite que varios peatones presentes
    /// en la senda no se pisen el booleano del detector.
    ///
    /// Vive en Core para evitar el ciclo de asmdef entre Traffic (NPCs) y Scoring (detector).
    /// </summary>
    public interface IPedestrianCrossingMultiNotifier : IPedestrianCrossingNotifier
    {
        /// <summary>
        /// Notifica que el agente con `notifierId` entro (present=true) o salio (present=false)
        /// de la zona. El detector mantiene un set de ids activos y considera que hay peatones
        /// mientras el set no este vacio.
        /// </summary>
        void NotifyByInstance(int notifierId, bool present);
    }
}
