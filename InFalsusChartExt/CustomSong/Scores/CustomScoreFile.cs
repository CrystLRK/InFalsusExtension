using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace InFalsusChartExt.CustomSong.Scores
{
    internal static class CustomScoreFile
    {
        private static readonly object Gate = new object();
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true,
            };

        internal static string Path => ResolvePath();
        internal static string BackupPath => Path + ".bak";

        internal static CustomScoreDocument Load(out string error)
        {
            lock (Gate)
            {
                error = null;
                string path = Path;
                if (!File.Exists(path))
                {
                    if (File.Exists(BackupPath))
                    {
                        if (TryReadValid(
                                BackupPath,
                                out CustomScoreDocument backup,
                                out string missingBackupError))
                        {
                            bool restored =
                                TryRestoreFromBackup(path);
                            error =
                                "custom-results.json missing; " +
                                (restored
                                    ? "restored backup"
                                    : "loaded backup; restore failed");
                            return backup;
                        }
                        error =
                            "custom-results.json missing; " +
                            "backup invalid (" +
                            missingBackupError + ")";
                        return null;
                    }
                    var created = new CustomScoreDocument();
                    if (Save(created, out string createError))
                        return created;
                    error =
                        "custom-results.json creation failed: " +
                        createError;
                    return null;
                }

                if (TryReadValid(path, out CustomScoreDocument document, out error))
                    return document;

                TryPreserveCorrupt(path);
                if (TryReadValid(
                        BackupPath,
                        out CustomScoreDocument fromBackup,
                        out string backupError))
                {
                    bool restored =
                        TryRestoreFromBackup(path);
                    error =
                        $"custom-results.json invalid ({error}); " +
                        (restored
                            ? "restored backup"
                            : "loaded backup; restore failed");
                    return fromBackup;
                }

                error =
                    $"custom-results.json invalid ({error}); " +
                    $"backup invalid ({backupError ?? "unavailable"})";
                return null;
            }
        }

        internal static bool Save(
            CustomScoreDocument document,
            out string error)
        {
            lock (Gate)
            {
                error = null;
                string path = Path;
                string temp = path + ".tmp";
                try
                {
                    if (!CustomScoreFileValidator.Validate(
                            document, out error))
                        return false;
                    string directory = System.IO.Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory))
                        throw new InvalidOperationException(
                            "custom-results.json has no parent directory");
                    Directory.CreateDirectory(directory);

                    string json = JsonSerializer.Serialize(
                        document, JsonOptions);
                    File.WriteAllText(
                        temp, json, new UTF8Encoding(false));

                    if (File.Exists(path))
                        File.Copy(path, BackupPath, true);
                    File.Move(temp, path, true);
                    return true;
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    try
                    {
                        if (File.Exists(temp))
                            File.Delete(temp);
                    }
                    catch
                    {
                    }
                    return false;
                }
            }
        }

        internal static string Key(
            string packId,
            string songId,
            string difficulty) =>
            (packId ?? string.Empty) + "\n" +
            (songId ?? string.Empty) + "\n" +
            (difficulty ?? string.Empty);

        private static bool TryReadValid(
            string path,
            out CustomScoreDocument document,
            out string error)
        {
            document = null;
            error = null;
            if (!File.Exists(path))
            {
                error = "file does not exist";
                return false;
            }
            try
            {
                string json = File.ReadAllText(path);
                document = JsonSerializer.Deserialize<
                    CustomScoreDocument>(json, JsonOptions);
                if (!CustomScoreFileValidator.Validate(
                        document, out error))
                {
                    document = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                document = null;
                return false;
            }
        }

        private static void TryPreserveCorrupt(string path)
        {
            try
            {
                string directory =
                    System.IO.Path.GetDirectoryName(path);
                string stem =
                    System.IO.Path.GetFileNameWithoutExtension(path);
                string target = System.IO.Path.Combine(
                    directory ?? string.Empty,
                    stem + ".corrupt-" +
                    DateTime.UtcNow.ToString(
                        "yyyyMMddHHmmssfff") +
                    ".json");
                File.Move(path, target);
            }
            catch
            {
            }
        }

        private static bool TryRestoreFromBackup(string path)
        {
            string temp = path + ".restore";
            try
            {
                File.Copy(BackupPath, temp, true);
                File.Move(temp, path, true);
                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch
                {
                }
                return false;
            }
        }

        private static string ResolvePath()
        {
            string modDirectory = System.IO.Path.GetDirectoryName(
                typeof(ChartExtMod).Assembly.Location);
            return System.IO.Path.Combine(
                modDirectory ?? Environment.CurrentDirectory,
                "InFalsusChartExt",
                "custom-results.json");
        }
    }
}
