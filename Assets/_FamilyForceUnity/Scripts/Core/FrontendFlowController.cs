using FamilyForceUnity.Content;
using FamilyForceUnity.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamilyForceUnity.Core
{
    public sealed class FrontendFlowController : MonoBehaviour
    {
        private enum FrontendState { Title, CharacterSelect, Playing }

        private const int PlayerOneHeroRow = 0;
        private const int PlayerOneLinkRow = 1;
        private const int PlayerTwoRow = 2;
        private const int PlayerTwoLinkRow = 3;
        private const int StartRow = 4;

        private PrototypeBootstrap bootstrap;
        private FrontendState state;
        private int focusRow;
        private int playerOneHero;
        private int playerOneLink = 1;
        private int playerTwoHero = 2;
        private int playerTwoLink = 3;
        private bool playerTwoActive;
        private bool confirmHeld;
        private bool cancelHeld;
        private bool diagnosticsHeld;
        private bool pauseHeld;
        private bool isPaused;
        private bool pauseConfirmHeld;
        private int pauseFocus;
        private bool showControls;
        private bool axisHeld;
        private string statusMessage;
        private int titleFocus;
        private GitHubUpdateService updater;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle rowStyle;
        private GUIStyle hintStyle;

        public void Configure(PrototypeBootstrap prototypeBootstrap)
        {
            bootstrap = prototypeBootstrap;
            state = FrontendState.Title;
            updater = FindFirstObjectByType<GitHubUpdateService>();
        }

        private void Update()
        {
            if (bootstrap == null) return;

            bool diagnostics = ReadDiagnostics() || ControllerDeviceRouter.ReadPlayerShare(0);
            if (diagnostics && !diagnosticsHeld)
            {
                var overlay = FindFirstObjectByType<ControllerDiagnosticsOverlay>();
                if (overlay != null) overlay.IsVisible = !overlay.IsVisible;
            }
            diagnosticsHeld = diagnostics;

            bool pause = ControllerDeviceRouter.ReadPlayerPause(0);
            if (state == FrontendState.Playing && pause && !pauseHeld)
            {
                isPaused = !isPaused;
                showControls = false;
                Time.timeScale = isPaused ? 0f : 1f;
            }
            pauseHeld = pause;

            if (state == FrontendState.Playing)
            {
                if (isPaused) HandlePauseInput();
                return;
            }

            bool confirm = ReadConfirm(0);
            bool cancel = ReadCancel();

            if (state == FrontendState.Title)
            {
                Vector2 navigation = ReadNavigation();
                if (navigation.sqrMagnitude < 0.25f) axisHeld = false;
                else if (!axisHeld && Mathf.Abs(navigation.y) >= Mathf.Abs(navigation.x))
                {
                    axisHeld = true;
                    titleFocus = (titleFocus + (navigation.y < 0f ? 1 : 3)) % 4;
                }
                if (confirm && !confirmHeld)
                {
                    if (titleFocus == 0) state = FrontendState.CharacterSelect;
                    else if (titleFocus == 1) updater?.Activate();
                    else if (titleFocus == 2) GameLocalization.Toggle();
                    else GameSettings.KidsMode = !GameSettings.KidsMode;
                }
            }
            else
            {
                HandleSelection(ReadNavigation());
                if (cancel && !cancelHeld)
                {
                    state = FrontendState.Title;
                    focusRow = PlayerOneHeroRow;
                }
                else if (confirm && !confirmHeld && focusRow == StartRow)
                {
                    bool started = bootstrap.BeginMatch(Choice(playerOneHero), Choice(playerOneLink), playerTwoActive,
                        Choice(playerTwoHero), Choice(playerTwoLink));
                    if (started)
                    {
                        state = FrontendState.Playing;
                        var overlay = FindFirstObjectByType<ControllerDiagnosticsOverlay>();
                        if (overlay != null) overlay.IsVisible = false;
                    }
                    else
                    {
                        statusMessage = "CONTENT NOT READY — STAYING IN CHARACTER SELECT";
                    }
                }
            }

            confirmHeld = confirm;
            cancelHeld = cancel;
        }

        private void HandlePauseInput()
        {
            Vector2 navigation = ReadNavigation();
            if (navigation.sqrMagnitude < 0.25f) axisHeld = false;
            else if (!axisHeld && Mathf.Abs(navigation.y) >= Mathf.Abs(navigation.x))
            {
                axisHeld = true;
                pauseFocus = (pauseFocus + (navigation.y < 0f ? 1 : 4)) % 5;
            }

            bool confirm = ReadConfirm(0);
            bool cancel = ReadCancel();
            if (cancel && !cancelHeld)
            {
                if (showControls) showControls = false;
                else ResumeGame();
            }
            else if (confirm && !pauseConfirmHeld)
            {
                if (showControls) showControls = false;
                else ExecutePauseChoice();
            }
            pauseConfirmHeld = confirm;
            cancelHeld = cancel;
        }

        private void ExecutePauseChoice()
        {
            switch (pauseFocus)
            {
                case 0: ResumeGame(); break;
                case 1: updater?.Activate(); break;
                case 2:
                    Time.timeScale = 1f;
                    isPaused = false;
                    showControls = false;
                    bootstrap.EndMatch();
                    state = FrontendState.Title;
                    titleFocus = 1;
                    break;
                case 3: showControls = true; break;
                case 4: Time.timeScale = 1f; isPaused = false; bootstrap.RestartMatch(); break;
            }
        }

        private void ResumeGame()
        {
            isPaused = false;
            showControls = false;
            Time.timeScale = 1f;
        }

        private void HandleSelection(Vector2 navigation)
        {
            if (navigation.sqrMagnitude < 0.25f)
            {
                axisHeld = false;
                return;
            }
            if (axisHeld) return;
            axisHeld = true;

            if (Mathf.Abs(navigation.y) > Mathf.Abs(navigation.x))
            {
                int direction = navigation.y > 0 ? -1 : 1;
                focusRow = (focusRow + direction + StartRow + 1) % (StartRow + 1);
                return;
            }

            int delta = navigation.x > 0 ? 1 : -1;
            switch (focusRow)
            {
                case PlayerOneHeroRow: playerOneHero = Wrap(playerOneHero + delta); break;
                case PlayerOneLinkRow: playerOneLink = Wrap(playerOneLink + delta); break;
                case PlayerTwoRow: playerTwoActive = delta > 0; break;
                case PlayerTwoLinkRow: if (playerTwoActive) playerTwoLink = Wrap(playerTwoLink + delta); break;
            }
        }

        private void OnGUI()
        {
            BuildStyles();
            if (state == FrontendState.Playing)
            {
                if (isPaused) DrawPauseOverlay();
                return;
            }
            DrawBackdrop();
            if (state == FrontendState.Title) DrawTitle();
            else DrawCharacterSelect();
        }

        private void DrawPauseOverlay()
        {
            DrawSolid(new Rect(0, 0, Screen.width, Screen.height), new Color(0.015f, 0.02f, 0.055f, 0.82f));
            float width = Mathf.Min(Screen.width * 0.8f, 760f);
            float left = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(left, Screen.height * 0.12f, width, 64f), showControls ? "CONTROLS" : "PAUSED", titleStyle);
            if (showControls)
            {
                string profile = ControllerDeviceRouter.DescribeAutomaticProfile(0);
                string controls = $"{profile}\n\nD-PAD / LEFT STICK   MOVE\nSQUARE   PUNCH / COMBO\nTRIANGLE   KICK\nCIRCLE   HEAVY\nX   JUMP\nR1   SPECIAL\nL1 / L2 / R2   LINK\nOPTIONS   PAUSE\n\nCIRCLE / BACK   RETURN";
                GUI.Label(new Rect(left, Screen.height * 0.25f, width, Screen.height * 0.64f), controls, rowStyle);
                return;
            }
            string updateText = updater != null ? updater.Status : "CHECK FOR UPDATE";
            string[] options =
            {
                GameLocalization.T("RESUME", "متابعة"),
                updateText,
                GameLocalization.T("RETURN TO MAIN MENU", "العودة للقائمة الرئيسية"),
                GameLocalization.T("CONTROLS", "أزرار التحكم"),
                GameLocalization.T("RESTART MISSION", "إعادة المهمة")
            };
            float rowHeight = Mathf.Clamp(Screen.height * 0.09f, 42f, 58f);
            float firstRow = Screen.height * 0.25f;
            for (int i = 0; i < options.Length; i++)
                DrawTitleButton(left, firstRow + i * rowHeight, width, options[i], pauseFocus == i);
            GUI.Label(new Rect(left, Mathf.Min(Screen.height - 30f, firstRow + options.Length * rowHeight + 8f), width, 28f),
                GameLocalization.T("D-pad / Confirm / Back", "الاتجاهات / تأكيد / رجوع"), hintStyle);
        }

        private void OnDestroy()
        {
            if (isPaused) Time.timeScale = 1f;
        }

        private void DrawBackdrop()
        {
            DrawSolid(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.035f, 0.08f));
            DrawSolid(new Rect(0, Screen.height * 0.62f, Screen.width, Screen.height * 0.38f), new Color(0.08f, 0.075f, 0.16f));
            DrawSolid(new Rect(0, Screen.height * 0.66f, Screen.width, 4), new Color(0.97f, 0.67f, 0.16f));
            for (int i = 0; i < 9; i++)
            {
                float width = Screen.width * (0.06f + (i % 3) * 0.025f);
                float height = Screen.height * (0.12f + (i % 4) * 0.055f);
                float x = i * Screen.width / 8.5f;
                DrawSolid(new Rect(x, Screen.height * 0.62f - height, width, height), new Color(0.1f, 0.16f, 0.29f));
            }
        }

        private void DrawTitle()
        {
            float width = Mathf.Min(Screen.width * 0.83f, 960f);
            float left = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(left, Screen.height * 0.19f, width, 96), "FAMILY FORCE", titleStyle);
            GUI.Label(new Rect(left, Screen.height * 0.43f, width, 34), "LOCAL CO-OP BEAT-'EM-UP", headingStyle);
            DrawTitleButton(left, Screen.height * 0.59f, width, GameLocalization.T("START GAME", "ابدأ اللعب"), titleFocus == 0);
            string updateLabel = updater != null ? updater.Status : "CHECK FOR UPDATE";
            DrawTitleButton(left, Screen.height * 0.68f, width, updateLabel, titleFocus == 1);
            DrawTitleButton(left, Screen.height * 0.77f, width, GameLocalization.Arabic ? "LANGUAGE: العربية" : "LANGUAGE: ENGLISH", titleFocus == 2);
            DrawTitleButton(left, Screen.height * 0.86f, width, $"DIFFICULTY: {GameSettings.DifficultyLabel}", titleFocus == 3);
            GUI.Label(new Rect(left, Screen.height - 22f, width, 20), $"VERSION {Application.version}   D-pad / Confirm", hintStyle);
        }

        private void DrawTitleButton(float left, float top, float width, string label, bool focused)
        {
            DrawSolid(new Rect(left + width * 0.18f, top, width * 0.64f, 46f),
                focused ? new Color(0.97f, 0.67f, 0.16f) : new Color(0.06f, 0.09f, 0.17f, 0.94f));
            var style = new GUIStyle(headingStyle)
            {
                normal = { textColor = focused ? new Color(0.05f, 0.04f, 0.08f) : Color.white },
                fontSize = 19
            };
            GUI.Label(new Rect(left + width * 0.18f, top + 6f, width * 0.64f, 34f), label, style);
        }

        private void DrawCharacterSelect()
        {
            float width = Mathf.Min(Screen.width * 0.9f, 1060f);
            float left = (Screen.width - width) * 0.5f;
            float top = Mathf.Max(20f, Screen.height * 0.08f);
            GUI.Label(new Rect(left, top, width, 44), GameLocalization.T("BUILD YOUR FAMILY FORCE", "اختر فريق فاميلي فورس"), headingStyle);
            GUI.Label(new Rect(left, top + 42f, width, 26), GameLocalization.T("Choose a hero and an independent Link companion. P2 is optional.", "اختر البطل والمرافق لكل لاعب. اللاعب الثاني اختياري."), hintStyle);

            float rowTop = top + 90f;
            DrawChoiceRow(left, rowTop, width, PlayerOneHeroRow, "P1 HERO", Choice(playerOneHero), true);
            DrawChoiceRow(left, rowTop + 52f, width, PlayerOneLinkRow, "P1 LINK", Choice(playerOneLink), true);
            string playerTwoValue = playerTwoActive ? $"{Choice(playerTwoHero).DisplayName}  /  {Choice(playerTwoHero).HeightCentimeters} cm" : "OPTIONAL — OFF";
            DrawChoiceRow(left, rowTop + 104f, width, PlayerTwoRow, "P2", null, playerTwoActive, playerTwoValue);
            DrawChoiceRow(left, rowTop + 156f, width, PlayerTwoLinkRow, "P2 LINK", Choice(playerTwoLink), playerTwoActive);
            DrawStartRow(left, rowTop + 224f, width);
            GUI.Label(new Rect(left, Screen.height - 38f, width, 26), $"Controllers found: {ControllerDeviceRouter.ControllerCount}. A second controller can join P2 with Confirm.", hintStyle);
            if (!string.IsNullOrEmpty(statusMessage))
                GUI.Label(new Rect(left, Screen.height - 64f, width, 26), statusMessage, headingStyle);
        }

        private void DrawChoiceRow(float left, float top, float width, int row, string label, CharacterDefinition character, bool enabled, string overrideValue = null)
        {
            bool focused = row == focusRow;
            Color background = focused ? new Color(0.17f, 0.55f, 0.88f, 0.95f) : new Color(0.06f, 0.09f, 0.17f, 0.94f);
            DrawSolid(new Rect(left, top, width, 44f), background);
            Color swatch = character != null ? character.PlaceholderColor : (enabled ? new Color(0.24f, 0.78f, 0.43f) : new Color(0.28f, 0.3f, 0.38f));
            DrawSolid(new Rect(left + 10f, top + 8f, 28f, 28f), swatch);
            string value = overrideValue ?? (character != null ? $"{character.DisplayName}  /  {character.HeightCentimeters} cm" : "OFF");
            GUI.Label(new Rect(left + 52f, top + 8f, width * 0.28f, 28f), label, rowStyle);
            GUI.Label(new Rect(left + width * 0.34f, top + 8f, width * 0.54f, 28f), value, rowStyle);
            GUI.Label(new Rect(left + width - 90f, top + 8f, 76f, 28f), focused ? "<  >" : "", rowStyle);
        }

        private void DrawStartRow(float left, float top, float width)
        {
            bool focused = focusRow == StartRow;
            DrawSolid(new Rect(left, top, width, 52f),
                focused ? new Color(0.97f, 0.67f, 0.16f) : new Color(0.45f, 0.28f, 0.07f));
            var style = new GUIStyle(headingStyle) { normal = { textColor = focused ? new Color(0.05f, 0.04f, 0.08f) : Color.white } };
            GUI.Label(new Rect(left, top + 9f, width, 34f), GameLocalization.T("START STREET MISSION", "ابدأ مهمة الشارع"), style);
        }

        private void BuildStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 52, normal = { textColor = new Color(0.95f, 0.94f, 1f) } };
            headingStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 24, normal = { textColor = new Color(0.97f, 0.67f, 0.16f) } };
            rowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 18, normal = { textColor = Color.white } };
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = new Color(0.73f, 0.79f, 0.92f) } };
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private CharacterDefinition Choice(int index)
        {
            return bootstrap.Roster.Count == 0 ? null : bootstrap.Roster[Wrap(index)];
        }

        private int Wrap(int index)
        {
            int count = bootstrap.Roster.Count;
            return count == 0 ? 0 : (index % count + count) % count;
        }

        private static Vector2 ReadNavigation()
        {
            Vector2 value = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                value.x = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
                value.y = (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            Vector2 controller = ControllerDeviceRouter.ReadPlayerMovement(0);
            if (controller.sqrMagnitude > value.sqrMagnitude) value = controller;
            return value;
        }

        private static bool ReadConfirm(int playerIndex)
        {
            Keyboard keyboard = Keyboard.current;
            bool keyboardConfirm = playerIndex == 0 && keyboard != null &&
                (keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed);
            return keyboardConfirm || ControllerDeviceRouter.ReadPlayerConfirm(playerIndex);
        }

        private static bool ReadCancel()
        {
            Keyboard keyboard = Keyboard.current;
            return (keyboard != null && (keyboard.escapeKey.isPressed || keyboard.backspaceKey.isPressed)) ||
                ControllerDeviceRouter.ReadPlayerCancel(0);
        }

        private static bool ReadDiagnostics()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.isPressed) return true;
            InputDevice controller = ControllerDeviceRouter.GetController(0);
            return (controller is Gamepad gamepad && gamepad.rightStickButton.isPressed) ||
                ControllerDeviceRouter.ReadPlayerDiagnostics(0);
        }
    }
}
