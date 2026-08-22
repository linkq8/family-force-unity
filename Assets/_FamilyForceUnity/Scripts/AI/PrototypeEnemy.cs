using FamilyForceUnity.Characters;
using FamilyForceUnity.Input;
using FamilyForceUnity.Combat;
using UnityEngine;

namespace FamilyForceUnity.AI
{
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        private LaneMotor motor;
        private AttackTokenManager tokens;
        private Transform target;
        private bool ownsToken;
        private FighterStateMachine fighter;
        private FighterStateMachine targetFighter;
        private MoveDefinition attack;
        private bool hitApplied;
        private int attackCooldown;
        private int attackDamage = 7;
        private int cooldownTicks = 75;

        public void Configure(float movementSpeed, int damage, int cooldown)
        {
            attackDamage = Mathf.Max(1, damage);
            cooldownTicks = Mathf.Max(30, cooldown);
            GetComponent<LaneMotor>()?.SetSpeed(movementSpeed);
            if (attack != null)
            {
                Destroy(attack);
                attack = CreateAttack();
            }
        }

        private void Start()
        {
            motor = GetComponent<LaneMotor>();
            fighter = GetComponent<FighterStateMachine>();
            tokens = FindFirstObjectByType<AttackTokenManager>();
            var player = FindFirstObjectByType<PrototypeFighterController>();
            target = player != null ? player.transform : null;
            targetFighter = player != null ? player.GetComponent<FighterStateMachine>() : null;
            attack = CreateAttack();
        }

        private void FixedUpdate()
        {
            if (target == null || tokens == null) return;
            if (fighter.IsDefeated)
            {
                motor.SimulateMove(Vector2.zero);
                if (ownsToken) { tokens.Release(this); ownsToken = false; }
                return;
            }

            if (attackCooldown > 0) attackCooldown--;
            if (!hitApplied && fighter.IsMoveActive && targetFighter != null)
            {
                Vector2 hitDelta = target.position - transform.position;
                if (Mathf.Abs(hitDelta.x) < 1.35f && Mathf.Abs(hitDelta.y) < 0.7f)
                    targetFighter.ApplyHit(attack.Damage, attack.HitPauseTicks, false);
                hitApplied = true;
            }

            Vector2 delta = target.position - transform.position;
            float distance = delta.magnitude;
            if (distance < 1.25f)
            {
                ownsToken |= tokens.TryAcquire(this);
                motor.SimulateMove(Vector2.zero);
                fighter.SetWalking(false);
                if (ownsToken && attackCooldown == 0 && fighter.TryAttack(attack))
                {
                    hitApplied = false;
                    attackCooldown = cooldownTicks;
                }
            }
            else
            {
                if (ownsToken)
                {
                    tokens.Release(this);
                    ownsToken = false;
                }
                motor.SimulateMove(delta.normalized * 0.45f);
                fighter.SetWalking(true);
            }
        }

        private void OnDisable()
        {
            if (ownsToken && tokens != null) tokens.Release(this);
        }

        private void OnDestroy()
        {
            if (attack != null) Destroy(attack);
        }

        private MoveDefinition CreateAttack() =>
            MoveDefinition.CreateRuntime(MoveId.Punch, 12, 4, 20, attackDamage, 3, new Vector2(0.7f, 0.12f));
    }
}
