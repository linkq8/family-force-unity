using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Input
{
    public sealed class ControllerDiagnosticsOverlay : MonoBehaviour
    {
        public bool IsVisible { get; set; }

        private GUIStyle labelStyle;
        private GUIStyle titleStyle;

        private void OnGUI()
        {
            if (!IsVisible) return;

            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };
            titleStyle ??= new GUIStyle(labelStyle) { fontSize = 22, fontStyle = FontStyle.Bold };

            float panelWidth = Mathf.Min(Screen.width - 24f, 940f);
            string[] legacyNames = ControllerDeviceRouter.GetLegacyControllerNames();
            float panelHeight = 69f + Mathf.Min(InputSystem.devices.Count + legacyNames.Length + 1, 10) * 25f;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Box(new Rect(12, Screen.height - panelHeight - 12, panelWidth, panelHeight), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(24, Screen.height - panelHeight - 4, panelWidth - 24, 30),
                $"INPUT DIAGNOSTICS — controllers: {ControllerDeviceRouter.ControllerCount}", titleStyle);

            GUI.Label(new Rect(24, Screen.height - panelHeight + 28, panelWidth - 24, 25),
                ControllerDeviceRouter.DescribeAutomaticProfile(0), labelStyle);

            int row = 1;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (row >= 7) break;
                bool supported = ControllerDeviceRouter.IsControllerLike(device);
                string marker = supported ? "READY" : "seen";
                GUI.Label(new Rect(24, Screen.height - panelHeight + 28 + row * 25, panelWidth - 24, 25),
                    $"[{marker}] {ControllerDeviceRouter.Describe(device)}", labelStyle);
                row++;
            }

            foreach (string legacyName in legacyNames)
            {
                if (row >= 9 || string.IsNullOrWhiteSpace(legacyName)) continue;
                GUI.Label(new Rect(24, Screen.height - panelHeight + 28 + row * 25, panelWidth - 24, 25),
                    $"[LEGACY READY] {legacyName}", labelStyle);
                row++;
            }

            if (row < 10)
                GUI.Label(new Rect(24, Screen.height - panelHeight + 28 + row * 25, panelWidth - 24, 25),
                    ControllerDeviceRouter.DescribeLegacyState(), labelStyle);
        }
    }
}
