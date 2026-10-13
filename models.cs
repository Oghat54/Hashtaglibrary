using System.Collections.Generic;

namespace HashtagCopier;

public class Character
{
    public string Name { get; set; } = "";
    public string Hashtag { get; set; } = "";
    public string CommunityTag { get; set; } = "";
    public int ClickCount { get; set; }
}

public class Franchise
{
    public string Name { get; set; } = "";
    public string Hashtag { get; set; } = "";
    public int ClickCount { get; set; }
    public List<Character> Characters { get; set; } = new();
}

public class AppData
{
    public string DefaultCommunityTag { get; set; } = "";
    public string MandatoryPart { get; set; } = "Автор:\n#anime #art";
    public List<Franchise> Franchises { get; set; } = new();
}