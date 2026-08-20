using System;
using UnityEngine;

namespace FamilyForceUnity.Core
{
    public sealed class SimulationClock : MonoBehaviour
    {
        public const int TickRate = 60;
        public const float TickDuration = 1f / TickRate;

        public static SimulationClock Instance { get; private set; }
        public ulong Tick { get; private set; }
        public event Action<ulong> Stepped;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Time.fixedDeltaTime = TickDuration;
            Time.maximumDeltaTime = TickDuration * 4f;
            Application.targetFrameRate = TickRate;
            QualitySettings.vSyncCount = 0;
        }

        private void FixedUpdate()
        {
            Tick++;
            Stepped?.Invoke(Tick);
        }
    }
}

