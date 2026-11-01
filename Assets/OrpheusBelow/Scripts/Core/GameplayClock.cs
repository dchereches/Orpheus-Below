using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Shared gameplay speed and the modifiers to restore after a pause.</summary>
    public static class GameplayClock
    {
        public static float NormalScale => DifficultySettings.EffectiveGameSpeed;

        public static float ResumeScale
        {
            get
            {
                var player = OrpheusPlayerController.Instance;
                if (player != null && (player.IsGameOver || player.IsVictory)) return NormalScale;
                float scale = NormalScale * Mathf.Lerp(1f, .28f, player != null ? player.LookBackProgress01 : 0f);
                if (OrpheusLyreCombat.Instance != null && OrpheusLyreCombat.Instance.IsHitstopActive)
                    scale = Mathf.Min(scale, .05f);
                return scale;
            }
        }

        /// <summary>Recalculate running gameplay without releasing an existing menu, speech, or external pause.</summary>
        public static void RefreshGameplayScale()
        {
            if (Time.timeScale > 0f && !UnderworldFrontEnd.BlocksGameplay && !OrpheusHud.IsTutorialModalActive)
                Time.timeScale = ResumeScale;
        }
    }
}
