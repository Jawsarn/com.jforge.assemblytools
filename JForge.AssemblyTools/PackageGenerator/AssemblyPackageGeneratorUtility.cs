using System.IO;
using JForge.AssemblyTools.Utility;
using UnityEditor;
using UnityEngine;

namespace JForge.AssemblyTools.PackageGenerator
{
    /// <summary>
    /// Entry points for running an <see cref="AssemblyPackageGenerator"/>'s "Generate Package" action outside
    /// the Inspector button - e.g. from an external tool or an AI coding agent working by file edits, or
    /// headless from the command line/CI. Unlike <see cref="Inheritance.InheritedAssemblyGenerator"/>, package
    /// generation was never auto-triggered by <c>OnValidate</c> in the first place - it's always been an
    /// explicit, one-shot action - so this exists purely to give it a programmatic entry point, not to fix a
    /// missing-trigger problem.
    /// </summary>
    public static class AssemblyPackageGeneratorUtility
    {
        /// <summary>
        /// Runs the package generation configured on a single <see cref="AssemblyPackageGenerator"/> asset -
        /// equivalent to clicking its Inspector's "Generate Package" button.
        /// </summary>
        /// <param name="targetAssetPath">A project-relative path to an <see cref="AssemblyPackageGenerator"/> asset.</param>
        /// <returns>Whether generation ran and reported success.</returns>
        public static bool GeneratePackage(string targetAssetPath)
        {
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                Debug.LogError($"[{nameof(AssemblyPackageGeneratorUtility)}] No target asset path provided.");
                return false;
            }

            var generator = AssetDatabase.LoadAssetAtPath<AssemblyPackageGenerator>(targetAssetPath);
            if (generator == null)
            {
                Debug.LogError($"[{nameof(AssemblyPackageGeneratorUtility)}] '{targetAssetPath}' is not an {nameof(AssemblyPackageGenerator)}.");
                return false;
            }

            return GeneratePackage(generator);
        }

        /// <summary>
        /// Runs the package generation configured on a single <see cref="AssemblyPackageGenerator"/> - equivalent
        /// to clicking its Inspector's "Generate Package" button.
        /// </summary>
        public static bool GeneratePackage(AssemblyPackageGenerator generator)
        {
            if (generator == null)
            {
                Debug.LogError($"[{nameof(AssemblyPackageGeneratorUtility)}] No generator provided.");
                return false;
            }

            if (generator.packageTemplate == null)
            {
                Debug.LogError($"[{nameof(AssemblyPackageGeneratorUtility)}] '{generator.name}' has no {nameof(AssemblyPackageGenerator.packageTemplate)} assigned.", generator);
                return false;
            }

            var destinationPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(generator));
            var success = generator.packageTemplate.GeneratePackage(generator.generatedPackageName, destinationPath);

            // GeneratePackage (and the post-processors it runs - e.g. TryGenerate on any copied
            // InheritedAssemblyGenerator) only mark things dirty; nothing else will persist that before a
            // batch-mode process exits on -quit.
            AssetDatabase.SaveAssets();

            Debug.Log($"[{nameof(AssemblyPackageGeneratorUtility)}] Generated package '{generator.generatedPackageName}' from '{generator.name}', success: {success}.", generator);
            return success;
        }

        /// <summary>
        /// <see cref="GeneratePackage(string)"/>, callable via Unity's <c>-executeMethod</c> batch-mode flag,
        /// which only supports parameterless static methods - the target is instead passed as an extra
        /// command-line argument: <c>-batchmode -nographics -projectPath &lt;path&gt; -executeMethod
        /// JForge.AssemblyTools.PackageGenerator.AssemblyPackageGeneratorUtility.GeneratePackageFromCommandLine
        /// -jforgeTarget Assets/Foo/MyPackageGenerator.asset -quit</c>.
        /// </summary>
        public static void GeneratePackageFromCommandLine()
        {
            var targetPath = PackageUtilities.GetCommandLineArgValue(PackageUtilities.CommandLineTargetArgName);
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogError($"[{nameof(AssemblyPackageGeneratorUtility)}] Missing required '{PackageUtilities.CommandLineTargetArgName} <path>' command-line argument.");
                return;
            }

            GeneratePackage(targetPath);
        }

        [MenuItem("Assets/" + PackageUtilities.CreateAssetMenuPath + "Generate Package", true)]
        private static bool ValidateGenerateSelected()
        {
            return Selection.activeObject is AssemblyPackageGenerator;
        }

        [MenuItem("Assets/" + PackageUtilities.CreateAssetMenuPath + "Generate Package")]
        private static void GenerateSelected()
        {
            GeneratePackage(Selection.activeObject as AssemblyPackageGenerator);
        }
    }
}
