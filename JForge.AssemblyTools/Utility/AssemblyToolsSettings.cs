using UnityEditor;
using UnityEngine;

namespace JForge.AssemblyTools.Utility
{
    /// <summary>
    /// Project-wide settings for this package, persisted at <c>ProjectSettings/JForgeAssemblyToolsSettings.asset</c>
    /// (checked into source control, shared by the whole team) via <see cref="ScriptableSingleton{T}"/>.
    /// <c>ProjectSettings/</c> isn't part of the AssetDatabase, so a regular <c>AssetDatabase.CreateAsset</c>-backed
    /// ScriptableObject can't live there - <see cref="ScriptableSingleton{T}"/> is Unity's mechanism for exactly
    /// this: an editor-only singleton with its own file-based persistence, independent of the AssetDatabase.
    /// Editable via Edit &gt; Project Settings &gt; JForge Assembly Tools (see <see cref="AssemblyToolsSettingsProvider"/>),
    /// by hand-editing the file directly, or via <see cref="DefaultUseGuidReferences"/>'s setter.
    /// </summary>
    [FilePath("ProjectSettings/JForgeAssemblyToolsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AssemblyToolsSettings : ScriptableSingleton<AssemblyToolsSettings>
    {
        [SerializeField]
        private bool defaultUseGuidReferences = true;

        /// <summary>
        /// Used by <see cref="AssemblySerializer"/> as the fallback reference style when a generated assembly's
        /// style can't be inferred from an existing reference (e.g. the base assembly has none yet). GUID
        /// references survive renames; name-based references are more readable but break if the referenced
        /// assembly is renamed. Setting this persists it to disk immediately.
        /// </summary>
        public static bool DefaultUseGuidReferences
        {
            get => instance.defaultUseGuidReferences;
            set
            {
                if (instance.defaultUseGuidReferences == value)
                {
                    return;
                }

                instance.defaultUseGuidReferences = value;
                instance.Save(true);
            }
        }
    }
}
