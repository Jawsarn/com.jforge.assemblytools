using System.Collections.Generic;
using UnityEngine;

namespace JForge.AssemblyTools.PackageGenerator.PostProcessors
{
    public interface IAssetPackageGeneratorAssetPostProcessor
    {
        void Setup(SetupPostProcessContext context);
        bool ShouldProcess(Object asset, PostProcessContext context);
        void Process(Object asset, PostProcessContext context);
    }
    
    public class SetupPostProcessContext
    {
        public List<IAssetPackageGeneratorAssetPostProcessor> postProcessors;
        public readonly IReadOnlyDictionary<Object, Object> oldToNewAssetMap;
        public readonly string featureName;
        public readonly string featureNameReplaceString;
        
        public SetupPostProcessContext(List<IAssetPackageGeneratorAssetPostProcessor> postProcessors,
            IReadOnlyDictionary<Object, Object> oldToNewAssetMap, string featureName, string featureNameReplaceString)
        {
            this.postProcessors = postProcessors;
            this.oldToNewAssetMap = oldToNewAssetMap;
            this.featureName = featureName;
            this.featureNameReplaceString = featureNameReplaceString;
        }
    }
    
    public class PostProcessContext
    {
        public readonly IReadOnlyList<IAssetPackageGeneratorAssetPostProcessor> postProcessors;
        public readonly IReadOnlyDictionary<Object, Object> oldToNewAssetMap;
        public readonly string featureName;
        public readonly string featureNameReplaceString;
        
        public PostProcessContext(IReadOnlyList<IAssetPackageGeneratorAssetPostProcessor> postProcessors, IReadOnlyDictionary<Object, Object> oldToNewAssetMap, string featureName, string featureNameReplaceString)
        {
            this.postProcessors = postProcessors;
            this.oldToNewAssetMap = oldToNewAssetMap;
            this.featureName = featureName;
            this.featureNameReplaceString = featureNameReplaceString;
        }
    }
}