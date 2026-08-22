using System.Collections.Generic;
using FamilyForceUnity.AI;
using FamilyForceUnity.Characters;
using FamilyForceUnity.Input;
using UnityEngine;

namespace FamilyForceUnity.Core
{
    public sealed class StageProgressionController : MonoBehaviour
    {
        private readonly List<List<GameObject>> waves = new();
        private int waveIndex;
        private Camera stageCamera;
        private GameObject gate;
        private float startedAt;
        private float activateNextAt = -1f;
        private bool waitingForNextWave;
        private float announcementUntil;
        public bool IsComplete { get; private set; }
        public int CurrentWave => Mathf.Min(waveIndex + 1, waves.Count);
        public int WaveCount => waves.Count;
        public float ElapsedSeconds => Time.time - startedAt;
        public bool IsWaveIncoming => waitingForNextWave;

        public void Configure(Camera camera, GameObject stageGate, params List<GameObject>[] configuredWaves)
        {
            waves.Clear(); waves.AddRange(configuredWaves); waveIndex = 0; IsComplete = false; stageCamera = camera; gate = stageGate; startedAt = Time.time; announcementUntil = Time.time + 2.5f;
            for (int i = 0; i < waves.Count; i++)
                foreach (GameObject enemy in waves[i]) enemy.SetActive(i == 0);
        }

        private void LateUpdate()
        {
            PrototypeFighterController[] players = FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None);
            int movementWave = waitingForNextWave ? Mathf.Max(0, waveIndex - 1) : waveIndex;
            float stageMaximum = movementWave == 0 ? 1.65f : movementWave == 1 ? 8.15f : 14.5f;
            for (int i = 0; i < players.Length; i++)
            {
                float minimum = -8.5f;
                float maximum = stageMaximum;
                if (players.Length > 1)
                {
                    PrototypeFighterController other = players[i == 0 ? 1 : 0];
                    minimum = Mathf.Max(minimum, other.transform.position.x - 4.8f);
                    maximum = Mathf.Min(maximum, other.transform.position.x + 4.8f);
                }
                players[i].GetComponent<LaneMotor>().SetHorizontalBounds(minimum, maximum);
            }
            if (stageCamera != null && players.Length > 0)
            {
                float average = 0f;
                foreach (var player in players) average += player.transform.position.x;
                average /= players.Length;
                Vector3 target = new(Mathf.Clamp(average + 1.25f, -7.5f, 9.5f), 0f, -10f);
                stageCamera.transform.position = Vector3.Lerp(stageCamera.transform.position, target, 0.07f);
            }
            if (waitingForNextWave)
            {
                if (Time.time < activateNextAt) return;
                waitingForNextWave = false;
                announcementUntil = Time.time + (waveIndex == waves.Count - 1 ? 3.5f : 2.2f);
                if (gate != null)
                {
                    if (waveIndex == 1) { gate.transform.position = new Vector3(8.35f, -0.2f, -0.2f); gate.SetActive(true); }
                    else gate.SetActive(false);
                }
                foreach (GameObject enemy in waves[waveIndex]) enemy.SetActive(true);
                return;
            }
            if (IsComplete || waves.Count == 0 || !WaveDefeated(waves[waveIndex])) return;
            waveIndex++;
            if (waveIndex >= waves.Count) { IsComplete = true; return; }
            foreach (PrototypeFighterController player in players)
                player.GetComponent<FighterStateMachine>()?.Heal(22);
            if (gate != null) gate.SetActive(false);
            waitingForNextWave = true;
            activateNextAt = Time.time + 2.5f;
        }

        private void OnGUI()
        {
            if (IsComplete || Time.time > announcementUntil) return;
            GUIStyle style = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold,
                normal = { textColor = waveIndex == waves.Count - 1 ? new Color(1f, 0.28f, 0.16f) : new Color(1f, 0.75f, 0.14f) } };
            string text = waveIndex == waves.Count - 1 ? GameLocalization.T("BOSS — KHALID, NEON CAPTAIN", "الزعيم — خالد، قائد النيون") :
                GameLocalization.T($"WAVE {waveIndex + 1}", $"الموجة {waveIndex + 1}");
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 64), text, style);
        }

        private static bool WaveDefeated(List<GameObject> wave)
        {
            foreach (GameObject enemy in wave)
                if (enemy != null && !enemy.GetComponent<FighterStateMachine>().IsDefeated) return false;
            return true;
        }
    }
}
