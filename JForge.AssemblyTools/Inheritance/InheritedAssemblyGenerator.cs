using System;
using System.Collections.Generic;
using System.IO;
using JForge.AssemblyTools.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JForge.AssemblyTools.Inheritance
{
    [CreateAssetMenu(fileName = nameof(InheritedAssemblyGenerator), menuName = PackageUtilities.CreateAssetMenuPath + nameof(InheritedAssemblyGenerator))]
    public class InheritedAssemblyGenerator : ScriptableObject
    {
        public AssemblyDefinitionAsset assemblyDefinitionBase;
        public string assemblyName;
        public string assemblyFileName;
        public string rootNamespace;
        public List<AssemblyDefinitionAsset> existingReferences;
        public List<AssemblyDefinitionAsset> additionalReferences;
        public AssemblyDefinitionAsset generatedDefinition;
        
        private void OnValidate()
        {
            // We delay the generation to avoid issues with the asset database
            EditorApplication.delayCall += GenerateDelayed;
        }

        private void GenerateDelayed()
        {
            if (this == null)
            {
                return;
            }
            
            Generate(false);
        }

        public virtual string GetAssemblyName()
        {
            return string.IsNullOrEmpty(assemblyName) ? name : assemblyName;
        }
        
        public virtual string GetAssemblyFileName()
        {
            return string.IsNullOrEmpty(assemblyFileName) ? name : assemblyFileName;
        }
        
        public virtual void Generate(bool forced = false)
        {
            if (assemblyDefinitionBase == null)
            {
                if (forced)
                {
                    Debug.LogError($"Missing {nameof(assemblyDefinitionBase)}", this);
                }
                return;
            }
            
            var assemblySerializer = new AssemblySerializer();
            if (!assemblySerializer.TryDeserialize(assemblyDefinitionBase.text))
            {
                if (forced)
                {
                    Debug.LogError($"Could not deserialize {nameof(assemblyDefinitionBase)}", this);
                }
                return;
            }
            
            assemblySerializer.SetAssemblyName(GetAssemblyName());
            if(!string.IsNullOrEmpty(rootNamespace))
            {
                assemblySerializer.SetRootNamespace(rootNamespace);
            }
            CacheExistingReferences(assemblySerializer.GetReferencesList());
            assemblySerializer.AddReferences(additionalReferences);
            
            var assemblyContent = assemblySerializer.SerializeToString();
            var generatorPath = AssetDatabase.GetAssetPath(this);
            var generatorDirectory = Path.GetDirectoryName(generatorPath);
            if (generatorDirectory == null)
            {
                return;
            }
            
            var assemblyDefinitionPath = Path.Combine(generatorDirectory, GetAssemblyFileName());
            if (!assemblyDefinitionPath.EndsWith(UnityFileExtensions.AssemblyDefinition))
            {
                assemblyDefinitionPath += UnityFileExtensions.AssemblyDefinition;
            }
            
            if (!ShouldGenerate(assemblyContent, assemblyDefinitionPath))
            {
                return;
            }

            GenerateAssemblyDefinition(assemblyDefinitionPath, assemblyContent);
        }

        private void CacheExistingReferences(IEnumerable<AssemblyDefinitionAsset> referenceAssets)
        {
            existingReferences.Clear();
            foreach (var asset in referenceAssets)
            {
                existingReferences.Add(asset);
                additionalReferences.Remove(asset);
            }
        }
        
        private bool ShouldGenerate(string content, string assemblyDefinitionPath)
        {
            if (generatedDefinition == null || !content.Equals(generatedDefinition.text)) 
            {
                return true;
            }

            var existingGeneratedDefinitionPath = AssetDatabase.GetAssetPath(generatedDefinition);
            return !string.Equals(Path.GetFullPath(existingGeneratedDefinitionPath), Path.GetFullPath(assemblyDefinitionPath), StringComparison.OrdinalIgnoreCase); // This might be issue for linux
        }
        
        private void GenerateAssemblyDefinition(string assemblyDefinitionPath, string content)
        {
            TryRemovePreviousGeneratedAssembly(assemblyDefinitionPath);
            File.WriteAllText(assemblyDefinitionPath, content);
            AssetDatabase.ImportAsset(assemblyDefinitionPath, ImportAssetOptions.ForceUpdate);
            generatedDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assemblyDefinitionPath);
            Debug.Log($"Generated assembly definition: {assemblyDefinitionPath}", generatedDefinition);
        }
        
        private void TryRemovePreviousGeneratedAssembly(string assemblyDefinitionPath)
        {
            if (generatedDefinition == null)
            {
                return;
            }
            
            var existingGeneratedDefinitionPath = AssetDatabase.GetAssetPath(generatedDefinition);
            if (ArePathsInSameFolder(assemblyDefinitionPath, existingGeneratedDefinitionPath))
            {
                File.Delete(existingGeneratedDefinitionPath);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("Ignoring deletion of exited generated definition as it resides in different directory.", generatedDefinition);
            }
            generatedDefinition = null;
        }
        
        private static bool ArePathsInSameFolder(string pathA, string pathB)
        {
            return Path.GetDirectoryName(pathA) == Path.GetDirectoryName(pathB);
        }
    }
}