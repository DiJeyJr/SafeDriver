using System.Collections.Generic;

namespace SafeDriver.Core
{
    /// <summary>
    /// Resultado final de un nivel. Leido por LevelEndPanel para mostrar el resumen.
    /// </summary>
    public class LevelResult
    {
        public int FinalScore;
        public bool Passed;
        public List<InfractionRecord> Infractions;
    }
}
