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
                return Mathf.Max(Controllers.Count, LegacyControllerCount);
            }
        }

        public static int LegacyControllerCount
        {
            get
            {
                string[] names = UnityEngine.Input.GetJoystickNames();
                int count = 0;
                foreach (string name in names)
                    if (!string.IsNullOrWhiteSpace(name)) count++;
                return count;
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

            if (device == null) return Vector2.zero;

            Vector2Control stick = device.TryGetChildControl<Vector2Control>("leftStick") ??
                device.TryGetChildControl<Vector2Control>("stick") ??
                device.TryGetChildControl<Vector2Control>("dpad");
            if (stick != null)
                return Vector2.ClampMagnitude(stick.ReadValue(), 1f);

            return Vector2.zero;
        }

        public static bool ReadConfirm(InputDevice device)
        {
            if (device is Gamepad gamepad)
                return gamepad.buttonSouth.isPressed;

            if (device is Joystick joystick)
                return joystick.trigger != null && joystick.trigger.isPressed;

            if (device != null)
            {
                ButtonControl south = device.TryGetChildControl<ButtonControl>("buttonSouth") ??
                    device.TryGetChildControl<ButtonControl>("button0") ??
                    device.TryGetChildControl<ButtonControl>("trigger");
                return south != null && south.isPressed;
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

        public static Vector2 ReadLegacyMovement(int playerIndex)
        {
            if (playerIndex != 0 || LegacyControllerCount == 0) return Vector2.zero;
            Vector2 stick = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));
            Vector2 dpad = new Vector2(
                UnityEngine.Input.GetAxisRaw("FFU DPad Axis 8"),
                -UnityEngine.Input.GetAxisRaw("Debug Horizontal"));
            return Vector2.ClampMagnitude(dpad.sqrMagnitude > stick.sqrMagnitude ? dpad : stick, 1f);
        }

        public static bool ReadLegacyConfirm(int playerIndex) =>
            playerIndex == 0 && LegacyControllerCount > 0 &&
            UnityEngine.Input.GetKey(KeyCode.JoystickButton0);

        public static bool ReadLegacyCancel(int playerIndex) =>
            playerIndex == 0 && LegacyControllerCount > 0 && UnityEngine.Input.GetKey(KeyCode.JoystickButton1);

        public static bool ReadLegacyDiagnostics(int playerIndex) =>
            playerIndex == 0 && LegacyControllerCount > 0 &&
            (UnityEngine.Input.GetKey(KeyCode.JoystickButton9) || UnityEngine.Input.GetKey(KeyCode.JoystickButton11));

        public static string[] GetLegacyControllerNames() => UnityEngine.Input.GetJoystickNames();

        public static string DescribeLegacyState()
        {
            float axis6 = UnityEngine.Input.GetAxisRaw("Debug Horizontal");
            float axis7 = UnityEngine.Input.GetAxisRaw("Debug Vertical");
            float axis8 = UnityEngine.Input.GetAxisRaw("FFU DPad Axis 8");
            float axis9 = UnityEngine.Input.GetAxisRaw("FFU DPad Axis 9");
            float axis10 = UnityEngine.Input.GetAxisRaw("FFU DPad Axis 10");
            string pressed = "none";
            for (int i = 0; i < 20; i++)
            {
                var key = (KeyCode)((int)KeyCode.JoystickButton0 + i);
                if (!UnityEngine.Input.GetKey(key)) continue;
                pressed = pressed == "none" ? i.ToString() : $"{pressed},{i}";
            }
            return $"RAW axes 6:{axis6:0.00} 7:{axis7:0.00} 8:{axis8:0.00} 9:{axis9:0.00} 10:{axis10:0.00} | buttons:{pressed}";
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

        public static bool IsControllerLike(InputDevice device)
        {
            if (device is Gamepad or Joystick) return true;
            if (device is Keyboard or Mouse or Touchscreen or Pointer) return false;
            if (device == null) return false;

            string identity = $"{device.description.manufacturer} {device.description.product} {device.displayName} {device.layout}".ToLowerInvariant();
            if (identity.Contains("sony") || identity.Contains("playstation") || identity.Contains("dualsense") ||
                identity.Contains("dualshock") || identity.Contains("wireless controller") || identity.Contains("xbox") ||
                identity.Contains("nintendo") || identity.Contains("joy-con"))
                return true;

            return device.TryGetChildControl<Vector2Control>("leftStick") != null ||
                device.TryGetChildControl<Vector2Control>("stick") != null;
        }

        private static void Refresh()
        {
            Controllers.Clear();
            foreach (InputDevice device in InputSystem.devices)
            {
                if (IsControllerLike(device))
                    Controllers.Add(device);
            }

            Controllers.Sort((left, right) => left.deviceId.CompareTo(right.deviceId));
        }
    }
}
