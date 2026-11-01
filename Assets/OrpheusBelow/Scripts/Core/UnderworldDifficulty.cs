using UnityEngine;

namespace OrpheusBelow
{
    public enum UnderworldDifficulty
    {
        Mortal = 0,    // Story / gentle: 0.70x damage taken, +30% breath recovery
        Heroic = 1,    // Balanced standard: 1.00x damage taken, standard breath
        Tartarus = 2,  // Punishing underworld trial: 1.40x damage taken, -15% breath recovery
        Alexa = 3      // Custom Alexa difficulty: fully configurable HP and Game Speed sliders
    }

    /// <summary>
    /// Global difficulty settings for Orpheus Below.
    /// Governs damage scaling, breath recovery, and timing tolerances across encounters.
    /// Features custom "Alexa Mode" with tailored HP and Game Speed sliders.
    /// Persisted across sessions via PlayerPrefs.
    /// </summary>
    public static class DifficultySettings
    {
        private const string PrefKey = "Orpheus_Difficulty";
        private const string AlexaHpKey = "Orpheus_Alexa_HP";
        private const string AlexaSpeedKey = "Orpheus_Alexa_GameSpeed";

        public const float DefaultAlexaHp = 150f;
        public const float DefaultAlexaGameSpeed = 1.00f;

        public static UnderworldDifficulty CurrentDifficulty { get; private set; } = UnderworldDifficulty.Mortal;
        public static float AlexaHp { get; private set; } = DefaultAlexaHp;
        public static float AlexaGameSpeed { get; private set; } = DefaultAlexaGameSpeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            CurrentDifficulty = (UnderworldDifficulty)PlayerPrefs.GetInt(PrefKey, (int)UnderworldDifficulty.Mortal);
            AlexaHp = Mathf.Clamp(PlayerPrefs.GetFloat(AlexaHpKey, DefaultAlexaHp), 25f, 500f);
            AlexaGameSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(AlexaSpeedKey, DefaultAlexaGameSpeed), 0.30f, 2.00f);
        }

        public static float DamageMultiplier => CurrentDifficulty switch
        {
            UnderworldDifficulty.Mortal => 0.70f,
            UnderworldDifficulty.Heroic => 1.00f,
            UnderworldDifficulty.Tartarus => 1.40f,
            UnderworldDifficulty.Alexa => 1.00f,
            _ => 1.00f
        };

        public static float BreathRecoveryMultiplier => CurrentDifficulty switch
        {
            UnderworldDifficulty.Mortal => 1.30f,
            UnderworldDifficulty.Heroic => 1.00f,
            UnderworldDifficulty.Tartarus => 0.85f,
            UnderworldDifficulty.Alexa => 1.15f,
            _ => 1.00f
        };

        public static float EffectiveGameSpeed => CurrentDifficulty switch
        {
            UnderworldDifficulty.Mortal => 0.70f,
            UnderworldDifficulty.Heroic => 1.00f,
            UnderworldDifficulty.Tartarus => 1.00f,
            UnderworldDifficulty.Alexa => AlexaGameSpeed,
            _ => 1.00f
        };

        public static float EffectiveMaxHealth(float standardMaxHealth = 100f)
        {
            return CurrentDifficulty == UnderworldDifficulty.Alexa ? AlexaHp : standardMaxHealth;
        }

        public static string GetName(UnderworldDifficulty diff) => diff switch
        {
            UnderworldDifficulty.Mortal => "MORTAL",
            UnderworldDifficulty.Heroic => "HEROIC",
            UnderworldDifficulty.Tartarus => "TARTARUS",
            UnderworldDifficulty.Alexa => "ALEXA (CUSTOM)",
            _ => "HEROIC"
        };

        public static string GetDescription(UnderworldDifficulty diff) => diff switch
        {
            UnderworldDifficulty.Mortal => "Gentle descent at 0.7x speed. Incoming damage reduced (0.7x), breath recovery +30%.",
            UnderworldDifficulty.Heroic => "Balanced trial at 1x speed. Intended classical challenge.",
            UnderworldDifficulty.Tartarus => "Wrath of Hades at 1x speed. Incoming damage increased (1.4x), breath recovery -15%.",
            UnderworldDifficulty.Alexa => $"Custom Alexa descent: {Mathf.RoundToInt(AlexaHp)} Max HP, {AlexaGameSpeed:0.00}x game speed. Adjust sliders below.",
            _ => ""
        };

        public static void SetDifficulty(UnderworldDifficulty diff)
        {
            CurrentDifficulty = diff;
            PlayerPrefs.SetInt(PrefKey, (int)diff);
            PlayerPrefs.Save();
            ApplyHealthToPlayer();
            GameplayClock.RefreshGameplayScale();
        }

        public static void SetAlexaHp(float hp)
        {
            AlexaHp = Mathf.Clamp(hp, 25f, 500f);
            PlayerPrefs.SetFloat(AlexaHpKey, AlexaHp);
            PlayerPrefs.Save();
            if (CurrentDifficulty == UnderworldDifficulty.Alexa)
            {
                ApplyHealthToPlayer();
            }
        }

        public static void SetAlexaGameSpeed(float speed)
        {
            AlexaGameSpeed = Mathf.Clamp(speed, 0.30f, 2.00f);
            PlayerPrefs.SetFloat(AlexaSpeedKey, AlexaGameSpeed);
            PlayerPrefs.Save();
            if (CurrentDifficulty == UnderworldDifficulty.Alexa)
            {
                GameplayClock.RefreshGameplayScale();
            }
        }

        public static void ApplyHealthToPlayer()
        {
            var player = OrpheusPlayerController.Instance;
            if (player != null)
            {
                player.ApplyDifficultyHealth();
            }
        }

        public static void CycleNext()
        {
            int next = ((int)CurrentDifficulty + 1) % 4;
            SetDifficulty((UnderworldDifficulty)next);
        }
    }
}
