using FamilyForceUnity.Combat;
using UnityEngine;

namespace FamilyForceUnity.Characters
{
    public enum FighterState
    {
        Idle,
        Walk,
        Attack,
        Jump,
        Special,
        Link,
        Hurt,
        Knockdown,
        Grab,
        Throw,
        GetUp
    }

    public sealed class FighterStateMachine : MonoBehaviour
    {
        [SerializeField] private FighterState state = FighterState.Idle;
        [SerializeField] private int health = 100;

        private MoveDefinition currentMove;
        private int stateTick;
        private int frozenTicks;

        public FighterState State => state;
        public int Health => health;
        public int StateTick => stateTick;
        public MoveDefinition CurrentMove => currentMove;
        public bool IsMoveActive => state == FighterState.Attack && currentMove != null && currentMove.IsActiveAt(stateTick);

        private void FixedUpdate()
        {
            if (frozenTicks > 0)
            {
                frozenTicks--;
                return;
            }

            stateTick++;
            if (state == FighterState.Attack && currentMove != null && stateTick >= currentMove.TotalTicks)
            {
                Enter(FighterState.Idle);
            }
        }

        public bool TryAttack(MoveDefinition move)
        {
            if (move == null || (state != FighterState.Idle && state != FighterState.Walk))
                return false;

            currentMove = move;
            Enter(FighterState.Attack);
            return true;
        }

        public void ApplyHit(int damage, int hitPauseTicks, bool knockdown)
        {
            health = Mathf.Max(0, health - Mathf.Max(0, damage));
            frozenTicks = Mathf.Max(frozenTicks, hitPauseTicks);
            Enter(knockdown || health == 0 ? FighterState.Knockdown : FighterState.Hurt);
        }

        public void SetWalking(bool walking)
        {
            if (state is FighterState.Idle or FighterState.Walk)
                Enter(walking ? FighterState.Walk : FighterState.Idle);
        }

        private void Enter(FighterState next)
        {
            if (state == next && next != FighterState.Attack)
                return;

            state = next;
            stateTick = 0;
            if (next != FighterState.Attack)
                currentMove = null;
        }
    }
}

