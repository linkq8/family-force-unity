using FamilyForceUnity.Characters;
using FamilyForceUnity.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Input
{
    public sealed class PrototypeFighterController : MonoBehaviour
    {
        private int playerIndex;
        private MoveDefinition lightAttack;
        private MoveDefinition comboPunchTwo;
        private MoveDefinition comboPunchThree;
        private MoveDefinition kick;
        private MoveDefinition heavyAttack;
        private MoveDefinition jump;
        private MoveDefinition special;
        private LaneMotor motor;
        private FighterStateMachine fighter;
        private bool attackHeld;
        private MoveDefinition activeInputMove;
        private bool hitApplied;
        public float FacingSign { get; private set; } = 1f;
        public int WeaponBonus { get; private set; }
        public int PlayerIndex => playerIndex;
        private int comboStep;
        private float lastPunchTime = -10f;

        public void EquipTemporaryWeapon(int damageBonus) => WeaponBonus = Mathf.Max(WeaponBonus, damageBonus);

        private void Awake()
        {
            motor = GetComponent<LaneMotor>();
            fighter = GetComponent<FighterStateMachine>();
        }

        public void Configure(int index, MoveDefinition attack, MoveDefinition punchTwo, MoveDefinition punchThree, MoveDefinition kickMove,
            MoveDefinition heavyMove, MoveDefinition jumpMove, MoveDefinition specialMove)
        {
            playerIndex = Mathf.Clamp(index, 0, 1);
            lightAttack = attack;
            comboPunchTwo = punchTwo;
            comboPunchThree = punchThree;
            kick = kickMove;
            heavyAttack = heavyMove;
            jump = jumpMove;
            special = specialMove;
        }

        private void FixedUpdate()
        {
            Vector2 movement = ReadMovement();
            if (Mathf.Abs(movement.x) > 0.1f) FacingSign = Mathf.Sign(movement.x);
            bool canWalk = fighter.State is FighterState.Idle or FighterState.Walk;
            if (canWalk)
                motor.SimulateMove(movement);
            fighter.SetWalking(canWalk && movement.sqrMagnitude > 0.01f);

            MoveDefinition requestedMove = ReadRequestedMove();
            bool attackPressed = requestedMove != null;
            if (attackPressed && !attackHeld && fighter.TryAttack(requestedMove))
            {
                activeInputMove = requestedMove;
                hitApplied = false;
            }
            attackHeld = attackPressed;

            if (!hitApplied && fighter.IsMoveActive)
            {
                ApplyHit(activeInputMove);
                hitApplied = true;
            }

            if (fighter.CurrentMove != null && fighter.CurrentMove.Id == MoveId.Jump)
            {
                float progress = fighter.StateTick / (float)Mathf.Max(1, fighter.CurrentMove.TotalTicks);
                motor.SetVisualHeight(Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 0.9f);
            }
            else motor.SetVisualHeight(0f);
        }

        private Vector2 ReadMovement()
        {
            Vector2 value = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (playerIndex == 0)
                {
                    value.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0) -
                        ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
                    value.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0) -
                        ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0);
                }
                else
                {
                    value.x = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
                    value.y = (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0);
                }
            }

            Vector2 controllerValue = ControllerDeviceRouter.ReadPlayerMovement(playerIndex);
            if (controllerValue.sqrMagnitude > value.sqrMagnitude)
                value = controllerValue;

            return Vector2.ClampMagnitude(value, 1f);
        }

        private MoveDefinition ReadRequestedMove()
        {
            Keyboard keyboard = Keyboard.current;
            bool keyboardPressed = keyboard != null &&
                (playerIndex == 0 ? keyboard.spaceKey.isPressed : keyboard.enterKey.isPressed);
            bool lightPressed = keyboardPressed || ControllerDeviceRouter.ReadPlayerLightAttack(playerIndex);
            if (lightPressed)
            {
                if (!attackHeld)
                {
                    if (Time.fixedTime - lastPunchTime > 0.85f) comboStep = 0;
                    comboStep = comboStep % 3 + 1;
                    lastPunchTime = Time.fixedTime;
                    return comboStep == 1 ? lightAttack : comboStep == 2 ? comboPunchTwo : comboPunchThree;
                }
                return activeInputMove ?? lightAttack;
            }
            if (ControllerDeviceRouter.ReadPlayerKick(playerIndex)) return kick;
            if (ControllerDeviceRouter.ReadPlayerHeavyAttack(playerIndex)) return heavyAttack;
            if (ControllerDeviceRouter.ReadPlayerJump(playerIndex)) return jump;
            if (ControllerDeviceRouter.ReadPlayerSpecial(playerIndex)) return special;
            return null;
        }

        private void ApplyHit(MoveDefinition move)
        {
            if (move == null || move.Id == MoveId.Jump) return;
            float reach = move.Id == MoveId.Special ? 2.2f : move.Id == MoveId.HeavyPunch ? 1.6f : 1.15f;
            foreach (var enemy in FindObjectsByType<FamilyForceUnity.AI.PrototypeEnemy>(FindObjectsSortMode.None))
            {
                Vector2 delta = enemy.transform.position - transform.position;
                float forwardDistance = delta.x * FacingSign;
                if (forwardDistance < -0.15f || forwardDistance > reach || Mathf.Abs(delta.y) > 0.8f) continue;
                var target = enemy.GetComponent<FighterStateMachine>();
                target.ApplyHit(move.Damage + WeaponBonus, move.HitPauseTicks, move.Id is MoveId.HeavyPunch or MoveId.Special);
                enemy.GetComponent<LaneMotor>().ApplyKnockback(new Vector2(move.Knockback.x * 0.22f * FacingSign, move.Knockback.y * 0.12f));
            }

            foreach (var prop in FindObjectsByType<FamilyForceUnity.World.BreakableProp>(FindObjectsSortMode.None))
            {
                Vector2 delta = prop.transform.position - transform.position;
                float forwardDistance = delta.x * FacingSign;
                if (forwardDistance < -0.15f || forwardDistance > reach || Mathf.Abs(delta.y) > 0.85f) continue;
                prop.ApplyHit(move.Damage + WeaponBonus);
            }
        }
    }
}
