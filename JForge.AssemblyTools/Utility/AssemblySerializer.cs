using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JForge.AssemblyTools.Utility
{
    public class AssemblySerializer
    {
        private const string GuidPrefix = "GUID:";
        private bool _useGUID;
        private dynamic _assemblyObject;

        public bool TryDeserialize(string assemblyContent, bool defaultUseGUID = false)
        {
            _assemblyObject = JsonConvert.DeserializeObject(assemblyContent);
            if (_assemblyObject != null)
            {
                EvaluateUseGUIDReferences(defaultUseGUID);
            }
            return _assemblyObject != null;
        }

        private void EvaluateUseGUIDReferences(bool defaultUseGuid)
        {
            _useGUID = HasReferences() ? AnyExistingReferencesUseGUID() : defaultUseGuid;
        }

        private bool HasReferences()
        {
            return _assemblyObject.references != null && _assemblyObject.references.Count > 0;
        }

        private bool AnyExistingReferencesUseGUID()
        {
            foreach (string referenceString in _assemblyObject.references)
            {
                if (referenceString.StartsWith(GuidPrefix))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetAssemblyName()
        {
            return _assemblyObject["name"];
        }
        
        public void SetAssemblyName(string assemblyName)
        {
            _assemblyObject["name"] = assemblyName;
        }
        
        public string GetRootNamespace()
        {
            return _assemblyObject["rootNamespace"];
        }
        
        public void SetRootNamespace(string rootNamespace)
        {
            _assemblyObject["rootNamespace"] = rootNamespace;
        }

        public void AddReferences(IEnumerable<AssemblyDefinitionAsset> additionalReferences)
        {            
            if (_assemblyObject.references == null)
            {
                _assemblyObject.references = new JArray();
            }
            
            foreach (var additionalReference in additionalReferences)
            {
                if (additionalReference == null)
                {
                    continue;
                }
            
                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(additionalReference));
                _assemblyObject.references.Add(_useGUID ? $"{GuidPrefix}{guid}" : additionalReference.name);
            }
        }
        
        public void SetReferences(IEnumerable<AssemblyDefinitionAsset> references)
        {
            if (_assemblyObject.references == null)
            {
                _assemblyObject.references = new JArray();
            }
            
            _assemblyObject.references.Clear();
            foreach (var reference in references)
            {
                var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(reference));
                _assemblyObject.references.Add(_useGUID ? $"{GuidPrefix}{guid}" : reference.name);
            }
        }

        public List<AssemblyDefinitionAsset> GetReferencesList()
        {
            if (!HasReferences())
            {
                return new List<AssemblyDefinitionAsset>();
            }
        
            var references = new List<AssemblyDefinitionAsset>(_assemblyObject.references.Count);
            foreach (string referenceString in _assemblyObject.references)
            {
                var guid = "";
                if (referenceString.StartsWith(GuidPrefix))
                {
                    guid = referenceString.Replace(GuidPrefix, "");
                }
                else
                {
                    var guids = AssetDatabase.FindAssets(referenceString + " t:asmdef");
                    if (guid.Length > 1)
                    {
                        Debug.LogError($"Multiple assembly definitions with the same name found of name: {referenceString}. Using the first one.");
                    }
                    
                    if (guids.Length > 0)
                    {
                        guid = guids[0];
                    }
                }
                var path = AssetDatabase.GUIDToAssetPath(guid);
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