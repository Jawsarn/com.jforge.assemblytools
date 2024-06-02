using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JForge.AssemblyTools.PackageGenerator
{
    [CustomEditor(typeof(AssemblyPackageGenerator))]
    public class AssemblyPackageGeneratorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var script = new PropertyField(serializedObject.FindProperty("m_Script"), "Script");
            var featureName = new PropertyField(serializedObject.FindProperty(nameof(AssemblyPackageGenerator.generatedPackageName)), "Generated Package Name");
            var packageTemplate = new PropertyField(serializedObject.FindProperty(nameof(AssemblyPackageGenerator.packageTemplate)), "Package Template");
            
            root.Add(script);
            root.Add(featureName);
            root.Add(packageTemplate);
            
            script.SetEnabled(false);
            
            var generateButton = new Button(() => {
                var assemblyPackageGenerator = (AssemblyPackageGenerator)target;
                if (assemblyPackageGenerator.packageTemplate != null)
                {
                    var destinationPath = $"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(assemblyPackageGenerator))}\\";
                    assemblyPackageGenerator.packageTemplate.GeneratePackage(assemblyPackageGenerator.generatedPackageName, destinationPath);
                }
            }) {
                text = "Generate Package",
            };
            generateButton.style.marginTop = 10;
            generateButton.style.height = 30;
            generateButton.style.fontSize = 14;
            root.Add(generateButton);
            
            return root;
        }
    }
}