using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;

namespace JForge.AssemblyTools.Utility
{
    public class AssemblySerializer
    {
        private const string GuidPrefix = "GUID:";
        private bool _useGUID;
        private JObject _assemblyObject;

        public bool TryDeserialize(string assemblyContent)
        {
            _assemblyObject = JsonConvert.DeserializeObject<JObject>(assemblyContent);
            if (_assemblyObject != null)
            {
                EvaluateUseGUIDReferences();
            }
            return _assemblyObject != null;
        }

        private void EvaluateUseGUIDReferences()
        {
            _useGUID = TryGetReferences(out var references) ? AnyExistingReferencesUseGUID(references) : AssemblyToolsSettings.DefaultUseGuidReferences;
        }

        // False for both a missing array and an empty one - callers treat "no references" and
        // "zero references" the same way, so there's no reason to distinguish them.
        private bool TryGetReferences(out JArray references)
        {
            references = _assemblyObject["references"] as JArray;
            return references != null && references.Count > 0;
        }

        private JArray GetOrCreateReferencesArray()
        {
            var references = _assemblyObject["references"] as JArray;
            if (references == null)
            {
                references = new JArray();
                _assemblyObject["references"] = references;
            }

            return references;
        }

        private static bool AnyExistingReferencesUseGUID(JArray references)
        {
            foreach (var referenceToken in references)
            {
                if (((string)referenceToken).StartsWith(GuidPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetAssemblyName()
        {
            return (string)_assemblyObject["name"];
        }

        public void SetAssemblyName(string assemblyName)
        {
            _assemblyObject["name"] = assemblyName;
        }

        public string GetRootNamespace()
        {
            return (string)_assemblyObject["rootNamespace"];
        }

        public void SetRootNamespace(string rootNamespace)
        {
            _assemblyObject["rootNamespace"] = rootNamespace;
        }

        public void AddReferences(IEnumerable<AssemblyDefinitionAsset> additionalReferences)
        {
            var references = GetOrCreateReferencesArray();

            foreach (var additionalReference in additionalReferences)
            {
                if (additionalReference == null)
                {
                    continue;
                }

                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(additionalReference));
                var referenceValue = _useGUID ? $"{GuidPrefix}{guid}" : additionalReference.name;

                // Guards against duplicate entries, e.g. when the inspector's list "+" button duplicates the
                // previous entry, or the same reference already came from the base assembly.
                if (ContainsReference(references, referenceValue))
                {
                    continue;
                }

                references.Add(referenceValue);
            }
        }

        private static bool ContainsReference(JArray references, string referenceValue)
        {
            foreach (var existingReference in references)
            {
                if (string.Equals((string)existingReference, referenceValue, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetReferences(IEnumerable<AssemblyDefinitionAsset> references)
        {
            var referencesArray = GetOrCreateReferencesArray();
            referencesArray.Clear();
            foreach (var reference in references)
            {
                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(reference));
                referencesArray.Add(_useGUID ? $"{GuidPrefix}{guid}" : reference.name);
            }
        }

        public List<AssemblyDefinitionAsset> GetReferencesList()
        {
            if (!TryGetReferences(out var referencesArray))
            {
                return new List<AssemblyDefinitionAsset>();
            }

            var references = new List<AssemblyDefinitionAsset>(referencesArray.Count);
            foreach (var referenceToken in referencesArray)
            {
                var referenceString = (string)referenceToken;

                // GUID references resolve directly; name references go through Unity's own assembly-name
                // index instead of a linear AssetDatabase.FindAssets search - faster, and always current
                // since Unity maintains that index itself rather than us caching it.
                var path = referenceString.StartsWith(GuidPrefix)
                    ? AssetDatabase.GUIDToAssetPath(referenceString.Substring(GuidPrefix.Length))
                    : CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(referenceString);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

                // If it's null, it may mean that we just haven't included some packages in our project which should be fine
                if (asset != null)
                {
                    references.Add(asset);
                }
            }

            return references;
        }

        public string SerializeToString()
        {
            return JsonConvert.SerializeObject(_assemblyObject, Formatting.Indented);
        }
    }
}
