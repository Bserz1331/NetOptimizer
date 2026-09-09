using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NetOptimizerV2
{
    public sealed class MetricRecoveryJournal
    {
        public string SessionId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public List<MetricRecoveryEntry> Entries { get; set; }
    }

    public sealed class MetricRecoveryEntry
    {
        public string InterfaceName { get; set; }
        public int OriginalMetric { get; set; }
        public bool OriginalAutomaticMetric { get; set; }
        public int ManagedPrimaryMetric { get; set; }
        public int ManagedBackupMetric { get; set; }
    }

    internal sealed class RecoveryReport
    {
        public bool FoundJournal { get; set; }
        public bool Restored { get; set; }
        public bool Cleared { get; set; }
        public List<string> Messages { get; private set; }

        public RecoveryReport()
        {
            Messages = new List<string>();
        }
    }

    internal static class RecoveryStore
    {
        private static readonly XmlSerializer Serializer =
            new XmlSerializer(typeof(MetricRecoveryJournal));

        public static string RecoveryDirectory
        {
            get { return SettingsStore.SettingsDirectory; }
        }

        public static string RecoveryPath
        {
            get { return Path.Combine(RecoveryDirectory, "failover-recovery.xml"); }
        }

        public static MetricRecoveryJournal Load(out string warning)
        {
            warning = null;
            try
            {
                if (!File.Exists(RecoveryPath))
                {
                    return null;
                }
                using (FileStream stream = new FileStream(
                    RecoveryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    MetricRecoveryJournal journal = (MetricRecoveryJournal)Serializer.Deserialize(stream);
                    if (journal == null || journal.Entries == null || journal.Entries.Count == 0)
                    {
                        warning = "找到空的 A/B recovery journal，已忽略。";
                        return null;
                    }
                    return journal;
                }
            }
            catch (Exception ex)
            {
                warning = "讀取 A/B recovery journal 失敗：" + ex.Message;
                return null;
            }
        }

        public static void Save(MetricRecoveryJournal journal)
        {
            if (journal == null) { throw new ArgumentNullException("journal"); }
            if (journal.Entries == null || journal.Entries.Count == 0)
            {
                throw new InvalidOperationException("recovery journal 沒有介面項目。");
            }

            Directory.CreateDirectory(RecoveryDirectory);
            string tempPath = RecoveryPath + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(
                    tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Serializer.Serialize(stream, journal);
                }

                if (File.Exists(RecoveryPath))
                {
                    try
                    {
                        File.Replace(tempPath, RecoveryPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(tempPath, RecoveryPath, true);
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                        File.Copy(tempPath, RecoveryPath, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, RecoveryPath);
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

        public static void Clear()
        {
            if (!File.Exists(RecoveryPath)) { return; }
            File.Delete(RecoveryPath);
        }
    }
}
