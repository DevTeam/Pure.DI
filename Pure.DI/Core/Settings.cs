namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class Settings : ISettings
    {
        private readonly Func<IBuildContext> _buildContext;
        private readonly IFileSystem _fileSystem;

        public Settings(
            Func<IBuildContext> buildContext, 
            IFileSystem fileSystem)
        {
            _buildContext = buildContext;
            _fileSystem = fileSystem;
        }

        public bool TryGetOutputPath(out string outputPath)
        {
            if (!GetSettings().TryGetValue(Setting.Output, out var output) || output == null)
            {
                outputPath = string.Empty;
                return false;
            }

            if (!Path.IsPathRooted(output))
            {
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), output);
            }

            _fileSystem.CreateDirectory(output);
            outputPath = output;
            return true;
        }

        private IDictionary<Setting, string> GetSettings() => _buildContext().Metadata.Settings;
    }
}