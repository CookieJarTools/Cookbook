using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;

namespace CookieJarTools.Cookbook.Editor.Index
{
    /// <summary>
    /// Who last changed a file and when, from git — asked in the background, since git can take a
    /// moment, and remembered until the file changes again. Nothing comes back when the file is
    /// not in a git repository or git is not installed; the Cookbook just says nothing.
    /// </summary>
    public static class GitInfo
    {
        private const int TimeoutMilliseconds = 4000;

        private static readonly Dictionary<string, (DateTime modified, string author, string date)> Known = new Dictionary<string, (DateTime, string, string)>();

        /// <param name="told">Called on the main thread with the author and date, or not at all when there is nothing to tell.</param>
        public static void LastChange(string localPath, Action<string, string> told)
        {
            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
            {
                return;
            }

            var modified = File.GetLastWriteTime(localPath);
            if (Known.TryGetValue(localPath, out var known) && known.modified == modified)
            {
                Tell(known.author, known.date, told);
                return;
            }

            Task.Run(() => Ask(localPath)).ContinueWith(asked => Answered(localPath, modified, asked.Result, told));
        }

        private static void Answered(string localPath, DateTime modified, (string author, string date) answer, Action<string, string> told)
        {
            EditorApplication.delayCall += () =>
            {
                Known[localPath] = (modified, answer.author, answer.date);
                Tell(answer.author, answer.date, told);
            };
        }

        private static void Tell(string author, string date, Action<string, string> told)
        {
            if (!string.IsNullOrEmpty(author))
            {
                told(author, date);
            }
        }

        /// <summary>git log for the one file: author name and short date, or nothing.</summary>
        private static (string author, string date) Ask(string localPath)
        {
            try
            {
                var start = new ProcessStartInfo("git", "log -1 --format=%an%x09%ad --date=short -- \"" + Path.GetFileName(localPath) + "\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(localPath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var git = Process.Start(start))
                {
                    var output = git.StandardOutput.ReadToEnd();
                    git.WaitForExit(TimeoutMilliseconds);
                    var parts = output.Trim().Split('\t');
                    return git.ExitCode == 0 && parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
                }
            }
            catch (Exception)
            {
                return (null, null);
            }
        }
    }
}
