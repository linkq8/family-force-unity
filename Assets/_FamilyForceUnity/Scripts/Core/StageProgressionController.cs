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
        public bool IsComplete { get; private set; }
        public int CurrentWave => Mathf.Min(waveIndex + 1, waves.Count);
        public int WaveCount => waves.Count;

        public void Configure(Camera camera, GameObject stageGate, params List<GameObject>[] configuredWaves)
        {
            waves.Clear(); waves.AddRange(configuredWaves); waveIndex = 0; IsComplete = false; stageCamera = camera; gate = stageGate;
            for (int i = 0; i < waves.Count; i++)
                foreach (GameObject enemy in waves[i]) enemy.SetActive(i == 0);
        }

        private void LateUpdate()
        {
            PrototypeFighterController[] players = FindObjectsByType<PrototypeFighterController>(FindObjectsSortMode.None);
            foreach (var player in players)
                player.GetComponent<LaneMotor>().SetHorizontalBounds(-8.5f, waveIndex == 0 ? 2.65f : 8.5f);
            if (stageCamera != null && players.Length > 0)
            {
                float average = 0f;
                foreach (var player in players) average += player.transform.position.x;
                average /= players.Length;
                Vector3 target = new(Mathf.Clamp(average + 1.25f, -3.6f, 3.6f), 0f, -10f);
                stageCamera.transform.position = Vector3.Lerp(stageCamera.transform.position, target, 0.07f);
            }
            if (IsComplete || waves.Count == 0 || !WaveDefeated(waves[waveIndex])) return;
            waveIndex++;
            if (waveIndex >= waves.Count) { IsComplete = true; return; }
            if (gate != null) gate.SetActive(false);
            foreach (GameObject enemy in waves[waveIndex]) enemy.SetActive(true);
        }

        private static bool WaveDefeated(List<GameObject> wave)
        {
            foreach (GameObject enemy in wave)
                if (enemy != null && !enemy.GetComponent<FighterStateMachine>().IsDefeated) return false;
            return true;
        }
    }
}
