using UnityEngine;

namespace OrpheusBelow
{
    public enum UnderworldDifficulty
    {
        Mortal = 0,    // Story / gentle: 0.70x damage taken, +30% breath recovery
        Heroic = 1,    // Balanced standard: 1.00x damage taken, standard breath
        Tartarus = 2   // Punishing underworld trial: 1.40x damage taken, -15% breath recovery
    }

    /// <summary>
    /// Global difficulty settings for Orpheus Below.
    /// Governs damage scaling, breath recovery, and timing tolerances across encounters.
    /// Persisted across sessions via PlayerPrefs.
    /// </summary>
    public static class DifficultySettings
    {
        private const string PrefKey = "Orpheus_Difficulty";

        public static UnderworldDifficulty CurrentDifficulty { get; private set; } = UnderworldDifficulty.Mortal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            CurrentDifficulty = (UnderworldDifficulty)PlayerPrefs.GetInt(PrefKey, (int)UnderworldDifficulty.Mortal);
        }

        public static float DamageMultiplier => CurrentDifficulty switch
        {
            UnderworldDifficulty.Mortal => 0.70f,
            UnderworldDifficulty.Heroic => 1.00f,
            UnderworldDifficulty.Tartarus => 1.40f,
            _ => 1.00f
        };

        public static float BreathRecoveryMultiplier => CurrentDifficulty switch
        {
            UnderworldDifficulty.Mortal => 1.30f,
            UnderworldDifficulty.Heroic => 1.00f,
            UnderworldDifficulty.Tartarus => 0.85f,
            _ => 1.00f
        };

        public static string GetName(UnderworldDifficulty diff) => diff switch
        {
            UnderworldDifficulty.Mortal => "MORTAL",
            UnderworldDifficulty.Heroic => "HEROIC",
            UnderworldDifficulty.Tartarus => "TARTARUS",
            _ => "HEROIC"
        };

        public static string GetDescription(UnderworldDifficulty diff) => diff switch
        {
            UnderworldDifficulty.Mortal => "Gentle descent at 0.7x speed. Incoming damage reduced (0.7x), breath recovery +30%.",
            UnderworldDifficulty.Heroic => "Balanced trial at 1x speed. Intended classical challenge.",
            UnderworldDifficulty.Tartarus => "Wrath of Hades at 1x speed. Incoming damage increased (1.4x), breath recovery -15%.",
            _ => ""
        };

        public static void SetDifficulty(UnderworldDifficulty diff)
        {
            CurrentDifficulty = diff;
            PlayerPrefs.SetInt(PrefKey, (int)diff);
            PlayerPrefs.Save();
            GameplayClock.RefreshGameplayScale();
        }

        public static void CycleNext()
        {
            int next = ((int)CurrentDifficulty + 1) % 3;
            SetDifficulty((UnderworldDifficulty)next);
        }
    }
}
