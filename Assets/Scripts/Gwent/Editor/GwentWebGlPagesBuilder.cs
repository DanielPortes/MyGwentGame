using System;
using System.IO;
using System.Linq;
using Gwent.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Gwent.Editor
{
    public static class GwentWebGlPagesBuilder
    {
        private const string DefaultOutputPath = "Builds/WebGL";
        private const string OutputPathEnvVar = "GWENT_WEBGL_BUILD_PATH";
        private const string OutputPathArg = "-gWebGlOutputPath";
        private const string WebGlEntryScene = "Assets/Scenes/GwentWebGL.unity";

        private static readonly string[] PlayableScenes =
        {
            "Assets/Scenes/StartGame.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/End.unity"
        };

        private static readonly string[] WebGlScenes =
        {
            WebGlEntryScene
        };

        public static void ApplySettings()
        {
            EnsurePlayableScenes();
            EnsureWebGlEntryScene();

            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = Mathf.Max(PlayerSettings.defaultWebScreenWidth, 960);
            PlayerSettings.defaultWebScreenHeight = Mathf.Max(PlayerSettings.defaultWebScreenHeight, 600);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        public static void Build()
        {
            ApplySettings();

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new InvalidOperationException("Unity WebGL Build Support is not installed for this editor.");
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            var outputPath = ResolveOutputPath();
            RecreateOutputDirectory(outputPath);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = WebGlScenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"WebGL build failed with result {report.summary.result}, {report.summary.totalErrors} errors and {report.summary.totalWarnings} warnings.");
            }

            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);

            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new FileNotFoundException("WebGL build did not produce index.html.", indexPath);
            }
        }

        private static void EnsurePlayableScenes()
        {
            var missingScenes = PlayableScenes.Where(scene => !File.Exists(scene)).ToArray();
            if (missingScenes.Length > 0)
            {
                throw new FileNotFoundException("Missing playable scene: " + string.Join(", ", missingScenes));
            }

            EditorBuildSettings.scenes = PlayableScenes
                .Select(scene => new EditorBuildSettingsScene(scene, true))
                .ToArray();
        }

        private static void EnsureWebGlEntryScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.018f, 1f);
            camera.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("Witcher 3 Gwent Runtime").AddComponent<GwentGameController>();

            Directory.CreateDirectory(Path.GetDirectoryName(WebGlEntryScene) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, WebGlEntryScene);
        }

        private static string ResolveOutputPath()
        {
            var path = Environment.GetEnvironmentVariable(OutputPathEnvVar);
            if (string.IsNullOrWhiteSpace(path))
            {
                var args = Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(args[i], OutputPathArg, StringComparison.OrdinalIgnoreCase))
                    {
                        path = args[i + 1];
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultOutputPath;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private static void RecreateOutputDirectory(string outputPath)
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            Directory.CreateDirectory(outputPath);
        }
    }
}
