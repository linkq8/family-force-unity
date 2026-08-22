using FamilyForceUnity.AI;
using FamilyForceUnity.Characters;
using FamilyForceUnity.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Core
{
    public sealed class CombatHudController : MonoBehaviour
    {
        private PrototypeBootstrap bootstrap;
        private bool confirmHeld;
        private GUIStyle label;
        private GUIStyle banner;

        public void Configure(PrototypeBootstrap owner) => bootstrap = owner;

        private void Update()
        {
            if (bootstrap == null || !bootstrap.MatchStarted || (!AllEnemiesDefeated() && !AllPlayersDefeated())) return;
            bool confirm = ControllerDeviceRouter.ReadPlayerConfirm(0) ||
                (Keyboard.current != null && Keyboard.current.enterKey.isPressed);
            if (confirm && !confirmHeld) bootstrap.RestartMatch();
            confirmHeld = confirm;
        }

        private void OnGUI()
        {
            if (bootstrap == null || !bootstrap.MatchStarted) return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            banner ??= new GUIStyle(label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };

            PrototypeFighterController[] players = FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                FighterStateMachine fighter = players[i].GetComponent<FighterStateMachine>();
                DrawHealth(new Rect(24, 22 + i * 46, 300, 24), $"P{i + 1}", fighter.Health, fighter.MaxHealth,
                    new Color(0.15f, 0.75f, 0.95f));
            }

            PrototypeEnemy[] enemies = FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                FighterStateMachine fighter = enemies[i].GetComponent<FighterStateMachine>();
                DrawHealth(new Rect(Screen.width - 324, 22 + i * 38, 300, 20), enemies[i].name,
                    fighter.Health, fighter.MaxHealth, new Color(0.9f, 0.22f, 0.2f));
            }

            StageProgressionController stage = FindFirstObjectByType<StageProgressionController>();
            if (stage != null && !stage.IsComplete)
                GUI.Label(new Rect(Screen.width * 0.38f, 18, Screen.width * 0.24f, 32), $"WAVE {stage.CurrentWave} / {stage.WaveCount}", banner);

            bool victory = AllEnemiesDefeated();
            bool defeat = AllPlayersDefeated();
            if (!victory && !defeat) return;
            DrawSolid(new Rect(0, Screen.height * 0.34f, Screen.width, Screen.height * 0.28f), new Color(0.02f, 0.03f, 0.08f, 0.9f));
            GUI.Label(new Rect(0, Screen.height * 0.39f, Screen.width, 54), victory ? "STAGE CLEAR!" : "TRY AGAIN", banner);
            GUI.Label(new Rect(0, Screen.height * 0.51f, Screen.width, 38), "Press X / Confirm to restart", banner);
        }

        private void DrawHealth(Rect rect, string title, int health, int maximum, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y - 19, rect.width, 20), $"{title}  {health}", label);
            DrawSolid(rect, new Color(0.08f, 0.09f, 0.12f, 0.95f));
            DrawSolid(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(health / (float)maximum), rect.height - 4), color);
        }

        private static bool AllEnemiesDefeated()
        {
            StageProgressionController stage = FindFirstObjectByType<StageProgressionController>();
            if (stage != null) return stage.IsComplete;
            PrototypeEnemy[] enemies = FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None);
            if (enemies.Length == 0) return false;
            foreach (PrototypeEnemy enemy in enemies)
                if (!enemy.GetComponent<FighterStateMachine>().IsDefeated) return false;
            return true;
        }

        private static bool AllPlayersDefeated()
        {
            PrototypeFighterController[] players = FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None);
            if (players.Length == 0) return false;
            foreach (PrototypeFighterController player in players)
                if (!player.GetComponent<FighterStateMachine>().IsDefeated) return false;
            return true;
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
