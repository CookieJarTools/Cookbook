using System;

namespace CookieJarTools.Cookbook.Editor.Actions
{
    /// <summary>Something a guide can look at in the project and say whether it is done.</summary>
    public sealed class DocCheck
    {
        private readonly Func<CheckOutcome> run;

        public DocCheck(string id, string name, string description, Func<CheckOutcome> run)
        {
            Id = id;
            Name = name;
            Description = description;
            this.run = run;
        }

        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        /// <summary>Looks. Anything it throws is caught and reported as not passing, not left to break the page.</summary>
        public CheckOutcome Run()
        {
            try
            {
                return run() ?? new CheckOutcome(true, "Passed");
            }
            catch (Exception exception)
            {
                var inner = exception.InnerException ?? exception;
                UnityEngine.Debug.LogException(inner);
                return new CheckOutcome(false, "The check itself failed: " + inner.Message);
            }
        }
    }
}
