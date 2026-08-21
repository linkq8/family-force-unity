using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FamilyForceUnity.Input
{
    public static class ControllerCalibrationProfile
    {
        public enum Action
        {
            Up,
            Down,
            Left,
            Right,
            Attack
        }

        private const string Prefix = "FFU.Controller.";

        public static bool IsComplete
        {
            get
            {
                foreach (Action action in System.Enum.GetValues(typeof(Action)))
                    if (string.IsNullOrEmpty(PlayerPrefs.GetString(Key(action), string.Empty))) return false;
                return true;
            }
        }

        public static bool TryCapture(out string controlPath, out float direction)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!ControllerDeviceRouter.IsControllerLike(device)) continue;

                foreach (InputControl control in device.allControls)
                {
                    if (control is ButtonControl button && button.isPressed)
                    {
                        controlPath = control.path;
                        direction = 1f;
                        return true;
                    }
                }

                foreach (InputControl control in device.allControls)
                {
                    if (control is AxisControl axis)
                    {
                        if (IsRestingTriggerAxis(control)) continue;
                        float value = axis.ReadValue();
                        if (Mathf.Abs(value) < 0.65f) continue;
                        controlPath = control.path;
                        direction = Mathf.Sign(value);
                        return true;
                    }
                }
            }

            controlPath = null;
            direction = 0f;
            return false;
        }

        public static bool AnyControlActuated()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!ControllerDeviceRouter.IsControllerLike(device)) continue;
                foreach (InputControl control in device.allControls)
                {
                    if (control is ButtonControl button && button.isPressed) return true;
                    if (control is AxisControl axis && !IsRestingTriggerAxis(control) &&
                        Mathf.Abs(axis.ReadValue()) >= 0.35f) return true;
                }
            }
            return false;
        }

        public static void Save(Action action, string path, float direction)
        {
            PlayerPrefs.SetString(Key(action), path);
            PlayerPrefs.SetFloat(DirectionKey(action), direction);
            PlayerPrefs.Save();
        }

        public static Vector2 ReadMovement()
        {
            if (!IsComplete) return Vector2.zero;
            float x = (Read(Action.Right) ? 1f : 0f) - (Read(Action.Left) ? 1f : 0f);
            float y = (Read(Action.Up) ? 1f : 0f) - (Read(Action.Down) ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static bool ReadAttack() => IsComplete && Read(Action.Attack);

        private static bool Read(Action action)
        {
            string path = PlayerPrefs.GetString(Key(action), string.Empty);
            if (string.IsNullOrEmpty(path)) return false;
            float expectedDirection = PlayerPrefs.GetFloat(DirectionKey(action), 1f);

            foreach (InputDevice device in InputSystem.devices)
            {
                if (!ControllerDeviceRouter.IsControllerLike(device)) continue;
                foreach (InputControl control in device.allControls)
                {
                    if (control.path != path) continue;
                    if (control is ButtonControl button) return button.isPressed;
                    if (control is AxisControl axis) return axis.ReadValue() * expectedDirection > 0.55f;
                }
            }
            return false;
        }

        private static string Key(Action action) => Prefix + action + ".Path";
        private static string DirectionKey(Action action) => Prefix + action + ".Direction";

        private static bool IsRestingTriggerAxis(InputControl control)
        {
            string name = control.name.ToLowerInvariant();
            return name.Contains("trigger") || name.Contains("brake") || name.Contains("gas");
        }
    }
}
