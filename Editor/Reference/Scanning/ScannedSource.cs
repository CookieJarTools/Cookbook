using System.Collections.Generic;

namespace CookieJarTools.Cookbook.Editor.Reference.Scanning
{
    /// <summary>What reading a script found: each documentation comment by the key of what it is on, and the types declared.</summary>
    public sealed class ScannedSource
    {
        public Dictionary<string, string> Comments { get; } = new Dictionary<string, string>();

        public List<string> Types { get; } = new List<string>();
    }
}
