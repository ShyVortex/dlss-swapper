using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace DLSS_Swapper.Avalonia.Helpers;

public static class ImageHelper
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<Bitmap?> LoadBitmapAsync(string pathOrUrl)
    {
        if (string.IsNullOrEmpty(pathOrUrl)) return null;

        try
        {
            // 1. Local file path
            if (File.Exists(pathOrUrl))
            {
                using var stream = File.OpenRead(pathOrUrl);
                return new Bitmap(stream);
            }

            // 2. HTTP / HTTPS URL
            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var candidateUrls = new List<string> { pathOrUrl };

                // If this is a Steam app image URL, prepare alternate CDN fallbacks
                var match = System.Text.RegularExpressions.Regex.Match(pathOrUrl, @"/apps/(\d+)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var appId = match.Groups[1].Value;
                    var steamFallbacks = new[]
                    {
                        $"https://shared.steamstatic.com/store_item_assets/steam/apps/{appId}/library_600x900.jpg",
                        $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900.jpg",
                        $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                        $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg",
                        $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg"
                    };

                    foreach (var fallback in steamFallbacks)
                    {
                        if (!candidateUrls.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                        {
                            candidateUrls.Add(fallback);
                        }
                    }
                }

                foreach (var url in candidateUrls)
                {
                    try
                    {
                        var response = await HttpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            await using var stream = await response.Content.ReadAsStreamAsync();
                            var memoryStream = new MemoryStream();
                            await stream.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;
                            return new Bitmap(memoryStream);
                        }
                    }
                    catch
                    {
                        // Try next URL
                    }
                }
            }
        }
        catch
        {
            // Fallback gracefully on any image load error
        }

        return null;
    }
}
