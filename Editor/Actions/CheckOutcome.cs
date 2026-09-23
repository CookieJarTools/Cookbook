namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>What a check found: passed or not, with a line for the reader either way.</summary>
    public sealed class CheckOutcome
    {
        public CheckOutcome(bool passed, string message)
        {
            Passed = passed;
            Message = message;
        }

        public bool Passed { get; }

        public string Message { get; }
    }
}
