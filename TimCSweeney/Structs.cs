using Discord;

namespace TimCSweeney;

public static class Structs
{
    public struct RegEx
    {
        public string Pattern { get; init; }
        public string Emote { get; init; }
        public bool CustomEmoji { get; init; }
    }

    public struct Activity
    {
        public ActivityType Type { get; init; }
        public string Text { get; init; }
    }
}