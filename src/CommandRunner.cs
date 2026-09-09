using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NetOptimizerV2
{
    internal static class CommandRunner
    {
        internal static string GetUsefulError(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return string.Empty; }

            // Keep PowerShell's escaped line-break tokens intact while parsing XML.
            // Replacing them before LoadXml can turn an otherwise valid CLIXML stream
            // into a partial/malformed document. Decode only the extracted text.
            string raw = text;
            if (ContainsIgnoreCase(raw, "PermissionDenied") ||
                ContainsIgnoreCase(raw, "Access is denied") ||
                ContainsIgnoreCase(raw, "0x80041003"))
            {
                return "權限不足：請以系統管理員身分執行 NetOptimizer。";
            }
            if (ContainsIgnoreCase(raw, "AutomaticMetric") &&
                (ContainsIgnoreCase(raw, "cannot convert") ||
                 ContainsIgnoreCase(raw, "ParameterBinding")))
            {
                return "Set-NetIPInterface 參數錯誤：AutomaticMetric 必須使用 Disabled 或 Enabled。";
            }
            int xmlStart = raw.IndexOf("<Objs", StringComparison.Ordinal);
            if (xmlStart >= 0)
            {
                try
                {
                    XmlDocument document = new XmlDocument();
                    document.LoadXml(raw.Substring(xmlStart).Trim());
                    // CLIXML normally declares a default namespace. local-name()
                    // keeps this parser compatible with both namespaced and minimal
                    // synthetic streams used by the self-test.
                    XmlNodeList errorNodes = document.SelectNodes(
                        "//*[local-name()='S' and @S='Error']");
                    if (errorNodes != null && errorNodes.Count > 0)
                    {
                        StringBuilder errors = new StringBuilder();
                        foreach (XmlNode node in errorNodes)
                        {
                            string value = node == null ? string.Empty : NormalizePowerShellText(node.InnerText);
                            if (value.Length == 0) { continue; }
                            if (errors.Length > 0) { errors.AppendLine(); }
                            errors.Append(value);
                        }
                        if (errors.Length > 0)
                        {
                            return errors.ToString().Trim();
                        }
                    }
                }
                catch
                {
                    // Fall through to a line-based cleanup if CLIXML is partial.
                }
            }

            // A redirected PowerShell error stream may contain more than one XML
            // fragment. Recover the error payload even when the outer document is
            // incomplete or has an unexpected wrapper.
            MatchCollection errorMatches = Regex.Matches(
                raw,
                "<S\\s+S=\"Error\">(.*?)</S>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (errorMatches.Count > 0)
            {
                StringBuilder errors = new StringBuilder();
                foreach (Match match in errorMatches)
                {
                    string value = NormalizePowerShellText(match.Groups[1].Value);
                    if (value.Length == 0) { continue; }
                    if (errors.Length > 0) { errors.AppendLine(); }
                    errors.Append(value);
                }
                if (errors.Length > 0)
                {
                    return errors.ToString().Trim();
                }
            }

            string normalized = NormalizePowerShellText(raw);
            using (StringReader reader = new StringReader(normalized))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string candidate = line.Trim();
                    if (candidate.Length == 0 ||
                        string.Equals(candidate, "#< CLIXML", StringComparison.OrdinalIgnoreCase) ||
                        candidate.StartsWith("<Objs", StringComparison.OrdinalIgnoreCase) ||
                        candidate.StartsWith("</Objs", StringComparison.OrdinalIgnoreCase) ||
                        candidate.StartsWith("<S ", StringComparison.OrdinalIgnoreCase) ||
                        candidate.StartsWith("</S", StringComparison.OrdinalIgnoreCase) ||
                        candidate.IndexOf("<S S=\"Error\">", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (candidate.StartsWith("<", StringComparison.Ordinal) &&
                         candidate.EndsWith(">", StringComparison.Ordinal)))
                    {
                        continue;
                    }
                    return candidate;
                }
            }
            return string.Empty;
        }

        private static string NormalizePowerShellText(string value)
        {
            return (value ?? string.Empty)
                .Replace("_x000D__x000A_", Environment.NewLine)
                .Replace("_x000D_", "\r")
                .Replace("_x000A_", "\n")
                .Replace("_x0009_", "\t")
                .Trim();
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(token) &&
                   value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task<CommandResult> RunAsync(
            string fileName,
            IList<string> arguments,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            Process process = null;
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = JoinArguments(arguments),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Environment.SystemDirectory
                };

                process = new Process { StartInfo = info };
                if (!process.Start())
                {
                    return Failed(fileName, "無法啟動命令。", -1);
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                Task waitTask = Task.Factory.StartNew(
                    delegate { process.WaitForExit(); },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                using (CancellationTokenSource timeoutCancellation =
                       CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    Task timeoutTask = Task.Delay(timeoutMs, timeoutCancellation.Token);
                    using (cancellationToken.Register(delegate { TryKill(process); }))
                    {
                        Task completed = await Task.WhenAny(waitTask, timeoutTask).ConfigureAwait(false);
                        if (completed != waitTask)
                        {
                            bool cancelled = cancellationToken.IsCancellationRequested;
                            TryKill(process);
                            try { await waitTask.ConfigureAwait(false); } catch { }
                            return new CommandResult
                            {
                                FileName = fileName,
                                ExitCode = -1,
                                TimedOut = !cancelled,
                                Cancelled = cancelled,
                                StandardOutput = SafeRead(outputTask),
                                StandardError = SafeRead(errorTask)
                            };
                        }

                        timeoutCancellation.Cancel();
                    }
                }

                string output = await outputTask.ConfigureAwait(false);
                string error = await errorTask.ConfigureAwait(false);
                return new CommandResult
                {
                    FileName = fileName,
                    ExitCode = process.ExitCode,
                    StandardOutput = output,
                    StandardError = error
                };
            }
            catch (OperationCanceledException)
            {
                if (process != null) { TryKill(process); }
                return new CommandResult
                {
                    FileName = fileName,
                    ExitCode = -1,
                    Cancelled = true,
                    StandardOutput = string.Empty,
                    StandardError = "已取消。"
                };
            }
            catch (Exception ex)
            {
                if (process != null) { TryKill(process); }
                return Failed(fileName, ex.Message, -1);
            }
            finally
            {
                if (process != null)
                {
                    process.Dispose();
                }
            }
        }

        private static CommandResult Failed(string fileName, string error, int exitCode)
        {
            return new CommandResult
            {
                FileName = fileName,
                ExitCode = exitCode,
                StandardOutput = string.Empty,
                StandardError = error
            };
        }

        private static string SafeRead(Task<string> task)
        {
            if (task == null || task.Status != TaskStatus.RanToCompletion)
            {
                return string.Empty;
            }
            try { return task.Result; } catch { return string.Empty; }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
        }

        private static string JoinArguments(IList<string> arguments)
        {
            StringBuilder builder = new StringBuilder();
            if (arguments == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0) { builder.Append(' '); }
                builder.Append(QuoteArgument(arguments[i]));
            }
            return builder.ToString();
        }

        private static string QuoteArgument(string value)
        {
            if (value == null) { return "\"\""; }
            if (value.Length == 0) { return "\"\""; }

            bool needsQuotes = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]) || value[i] == '"')
                {
                    needsQuotes = true;
                    break;
                }
            }
            if (!needsQuotes) { return value; }

            StringBuilder builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            int backslashes = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                    builder.Append('"');
                    backslashes = 0;
                    continue;
                }
                builder.Append('\\', backslashes);
                builder.Append(c);
                backslashes = 0;
            }
            builder.Append('\\', backslashes * 2);
            builder.Append('"');
            return builder.ToString();
        }
    }
}
