using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetOptimizerV2
{
    internal sealed class UpdateInfo
    {
        public UpdateInfo(Version version, string tagName, string releaseUrl)
        {
            Version = version;
            VersionKey = version == null ? string.Empty : version.ToString();
            DisplayVersion = version == null
                ? (tagName ?? string.Empty)
                : "v" + version.Major + "." + version.Minor + "." + version.Build;
            TagName = tagName ?? string.Empty;
            ReleaseUrl = releaseUrl ?? string.Empty;
        }

        public Version Version { get; private set; }
        public string VersionKey { get; private set; }
        public string DisplayVersion { get; private set; }
        public string TagName { get; private set; }
        public string ReleaseUrl { get; private set; }
    }

    internal sealed class UpdateCheckResult
    {
        private UpdateCheckResult()
        {
        }

        public bool Succeeded { get; private set; }
        public bool IsUpdateAvailable { get; private set; }
        public UpdateInfo Update { get; private set; }
        public string Error { get; private set; }

        public static UpdateCheckResult Success(UpdateInfo update)
        {
            return new UpdateCheckResult
            {
                Succeeded = true,
                IsUpdateAvailable = update != null,
                Update = update,
                Error = string.Empty
            };
        }

        public static UpdateCheckResult Failure(string error)
        {
            return new UpdateCheckResult
            {
                Succeeded = false,
                IsUpdateAvailable = false,
                Error = error ?? "Unknown update check error."
            };
        }
    }

    public sealed class UpdateState
    {
        public DateTime LastSuccessfulCheckUtc { get; set; }
        public string LastNotifiedVersion { get; set; }
        public string IgnoredVersion { get; set; }

        public void Normalize()
        {
            LastNotifiedVersion = Trim(LastNotifiedVersion);
            IgnoredVersion = Trim(IgnoredVersion);
        }

        private static string Trim(string value)
        {
            value = (value ?? string.Empty).Trim();
            return value.Length > 64 ? value.Substring(0, 64) : value;
        }
    }

    internal static class UpdateStateStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(UpdateState));

        public static string StatePath
        {
            get { return Path.Combine(SettingsStore.SettingsDirectory, "update-state.xml"); }
        }

        public static UpdateState Load(out string warning)
        {
            warning = null;
            try
            {
                if (!File.Exists(StatePath))
                {
                    return new UpdateState();
                }

                using (FileStream stream = new FileStream(
                    StatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    UpdateState state = (UpdateState)Serializer.Deserialize(stream);
                    if (state == null)
                    {
                        warning = "更新狀態檔是空的，已改用預設值。";
                        return new UpdateState();
                    }
                    state.Normalize();
                    return state;
                }
            }
            catch (Exception ex)
            {
                warning = "讀取更新狀態失敗，已改用預設值：" + ex.Message;
                return new UpdateState();
            }
        }

        public static void Save(UpdateState state)
        {
            if (state == null) { throw new ArgumentNullException("state"); }
            state.Normalize();
            Directory.CreateDirectory(SettingsStore.SettingsDirectory);
            string tempPath = StatePath + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Serializer.Serialize(stream, state);
                }

                if (File.Exists(StatePath))
                {
                    try
                    {
                        File.Replace(tempPath, StatePath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(tempPath, StatePath, true);
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                        File.Copy(tempPath, StatePath, true);
                        File.Delete(tempPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        File.Copy(tempPath, StatePath, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, StatePath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }

    internal static class UpdateChecker
    {
        internal const string RepositoryApiUrl =
            "https://api.github.com/repos/Bserz1331/NetOptimizer/releases/latest";
        internal const string RepositoryReleaseUrl =
            "https://github.com/Bserz1331/NetOptimizer/releases";
        internal const int RequestTimeoutMs = 5000;
        internal static readonly TimeSpan AutomaticCheckInterval = TimeSpan.FromHours(24);

        public static bool ShouldCheck(UpdateState state, bool force, DateTime utcNow)
        {
            if (force || state == null || state.LastSuccessfulCheckUtc == DateTime.MinValue)
            {
                return true;
            }

            DateTime last = state.LastSuccessfulCheckUtc.ToUniversalTime();
            return last > utcNow || utcNow - last >= AutomaticCheckInterval;
        }

        public static async Task<UpdateCheckResult> CheckAsync(
            Version currentVersion,
            CancellationToken token)
        {
            if (currentVersion == null)
            {
                return UpdateCheckResult.Failure("Current application version is unavailable.");
            }

            // .NET Framework versions on older Windows installations may not
            // select TLS 1.2 by default, while GitHub requires it.
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }
            catch { }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RepositoryApiUrl);
            request.Method = "GET";
            request.Accept = "application/vnd.github+json";
            request.UserAgent = "NetOptimizer/" + FormatVersion(currentVersion);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = RequestTimeoutMs;
            request.ReadWriteTimeout = RequestTimeoutMs;

            using (CancellationTokenSource timeout = new CancellationTokenSource(RequestTimeoutMs))
            using (CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token))
            using (CancellationTokenRegistration abort = linked.Token.Register(delegate
            {
                try { request.Abort(); } catch { }
            }))
            {
                try
                {
                    using (HttpWebResponse response =
                        (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            return UpdateCheckResult.Failure(
                                "GitHub returned HTTP " + (int)response.StatusCode + ".");
                        }

                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                            return ParseReleaseJson(json, currentVersion);
                        }
                    }
                }
                catch (WebException ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return UpdateCheckResult.Failure("Update check cancelled.");
                    }
                    if (timeout.IsCancellationRequested)
                    {
                        return UpdateCheckResult.Failure("Update check timed out.");
                    }
                    return UpdateCheckResult.Failure(SummarizeException(ex));
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return UpdateCheckResult.Failure("Update check cancelled.");
                    }
                    return UpdateCheckResult.Failure(SummarizeException(ex));
                }
            }
        }

        internal static UpdateCheckResult ParseReleaseJson(string json, Version currentVersion)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return UpdateCheckResult.Failure("GitHub returned an empty release response.");
            }

            try
            {
                GitHubReleasePayload payload;
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(GitHubReleasePayload));
                    payload = (GitHubReleasePayload)serializer.ReadObject(stream);
                }

                if (payload == null || payload.Draft || payload.Prerelease ||
                    string.IsNullOrWhiteSpace(payload.TagName))
                {
                    return UpdateCheckResult.Success(null);
                }

                Version releaseVersion = ParseVersion(payload.TagName);
                if (releaseVersion == null)
                {
                    return UpdateCheckResult.Failure("GitHub release tag is not a valid version.");
                }

                Version normalizedCurrent = NormalizeVersion(currentVersion);
                if (releaseVersion.CompareTo(normalizedCurrent) <= 0)
                {
                    return UpdateCheckResult.Success(null);
                }

                string releaseUrl = IsTrustedReleaseUrl(payload.HtmlUrl)
                    ? payload.HtmlUrl
                    : RepositoryReleaseUrl;
                return UpdateCheckResult.Success(
                    new UpdateInfo(releaseVersion, payload.TagName, releaseUrl));
            }
            catch (Exception ex)
            {
                return UpdateCheckResult.Failure("Unable to read release metadata: " + ex.Message);
            }
        }

        internal static Version ParseVersion(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) { return null; }
            string value = tagName.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            Version parsed;
            if (!Version.TryParse(value, out parsed)) { return null; }
            return NormalizeVersion(parsed);
        }

        internal static void RunSelfTest()
        {
            const string json =
                "{\"tag_name\":\"v3.0.15\",\"html_url\":\"https://github.com/Bserz1331/NetOptimizer/releases/tag/v3.0.15\",\"draft\":false,\"prerelease\":false}";
            UpdateCheckResult update = ParseReleaseJson(json, new Version(3, 0, 14, 0));
            if (!update.Succeeded || !update.IsUpdateAvailable || update.Update == null ||
                update.Update.DisplayVersion != "v3.0.15" ||
                update.Update.ReleaseUrl.IndexOf("github.com/Bserz1331/NetOptimizer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("更新檢查器未正確辨識新版本。");
            }

            UpdateCheckResult current = ParseReleaseJson(json, new Version(3, 0, 15));
            if (!current.Succeeded || current.IsUpdateAvailable)
            {
                throw new InvalidOperationException("更新檢查器錯誤地提示目前版本更新。");
            }

            UpdateState fresh = new UpdateState();
            if (!ShouldCheck(fresh, false, DateTime.UtcNow) ||
                ShouldCheck(new UpdateState
                {
                    LastSuccessfulCheckUtc = DateTime.UtcNow.AddMinutes(-5)
                }, false, DateTime.UtcNow) ||
                !ShouldCheck(fresh, true, DateTime.UtcNow))
            {
                throw new InvalidOperationException("更新檢查間隔或手動檢查判斷錯誤。");
            }

            Console.WriteLine("NetOptimizer update checker test: PASS");
        }

        internal static void RunLiveSelfTest()
        {
            UpdateCheckResult result = CheckAsync(
                typeof(UpdateChecker).Assembly.GetName().Version,
                CancellationToken.None).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Error);
            }

            Console.WriteLine(
                "NetOptimizer live update check: PASS (" +
                (result.IsUpdateAvailable ? result.Update.DisplayVersion : "latest") + ")");
        }

        private static bool IsTrustedReleaseUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return uri.AbsolutePath.StartsWith(
                "/Bserz1331/NetOptimizer/releases/", StringComparison.OrdinalIgnoreCase);
        }

        private static Version NormalizeVersion(Version version)
        {
            if (version == null) { return null; }
            return new Version(
                version.Major,
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build),
                Math.Max(0, version.Revision));
        }

        private static string FormatVersion(Version version)
        {
            Version normalized = NormalizeVersion(version);
            return normalized == null
                ? "unknown"
                : normalized.Major + "." + normalized.Minor + "." + normalized.Build;
        }

        private static string SummarizeException(Exception ex)
        {
            if (ex == null) { return "Unknown update check error."; }
            string message = ex.Message;
            return string.IsNullOrWhiteSpace(message)
                ? ex.GetType().Name
                : ex.GetType().Name + ": " + message;
        }

        [DataContract]
        private sealed class GitHubReleasePayload
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "html_url")]
            public string HtmlUrl { get; set; }

            [DataMember(Name = "draft")]
            public bool Draft { get; set; }

            [DataMember(Name = "prerelease")]
            public bool Prerelease { get; set; }
        }
    }
}
