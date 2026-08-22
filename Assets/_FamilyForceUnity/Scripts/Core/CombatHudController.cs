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
        private GUIStyle enemyLabel;
        private GUIStyle controlsHint;
        private bool joinHeld;

        public void Configure(PrototypeBootstrap owner) => bootstrap = owner;

        private void Update()
        {
            bool join = bootstrap != null && bootstrap.MatchStarted && ControllerDeviceRouter.ControllerCount >= 2 &&
                ControllerDeviceRouter.ReadPlayerConfirm(1);
            if (join && !joinHeld) bootstrap.JoinPlayerTwo();
            joinHeld = join;
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
            enemyLabel ??= new GUIStyle(label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            controlsHint ??= new GUIStyle(label) { fontSize = 12, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.86f, 0.96f) } };

            PrototypeFighterController[] players = FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                FighterStateMachine fighter = players[i].GetComponent<FighterStateMachine>();
                DrawHealth(new Rect(24, 22 + i * 46, 300, 24), $"P{i + 1}", fighter.Health, fighter.MaxHealth,
                    new Color(0.15f, 0.75f, 0.95f));
                LinkCompanionAssist link = players[i].GetComponent<LinkCompanionAssist>();
                string linkStatus = link != null && link.CooldownRemaining > 0f ? $"LINK {link.CooldownRemaining:0.0}s" : "LINK READY";
                string weapon = players[i].WeaponBonus > 0 ? $"  WEAPON +{players[i].WeaponBonus}" : "";
                string action = Time.time < players[i].ActionLabelUntil ? $"  {players[i].LastActionLabel}" : "";
                GUI.Label(new Rect(330, 22 + i * 46, 360, 24), linkStatus + weapon + action, label);
            }
            bool hasP2 = false;
            foreach (var player in players) if (player.PlayerIndex == 1) hasP2 = true;
            if (!hasP2 && ControllerDeviceRouter.ControllerCount >= 2)
                GUI.Label(new Rect(Screen.width * 0.3f, Screen.height - 50, Screen.width * 0.4f, 34), GameLocalization.T("P2 — PRESS X TO JOIN", "اللاعب 2 — اضغط X للانضمام"), banner);

            PrototypeEnemy[] enemies = FindObjectsByType<PrototypeEnemy>(FindObjectsSortMode.None);
            int enemyRow = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].GetComponent<BossPhaseController>() != null) continue;
                FighterStateMachine fighter = enemies[i].GetComponent<FighterStateMachine>();
                Rect enemyRect = new(Screen.width - 254, 28 + enemyRow * 34, 230, 14);
                GUI.Label(new Rect(enemyRect.x, enemyRect.y - 15, enemyRect.width, 16), $"{ShortEnemyName(enemies[i].name)}  {fighter.Health}", enemyLabel);
                DrawBar(enemyRect, fighter.Health, fighter.MaxHealth, new Color(0.9f, 0.22f, 0.2f));
                enemyRow++;
            }
            BossPhaseController boss = FindFirstObjectByType<BossPhaseController>();
            if (boss != null)
            {
                FighterStateMachine bossFighter = boss.GetComponent<FighterStateMachine>();
                DrawHealth(new Rect(Screen.width * 0.25f, Screen.height - 42f, Screen.width * 0.5f, 24f),
                    GameLocalization.T("BOSS — KHALID", "الزعيم — خالد"), bossFighter.Health, bossFighter.MaxHealth,
                    new Color(0.95f, 0.12f, 0.08f));
            }
            GUI.Label(new Rect(Screen.width * 0.12f, Screen.height - 76f, Screen.width * 0.76f, 22f),
                "SQUARE Punch   TRIANGLE Kick   CIRCLE Heavy   X Jump   R1 Special   L1/L2/R2 Link   START Pause", controlsHint);

            StageProgressionController stage = FindFirstObjectByType<StageProgressionController>();
            if (stage != null && !stage.IsComplete)
                GUI.Label(new Rect(Screen.width * 0.38f, 105, Screen.width * 0.24f, 32), GameLocalization.T($"WAVE {stage.CurrentWave} / {stage.WaveCount}", $"الموجة {stage.CurrentWave} / {stage.WaveCount}"), banner);

            bool victory = AllEnemiesDefeated();
            bool defeat = AllPlayersDefeated();
            if (!victory && !defeat) return;
            DrawSolid(new Rect(0, Screen.height * 0.34f, Screen.width, Screen.height * 0.28f), new Color(0.02f, 0.03f, 0.08f, 0.9f));
            GUI.Label(new Rect(0, Screen.height * 0.39f, Screen.width, 54), victory ? GameLocalization.T("MISSION COMPLETE!", "اكتملت المهمة!") : GameLocalization.T("TRY AGAIN", "حاول مرة أخرى"), banner);
            string result = victory && stage != null ? $"{Mathf.FloorToInt(stage.ElapsedSeconds / 60f):00}:{Mathf.FloorToInt(stage.ElapsedSeconds % 60f):00}   " : "";
            GUI.Label(new Rect(0, Screen.height * 0.51f, Screen.width, 38), result + GameLocalization.T("Press X / Confirm to restart", "اضغط X لإعادة المهمة"), banner);
        }

        private void DrawHealth(Rect rect, string title, int health, int maximum, Color color)
        {
            GUI.Label(new Rect(rect.x, rect.y - 19, rect.width, 20), $"{title}  {health}", label);
            DrawBar(rect, health, maximum, color);
        }

        private static void DrawBar(Rect rect, int health, int maximum, Color color)
        {
            DrawSolid(rect, new Color(0.08f, 0.09f, 0.12f, 0.95f));
            DrawSolid(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(health / (float)maximum), rect.height - 4), color);
        }

        private static string ShortEnemyName(string value)
        {
            if (value.StartsWith("Alley")) return "RUNNER";
            if (value.StartsWith("Neon")) return "NEON GUARD";
            if (value.StartsWith("Dock")) return "BRUISER";
            if (value.StartsWith("Captain")) return "ELITE";
            return value.Replace(" — Street Guard", "");
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
