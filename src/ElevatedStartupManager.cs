using System;
using System.Diagnostics;
using System.IO;

namespace NetOptimizerV2
{
    internal static class ElevatedStartupManager
    {
        internal const string TaskName = "NetOptimizer Elevated Startup";

        public static bool IsEnabled()
        {
            try
            {
                ProcessResult result = Run(BuildQueryArguments());
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled, string executablePath)
        {
            if (!NetworkInfo.IsAdministrator())
            {
                throw new InvalidOperationException(
                    "Administrator permission is required to configure the elevated startup task.");
            }

            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                throw new FileNotFoundException("The NetOptimizer executable was not found.", executablePath);
            }

            if (!enabled && !IsEnabled())
            {
                return;
            }

            ProcessResult result = Run(enabled
                ? BuildCreateArguments(executablePath)
                : BuildDeleteArguments());
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(FormatFailure(result));
            }
        }

        internal static string BuildTaskAction(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", "executablePath");
            }

            return "\"" + executablePath.Replace("\"", "\\\"") + "\" --startup --auto-start";
        }

        internal static string BuildCreateArguments(string executablePath)
        {
            return "/Create /TN " + QuoteArgument(TaskName) +
                   " /SC ONLOGON /RL HIGHEST /TR " +
                   QuoteArgument(BuildTaskAction(executablePath)) + " /F";
        }

        internal static string BuildDeleteArguments()
        {
            return "/Delete /TN " + QuoteArgument(TaskName) + " /F";
        }

        internal static string BuildQueryArguments()
        {
            return "/Query /TN " + QuoteArgument(TaskName) + " /FO LIST";
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static ProcessResult Run(string arguments)
        {
            string schtasksPath = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = schtasksPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Environment.SystemDirectory
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Unable to start Task Scheduler command.");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("Task Scheduler command timed out.");
                }

                return new ProcessResult(process.ExitCode, output, error);
            }
        }

        private static string FormatFailure(ProcessResult result)
        {
            string detail = (result.Error + " " + result.Output).Trim();
            if (detail.Length > 600)
            {
                detail = detail.Substring(0, 600);
            }
            return "Task Scheduler command failed (exit code " + result.ExitCode + "): " + detail;
        }

        private sealed class ProcessResult
        {
            public ProcessResult(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output ?? string.Empty;
                Error = error ?? string.Empty;
            }

            public int ExitCode { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }
        }
    }
}
