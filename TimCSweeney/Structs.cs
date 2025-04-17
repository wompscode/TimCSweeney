using System.Text.RegularExpressions;
using Discord;

namespace TimCSweeney;

public static class Structs
{
    public struct RegEx
    {
        public Regex Pattern { get; init; }
        public string Emote { get; init; }
        public bool CustomEmoji { get; init; }
    }

    public struct CVExpression
    {
        public string Filename { get; init; }
        public string Emote { get; init; }
        public bool CustomEmoji { get; init; }
        public double ConfidenceThreshold { get; init; }
    }
    
    public struct Activity
    {
        public ActivityType Type { get; init; }
        public string Text { get; init; }
    }
}