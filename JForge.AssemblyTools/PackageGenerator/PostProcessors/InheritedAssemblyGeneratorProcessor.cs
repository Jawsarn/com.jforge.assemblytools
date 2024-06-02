using JForge.AssemblyTools.Inheritance;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JForge.AssemblyTools.PackageGenerator.PostProcessors
{
    public class InheritedAssemblyGeneratorProcessor : IAssetPackageGeneratorAssetPostProcessor
    {
        public void Setup(SetupPostProcessContext context) { }

        public bool ShouldProcess(Object asset, PostProcessContext context)
        {
            return asset is InheritedAssemblyGenerator;
        }

        public void Process(Object asset, PostProcessContext context)
        {
            var assemblyPackageGenerator = (InheritedAssemblyGenerator)asset;
            for (int i = 0; i < assemblyPackageGenerator.additionalReferences.Count; i++)
            {
                var reference = assemblyPackageGenerator.additionalReferences[i];
                if (context.oldToNewAssetMap.TryGetValue(reference, out var newReference))
                {
                    assemblyPackageGenerator.additionalReferences[i] = newReference as AssemblyDefinitionAsset;
                }
            }
            assemblyPackageGenerator.generatedDefinition = null; // Remove reference to generated definition, as this will point to the template definition
            assemblyPackageGenerator.assemblyName = assemblyPackageGenerator.assemblyName?.Replace(context.featureNameReplaceString, context.featureName);
            assemblyPackageGenerator.assemblyFileName = assemblyPackageGenerator.assemblyFileName?.Replace(context.featureNameReplaceString, context.featureName);
            assemblyPackageGenerator.rootNamespace = assemblyPackageGenerator.rootNamespace?.Replace(context.featureNameReplaceString, context.featureName);
            EditorUtility.SetDirty(assemblyPackageGenerator);
            assemblyPackageGenerator.Generate(true);
        }
    }
}