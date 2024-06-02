using System.IO;
using JForge.AssemblyTools.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JForge.AssemblyTools.PackageGenerator.PostProcessors
{
    public class AssemblyDefinitionProcessor : IAssetPackageGeneratorAssetPostProcessor
    {
        public void Setup(SetupPostProcessContext context) { }

        public bool ShouldProcess(Object asset, PostProcessContext context)
        {
            return asset is AssemblyDefinitionAsset;
        }

        public void Process(Object asset, PostProcessContext context)
        {
            var assemblyDefinitionAsset = (AssemblyDefinitionAsset)asset;

            var assemblySerializer = new AssemblySerializer();
            if (!assemblySerializer.TryDeserialize(assemblyDefinitionAsset.text))
            {
                Debug.LogError($"Could not deserialize {nameof(assemblyDefinitionAsset)}");
                return;
            }
            
            var anyInformationUpdated = false;
            anyInformationUpdated |= TryUpdateAssemblyReferences(assemblyDefinitionAsset, assemblySerializer, context);
            anyInformationUpdated |= TryUpdateAssemblyName(assemblyDefinitionAsset, assemblySerializer, context);
            anyInformationUpdated |= TryUpdateRootNamespace(assemblyDefinitionAsset, assemblySerializer, context);
            
            if (!anyInformationUpdated)
            {
                return;
            }
            
            var content = assemblySerializer.SerializeToString();
            var assemblyDefinitionPath = AssetDatabase.GetAssetPath(assemblyDefinitionAsset);
            File.WriteAllText(assemblyDefinitionPath, content);
            AssetDatabase.ImportAsset(assemblyDefinitionPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private bool TryUpdateAssemblyReferences(AssemblyDefinitionAsset assemblyDefinitionAsset, AssemblySerializer assemblySerializer, PostProcessContext context)
        {
            var anyInformationUpdated = false;
            var references = assemblySerializer.GetReferencesList();
            for (int i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                if (!context.oldToNewAssetMap.TryGetValue(reference, out var newReference) ||
                    newReference is not AssemblyDefinitionAsset referenceAsset)
                {
                    continue;
                }
                
                references[i] = referenceAsset;
                anyInformationUpdated = true;
            }

            if (anyInformationUpdated)
            {
                assemblySerializer.SetReferences(references);
            }

            return anyInformationUpdated;
        }
        
        private bool TryUpdateAssemblyName(AssemblyDefinitionAsset assemblyDefinitionAsset, AssemblySerializer assemblySerializer, PostProcessContext context)
        {
            var assemblyName = assemblySerializer.GetAssemblyName();
            if (string.IsNullOrEmpty(assemblyName) || !assemblyName.Contains(context.featureNameReplaceString))
            {
                return false;
            }
            
            assemblyName = assemblyName.Replace(context.featureNameReplaceString, context.featureName);
            assemblySerializer.SetAssemblyName(assemblyName);
            return true;
        }
        
        private bool TryUpdateRootNamespace(AssemblyDefinitionAsset assemblyDefinitionAsset, AssemblySerializer assemblySerializer, PostProcessContext context)
        {
            var rootNamespace = assemblySerializer.GetRootNamespace();
            if (string.IsNullOrEmpty(rootNamespace) || !rootNamespace.Contains(context.featureNameReplaceString))
            {
                return false;
            }
            
            rootNamespace = rootNamespace.Replace(context.featureNameReplaceString, context.featureName);
            assemblySerializer.SetRootNamespace(rootNamespace);
            return true;
        }
    }
}