using Object = UnityEngine.Object;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>How running an action went: done, cancelled by the reader, or failed — with what it made, if anything.</summary>
    public sealed class ActionOutcome
    {
        private ActionOutcome(bool done, bool cancelled, string message, Object made)
        {
            Done = done;
            Cancelled = cancelled;
            Message = message;
            Made = made;
        }

        public bool Done { get; }

        public bool Cancelled { get; }

        public string Message { get; }

        public Object Made { get; }

        public static ActionOutcome Finished(Object made, string message = null)
        {
            return new ActionOutcome(true, false, message ?? (made != null ? "Made " + made.name : "Done"), made);
        }

        public static ActionOutcome Cancel()
        {
            return new ActionOutcome(false, true, null, null);
        }

        public static ActionOutcome Failed(string message)
        {
            return new ActionOutcome(false, false, message, null);
        }
    }
}
