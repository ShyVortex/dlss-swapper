using System;
using System.IO;
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
                var response = await HttpClient.GetAsync(pathOrUrl);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    return new Bitmap(memoryStream);
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
