using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FamilyForceUnity.Input
{
    public static class ControllerDeviceRouter
    {
        private static readonly List<InputDevice> Controllers = new();

        public static int ControllerCount
        {
            get
            {
                Refresh();
                return Controllers.Count;
            }
        }

        public static InputDevice GetController(int playerIndex)
        {
            Refresh();
            return playerIndex >= 0 && playerIndex < Controllers.Count ? Controllers[playerIndex] : null;
        }

        public static Vector2 ReadMovement(InputDevice device)
        {
            if (device is Gamepad gamepad)
            {
                Vector2 value = gamepad.leftStick.ReadValue();
                Vector2 dpad = gamepad.dpad.ReadValue();
                return Vector2.ClampMagnitude(dpad.sqrMagnitude > value.sqrMagnitude ? dpad : value, 1f);
            }

            if (device is Joystick joystick)
                return Vector2.ClampMagnitude(joystick.stick.ReadValue(), 1f);

            return Vector2.zero;
        }

        public static bool ReadConfirm(InputDevice device)
        {
            if (device is Gamepad gamepad)
                return gamepad.buttonSouth.isPressed || gamepad.startButton.isPressed;

            if (device is Joystick joystick)
            {
                if (joystick.trigger != null && joystick.trigger.isPressed)
                    return true;

                foreach (InputControl control in joystick.allControls)
                {
                    if (control is ButtonControl button && button.isPressed)
                        return true;
                }
            }

            return false;
        }

        public static bool ReadCancel(InputDevice device)
        {
            if (device is Gamepad gamepad)
                return gamepad.buttonEast.isPressed || gamepad.selectButton.isPressed;

            if (device is Joystick joystick)
                return joystick.trigger != null && joystick.trigger.isPressed;

            return false;
        }

        public static string Describe(InputDevice device)
        {
            if (device == null) return "Not assigned";
            string manufacturer = string.IsNullOrWhiteSpace(device.description.manufacturer)
                ? "Unknown maker"
                : device.description.manufacturer;
            string product = string.IsNullOrWhiteSpace(device.description.product)
                ? device.displayName
                : device.description.product;
            return $"{manufacturer} / {product} | layout={device.layout} | id={device.deviceId}";
        }

        private static void Refresh()
        {
            Controllers.Clear();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Gamepad or Joystick)
                    Controllers.Add(device);
            }

            Controllers.Sort((left, right) => left.deviceId.CompareTo(right.deviceId));
        }
    }
}
