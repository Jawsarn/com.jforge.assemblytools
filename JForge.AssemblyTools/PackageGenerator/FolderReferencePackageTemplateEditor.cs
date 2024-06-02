using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace JForge.AssemblyTools.PackageGenerator
{
    [CustomEditor(typeof(DirectoryReferencePackageTemplate))]
    public class FolderReferencePackageTemplateEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var script = new PropertyField(serializedObject.FindProperty("m_Script"), "Script");
            var folderReference = new PropertyField(serializedObject.FindProperty(nameof(DirectoryReferencePackageTemplate.directoryReference)), "Folder Reference");
            var folderPath = new PropertyField(serializedObject.FindProperty(nameof(DirectoryReferencePackageTemplate.directoryPath)), "Folder Path");
            var packageNameReplaceString = new PropertyField(serializedObject.FindProperty(nameof(DirectoryReferencePackageTemplate.packageNameReplaceString)), "PackageName Replace String");
            
            root.Add(script);
            root.Add(folderReference);
            root.Add(folderPath);
            root.Add(packageNameReplaceString);
            
            script.SetEnabled(false);
            folderReference.SetEnabled(false);
            folderPath.SetEnabled(false);
            
            var folderReferencePackageTemplate = (DirectoryReferencePackageTemplate)target;
            
            var previewRoot = new VisualElement();
            GenerateStructureHierarchyPreview(previewRoot, folderReferencePackageTemplate);
            
            var generateButton = new Button(() => {
                var path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        var relativePath = $"Assets{path.Substring(Application.dataPath.Length)}";
                        folderReferencePackageTemplate.directoryReference = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
                        folderReferencePackageTemplate.directoryPath = $"{relativePath}/";
                        EditorUtility.SetDirty(folderReferencePackageTemplate);
                        GenerateStructureHierarchyPreview(previewRoot, folderReferencePackageTemplate);
                    }
                    else
                    {
                        Debug.LogError("Selected folder is not within the project's Assets folder");
                    }
                }
            }) {
                text = "Select Folder",
            };
            generateButton.style.marginTop = 10;
            generateButton.style.height = 30;
            generateButton.style.fontSize = 14;
            root.Add(generateButton);
            root.Add(previewRoot);
            
            return root;
        }

        private void GenerateStructureHierarchyPreview(VisualElement containerElement,
            DirectoryReferencePackageTemplate directoryReferencePackageTemplate)
        {
            containerElement.Clear();
            var path = directoryReferencePackageTemplate.directoryPath;
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            
            var folder = new DirectoryInfo(path);
            var indentLevel = 0;
            AddItem(containerElement, folder, indentLevel, true);
        }
        
        private void AddItem(VisualElement containerElement, FileSystemInfo item, int indentLevel, bool isLast)
        {
            const int indentPerLevel = 40;  // Base indent space per level
            // var indent = new string(' ', indentLevel * indentPerLevel);
            var branch = isLast ? "└── " : "├── ";

            // Create label for the folder or file
            var itemLabel = new Label(branch + item.Name);
            containerElement.Add(itemLabel);
            itemLabel.style.marginLeft = indentPerLevel * indentLevel;

            // Check if the item is a directory and process accordingly
            if (item is not DirectoryInfo dir)
            {
                return;
            }
            // Set label bolded as it is a folder
            itemLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Get all subfolders and files
            var subFolders = dir.GetDirectories();
            var files = dir.GetFiles();
            var totalItems = subFolders.Length + files.Length;
            var itemCount = 0;

            // Add files under this folder
            foreach (var file in files)
            {
                itemCount++;
                AddItem(containerElement, file, indentLevel + 1, itemCount == totalItems);
            }
            
            // Add all subfolders recursively
            foreach (var subFolder in subFolders)
            {
                itemCount++;
                AddItem(containerElement, subFolder, indentLevel + 1, itemCount == totalItems);
            }
        }
    }
}