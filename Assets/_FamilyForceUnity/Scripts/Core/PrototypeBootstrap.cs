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

        private readonly List<Texture2D> runtimeTextures = new();
        private bool matchStarted;

        public IReadOnlyList<CharacterDefinition> Roster => roster;

        private void Awake()
        {
            EnsureSingleton<SimulationClock>("Simulation Clock");
            EnsureSingleton<LocalPlayerDeviceRegistry>("Local Player Devices");
            EnsureSingleton<ControllerDiagnosticsOverlay>("Controller Diagnostics");
            BuildCamera();

            var tokens = EnsureSingleton<AttackTokenManager>("Attack Token Manager");
            tokens.ConfigureCapacity(2);

            var frontend = gameObject.AddComponent<FrontendFlowController>();
            frontend.Configure(this);
        }

        public void BeginMatch(CharacterDefinition playerOne, CharacterDefinition playerOneLink,
            bool hasPlayerTwo, CharacterDefinition playerTwo, CharacterDefinition playerTwoLink)
        {
            if (matchStarted || playerOne == null) return;
            matchStarted = true;

            BuildArena();
            CreateFighter($"P1 — {playerOne.DisplayName}", new Vector2(-3f, -0.5f), playerOne, 0);
            if (hasPlayerTwo && playerTwo != null)
                CreateFighter($"P2 — {playerTwo.DisplayName}", new Vector2(-1.8f, -1.2f), playerTwo, 1);

            CreateEnemy("Enemy A", new Vector2(3.6f, -0.3f), new Color(0.85f, 0.18f, 0.25f));
            CreateEnemy("Enemy B", new Vector2(5.2f, 0.8f), new Color(0.66f, 0.15f, 0.73f));
            CreateEnemy("Enemy C", new Vector2(6.2f, -1.3f), new Color(0.75f, 0.28f, 0.12f));
        }

        private void OnDestroy()
        {
            foreach (var texture in runtimeTextures)
                if (texture != null) Destroy(texture);
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
            fighter.AddComponent<LaneMotor>();
            fighter.AddComponent<FighterStateMachine>();
            var controller = fighter.AddComponent<PrototypeFighterController>();
            controller.Configure(playerIndex, lightAttack);
        }

        private void CreateEnemy(string label, Vector2 position, Color color)
        {
            var enemy = CreateBlock(label, position, new Vector2(0.62f, 1.2f), color, 8);
            enemy.AddComponent<LaneMotor>();
            enemy.AddComponent<FighterStateMachine>();
            enemy.AddComponent<PrototypeEnemy>();
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
