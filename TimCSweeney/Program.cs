using System.Diagnostics;
using static TimCSweeney.Structs;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Tesseract;

namespace TimCSweeney;

internal static class Program
{
    private static DiscordSocketClient? _client;
    private static readonly HttpClient HttpClient = new();
    private static bool _discordNetRawLog;
    
    private static readonly RegEx[] Patterns =
    [
        new()
        {
            Pattern = new Regex(@"\bepic\b|\bunreal\b|\btilted\b|\bfortnite\b|sween|\bswinny\b|\bfort\b|\bnite\b|jenkin|\bjames\b|\beric\b|\bswussy\b|\btim\b|\btimato\b|\bfirtnite\b", RegexOptions.IgnoreCase),
            Emote = "",
            CustomEmoji = true
        },
        new()
        {
            Pattern = new Regex(@"\bblazing\b|\bblazing fast\b|\bmemory safe\b|\bblazingly fast\b", RegexOptions.IgnoreCase),
            Emote = ":rocket:",
            CustomEmoji = false
        }
    ];

    private static readonly Structs.Activity Activity = new () {
        Text = "Epic v Apple",
        Type = ActivityType.Competing
    };
    private const int Delay = 250;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Initialising..");
        _discordNetRawLog = File.Exists(@"shouldLog");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            TesseractEnviornment.CustomSearchPath = $"{Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}","runtimes")}";
            Console.WriteLine($"you are running on Linux! if {Path.Combine($"{AppDomain.CurrentDomain.BaseDirectory}","runtimes")} doesn't exist, you are missing tesseract and leptonica linux natives.");
        }
        
        var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        Console.WriteLine($"Running with Tesseract {engine.Version}");
        engine.Dispose();
        
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildEmojis
        };
        
        _client = new DiscordSocketClient(config);

        _client.Log += Log;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;

        var token = await File.ReadAllTextAsync("token");

        await _client.LoginAsync(TokenType.Bot, token.Trim());
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private static async Task HandleReactions(List<IEmote> queue, SocketMessage arg)
    {
        if (queue.Count > 0)
        {
            foreach (var emote in queue)
            {
                await arg.AddReactionAsync(emote);
                await Task.Delay(Delay);
            }
        }
    }

    private static async Task MessageReceivedAsync(SocketMessage arg)
    {
        if (_client != null && (arg.Author.IsBot || arg.Author.Id == _client.CurrentUser.Id)) return;
        List<IEmote> queue = new ();
        List<string> ocr = new ();

        if (arg.Attachments.Count > 0)
        {
            foreach (var attachment in arg.Attachments)
            {
                if (attachment.ContentType.Contains("image"))
                {  
                    Console.WriteLine("OCR: attachment in message, downloading to memory and running tesseract");
                    byte[] response = await HttpClient.GetByteArrayAsync(attachment.Url);
                    if (response.Length < 1)
                    {
                        Console.WriteLine("OCR: response less than 1 byte in size, not continuing");
                        return;
                    }
                    var imageMemoryStream = new MemoryStream(response);

                    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                    Console.WriteLine("OCR: new tesseractengine created");
                    using var img = Pix.LoadFromMemory(imageMemoryStream.ToArray());
                    Console.WriteLine("OCR: loaded picture from memory");
                    using var page = engine.Process(img);
                    Console.WriteLine("OCR: processing image");
                    
                    var txt = page.GetText();
                    Console.WriteLine($"OCR: found {txt}, adding to ocr List.");
                    ocr.Add(txt);
                    await imageMemoryStream.DisposeAsync();
                }
            }
        }

        foreach (var _ in Patterns)
        {
            Regex regex = _.Pattern;
            bool matched = false;
            if (regex.IsMatch(arg.Content))
            {
                Console.WriteLine($"RegEx: Matched \"{arg.Content}\" with {_.Pattern}");
                if (_.CustomEmoji)
                {
                    Emote emote = Emote.Parse(_.Emote);
                    if (queue.FirstOrDefault(emote1 => emote1.Name == emote.Name) != null)
                    {
                        Console.WriteLine("RegEx: Already in reaction queue.");
                        continue;
                    }
                    Console.WriteLine("RegEx: Reaction queued.");
                    queue.Add(emote);
                    matched = true;
                }
                else
                {
                    Emoji emoji = Emoji.Parse(_.Emote);
                    if (queue.FirstOrDefault(emote1 => emote1.Name == emoji.Name) != null)
                    {
                        Console.WriteLine("RegEx: Already in reaction queue.");
                        continue;
                    }
                    Console.WriteLine("RegEx: Reaction queued.");
                    queue.Add(emoji);
                    matched = true;
                }
            }
            
            if (matched == false && ocr.Count > 0)
            {
                foreach (string text in ocr)
                {
                    if (regex.IsMatch(text))
                    {
                        Console.WriteLine($"RegEx: Matched \"{text}\" with {_.Pattern}");
                        if (_.CustomEmoji)
                        {
                            Emote emote = Emote.Parse(_.Emote);
                            if (queue.FirstOrDefault(emote1 => emote1.Name == emote.Name) != null)
                            {
                                Console.WriteLine("RegEx: Already in reaction queue.");
                                continue;
                            }
                            Console.WriteLine("RegEx: Reaction queued.");
                            queue.Add(emote);
                        }
                        else
                        {
                            Emoji emoji = Emoji.Parse(_.Emote);
                            if (queue.FirstOrDefault(emote1 => emote1.Name == emoji.Name) != null)
                            {
                                Console.WriteLine("RegEx: Already in reaction queue.");
                                continue;
                            }
                            Console.WriteLine("RegEx: Reaction queued.");
                            queue.Add(emoji);
                        }
                    }
                }
            }
        }
        await HandleReactions(queue, arg);
    }

    private static Task ReadyAsync()
    {
        if (_client == null)
        {
            Console.WriteLine("FATAL: somehow reached Ready but no client, exiting");
            Environment.Exit(0);
        }
        Console.WriteLine("Connected.");
        _client.SetGameAsync(Activity.Text, null, Activity.Type);
        return Task.CompletedTask;
    }

    private static Task Log(LogMessage msg)
    { 
        if(_discordNetRawLog) Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}