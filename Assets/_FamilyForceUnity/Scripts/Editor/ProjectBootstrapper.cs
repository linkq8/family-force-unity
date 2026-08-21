using System.Collections.Generic;
using FamilyForceUnity.Combat;
using FamilyForceUnity.Content;
using FamilyForceUnity.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FamilyForceUnity.Editor
{
    public static class ProjectBootstrapper
    {
        private const string Root = "Assets/_FamilyForceUnity";
        private const string ScenePath = Root + "/Scenes/VerticalSlice.unity";
        private const string MovePath = Root + "/Content/Base/Move_Punch.asset";

        [MenuItem("Tools/Family Force Unity/Build Vertical Slice Foundation")]
        public static void CreateVerticalSlice()
        {
            EnsureFolder(Root + "/Content/Base");
            EnsureFolder(Root + "/Scenes");

            MoveDefinition punch = LoadOrCreateMove();
            List<CharacterDefinition> characters = CreateCharacters();
            CreateCustomerPack(characters);
            // Persist ScriptableObjects before serializing them into the scene.
            // Without this, Unity writes fileID: 0 references in a fresh project.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CreateScene(punch, characters);
            ConfigureProject();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Family Force Unity foundation created successfully.");
        }

        public static void CreateVerticalSliceBatch()
        {
            CreateVerticalSlice();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Family Force Unity/Build Development APK")]
        public static void BuildDevelopmentApk()
        {
            CreateVerticalSlice();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/FamilyForceUnity-dev.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception($"Android build failed: {report.summary.result}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        private static MoveDefinition LoadOrCreateMove()
        {
            var move = AssetDatabase.LoadAssetAtPath<MoveDefinition>(MovePath);
            if (move != null) return move;

            move = ScriptableObject.CreateInstance<MoveDefinition>();
            move.Configure(MoveId.Punch, 5, 3, 8, 10, 4, new Vector2(1.5f, 0.2f));
            AssetDatabase.CreateAsset(move, MovePath);
            return move;
        }

        private static List<CharacterDefinition> CreateCharacters()
        {
            var specifications = new[]
            {
                new CharacterSpec("essa", "Essa", 177, new Color(0.12f, 0.62f, 0.95f)),
                new CharacterSpec("adam", "Adam", 108, new Color(1f, 0.67f, 0.15f)),
                new CharacterSpec("shaikha", "Shaikha", 108, new Color(0.91f, 0.25f, 0.55f)),
                new CharacterSpec("sulaiman", "Sulaiman", 124, new Color(0.24f, 0.78f, 0.43f))
            };

            var output = new List<CharacterDefinition>();
            foreach (var specification in specifications)
            {
                string path = $"{Root}/Content/Base/Character_{specification.Name}.asset";
                var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (character == null)
                {
                    character = ScriptableObject.CreateInstance<CharacterDefinition>();
                    character.Configure(specification.Id, specification.Name, specification.Height, specification.Color);
                    AssetDatabase.CreateAsset(character, path);
                }
                output.Add(character);
            }
            return output;
        }

        private static void CreateCustomerPack(List<CharacterDefinition> characters)
        {
            string path = $"{Root}/Content/CustomerPacks/CustomerPack_Base.asset";
            EnsureFolder(Root + "/Content/CustomerPacks");
            var pack = AssetDatabase.LoadAssetAtPath<CustomerPackDefinition>(path);
            if (pack == null)
            {
                pack = ScriptableObject.CreateInstance<CustomerPackDefinition>();
                AssetDatabase.CreateAsset(pack, path);
            }

            var serializedPack = new SerializedObject(pack);
            var list = serializedPack.FindProperty("characters");
            list.arraySize = characters.Count;
            for (int i = 0; i < characters.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            serializedPack.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateScene(MoveDefinition punch, List<CharacterDefinition> characters)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Family Force Unity — Prototype");
            var bootstrap = root.AddComponent<PrototypeBootstrap>();
            bootstrap.ConfigureContent(punch, characters);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Family Force Unity";
            PlayerSettings.productName = "Family Force Unity";
            PlayerSettings.bundleVersion = "0.2.3";
            PlayerSettings.Android.bundleVersionCode = 6;
            PlayerSettings.defaultScreenWidth = 640;
            PlayerSettings.defaultScreenHeight = 360;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.familyforceunity.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = false;
            ConfigureInputBackend();
        }

        private static void ConfigureInputBackend()
        {
            Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsAssets.Length == 0) return;

            var serializedSettings = new SerializedObject(settingsAssets[0]);
            SerializedProperty inputHandler = serializedSettings.FindProperty("activeInputHandler");
            if (inputHandler == null) return;
            inputHandler.intValue = 2;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private readonly struct CharacterSpec
        {
            public readonly string Id;
            public readonly string Name;
            public readonly int Height;
            public readonly Color Color;

            public CharacterSpec(string id, string name, int height, Color color)
            {
                Id = id;
                Name = name;
                Height = height;
                Color = color;
            }
        }
    }
}
