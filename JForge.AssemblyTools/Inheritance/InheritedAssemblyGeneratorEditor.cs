using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JForge.AssemblyTools.Inheritance
{
    [CustomEditor(typeof(InheritedAssemblyGenerator))]
    public class InheritedAssemblyGeneratorEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var script = new PropertyField(serializedObject.FindProperty("m_Script"), "Script");
            var assemblyDefinitionBase = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyDefinitionBase)), "Assembly Definition Base");
            var assemblyName = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyName)), "Assembly Name");
            var assemblyFileName = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyFileName)), "Assembly File Name");
            var rootNamespace = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.rootNamespace)), "Root Namespace");
            var existingReferencesList = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.existingReferences)), "Inherited References");
            var additionalReferencesList = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.additionalReferences)), "Additional References");
            var generatedDefinition = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.generatedDefinition)), "Generated Definition");
            
            root.Add(script);
            root.Add(assemblyDefinitionBase);
            root.Add(assemblyName);
            root.Add(assemblyFileName);
            root.Add(rootNamespace);
            root.Add(existingReferencesList);
            root.Add(additionalReferencesList);
            root.Add(generatedDefinition);
            
            script.SetEnabled(false);
            existingReferencesList.SetEnabled(false);
            generatedDefinition.SetEnabled(false);
            
            var generateButton = new Button(() => {
                var inheritedAssemblyGenerator = (InheritedAssemblyGenerator)target;
                inheritedAssemblyGenerator.Generate();
            }) {
                text = "Force Regenerate Assembly",
            };
            generateButton.style.marginTop = 10;
            generateButton.style.height = 30;
            generateButton.style.fontSize = 14;
            root.Add(generateButton);
    
            return root;
        }
    }
}