// LFInteractive LLC. - All Rights Reserved
using Chase.Networking;
using CLMath;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace YoutubeCipher;

internal class Program
{
    private static string? AttemptGetDefaultUrl(string html) => JObject.Parse("{" + Regex.Match(html, "\"adaptiveFormats\":\\[(.*?)\\]").Value + "}")["adaptiveFormats"]?.ToObject<JArray>()?.Last().ToObject<JObject>()?["url"]?.Value<string>();

    private static string DecodeUrl(string sig)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Decoding URL...");
        Console.ResetColor();

        string[] urlParts = sig.Split('&');
        Dictionary<string, string> urlMap = new Dictionary<string, string>();
        foreach (string urlPart in urlParts)
        {
            string[] parts = urlPart.Split("=");
            urlMap.Add(parts[0], parts[1]);
        }
        string url = Uri.UnescapeDataString(urlMap["url"]);
        string seceret = Uri.UnescapeDataString(urlMap["s"]);

        string decodedSig = Uri.EscapeDataString(PerformOperations(seceret));

        return $"{url}&sig={decodedSig}";
    }

    private static void DownloadFile(string url, string path)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Starting Download...");
        Console.ResetColor();

        using NetworkClient client = new();
        bool first = false;
        client.DownloadFileAsync(new Uri(url), path, (s, e) =>
        {
            if (first)
            {
                Console.CursorTop -= 1;
                Console.CursorLeft = 0;
            }
            first = true;
            Console.WriteLine($"Downloading {e.Percentage:p2} - {CLFileMath.AdjustedFileSize(e.BytesPerSecond)}/s");
        }).Wait();
    }

    private static string GetHTML(Uri url)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Parsing HTML...");
        Console.ResetColor();

        using HttpClient client = new();
        string data = client.GetStringAsync(url).Result;
        return data;
    }

    private static string GetOutputPath(string html)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Getting Filename...");
        Console.ResetColor();
        string file = Regex.Match(html, "(?<=<title>)(.*?)(?=<\\/title>)").Value;
        StringBuilder fileBuilder = new();
        foreach (char c in file)
        {
            if (!Path.GetInvalidFileNameChars().Contains(c))
            {
                fileBuilder.Append(c);
            }
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileBuilder.ToString() + ".mp4");
    }

    private static string? GetSig(string html) => JObject.Parse("{" + Regex.Match(html, "\"adaptiveFormats\":\\[(.*?)\\]").Value + "}")["adaptiveFormats"]?.ToObject<JArray>()?.Last().ToObject<JObject>()?["signatureCipher"]?.Value<string>();

    private static Uri GetYoutubeURL()
    {
        Console.Write("Enter Youtube URL: ");
        string input = Console.ReadLine() ?? "";
        if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
            return uri;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unable to parse url: {input}");
        Console.ResetColor();
        return GetYoutubeURL();
    }

    private static void Main()
    {
        string html = GetHTML(GetYoutubeURL());
        string file = GetOutputPath(html);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Attempting to get Default URL...");
        Console.ResetColor();

        string? url = AttemptGetDefaultUrl(html);
        if (url == null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Parsing the Cipher...");
            Console.ResetColor();

            url = DecodeUrl(GetSig(html));
        }
        DownloadFile(url, file);
        Process.Start(new ProcessStartInfo()
        {
            FileName = file,
            UseShellExecute = true,
        });
    }

    private static string PerformOperations(string data)
    {
        string[] splitData = Array.ConvertAll(data.ToCharArray(), c => c.ToString());
        Swap(ref splitData, 7);
        Reverse(ref splitData);
        Splice(ref splitData, 1);
        Swap(ref splitData, 68);
        Swap(ref splitData, 53);
        Splice(ref splitData, 3);
        return string.Join("", splitData);
    }

    private static void Reverse(ref string[] array)
    {
        array = array.Reverse().ToArray();
    }

    private static void Splice(ref string[] array, int removeCount)
    {
        array = array.Skip(removeCount).ToArray();
    }

    private static void Swap(ref string[] array, int index)
    {
        string first = array.First();
        array[0] = array[index % array.Length];
        array[index % array.Length] = first;
    }
}