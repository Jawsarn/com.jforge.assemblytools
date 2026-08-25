using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JForge.AssemblyTools.Utility
{
    internal static class AssemblyToolsSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/JForge Assembly Tools", SettingsScope.Project)
            {
                label = "Assembly Tools",
                activateHandler = (searchContext, root) =>
                {
                    root.style.marginLeft = 10;
                    root.style.marginTop = 10;

                    var header = new Label("Assembly Tools")
                    {
                        style =
                        {
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 14,
                        },
                    };
                    root.Add(header);

                    var toggle = new Toggle("Default To GUID References")
                    {
                        value = AssemblyToolsSettings.DefaultUseGuidReferences,
                        tooltip = "When a generated assembly's reference style can't be inferred from an existing " +
                            "reference (e.g. the base assembly has none yet), default to GUID-based references. " +
                            "GUID references survive renames; name-based references are more readable but break " +
                            "if the referenced assembly is renamed.",
                    };
                    toggle.style.marginTop = 10;

                    // AssemblyToolsSettings.DefaultUseGuidReferences's setter persists to disk itself
                    // (it's a ScriptableSingleton, not an AssetDatabase-tracked asset), so no separate
                    // SerializedObject/Save() step is needed here.
                    toggle.RegisterValueChangedCallback(evt => AssemblyToolsSettings.DefaultUseGuidReferences = evt.newValue);

                    root.Add(toggle);
                },
                keywords = new[] { "Assembly", "Reference", "GUID" },
            };
        }
    }
}
