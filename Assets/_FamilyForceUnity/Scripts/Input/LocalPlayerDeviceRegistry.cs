using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Input
{
    public sealed class LocalPlayerDeviceRegistry : MonoBehaviour
    {
        private readonly Dictionary<int, InputDevice> assignedDevices = new();

        public event Action<int, InputDevice> DeviceAssigned;
        public event Action<int> DeviceDisconnected;

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;
        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;

        public bool TryAssign(int playerIndex, InputDevice device)
        {
            if (playerIndex is < 0 or > 1 || device == null)
                return false;

            foreach (var pair in assignedDevices)
            {
                if (pair.Key != playerIndex && pair.Value == device)
                    return false;
            }

            assignedDevices[playerIndex] = device;
            DeviceAssigned?.Invoke(playerIndex, device);
            return true;
        }

        public InputDevice GetAssignedDevice(int playerIndex) =>
            assignedDevices.TryGetValue(playerIndex, out var device) ? device : null;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change is not (InputDeviceChange.Disconnected or InputDeviceChange.Removed))
                return;

            foreach (var pair in assignedDevices)
            {
                if (pair.Value == device)
                    DeviceDisconnected?.Invoke(pair.Key);
            }
        }
    }
}

