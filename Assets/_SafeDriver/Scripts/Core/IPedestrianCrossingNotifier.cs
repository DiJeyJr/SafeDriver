namespace SafeDriver.Core
{
    /// <summary>
    /// Interfaz que rompe la dependencia circular Traffic <-> Scoring.
    /// NPCPedestrianAI (Traffic) llama SetPedestriansPresent() sobre esta interfaz.
    /// PedestrianCrossingDetector (Scoring) la implementa.
    /// La interfaz vive en Core, accesible para ambas capas.
    /// </summary>
    public interface IPedestrianCrossingNotifier
    {
        void SetPedestriansPresent(bool present);
    }
}
