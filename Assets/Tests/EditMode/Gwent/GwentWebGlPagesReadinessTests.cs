using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gwent.Tests
{
    public class GwentWebGlPagesReadinessTests
    {
        [Test]
        public void BuildSettingsKeepPlayableScenesEnabled()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Assets/Scenes/StartGame.unity",
                    "Assets/Scenes/Game.unity",
                    "Assets/Scenes/End.unity"
                },
                enabledScenes);
        }

        [Test]
        public void WebGlSettingsAreGitHubPagesFriendly()
        {
            Assert.AreEqual(WebGLCompressionFormat.Disabled, PlayerSettings.WebGL.compressionFormat);
            Assert.IsTrue(PlayerSettings.WebGL.dataCaching);
            Assert.IsTrue(PlayerSettings.runInBackground);
            Assert.GreaterOrEqual(PlayerSettings.defaultWebScreenWidth, 960);
            Assert.GreaterOrEqual(PlayerSettings.defaultWebScreenHeight, 600);
        }

        [Test]
        public void GitHubPagesWorkflowDeploysCommittedWebGlArtifact()
        {
            var workflowPath = Path.Combine(Directory.GetCurrentDirectory(), ".github", "workflows", "webgl-pages.yml");

            Assert.IsTrue(File.Exists(workflowPath), "A GitHub Pages WebGL workflow must exist.");

            var workflow = File.ReadAllText(workflowPath);

            StringAssert.Contains("actions/configure-pages@v5", workflow);
            StringAssert.Contains("actions/upload-pages-artifact@v4", workflow);
            StringAssert.Contains("actions/deploy-pages@v4", workflow);
            StringAssert.Contains("path: docs/webgl", workflow);
            StringAssert.DoesNotContain("UNITY_LICENSE", workflow);
        }

        [Test]
        public void WebGlEntrySceneBootstrapsWitcher3GwentRuntime()
        {
            var scenePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scenes", "GwentWebGL.unity");

            Assert.IsTrue(File.Exists(scenePath), "WebGL builds should use a clean entry scene instead of the legacy prototype scene.");

            var scene = File.ReadAllText(scenePath);

            StringAssert.Contains("Witcher 3 Gwent Runtime", scene);
            StringAssert.Contains("guid: bc2f372cbe1e1994a92653198a5d9abf", scene);
        }

        [Test]
        public void WebGlEntryScenePreparationDoesNotRewriteExistingScene()
        {
            var scenePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scenes", "GwentWebGL.unity");
            var before = File.ReadAllText(scenePath);
            var builderType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("Gwent.Editor.GwentWebGlPagesBuilder"))
                .FirstOrDefault(type => type != null);

            Assert.NotNull(builderType, "The WebGL Pages builder must be available in edit mode.");

            builderType.GetMethod("EnsureWebGlEntryScene", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);

            Assert.AreEqual(before, File.ReadAllText(scenePath));
        }

        [Test]
        public void CommittedWebGlPagesArtifactContainsPlayableBuild()
        {
            var pagesPath = Path.Combine(Directory.GetCurrentDirectory(), "docs", "webgl");

            Assert.IsTrue(File.Exists(Path.Combine(pagesPath, "index.html")));
            Assert.IsTrue(File.Exists(Path.Combine(pagesPath, ".nojekyll")));
            Assert.IsTrue(Directory.Exists(Path.Combine(pagesPath, "Build")));
            Assert.IsTrue(Directory.Exists(Path.Combine(pagesPath, "TemplateData")));
            Assert.IsTrue(File.Exists(Path.Combine(pagesPath, "Build", "WebGL.loader.js")));
            Assert.IsTrue(File.Exists(Path.Combine(pagesPath, "Build", "WebGL.wasm")));
        }
    }
}
