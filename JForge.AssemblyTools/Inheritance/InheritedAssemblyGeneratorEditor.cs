using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace JForge.AssemblyTools.Inheritance
{
    [CustomEditor(typeof(InheritedAssemblyGenerator))]
    public class InheritedAssemblyGeneratorEditor : Editor
    {
        private HelpBox _duplicateReferencesWarning;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var script = new PropertyField(serializedObject.FindProperty("m_Script"), "Script");
            var assemblyDefinitionBase = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyDefinitionBase)), "Assembly Definition Base");

            // These are plain text fields, so a normal bound field/PropertyField would regenerate on every
            // keystroke. isDelayed makes the field (and therefore the bound property and OnValidate) only
            // update on Enter/focus-lost. Reference fields below don't need this - picking an object is
            // already a single discrete change, not a stream of keystrokes.
            var assemblyName = new TextField("Assembly Name") { isDelayed = true };
            assemblyName.AddToClassList("unity-base-field__aligned");
            assemblyName.BindProperty(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyName)));

            var assemblyFileName = new TextField("Assembly File Name") { isDelayed = true };
            assemblyFileName.AddToClassList("unity-base-field__aligned");
            assemblyFileName.BindProperty(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.assemblyFileName)));

            var rootNamespace = new TextField("Root Namespace") { isDelayed = true };
            rootNamespace.AddToClassList("unity-base-field__aligned");
            rootNamespace.BindProperty(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.rootNamespace)));

            var existingReferencesList = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.existingReferences)), "Inherited References");
            var additionalReferencesList = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.additionalReferences)), "Additional References");
            var generatedDefinition = new PropertyField(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.generatedDefinition)), "Generated Definition");

            _duplicateReferencesWarning = new HelpBox(string.Empty, HelpBoxMessageType.Warning) { style = { display = DisplayStyle.None } };

            root.Add(script);
            root.Add(assemblyDefinitionBase);
            root.Add(assemblyName);
            root.Add(assemblyFileName);
            root.Add(rootNamespace);
            root.Add(existingReferencesList);
            root.Add(additionalReferencesList);
            root.Add(_duplicateReferencesWarning);
            root.Add(generatedDefinition);

            root.TrackPropertyValue(serializedObject.FindProperty(nameof(InheritedAssemblyGenerator.additionalReferences)), RefreshDuplicateReferencesWarning);
            RefreshDuplicateReferencesWarning();
            
            script.SetEnabled(false);
            existingReferencesList.SetEnabled(false);
            generatedDefinition.SetEnabled(false);
            
            var generateButton = new Button(() => {
                var inheritedAssemblyGenerator = (InheritedAssemblyGenerator)target;
                inheritedAssemblyGenerator.TryGenerate(true);
            }) {
                text = "Force Regenerate Assembly",
            };
            generateButton.style.marginTop = 10;
            generateButton.style.height = 30;
            generateButton.style.fontSize = 14;
            root.Add(generateButton);
    
            return root;
        }

        private void RefreshDuplicateReferencesWarning(SerializedProperty changedProperty = null)
        {
            var generator = (InheritedAssemblyGenerator)target;
            var duplicates = generator.GetDuplicateAdditionalReferences();
            if (duplicates.Count == 0)
            {
                _duplicateReferencesWarning.style.display = DisplayStyle.None;
                return;
            }

            var names = new List<string>();
            foreach (var duplicate in duplicates)
            {
                names.Add(duplicate.name);
            }

            _duplicateReferencesWarning.text = $"Additional References has a duplicate entry (likely from clicking \"+\", which duplicates the last entry) for: {string.Join(", ", names)}. The duplicate is ignored when generating - remove it here.";
            _duplicateReferencesWarning.style.display = DisplayStyle.Flex;
        }
    }
}