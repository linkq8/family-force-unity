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
        private LaneMotor motor;
        private FighterStateMachine fighter;
        private bool attackHeld;

        private void Awake()
        {
            motor = GetComponent<LaneMotor>();
            fighter = GetComponent<FighterStateMachine>();
        }

        public void Configure(int index, MoveDefinition attack)
        {
            playerIndex = Mathf.Clamp(index, 0, 1);
            lightAttack = attack;
        }

        private void FixedUpdate()
        {
            Vector2 movement = ReadMovement();
            bool canWalk = fighter.State is FighterState.Idle or FighterState.Walk;
            if (canWalk)
                motor.SimulateMove(movement);
            fighter.SetWalking(canWalk && movement.sqrMagnitude > 0.01f);

            bool attackPressed = ReadAttack();
            if (attackPressed && !attackHeld)
                fighter.TryAttack(lightAttack);
            attackHeld = attackPressed;
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

            InputDevice controller = ControllerDeviceRouter.GetController(playerIndex);
            Vector2 controllerValue = ControllerDeviceRouter.ReadMovement(controller);
            if (controllerValue.sqrMagnitude > value.sqrMagnitude)
                value = controllerValue;

            Vector2 legacyValue = ControllerDeviceRouter.ReadLegacyMovement(playerIndex);
            if (legacyValue.sqrMagnitude > value.sqrMagnitude)
                value = legacyValue;

            return Vector2.ClampMagnitude(value, 1f);
        }

        private bool ReadAttack()
        {
            Keyboard keyboard = Keyboard.current;
            bool keyboardPressed = keyboard != null &&
                (playerIndex == 0 ? keyboard.spaceKey.isPressed : keyboard.enterKey.isPressed);
            bool controllerPressed = ControllerDeviceRouter.ReadConfirm(ControllerDeviceRouter.GetController(playerIndex));
            return keyboardPressed || controllerPressed || ControllerDeviceRouter.ReadLegacyConfirm(playerIndex);
        }
    }
}
