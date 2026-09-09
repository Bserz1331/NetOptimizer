using System;
using System.IO;
using System.Xml.Serialization;

namespace NetOptimizerV2
{
    internal static class SettingsStore
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(MonitorSettings));

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
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return MonitorSettings.CreateDefault();
                }

                using (FileStream stream = new FileStream(
                    SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    MonitorSettings settings = (MonitorSettings)Serializer.Deserialize(stream);
                    if (settings == null)
                    {
                        warning = "設定檔是空的，已改用預設值。";
                        return MonitorSettings.CreateDefault();
                    }
                    settings.Normalize();
                    return settings;
                }
            }
            catch (Exception ex)
            {
                warning = "讀取設定檔失敗，已改用預設值：" + ex.Message;
                return MonitorSettings.CreateDefault();
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
            Directory.CreateDirectory(SettingsDirectory);
            string tempPath = SettingsPath + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Serializer.Serialize(stream, settings);
                }

                if (File.Exists(SettingsPath))
                {
                    try
                    {
                        File.Replace(tempPath, SettingsPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(tempPath, SettingsPath, true);
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                        File.Copy(tempPath, SettingsPath, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, SettingsPath);
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
}
