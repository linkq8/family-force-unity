using UnityEngine;

namespace FamilyForceUnity.Core
{
    public static class GameSettings
    {
        private const string DifficultyKey = "ffu_difficulty";
        public static bool KidsMode
        {
            get => PlayerPrefs.GetInt(DifficultyKey, 0) == 0;
            set { PlayerPrefs.SetInt(DifficultyKey, value ? 0 : 1); PlayerPrefs.Save(); }
        }

        public static string DifficultyLabel => KidsMode ? "KIDS" : "NORMAL";
        public static int EnemyDamage(int normal) => KidsMode ? Mathf.Max(2, Mathf.RoundToInt(normal * 0.72f)) : normal;
        public static int EnemyCooldown(int normal) => KidsMode ? Mathf.RoundToInt(normal * 1.22f) : normal;
        public static int BetweenWaveHeal => KidsMode ? 30 : 18;
    }
}
