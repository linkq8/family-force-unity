using System.Collections.Generic;
using FamilyForceUnity.AI;
using FamilyForceUnity.Characters;
using FamilyForceUnity.Combat;
using FamilyForceUnity.Content;
using FamilyForceUnity.Input;
using UnityEngine;

namespace FamilyForceUnity.Core
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private MoveDefinition lightAttack;
        [SerializeField] private List<CharacterDefinition> roster = new();
        [SerializeField] private List<Sprite> essaAnimationFrames = new();

        private readonly List<Texture2D> runtimeTextures = new();
        private readonly List<MoveDefinition> runtimeMoves = new();
        private bool matchStarted;

        public IReadOnlyList<CharacterDefinition> Roster => roster;

        public void ConfigureContent(MoveDefinition move, List<CharacterDefinition> definitions, List<Sprite> essaFrames = null)
        {
            lightAttack = move;
            roster = definitions != null ? new List<CharacterDefinition>(definitions) : new List<CharacterDefinition>();
            essaAnimationFrames = essaFrames != null ? new List<Sprite>(essaFrames) : new List<Sprite>();
        }

        private void Awake()
        {
            EnsureRuntimeContent();
            EnsureSingleton<SimulationClock>("Simulation Clock");
            EnsureSingleton<LocalPlayerDeviceRegistry>("Local Player Devices");
            EnsureSingleton<AndroidNativeInputBridge>("Android Input Bridge");
            EnsureSingleton<ControllerDiagnosticsOverlay>("Controller Diagnostics");
            EnsureSingleton<GitHubUpdateService>("GitHub Update Service");
            BuildCamera();

            var tokens = EnsureSingleton<AttackTokenManager>("Attack Token Manager");
            tokens.ConfigureCapacity(2);

            var frontend = gameObject.AddComponent<FrontendFlowController>();
            frontend.Configure(this);
        }

        private void EnsureRuntimeContent()
        {
            if (lightAttack == null)
                lightAttack = MoveDefinition.CreateRuntimePunch();

            bool rosterIsUsable = roster.Count >= 4;
            for (int i = 0; i < roster.Count && rosterIsUsable; i++)
                rosterIsUsable = roster[i] != null;
            if (rosterIsUsable) return;

            roster.Clear();
            roster.Add(CharacterDefinition.CreateRuntime("essa", "Essa", 177, new Color(0.12f, 0.62f, 0.95f)));
            roster.Add(CharacterDefinition.CreateRuntime("adam", "Adam", 108, new Color(1f, 0.67f, 0.15f)));
            roster.Add(CharacterDefinition.CreateRuntime("shaikha", "Shaikha", 108, new Color(0.91f, 0.25f, 0.55f)));
            roster.Add(CharacterDefinition.CreateRuntime("sulaiman", "Sulaiman", 124, new Color(0.24f, 0.78f, 0.43f)));
        }

        public bool BeginMatch(CharacterDefinition playerOne, CharacterDefinition playerOneLink,
            bool hasPlayerTwo, CharacterDefinition playerTwo, CharacterDefinition playerTwoLink)
        {
            if (matchStarted || playerOne == null) return false;
            matchStarted = true;

            BuildArena();
            CreateFighter($"P1 — {playerOne.DisplayName}", new Vector2(-3f, -0.5f), playerOne, 0);
            if (hasPlayerTwo && playerTwo != null)
                CreateFighter($"P2 — {playerTwo.DisplayName}", new Vector2(-1.8f, -1.2f), playerTwo, 1);

            CreateStreetGuard("Rami — Street Guard", new Vector2(3.6f, -0.3f));
            CreateEnemy("Enemy B", new Vector2(5.2f, 0.8f), new Color(0.66f, 0.15f, 0.73f));
            CreateEnemy("Enemy C", new Vector2(6.2f, -1.3f), new Color(0.75f, 0.28f, 0.12f));
            return true;
        }

        private void OnDestroy()
        {
            foreach (var texture in runtimeTextures)
                if (texture != null) Destroy(texture);
            if (lightAttack != null && lightAttack.name == "Runtime_Punch") Destroy(lightAttack);
            foreach (var move in runtimeMoves)
                if (move != null) Destroy(move);
            foreach (var character in roster)
                if (character != null && character.name.StartsWith("Runtime_")) Destroy(character);
        }

        private void BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 2.8125f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.07f);
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void BuildArena()
        {
            CreateBlock("Sky", new Vector2(0f, 1.6f), new Vector2(18f, 2.5f), new Color(0.05f, 0.09f, 0.18f), -20);
            CreateBlock("City", new Vector2(0f, 0.55f), new Vector2(18f, 1.3f), new Color(0.12f, 0.18f, 0.29f), -15);
            CreateBlock("Street", new Vector2(0f, -1.25f), new Vector2(18f, 2.6f), new Color(0.16f, 0.17f, 0.2f), -10);
            CreateBlock("Lane Stripe", new Vector2(0f, -1.45f), new Vector2(18f, 0.06f), new Color(0.95f, 0.67f, 0.16f), -9);
        }

        private void CreateFighter(string label, Vector2 position, CharacterDefinition character, int playerIndex)
        {
            float heightScale = Mathf.Clamp(character.HeightCentimeters / 135f, 0.82f, 1.35f);
            var fighter = CreateBlock(label, position, new Vector2(0.65f, heightScale), character.PlaceholderColor, 10);
            Sprite[] animatedFrames = null;
            if (character.CharacterId == "essa" && essaAnimationFrames.Count >= 16)
            {
                animatedFrames = essaAnimationFrames.ToArray();
                fighter.transform.localScale = Vector3.one;
                var renderer = fighter.GetComponent<SpriteRenderer>();
                renderer.sprite = animatedFrames[0];
                renderer.color = Color.white;
            }
            fighter.AddComponent<LaneMotor>();
            fighter.AddComponent<FighterStateMachine>();
            var punchVisual = CreatePunchVisual(fighter.transform, character.PlaceholderColor);
            var visual = fighter.AddComponent<PrototypeFighterVisual>();
            visual.Configure(fighter.GetComponent<SpriteRenderer>(), punchVisual, animatedFrames);
            var controller = fighter.AddComponent<PrototypeFighterController>();
            MoveDefinition kick = CreateMove(MoveId.Kick, 6, 4, 10, 14, 4, new Vector2(1.8f, 0.3f));
            MoveDefinition heavy = CreateMove(MoveId.HeavyPunch, 11, 4, 16, 24, 7, new Vector2(3f, 0.45f));
            MoveDefinition jump = CreateMove(MoveId.Jump, 2, 20, 4, 0, 0, Vector2.zero);
            MoveDefinition special = CreateMove(MoveId.Special, 8, 9, 22, 34, 9, new Vector2(4f, 0.7f));
            controller.Configure(playerIndex, lightAttack, kick, heavy, jump, special);
        }

        private MoveDefinition CreateMove(MoveId id, int startup, int active, int recovery,
            int damage, int pause, Vector2 knockback)
        {
            MoveDefinition move = MoveDefinition.CreateRuntime(id, startup, active, recovery, damage, pause, knockback);
            runtimeMoves.Add(move);
            return move;
        }

        private GameObject CreatePunchVisual(Transform parent, Color characterColor)
        {
            var punch = new GameObject("Punch — Active Frames");
            punch.transform.SetParent(parent, false);
            punch.transform.localPosition = new Vector3(0.78f, 0.08f, -0.05f);
            punch.transform.localScale = new Vector3(0.5f, 0.28f, 1f);
            var renderer = punch.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(Color.Lerp(characterColor, Color.white, 0.65f));
            renderer.sortingOrder = 12;
            punch.SetActive(false);
            return punch;
        }

        private void CreateEnemy(string label, Vector2 position, Color color)
        {
            var enemy = CreateBlock(label, position, new Vector2(0.62f, 1.2f), color, 8);
            enemy.AddComponent<LaneMotor>();
            enemy.AddComponent<FighterStateMachine>();
            enemy.AddComponent<PrototypeEnemy>();
        }

        private void CreateStreetGuard(string label, Vector2 position)
        {
            var enemy = CreateBlock(label, position, new Vector2(0.68f, 0.78f), new Color(0.12f, 0.18f, 0.27f), 8);
            enemy.AddComponent<LaneMotor>();
            enemy.AddComponent<FighterStateMachine>();

            GameObject head = CreateBlock("Rami Head", Vector2.zero, new Vector2(0.48f, 0.42f), new Color(0.68f, 0.42f, 0.28f), 10);
            AttachPart(head, enemy.transform, new Vector2(0f, 0.64f));
            GameObject hair = CreateBlock("Rami Hair", Vector2.zero, new Vector2(0.5f, 0.12f), new Color(0.055f, 0.04f, 0.035f), 11);
            AttachPart(hair, enemy.transform, new Vector2(0f, 0.83f));
            GameObject belt = CreateBlock("Rami Belt", Vector2.zero, new Vector2(0.7f, 0.11f), new Color(0.95f, 0.65f, 0.12f), 10);
            AttachPart(belt, enemy.transform, new Vector2(0f, -0.13f));
            CreateAndAttachPart("Rami Left Leg", enemy.transform, new Vector2(-0.18f, -0.62f), new Vector2(0.22f, 0.58f), new Color(0.08f, 0.1f, 0.15f), 7);
            CreateAndAttachPart("Rami Right Leg", enemy.transform, new Vector2(0.18f, -0.62f), new Vector2(0.22f, 0.58f), new Color(0.08f, 0.1f, 0.15f), 7);
            GameObject attackArm = CreateAndAttachPart("Rami Attack Arm", enemy.transform, new Vector2(-0.47f, 0.12f), new Vector2(0.18f, 0.7f), new Color(0.68f, 0.42f, 0.28f), 9);
            CreateAndAttachPart("Rami Right Arm", enemy.transform, new Vector2(0.47f, 0.12f), new Vector2(0.18f, 0.7f), new Color(0.68f, 0.42f, 0.28f), 9);

            var visual = enemy.AddComponent<PrototypeEnemyVisual>();
            visual.Configure(attackArm.transform);
            enemy.AddComponent<PrototypeEnemy>();
        }

        private GameObject CreateAndAttachPart(string label, Transform parent, Vector2 localPosition,
            Vector2 size, Color color, int order)
        {
            GameObject part = CreateBlock(label, Vector2.zero, size, color, order);
            AttachPart(part, parent, localPosition);
            return part;
        }

        private static void AttachPart(GameObject part, Transform parent, Vector2 localPosition)
        {
            Vector3 scale = part.transform.localScale;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.01f);
            part.transform.localScale = new Vector3(scale.x / parent.localScale.x, scale.y / parent.localScale.y, 1f);
        }

        private GameObject CreateBlock(string label, Vector2 position, Vector2 size, Color color, int order)
        {
            var item = new GameObject(label);
            item.transform.position = new Vector3(position.x, position.y, position.y);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite(color);
            renderer.sortingOrder = order;
            return item;
        }

        private Sprite CreateSolidSprite(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Placeholder {color}"
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            runtimeTextures.Add(texture);
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static T EnsureSingleton<T>(string label) where T : Component
        {
            T existing = FindFirstObjectByType<T>();
            if (existing != null) return existing;
            return new GameObject(label).AddComponent<T>();
        }

    }
}
