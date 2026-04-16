namespace SafeDriver.Audio
{
    /// <summary>
    /// Paths y parametros centralizados de FMOD Studio.
    /// Si un path cambia en FMOD Studio, solo se actualiza aca.
    /// </summary>
    public static class FMODEvents
    {
        // Vehicle
        public const string ENGINE_LOOP    = "event:/SafeDriver/Vehicle/Engine_Loop";
        public const string TIRE_SCREECH   = "event:/SafeDriver/Vehicle/Tire_Screech";
        public const string HORN           = "event:/SafeDriver/Vehicle/Horn";

        // Traffic
        public const string NPC_ENGINE     = "event:/SafeDriver/Traffic/NPC_Engine_Loop";
        public const string PED_AMBIENCE   = "event:/SafeDriver/Traffic/Pedestrian_Ambience";
        public const string CITY_AMBIENCE  = "event:/SafeDriver/Traffic/City_Ambience";

        // Feedback
        public const string INFRACTION     = "event:/SafeDriver/Feedback/Infraction";
        public const string SUCCESS        = "event:/SafeDriver/Feedback/Success";
        public const string LEVEL_COMPLETE = "event:/SafeDriver/Feedback/LevelComplete";

        // UI
        public const string BUTTON_CLICK   = "event:/SafeDriver/UI/Button_Click";

        // Parametros de automatizacion
        public const string PARAM_RPM      = "RPM";
        public const string PARAM_LOAD     = "Load";
    }
}
