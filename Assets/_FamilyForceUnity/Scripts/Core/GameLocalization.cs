using UnityEngine;

namespace FamilyForceUnity.Core
{
    public static class GameLocalization
    {
        private const string Key = "ffu_language_arabic";
        public static bool Arabic => PlayerPrefs.GetInt(Key,
            Application.systemLanguage == SystemLanguage.Arabic ? 1 : 0) == 1;

        public static string T(string english, string arabic) => Arabic ? arabic : english;

        public static void Toggle()
        {
            PlayerPrefs.SetInt(Key, Arabic ? 0 : 1);
            PlayerPrefs.Save();
        }
    }
}
