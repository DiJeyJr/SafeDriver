namespace SafeDriver.Core
{
    /// <summary>
    /// Estados del flujo principal de SafeDriver.
    /// - MainMenu:  antes de cargar nivel (placeholder para menu raiz)
    /// - Driving:   simulacion activa, input del jugador aplicado al vehiculo
    /// - SafeFail:  infraccion grave, vehiculo se detiene suavemente y aparece pantalla pedagogica
    /// - LevelEnd:  nivel completado o fallado; muestra resumen
    /// - Paused:    simulacion congelada (menu de pausa, settings in-game)
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Driving,
        SafeFail,
        LevelEnd,
        Paused
    }
}
