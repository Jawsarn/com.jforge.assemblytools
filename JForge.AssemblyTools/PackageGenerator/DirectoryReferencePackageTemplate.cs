using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JForge.AssemblyTools.PackageGenerator.PostProcessors;
using JForge.AssemblyTools.Inheritance;
using JForge.AssemblyTools.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JForge.AssemblyTools.PackageGenerator
{
    [CreateAssetMenu(fileName = nameof(DirectoryReferencePackageTemplate), menuName = PackageUtilities.CreateAssetMenuPath + nameof(DirectoryReferencePackageTemplate))]
    public class DirectoryReferencePackageTemplate : PackageTemplate
    {
        public DefaultAsset directoryReference;
        public string directoryPath;
        public string packageNameReplaceString = "#";
        
        public override bool GeneratePackage(string packageName, string destinationPath)
        {
            return GeneratePackageToDirectory(packageName, directoryPath, destinationPath);
        }

        private bool GeneratePackageToDirectory(string packageName, string sourceFolderRelativePath, string destinationFolderRelativePath)
        {
            if (!AssetDatabase.IsValidFolder(sourceFolderRelativePath))
            {
                Debug.LogError("Source directory path is invalid: " + sourceFolderRelativePath);
                return false;
            }
            if (!AssetDatabase.IsValidFolder(destinationFolderRelativePath))
            {
                Debug.LogError("Destination directory path is invalid: " + destinationFolderRelativePath);
                return false;
            }
            
            // SourcePath is the path to the folder, which should also be copied
            var oldNewAssetsMap = new Dictionary<Object, Object>();
            var folderName = new DirectoryInfo(sourceFolderRelativePath).Name.Replace(packageNameReplaceString, packageName);
            var newFolderPath = Path.Combine(destinationFolderRelativePath, folderName);
            if (!AssetDatabase.IsValidFolder(newFolderPath))
            {
                AssetDatabase.CreateFolder(destinationFolderRelativePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), folderName);
            }
        
            RecursivelyCopyAssets(packageNameReplaceString, packageName, sourceFolderRelativePath, newFolderPath,oldNewAssetsMap);
            PostProcessAssets(packageNameReplaceString, packageName, oldNewAssetsMap);
            return true;
        }

        private static void RecursivelyCopyAssets(string packageNameReplaceString, string packageName, 
            string sourceFolderRelativePath, string destinationRelativePath, IDictionary<Object, Object> oldNewAssetsMap)
        {
            var assetGUIDs = AssetDatabase.FindAssets("", new[] { sourceFolderRelativePath });

            // Maintain a list of directories to ensure they're created in the destination
            var verifiedDirectories = new HashSet<string>();

            foreach (var guid in assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Determine the relative path for the asset or folder
                var relativeAssetPath = assetPath.Substring(sourceFolderRelativePath.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                relativeAssetPath = relativeAssetPath.Replace(packageNameReplaceString, packageName);
                var destinationAssetPath = Path.Combine(destinationRelativePath, relativeAssetPath);

                // Directory Handling: Create folder structure in destination if it doesn't exist
                var directoryPath = Path.GetDirectoryName(destinationAssetPath);
                if (!verifiedDirectories.Contains(directoryPath))
                {
                    CreateSubDirectoriesToPath(directoryPath, verifiedDirectories);
                }

                // Only copy files; folders are already handled
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    if (assetPath.EndsWith(".meta")) // Skip meta files in direct copying
                        continue;

                    // If is assembly definition type and we have a inheritedAssemblyGenerator asset in same folder, we skip it
                    if (assetPath.EndsWith(".asmdef") && AssetDatabase.LoadAllAssetsAtPath(assetPath).Any(a => a is InheritedAssemblyGenerator))
                    {
                        Debug.Log("Skipping asmdef");
                        continue;
                    }
                    
                    if (AssetDatabase.LoadAssetAtPath<Object>(destinationAssetPath) != null)
                    {
                        AssetDatabase.DeleteAsset(destinationAssetPath);
                    }

                    AssetDatabase.CopyAsset(assetPath, destinationAssetPath);
                    Debug.Log($"Copying {assetPath} to {destinationAssetPath}");

                    var oldAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    var newAsset = AssetDatabase.LoadAssetAtPath<Object>(destinationAssetPath);
                    oldNewAssetsMap.Add(oldAsset, newAsset);
                }
            }
            AssetDatabase.Refresh();
        }

        private static void CreateSubDirectoriesToPath(string directoryPath, ISet<string> createdDirectories)
        {
            var subFolders = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var currentPath = subFolders[0];
            for (int i = 1; i < subFolders.Length; i++)
            {
                var nextFolder = subFolders[i];
                var nextPath = Path.Combine(currentPath, nextFolder);
                if (!AssetDatabase.IsValidFolder(nextPath) && !createdDirectories.Contains(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, nextFolder);
                }

                currentPath = nextPath;
                createdDirectories.Add(nextPath);
            }
        }

        private void PostProcessAssets(string packageNameReplaceString, string packageName, Dictionary<Object, Object> oldNewAssetsMap)
        {
            var postProcessorTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IAssetPackageGeneratorAssetPostProcessor)))
                .Where(t => !t.IsAbstract);
            
            var postProcessors = postProcessorTypes
                .Select(t => (IAssetPackageGeneratorAssetPostProcessor)Activator.CreateInstance(t))
                .ToList();
            
            var setupContext = new SetupPostProcessContext(new List<IAssetPackageGeneratorAssetPostProcessor>(postProcessors), 
                oldNewAssetsMap, packageName, packageNameReplaceString);
            
            foreach (var processor in postProcessors)
            {
                processor.Setup(setupContext);
            }
            
            // We allow user to remove or add processors in the setup context
            var finalProcessors = setupContext.postProcessors;
            var processContext = new PostProcessContext(finalProcessors, oldNewAssetsMap, packageName, packageNameReplaceString);
            
            var items = oldNewAssetsMap.Values.ToArray();
            foreach (var item in items)
            {
                foreach (var processor in finalProcessors)
                {
                    if(processor.ShouldProcess(item, processContext))
                    {
                        processor.Process(item, processContext);
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        private void OnValidate()
        {
            var oldPath = directoryPath;
            var newPath = directoryReference != null ? $"{AssetDatabase.GetAssetPath(directoryReference)}/" : null;
            if (oldPath == newPath)
            {
                return;
            }
            
            directoryPath = newPath;
            EditorUtility.SetDirty(this);
        }
    }
}