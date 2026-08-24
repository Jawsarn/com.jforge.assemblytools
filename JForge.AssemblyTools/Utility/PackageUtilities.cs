using System;

namespace JForge.AssemblyTools.Utility
{
    public static class PackageUtilities
    {
        public const string CreateAssetMenuPath = "JForge/AssemblyTools/";

        /// <summary>
        /// The command-line argument name used by this package's <c>-executeMethod</c> entry points to pass a
        /// target asset path, since <c>-executeMethod</c> only supports parameterless static methods.
        /// </summary>
        public const string CommandLineTargetArgName = "-jforgeTarget";

        /// <summary>
        /// Reads the value following <paramref name="argName"/> in the process's command-line arguments (e.g.
        /// <c>-jforgeTarget Assets/Foo/MyGenerator.asset</c>), or null if it isn't present.
        /// </summary>
        public static string GetCommandLineArgValue(string argName)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
