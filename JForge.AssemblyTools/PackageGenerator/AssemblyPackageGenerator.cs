using JForge.AssemblyTools.Utility;
using UnityEngine;

namespace JForge.AssemblyTools.PackageGenerator
{
    [CreateAssetMenu(fileName = nameof(AssemblyPackageGenerator), menuName = PackageUtilities.CreateAssetMenuPath + nameof(AssemblyPackageGenerator))]
    public class AssemblyPackageGenerator : ScriptableObject
    {
        public string generatedPackageName;
        public PackageTemplate packageTemplate;
    }
}
