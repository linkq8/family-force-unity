using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FamilyForceUnity.Input
{
    public sealed class AndroidNativeInputBridge : MonoBehaviour
    {
        private sealed class ControllerState
        {
            public Vector2 LeftStick;
            public Vector2 Dpad;
            public readonly HashSet<int> Buttons = new();
            public string HostFamily = "ANDROID";
            public string ControllerFamily = "GENERIC";
            public string Description = "Android controller";
        }

        private static readonly Dictionary<int, ControllerState> States = new();
        private static readonly List<int> PlayerDevices = new();

        public static int ControllerCount => PlayerDevices.Count;
        public static bool IsActive => PlayerDevices.Count > 0;

        private void Awake()
        {
            gameObject.name = "Android Input Bridge";
            DontDestroyOnLoad(gameObject);
        }

        public void OnNativeMotion(string payload)
        {
            string[] values = payload.Split('|');
            if (values.Length < 9 || !int.TryParse(values[0], out int deviceId)) return;
            ControllerState state = GetOrAdd(deviceId);
            state.LeftStick = new Vector2(Parse(values[1]), -Parse(values[2]));
            state.Dpad = new Vector2(Parse(values[3]), -Parse(values[4]));
        }

        public void OnNativeKey(string payload)
        {
            string[] values = payload.Split('|');
            if (values.Length < 3 || !int.TryParse(values[0], out int deviceId) ||
                !int.TryParse(values[1], out int keyCode)) return;
            ControllerState state = GetOrAdd(deviceId);
            bool pressed = values[2] == "1";
            if (pressed) state.Buttons.Add(keyCode);
            else state.Buttons.Remove(keyCode);

            if (keyCode == 19) SetDpadKey(state, Vector2.up, pressed);
            else if (keyCode == 20) SetDpadKey(state, Vector2.down, pressed);
            else if (keyCode == 21) SetDpadKey(state, Vector2.left, pressed);
            else if (keyCode == 22) SetDpadKey(state, Vector2.right, pressed);
        }

        public void OnNativeDevice(string payload)
        {
            string[] values = payload.Split('|');
            if (values.Length < 8 || !int.TryParse(values[0], out int deviceId)) return;
            ControllerState state = GetOrAdd(deviceId);
            state.HostFamily = values[1];
            string name = values[4];
            int.TryParse(values[5], out int vendorId);
            string identity = $"{name} {values[7]}".ToLowerInvariant();
            state.ControllerFamily = vendorId == 1356 || identity.Contains("sony") ||
                identity.Contains("dualsense") || identity.Contains("wireless controller") ? "PLAYSTATION" :
                identity.Contains("xbox") || identity.Contains("microsoft") ? "XBOX" :
                vendorId == 1406 || identity.Contains("nintendo") || identity.Contains("joy-con") ? "NINTENDO" :
                "GENERIC";
            state.Description = $"{state.HostFamily}: {state.ControllerFamily} / {name}";
        }

        public static Vector2 ReadMovement(int playerIndex)
        {
            ControllerState state = GetPlayerState(playerIndex);
            if (state == null) return Vector2.zero;
            Vector2 value = state.Dpad.sqrMagnitude > state.LeftStick.sqrMagnitude ? state.Dpad : state.LeftStick;
            return Vector2.ClampMagnitude(value, 1f);
        }

        public static bool ReadSouth(int playerIndex) => ReadButton(playerIndex, 96);
        public static bool ReadEast(int playerIndex) => ReadButton(playerIndex, 97);
        public static bool ReadNorth(int playerIndex) => ReadButton(playerIndex, 99);
        public static bool ReadWest(int playerIndex) => ReadButton(playerIndex, 100);
        public static bool ReadL3(int playerIndex) => ReadButton(playerIndex, 106);
        public static bool ReadR3(int playerIndex) => ReadButton(playerIndex, 107);
        public static bool ReadL1(int playerIndex) => ReadButton(playerIndex, 102);
        public static bool ReadR1(int playerIndex) => ReadButton(playerIndex, 103);
        public static bool ReadL2(int playerIndex) => ReadButton(playerIndex, 104);
        public static bool ReadR2(int playerIndex) => ReadButton(playerIndex, 105);
        public static bool ReadStart(int playerIndex) => ReadButton(playerIndex, 108);
        public static bool ReadSelect(int playerIndex) => ReadButton(playerIndex, 109);

        public static bool UsesXiaomiMapping(int playerIndex)
        {
            ControllerState state = GetPlayerState(playerIndex);
            return state != null && state.HostFamily == "XIAOMI_TV" && state.ControllerFamily == "PLAYSTATION";
        }

        public static string DescribePlayer(int playerIndex)
        {
            ControllerState state = GetPlayerState(playerIndex);
            return state?.Description ?? "Waiting for Android controller input";
        }

        private static bool ReadButton(int playerIndex, int keyCode)
        {
            ControllerState state = GetPlayerState(playerIndex);
            return state != null && state.Buttons.Contains(keyCode);
        }

        private static ControllerState GetPlayerState(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= PlayerDevices.Count) return null;
            return States.TryGetValue(PlayerDevices[playerIndex], out ControllerState state) ? state : null;
        }

        private static ControllerState GetOrAdd(int deviceId)
        {
            if (States.TryGetValue(deviceId, out ControllerState state)) return state;
            state = new ControllerState();
            States.Add(deviceId, state);
            if (!PlayerDevices.Contains(deviceId) && PlayerDevices.Count < 2) PlayerDevices.Add(deviceId);
            return state;
        }

        private static void SetDpadKey(ControllerState state, Vector2 direction, bool pressed)
        {
            if (pressed) state.Dpad += direction;
            else state.Dpad -= direction;
            state.Dpad = Vector2.ClampMagnitude(state.Dpad, 1f);
        }

        private static float Parse(string value) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
    }
}
