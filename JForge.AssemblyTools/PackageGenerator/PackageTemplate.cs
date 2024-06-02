using UnityEngine;

namespace JForge.AssemblyTools
{
    public abstract class PackageTemplate : ScriptableObject
    {
        public abstract bool GeneratePackage(string packageName, string destinationPath);
    }
}