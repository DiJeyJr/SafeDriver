using UnityEngine;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Persistencia de progreso con PlayerPrefs:
    ///   - niveles desbloqueados (por levelId)
    ///   - mejor puntaje por nivel
    ///   - flag de campania completada (habilita el modo libre)
    ///
    /// Nota: PlayerPrefs no permite enumerar claves, por eso el reset es por-nivel
    /// (lo maneja el menu de seleccion que conoce el catalogo). No se usa DeleteAll
    /// para no pisar otras claves del proyecto (ej. volumen).
    /// </summary>
    public static class LevelProgress
    {
        private const string UnlockedKey  = "sd_unlocked_";   // + levelId -> 1/0
        private const string BestScoreKey = "sd_best_";        // + levelId -> int
        private const string CampaignDoneKey = "sd_campaign_done";

        public static bool IsUnlocked(string levelId)
            => PlayerPrefs.GetInt(UnlockedKey + levelId, 0) == 1;

        public static void Unlock(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.SetInt(UnlockedKey + levelId, 1);
            PlayerPrefs.Save();
        }

        public static int GetBestScore(string levelId)
            => PlayerPrefs.GetInt(BestScoreKey + levelId, 0);

        public static void ReportScore(string levelId, int score)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            if (score > GetBestScore(levelId))
            {
                PlayerPrefs.SetInt(BestScoreKey + levelId, score);
                PlayerPrefs.Save();
            }
        }

        public static bool IsCampaignComplete
            => PlayerPrefs.GetInt(CampaignDoneKey, 0) == 1;

        public static void MarkCampaignComplete()
        {
            PlayerPrefs.SetInt(CampaignDoneKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Borra el progreso de un nivel puntual (desbloqueo + best score).</summary>
        public static void ResetLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.DeleteKey(UnlockedKey + levelId);
            PlayerPrefs.DeleteKey(BestScoreKey + levelId);
            PlayerPrefs.Save();
        }
    }
}
