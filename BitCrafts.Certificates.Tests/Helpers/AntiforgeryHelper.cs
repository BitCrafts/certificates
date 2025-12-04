using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BitCrafts.Certificates.Tests.Helpers;

public static class AntiforgeryHelper
{
    private static readonly Regex TokenRegex = new("name=\"__RequestVerificationToken\".*?value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static async Task<(string token, string? cookie)> FetchTokenAsync(HttpClient client, string getPath)
    {
        var resp = await client.GetAsync(getPath);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();
        var m = TokenRegex.Match(html);
        if (!m.Success)
            throw new InvalidOperationException($"Antiforgery token not found in {getPath}");

        // Extract cookie if any
        resp.Headers.TryGetValues("Set-Cookie", out var cookies);
        string? cookie = null;
        if (cookies != null)
        {
            // Take the first cookie (contains .AspNetCore.Antiforgery)
            cookie = string.Join("; ", cookies);
            var semi = cookie.IndexOf(';');
            if (semi > 0) cookie = cookie.Substring(0, semi);
        }

        return (m.Groups[1].Value, cookie);
    }

    public static HttpRequestMessage CreateFormPost(string path, (string name, string value)[] fields, string? cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(fields.ToKeyValuePairs())
        };
        if (!string.IsNullOrEmpty(cookie))
        {
            req.Headers.Add("Cookie", cookie);
        }
        return req;
    }

    private static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this (string name, string value)[] fields)
    {
        foreach (var (name, value) in fields)
            yield return new KeyValuePair<string, string>(name, value);
    }
}
