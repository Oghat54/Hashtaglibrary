using System;
using System.IO;
using System.Text.Json;

namespace HashtagCopier;

public static class DataService
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "HashtagCopier", "data.json");

    public static AppData Load()
    {
        AppData data;
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                data = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
            }
            else data = new AppData();
        }
        catch { data = new AppData(); }

        // Миграции для старых баз
        foreach (var f in data.Franchises)
        {
            if (string.IsNullOrWhiteSpace(f.Hashtag))
                f.Hashtag = SanitizeTag(f.Name);

            foreach (var c in f.Characters)
            {
                if (string.IsNullOrWhiteSpace(c.Hashtag))
                    c.Hashtag = SanitizeTag(c.Name);
                if (string.IsNullOrWhiteSpace(c.CommunityTag))
                    c.CommunityTag = data.DefaultCommunityTag;
            }
        }

        if (string.IsNullOrWhiteSpace(data.MandatoryPart))
            data.MandatoryPart = "Автор:\n#anime #art";

        return data;
    }

    public static void Save(AppData data)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Убирает # и @ по краям, заменяет пробелы на _. Регистр НЕ трогает.
    /// </summary>
    public static string SanitizeTag(string raw)
    {
        raw = raw.Trim().TrimStart('#').TrimStart('@');
        raw = raw.Replace(' ', '_');
        return raw;
    }

    public static string FranchiseHashtag(Franchise f) =>
        "#" + SanitizeTag(f.Hashtag);

    public static string CharacterHashtag(Character c) =>
        "#" + SanitizeTag(c.Hashtag) + "@" + SanitizeTag(c.CommunityTag);
}