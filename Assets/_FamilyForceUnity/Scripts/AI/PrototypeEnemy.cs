using FamilyForceUnity.Characters;
using UnityEngine;

namespace FamilyForceUnity.AI
{
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        private LaneMotor motor;
        private AttackTokenManager tokens;
        private Transform target;
        private bool ownsToken;

        private void Start()
        {
            motor = GetComponent<LaneMotor>();
            tokens = FindFirstObjectByType<AttackTokenManager>();
            var player = GameObject.Find("Essa — P1");
            target = player != null ? player.transform : null;
        }

        private void FixedUpdate()
        {
            if (target == null || tokens == null) return;

            Vector2 delta = target.position - transform.position;
            float distance = delta.magnitude;
            if (distance < 1.25f)
            {
                ownsToken |= tokens.TryAcquire(this);
                motor.SimulateMove(Vector2.zero);
            }
            else
            {
                if (ownsToken)
                {
                    tokens.Release(this);
                    ownsToken = false;
                }
                motor.SimulateMove(delta.normalized * 0.45f);
            }
        }

        private void OnDisable()
        {
            if (ownsToken && tokens != null) tokens.Release(this);
        }
    }
}

