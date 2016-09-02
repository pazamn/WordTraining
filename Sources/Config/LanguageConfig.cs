using System.Diagnostics;

namespace WordTraining.Config
{
    [DebuggerDisplay("{Name} ({Alias}) - {IconPath}")]
    public class LanguageConfig
    {
        public string Alias { get; set; }
        public string IconPath { get; set; }
        public string Name { get; set; }
    }
}