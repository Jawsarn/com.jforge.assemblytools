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
            // Doesn't touch the AssetDatabase and doesn't depend on assemblyDefinitionBase being set, so it's
            // safe (and necessary) to run this immediately - TryGenerate() below bails out before ever checking
            // for duplicates if the base assembly isn't assigned yet or fails to deserialize.
            WarnAboutDuplicateAdditionalReferences();

            // We delay the generation to avoid issues with the asset database
            EditorApplication.delayCall += GenerateDelayed;
        }

        private void GenerateDelayed()
        {
            if (this == null)
            {
                return;
            }
            
            TryGenerate(false);
        }

        public virtual string GetAssemblyName()
        {
            return string.IsNullOrEmpty(assemblyName) ? name : assemblyName;
        }
        
        public virtual string GetAssemblyFileName()
        {
            return string.IsNullOrEmpty(assemblyFileName) ? name : assemblyFileName;
        }
        
        public virtual bool TryGenerate(bool forced = false)
        {
            if (!TryComputeContent(forced, out var assemblyContent, out var assemblyDefinitionPath))
            {
                return false;
            }

            if (!ShouldGenerate(assemblyContent, assemblyDefinitionPath))
            {
                return false;
            }

            GenerateAssemblyDefinition(assemblyDefinitionPath, assemblyContent);
            return true;
        }
        
        public bool NeedsRegeneration()
        {
            return TryComputeContent(false, out var assemblyContent, out var assemblyDefinitionPath)
                && ShouldGenerate(assemblyContent, assemblyDefinitionPath);
        }

        private bool TryComputeContent(bool forced, out string assemblyContent, out string assemblyDefinitionPath)
        {
            assemblyContent = null;
            assemblyDefinitionPath = null;

            if (assemblyDefinitionBase == null)
            {
                if (forced)
                {
                    Debug.LogError($"Missing {nameof(assemblyDefinitionBase)}", this);
                }
                return false;
            }

            var assemblySerializer = new AssemblySerializer();
            if (!assemblySerializer.TryDeserialize(assemblyDefinitionBase.text))
            {
                if (forced)
                {
                    Debug.LogError($"Could not deserialize {nameof(assemblyDefinitionBase)}", this);
                }
                return false;
            }

            assemblySerializer.SetAssemblyName(GetAssemblyName());
            if(!string.IsNullOrEmpty(rootNamespace))
            {
                assemblySerializer.SetRootNamespace(rootNamespace);
            }
            CacheExistingReferences(assemblySerializer.GetReferencesList());
            if (forced)
            {
                // Non-forced calls come from OnValidate, which already warns directly and immediately;
                // avoid logging the same warning twice for every interactive edit.
                WarnAboutDuplicateAdditionalReferences();
            }
            assemblySerializer.AddReferences(additionalReferences);

            assemblyContent = assemblySerializer.SerializeToString();
            var generatorPath = AssetDatabase.GetAssetPath(this);
            var generatorDirectory = Path.GetDirectoryName(generatorPath);
            if (generatorDirectory == null)
            {
                assemblyContent = null;
                return false;
            }

            assemblyDefinitionPath = Path.Combine(generatorDirectory, GetAssemblyFileName());
            if (!assemblyDefinitionPath.EndsWith(UnityFileExtensions.AssemblyDefinition))
            {
                assemblyDefinitionPath += UnityFileExtensions.AssemblyDefinition;
            }

            return true;
        }

        private void CacheExistingReferences(IEnumerable<AssemblyDefinitionAsset> referenceAssets)
        {
            existingReferences.Clear();
            foreach (var asset in referenceAssets)
            {
                existingReferences.Add(asset);
                additionalReferences.Remove(asset);
            }

            // Mutating these directly (not via SerializedProperty) doesn't tell Unity this object changed -
            // without this, the change can be silently lost on the next domain reload.
            EditorUtility.SetDirty(this);
        }
        
        public HashSet<AssemblyDefinitionAsset> GetDuplicateAdditionalReferences()
        {
            var seen = new HashSet<AssemblyDefinitionAsset>();
            var duplicates = new HashSet<AssemblyDefinitionAsset>();
            foreach (var reference in additionalReferences)
            {
                if (reference == null)
                {
                    continue;
                }

                if (!seen.Add(reference))
                {
                    duplicates.Add(reference);
                }
            }

            return duplicates;
        }

        private void WarnAboutDuplicateAdditionalReferences()
        {
            foreach (var duplicate in GetDuplicateAdditionalReferences())
            {
                Debug.LogWarning($"{nameof(InheritedAssemblyGenerator)} '{name}' has a duplicate entry for '{duplicate.name}' in {nameof(additionalReferences)} - the extra entry is ignored when generating. Remove it to clear this warning.", this);
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
            EditorUtility.SetDirty(this);
            Debug.Log($"Generated assembly definition: {assemblyDefinitionPath}", generatedDefinition);
        }
        
        private void TryRemovePreviousGeneratedAssembly(string assemblyDefinitionPath)
        {
            if (generatedDefinition == null)
            {
                return;
            }

            var existingGeneratedDefinitionPath = AssetDatabase.GetAssetPath(generatedDefinition);
            if (ArePathsEqual(existingGeneratedDefinitionPath, assemblyDefinitionPath))
            {
                // Regenerating in place: leave the existing file (and its .meta/GUID) alone.
                // It will be overwritten with the new content, so the GUID stays stable for anything referencing it.
                return;
            }

            if (ArePathsInSameFolder(assemblyDefinitionPath, existingGeneratedDefinitionPath))
            {
                AssetDatabase.DeleteAsset(existingGeneratedDefinitionPath);
            }
            else
            {
                Debug.LogWarning("Ignoring deletion of exited generated definition as it resides in different directory.", generatedDefinition);
            }
            generatedDefinition = null;
        }

        private static bool ArePathsEqual(string pathA, string pathB)
        {
            return string.Equals(Path.GetFullPath(pathA), Path.GetFullPath(pathB), StringComparison.OrdinalIgnoreCase); // This might be issue for linux
        }

        private static bool ArePathsInSameFolder(string pathA, string pathB)
        {
            return Path.GetDirectoryName(pathA) == Path.GetDirectoryName(pathB);
        }
    }
}