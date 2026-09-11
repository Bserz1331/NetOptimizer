using System;
using System.IO;
using Microsoft.Win32;

namespace NetOptimizerV2
{
    internal enum StartupRegistrationMode
    {
        None,
        CurrentUserRun,
        ElevatedTask
    }

    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "NetOptimizer";
        private const string StartupArgument = "--startup";

        public static bool IsEnabled()
        {
            return IsEnabled(null);
        }

        public static bool IsEnabled(string executablePath)
        {
            return GetMode(executablePath) != StartupRegistrationMode.None;
        }

        public static StartupRegistrationMode GetMode(string executablePath)
        {
            if (ElevatedStartupManager.IsEnabled())
            {
                return StartupRegistrationMode.ElevatedTask;
            }

            return IsRunEntryEnabled(executablePath)
                ? StartupRegistrationMode.CurrentUserRun
                : StartupRegistrationMode.None;
        }

        public static bool IsProtectedInstallPath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            try
            {
                string fullPath = NormalizePath(executablePath);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                return IsWithinPath(fullPath, programFiles) || IsWithinPath(fullPath, programFilesX86);
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled, string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", "executablePath");
            }

            if (!enabled)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
                return;
            }

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("Unable to open the current-user startup registry key.");
                }

                key.SetValue(ValueName, BuildCommandLine(executablePath), RegistryValueKind.String);
            }
        }

        private static bool IsRunEntryEnabled(string executablePath)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    object value = key == null ? null : key.GetValue(ValueName, null);
                    if (value == null)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(executablePath))
                    {
                        return true;
                    }

                    return string.Equals(
                        value.ToString(),
                        BuildCommandLine(executablePath),
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
        }

        private static bool IsWithinPath(string fullPath, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            string normalizedRoot = NormalizePath(rootPath);
            return string.Equals(fullPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar,
                                       StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar,
                                       StringComparison.OrdinalIgnoreCase);
        }

        internal static string BuildCommandLine(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", "executablePath");
            }

            return "\"" + executablePath.Replace("\"", "\\\"") + "\" " + StartupArgument;
        }
    }
}
