using System;
using System.IO;
using System.Xml.Serialization;

namespace NetOptimizerV2
{
    internal static class SettingsStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(MonitorSettings));
        private static readonly object Sync = new object();

        public static string SettingsDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NetOptimizer");
            }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(SettingsDirectory, "settings.xml"); }
        }

        public static MonitorSettings Load(out string warning)
        {
            warning = null;
            lock (Sync)
            {
                try
                {
                    using (FileStream stream = new FileStream(
                        SettingsPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete))
                    {
                        MonitorSettings settings = (MonitorSettings)Serializer.Deserialize(stream);
                        if (settings == null)
                        {
                            warning = "設定檔是空的，已改用預設值。";
                            return MonitorSettings.CreateDefault(Localization.DetectWindowsDefault());
                        }
                        settings.Normalize();
                        return settings;
                    }
                }
                catch (FileNotFoundException)
                {
                    return MonitorSettings.CreateDefault(Localization.DetectWindowsDefault());
                }
                catch (DirectoryNotFoundException)
                {
                    return MonitorSettings.CreateDefault(Localization.DetectWindowsDefault());
                }
                catch (Exception ex)
                {
                    warning = "讀取設定檔失敗，已改用預設值：" + ex.Message;
                    return MonitorSettings.CreateDefault(Localization.DetectWindowsDefault());
                }
            }
        }

        public static void Save(MonitorSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings = settings.Clone();
            settings.Normalize();
            lock (Sync)
            {
                Directory.CreateDirectory(SettingsDirectory);
                string tempPath = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (FileStream stream = new FileStream(
                        tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        Serializer.Serialize(stream, settings);
                    }

                    CommitTempFile(tempPath, SettingsPath);
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

        private static void CommitTempFile(string tempPath, string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                try
                {
                    File.Move(tempPath, targetPath);
                    return;
                }
                catch (IOException)
                {
                    if (!File.Exists(targetPath)) { throw; }
                }
            }

            try
            {
                File.Replace(tempPath, targetPath, null);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            File.Copy(tempPath, targetPath, true);
            File.Delete(tempPath);
        }
    }
}
