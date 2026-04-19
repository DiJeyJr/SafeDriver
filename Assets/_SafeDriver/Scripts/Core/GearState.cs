namespace SafeDriver.Core
{
    public enum GearState
    {
        Drive,          // D — moviendo adelante o listo para avanzar
        Reverse,        // R — moviendo atras o listo para retroceder
        ReverseArming,  // R parpadeante — armando transicion D -> R
        DriveArming     // D parpadeante — armando transicion R -> D
    }
}
