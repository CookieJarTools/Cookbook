using System;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>Something a page can run with a button: a method marked [CookbookAction], or one of the CookBook's own.</summary>
    public sealed class DocAction
    {
        private readonly Func<ActionOutcome> run;

        public DocAction(string id, string name, string description, Func<ActionOutcome> run)
        {
            Id = id;
            Name = name;
            Description = description;
            this.run = run;
        }

        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        /// <summary>Runs it. Anything it throws is caught and reported as a failure, not left to break the page.</summary>
        public ActionOutcome Run()
        {
            try
            {
                return run() ?? ActionOutcome.Finished(null);
            }
            catch (Exception exception)
            {
                var inner = exception.InnerException ?? exception;
                UnityEngine.Debug.LogException(inner);
                return ActionOutcome.Failed(inner.Message);
            }
        }
    }
}
