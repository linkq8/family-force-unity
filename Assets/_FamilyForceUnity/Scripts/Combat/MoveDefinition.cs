using System;
using UnityEngine;

namespace FamilyForceUnity.Combat
{
    public enum MoveId
    {
        Punch,
        Kick,
        HeavyPunch,
        HeavyKick,
        Jump,
        Special,
        Link,
        Grab,
        Throw
    }

    [Serializable]
    public struct HitRegion
    {
        public Vector2 Center;
        public Vector2 Size;
    }

    [CreateAssetMenu(menuName = "Family Force Unity/Combat/Move", fileName = "Move_")]
    public sealed class MoveDefinition : ScriptableObject
    {
        [SerializeField] private MoveId id;
        [Min(0), SerializeField] private int startupTicks = 5;
        [Min(1), SerializeField] private int activeTicks = 3;
        [Min(0), SerializeField] private int recoveryTicks = 8;
        [Min(0), SerializeField] private int damage = 10;
        [Min(0), SerializeField] private int hitPauseTicks = 4;
        [SerializeField] private Vector2 knockback = new(1.5f, 0.2f);
        [SerializeField] private HitRegion hitRegion = new() { Center = new Vector2(0.7f, 0.5f), Size = new Vector2(0.8f, 0.7f) };

        public MoveId Id => id;
        public int StartupTicks => startupTicks;
        public int ActiveTicks => activeTicks;
        public int RecoveryTicks => recoveryTicks;
        public int TotalTicks => startupTicks + activeTicks + recoveryTicks;
        public int Damage => damage;
        public int HitPauseTicks => hitPauseTicks;
        public Vector2 Knockback => knockback;
        public HitRegion HitRegion => hitRegion;

        public bool IsActiveAt(int elapsedTick) =>
            elapsedTick >= startupTicks && elapsedTick < startupTicks + activeTicks;

        public static MoveDefinition CreateRuntimePunch()
        {
            var move = CreateInstance<MoveDefinition>();
            move.id = MoveId.Punch;
            move.startupTicks = 5;
            move.activeTicks = 3;
            move.recoveryTicks = 8;
            move.damage = 10;
            move.hitPauseTicks = 4;
            move.knockback = new Vector2(1.5f, 0.2f);
            move.name = "Runtime_Punch";
            return move;
        }

#if UNITY_EDITOR
        public void Configure(MoveId moveId, int startup, int active, int recovery, int moveDamage, int pause, Vector2 force)
        {
            id = moveId;
            startupTicks = Mathf.Max(0, startup);
            activeTicks = Mathf.Max(1, active);
            recoveryTicks = Mathf.Max(0, recovery);
            damage = Mathf.Max(0, moveDamage);
            hitPauseTicks = Mathf.Max(0, pause);
            knockback = force;
        }
#endif
    }
}
