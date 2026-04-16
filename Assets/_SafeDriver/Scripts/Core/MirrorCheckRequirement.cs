namespace SafeDriver.Core
{
    /// <summary>
    /// Maniobras que requieren chequeo de espejo/s antes de ejecutarse.
    /// - TurnLeft:  requiere mirar el espejo lateral izquierdo
    /// - TurnRight: requiere mirar el espejo lateral derecho
    /// - Reverse:   requiere mirar el retrovisor (cabeza hacia arriba/atras)
    /// - None:      sin requerimiento (maniobra libre)
    /// </summary>
    public enum MirrorCheckRequirement
    {
        None,
        TurnLeft,
        TurnRight,
        Reverse
    }

    /// <summary>Espejo especifico del auto (util para telemetria / puntos por cada espejo).</summary>
    public enum MirrorType
    {
        LeftSide,
        RightSide,
        Rearview
    }
}
