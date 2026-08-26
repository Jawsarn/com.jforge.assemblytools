using System.Collections.Generic;
using JForge.AssemblyTools.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JForge.AssemblyTools.Inheritance
{
    /// <summary>
    /// Entry points for regenerating <see cref="InheritedAssemblyGenerator"/> output without going through
    /// <c>OnValidate</c>. Needed because editing a .asmdef or an <see cref="InheritedAssemblyGenerator"/>'s
    /// .asset file directly (e.g. by an external tool or an AI coding agent, rather than through the Unity
    /// Inspector) never fires <c>OnValidate</c>, so <see cref="InheritedAssemblyGenerator.TryGenerate"/> would
    /// otherwise never run for that edit at all.
    /// </summary>
    public static class InheritedAssemblyGeneratorUtility
    {
        /// <summary>
        /// Regenerates only the generator(s) affected by a single changed file - the fast, default path for
        /// "I just edited one file". Does not touch any other generator in the project, and does not walk
        /// further down an inheritance chain beyond the direct target(s) - use <see cref="RegenerateAll"/> if
        /// the whole project needs to be brought up to date.
        /// </summary>
        /// <param name="targetAssetPath">
        /// A project-relative path (e.g. "Assets/Foo/MyGenerator.asset") to either:
        /// <list type="bullet">
        /// <item>an <see cref="InheritedAssemblyGenerator"/> asset itself (resolved directly, no project scan), or</item>
        /// <item>an assembly definition used as some generator's <c>assemblyDefinitionBase</c> (resolved via a
        /// scan across existing generators' <c>assemblyDefinitionBase</c> field - looking up "who depends on
        /// this" has no cheaper option in the AssetDatabase, but this only compares a field per generator; the
        /// expensive work, actually regenerating, still only runs on the match(es)).</item>
        /// </list>
        /// </param>
        /// <returns>Whether any generator's output actually changed.</returns>
        public static bool RegenerateTarget(string targetAssetPath)
        {
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                Debug.LogError($"[{nameof(InheritedAssemblyGeneratorUtility)}] No target asset path provided.");
                return false;
            }

            var directGenerator = AssetDatabase.LoadAssetAtPath<InheritedAssemblyGenerator>(targetAssetPath);
            if (directGenerator != null)
            {
                return RegenerateAndSaveIfChanged(new List<InheritedAssemblyGenerator> { directGenerator });
            }

            var baseAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(targetAssetPath);
            if (baseAsset != null)
            {
                var dependents = FindGeneratorsWithBase(baseAsset);
                if (dependents.Count == 0)
                {
                    Debug.Log($"[{nameof(InheritedAssemblyGeneratorUtility)}] No {nameof(InheritedAssemblyGenerator)} targets '{targetAssetPath}' as its base assembly - nothing to regenerate.");
                    return false;
                }

                return RegenerateAndSaveIfChanged(dependents);
            }

            Debug.LogError($"[{nameof(InheritedAssemblyGeneratorUtility)}] '{targetAssetPath}' is neither an {nameof(InheritedAssemblyGenerator)} nor an assembly definition - nothing to regenerate.");
            return false;
        }

        /// <summary>
        /// Regenerates every <see cref="InheritedAssemblyGenerator"/> in the project. Materially more
        /// expensive than <see cref="RegenerateTarget"/> - every generator in the project actually runs
        /// <see cref="InheritedAssemblyGenerator.TryGenerate"/>, not just the ones affected by a specific change.
        /// Prefer <see cref="RegenerateTarget"/> when the changed file is known; if you're only unsure whether
        /// anything needs regenerating at all, check <see cref="AnyRootAssemblyChanged"/> first instead of
        /// calling this reflexively.
        /// </summary>
        [MenuItem(PackageUtilities.CreateAssetMenuPath + "Regenerate All Inherited Assemblies")]
        public static void RegenerateAll()
        {
            var generators = FindAllGenerators();

            // A chain (Gen1 -> Gen2Generator -> Gen3Generator) needs multiple passes: Gen3Generator reads
            // Gen2's *generated* output, which only reflects Gen1's latest state once Gen2Generator has
            // already run in this same pass. Keep looping until a full pass makes no further changes.
            var anyChangedThisRun = false;
            var maxPasses = generators.Count + 1;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var anyChangedThisPass = false;
                foreach (var generator in generators)
                {
                    if (generator.TryGenerate(true))
                    {
                        anyChangedThisPass = true;
                        anyChangedThisRun = true;
                    }
                }

                if (!anyChangedThisPass)
                {
                    break;
                }
            }

            if (anyChangedThisRun)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[{nameof(InheritedAssemblyGeneratorUtility)}] Regenerated {generators.Count} inherited assembly generator(s), changes made: {anyChangedThisRun}.");
        }

        /// <summary>
        /// Reports whether any "root" <see cref="InheritedAssemblyGenerator"/> - one whose
        /// <c>assemblyDefinitionBase</c> is a hand-authored assembly, not itself the <c>generatedDefinition</c>
        /// of some other generator in the project - would produce different output right now. Cheap: the same
        /// read-only computation as <see cref="InheritedAssemblyGenerator.NeedsRegeneration"/>, no file I/O.
        /// </summary>
        /// <remarks>
        /// Use this to decide whether <see cref="RegenerateAll"/> is actually worth running, instead of calling
        /// it reflexively: if nothing at the root of any chain has changed, nothing further down the chain
        /// should need re-deriving either - as long as the project was already fully regenerated as of the
        /// last <see cref="RegenerateAll"/>/<see cref="RegenerateTarget"/> call (the same assumption
        /// <see cref="RegenerateTarget"/>'s own base-&gt;dependents lookup already relies on). This does not
        /// catch a middle-of-chain generator's own fields being edited directly without going through
        /// <see cref="RegenerateTarget"/> - that specific case is what <see cref="RegenerateTarget"/> itself
        /// exists to handle, and it's cheap enough to just always call after a known edit rather than checking
        /// first.
        /// </remarks>
        public static bool AnyRootAssemblyChanged()
        {
            var generators = FindAllGenerators();
            var generatedDefinitions = new HashSet<AssemblyDefinitionAsset>();
            foreach (var generator in generators)
            {
                if (generator.generatedDefinition != null)
                {
                    generatedDefinitions.Add(generator.generatedDefinition);
                }
            }

            foreach (var generator in generators)
            {
                var isRoot = generator.assemblyDefinitionBase != null && !generatedDefinitions.Contains(generator.assemblyDefinitionBase);
                if (isRoot && generator.NeedsRegeneration())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <see cref="AnyRootAssemblyChanged"/>, callable via <c>-executeMethod</c> as a CI staleness gate:
        /// exits the Editor process with code 1 if a root assembly has changed (regeneration is needed but
        /// hasn't been run/committed), or 0 otherwise. <b>Only call this from a dedicated batch-mode
        /// invocation</b> - <c>EditorApplication.Exit</c> forcibly quits the Editor, which would be destructive
        /// in an interactive session.
        /// </summary>
        public static void CheckRootAssembliesFromCommandLine()
        {
            var changed = AnyRootAssemblyChanged();
            Debug.Log($"[{nameof(InheritedAssemblyGeneratorUtility)}] Root assembly change detected: {changed}.");
            EditorApplication.Exit(changed ? 1 : 0);
        }

        /// <summary>
        /// <see cref="RegenerateTarget"/>, callable via Unity's <c>-executeMethod</c> batch-mode flag, which
        /// only supports parameterless static methods - the target is instead passed as an extra command-line
        /// argument: <c>-batchmode -nographics -projectPath &lt;path&gt; -executeMethod
        /// JForge.AssemblyTools.Inheritance.InheritedAssemblyGeneratorUtility.RegenerateTargetFromCommandLine
        /// -jforgeTarget Assets/Foo/MyGenerator.asset -quit</c>.
        /// </summary>
        public static void RegenerateTargetFromCommandLine()
        {
            var targetPath = PackageUtilities.GetCommandLineArgValue(PackageUtilities.CommandLineTargetArgName);
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogError($"[{nameof(InheritedAssemblyGeneratorUtility)}] Missing required '{PackageUtilities.CommandLineTargetArgName} <path>' command-line argument.");
                return;
            }

            RegenerateTarget(targetPath);
        }

        [MenuItem("Assets/" + PackageUtilities.CreateAssetMenuPath + "Regenerate Inherited Assembly", true)]
        private static bool ValidateRegenerateSelected()
        {
            var selected = Selection.activeObject;
            return selected is InheritedAssemblyGenerator || selected is AssemblyDefinitionAsset;
        }

        [MenuItem("Assets/" + PackageUtilities.CreateAssetMenuPath + "Regenerate Inherited Assembly")]
        private static void RegenerateSelected()
        {
            RegenerateTarget(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        private static bool RegenerateAndSaveIfChanged(List<InheritedAssemblyGenerator> generators)
        {
            var anyChanged = false;
            foreach (var generator in generators)
            {
                if (generator.TryGenerate(true))
                {
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[{nameof(InheritedAssemblyGeneratorUtility)}] Regenerated {generators.Count} target(s), changes made: {anyChanged}.");
            return anyChanged;
        }

        private static List<InheritedAssemblyGenerator> FindGeneratorsWithBase(AssemblyDefinitionAsset baseAsset)
        {
            var matches = new List<InheritedAssemblyGenerator>();
            foreach (var generator in FindAllGenerators())
            {
                if (generator.assemblyDefinitionBase == baseAsset)
                {
                    matches.Add(generator);
                }
            }

            return matches;
        }

        private static List<InheritedAssemblyGenerator> FindAllGenerators()
        {
            var generators = new List<InheritedAssemblyGenerator>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(InheritedAssemblyGenerator)}"))
            {
                var generator = AssetDatabase.LoadAssetAtPath<InheritedAssemblyGenerator>(AssetDatabase.GUIDToAssetPath(guid));
                if (generator != null)
                {
                    generators.Add(generator);
                }
            }

            return generators;
        }
    }
}
