using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TouchCloudPad
{
    /// <summary>
    /// Locates the locally-installed Chrome and launches it with a dedicated
    /// --user-data-dir per account. Each account gets its own profile folder,
    /// so its cookies / login session are kept separate and switch instantly:
    /// that is the lightweight, robust way to handle "multi-account cookies"
    /// without tampering with Chrome's encrypted cookie store.
    /// </summary>
    internal static class Browser
    {
        /// <summary>Locate chrome.exe, or null if not installed.</summary>
        public static string FindChrome(string custom = null)
        {
            if (!string.IsNullOrEmpty(custom) && File.Exists(custom)) return custom;

            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe"),
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;

            // registry App Paths
            try
            {
                foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
                {
                    using (var k = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe"))
                    {
                        if (k != null)
                        {
                            var v = k.GetValue(null) as string;
                            if (!string.IsNullOrEmpty(v) && File.Exists(v)) return v;
                        }
                    }
                }
            }
            catch { }

            // last resort: try to resolve via PATH
            try
            {
                var psi = new ProcessStartInfo("where.exe", "chrome.exe")
                { CreateNoWindow = true, RedirectStandardOutput = true, UseShellExecute = false };
                using (var p = Process.Start(psi))
                {
                    string line = null;
                    if (p != null && p.StandardOutput != null) line = p.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line) && File.Exists(line.Trim())) return line.Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>Launch Chrome with the given profile folder (created if missing) and URL.</summary>
        public static bool Launch(AccountProfile profile, Config cfg)
        {
            string chrome = FindChrome(profile != null ? profile.ChromePath : null);
            if (chrome == null) return false;

            // default profile dir per account
            string userData = null;
            if (profile != null)
            {
                userData = string.IsNullOrEmpty(profile.ProfileDir)
                    ? Path.Combine(Config.DefaultProfileRoot,
                        Sanitize(profile.GameId + "_" + profile.Name))
                    : profile.ProfileDir;
                try { Directory.CreateDirectory(userData); } catch { }
            }

            string url = profile != null && !string.IsNullOrEmpty(profile.Url) ? profile.Url : "about:blank";

            var psi = new ProcessStartInfo
            {
                FileName = chrome,
                UseShellExecute = false,
            };
            string args = "";
            if (userData != null) args += "--user-data-dir=\"" + userData + "\" ";
            args += "--new-window \"" + url + "\"";
            psi.Arguments = args;

            try { Process.Start(psi); return true; }
            catch { return false; }
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "account" : name;
        }
    }
}
