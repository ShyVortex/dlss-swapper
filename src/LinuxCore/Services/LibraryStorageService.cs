using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using DLSS_Swapper.Core.Models;

namespace DLSS_Swapper.Core.Services;

public class LibraryStorageService
{
    private static readonly HttpClient HttpClient = new();

    public static string StorageFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper");

    public static string DllsFolder => Path.Combine(StorageFolder, "dlls");

    public async Task<(bool Success, int ExportedCount, string ErrorMessage)> ExportAllToZipAsync(string zipDestinationPath, Action<int, int>? progressCallback = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(DllsFolder))
                    return (false, 0, "Library directory does not exist.");

                var allFiles = Directory.GetFiles(DllsFolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (allFiles.Count == 0)
                    return (false, 0, "No DLLs found in storage to export.");

                if (File.Exists(zipDestinationPath))
                {
                    File.Delete(zipDestinationPath);
                }

                using var zipStream = File.Create(zipDestinationPath);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

                int count = 0;
                int total = allFiles.Count;
                progressCallback?.Invoke(0, total);

                foreach (var file in allFiles)
                {
                    var relativePath = Path.GetRelativePath(DllsFolder, file);
                    archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                    count++;
                    progressCallback?.Invoke(count, total);
                }

                return (true, count, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        });
    }

    public LibraryStorageService()
    {
        Directory.CreateDirectory(StorageFolder);
        Directory.CreateDirectory(DllsFolder);
    }

    public static string[] GetPossibleDllFilenamesForType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "dlss" => new[] { "nvngx_dlss.dll" },
            "dlss_g" => new[] { "nvngx_dlssg.dll" },
            "dlss_d" => new[] { "nvngx_dlssd.dll" },
            "fsr_31_dx12" => new[] { "amd_fidelityfx_dx12.dll", "ffx_fsr31_x64.dll", "ffx_fsr31_dx12_x64.dll", "ffx_fsr3_x64.dll", "ffx_fsr2_x64.dll" },
            "fsr_31_vk" => new[] { "amd_fidelityfx_vk.dll", "ffx_fsr31_vk_x64.dll" },
            "xess" => new[] { "libxess.dll" },
            "xess_dx11" => new[] { "libxess_dx11.dll" },
            "xess_fg" => new[] { "libxess_fg.dll" },
            "xell" => new[] { "libxell.dll" },
            _ => new[] { "nvngx_dlss.dll" }
        };
    }

    public static string GetDllFilenameForType(string type)
    {
        return GetPossibleDllFilenamesForType(type)[0];
    }

    public string GetExpectedRecordFolder(string type, DllRecordModel record)
    {
        var recordType = type.ToLowerInvariant();
        return Path.Combine(DllsFolder, recordType, $"{recordType}_v{record.Version}_{record.Md5Hash}");
    }

    public string GetExpectedDllPath(string type, DllRecordModel record)
    {
        var folder = GetExpectedRecordFolder(type, record);
        var filename = GetDllFilenameForType(type);
        return Path.Combine(folder, filename);
    }

    public bool IsDownloaded(string type, DllRecordModel record)
    {
        var dllPath = GetExpectedDllPath(type, record);
        return File.Exists(dllPath);
    }

    public bool ImportLocalFile(string filePath, string type)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var recordType = type.ToLowerInvariant();
            var importedFolder = Path.Combine(DllsFolder, recordType, $"imported_{Guid.NewGuid():N}");
            Directory.CreateDirectory(importedFolder);

            if (ext == ".zip")
            {
                ZipFile.ExtractToDirectory(filePath, importedFolder, true);
            }
            else if (ext == ".dll")
            {
                var targetDllName = GetDllFilenameForType(recordType);
                File.Copy(filePath, Path.Combine(importedFolder, targetDllName), true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteRecord(string type, DllRecordModel record)
    {
        try
        {
            var folder = GetExpectedRecordFolder(type, record);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
                return true;
            }
        }
        catch
        {
        }
        return false;
    }

    public async Task<bool> DownloadAndExtractAsync(string type, DllRecordModel record, Action<double>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(record.DownloadUrl)) return false;

        try
        {
            var targetFolder = GetExpectedRecordFolder(type, record);
            Directory.CreateDirectory(targetFolder);

            var tempZipPath = Path.Combine(targetFolder, "package.zip");

            using (var response = await HttpClient.GetAsync(record.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? record.ZipFileSize;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    if (totalBytes > 0)
                    {
                        progressCallback?.Invoke((double)totalRead / totalBytes);
                    }
                }
            }

            // Extract ZIP archive contents
            ZipFile.ExtractToDirectory(tempZipPath, targetFolder, true);

            // Clean up temporary ZIP
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            // Post-extraction fallback: relocate extracted DLL if in subfolder or different case
            var expectedPath = GetExpectedDllPath(type, record);
            if (!File.Exists(expectedPath))
            {
                var foundDlls = Directory.GetFiles(targetFolder, "*.dll", SearchOption.AllDirectories);
                if (foundDlls.Length > 0 && !string.Equals(foundDlls[0], expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Copy(foundDlls[0], expectedPath, overwrite: true);
                    }
                    catch
                    {
                    }
                }
            }

            return IsDownloaded(type, record);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ManifestModel?> LoadManifestAsync()
    {
        var jsonDir = Path.Combine(StorageFolder, "json");
        Directory.CreateDirectory(jsonDir);
        var cachedManifestPath = Path.Combine(jsonDir, "manifest.json");

        // 1. Try to download latest manifest online
        try
        {
            using var response = await HttpClient.GetAsync("https://beeradmoore.github.io/dlss-swapper/manifest.json");
            if (response.IsSuccessStatusCode)
            {
                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(cachedManifestPath, contentBytes);
                using var onlineStream = new MemoryStream(contentBytes);
                var onlineManifest = await JsonSerializer.DeserializeAsync<ManifestModel>(onlineStream);
                if (onlineManifest != null) return onlineManifest;
            }
        }
        catch
        {
        }

        // 2. Check for cached manifest.json on disk
        if (File.Exists(cachedManifestPath))
        {
            try
            {
                using var stream = File.OpenRead(cachedManifestPath);
                var cached = await JsonSerializer.DeserializeAsync<ManifestModel>(stream);
                if (cached != null) return cached;
            }
            catch
            {
            }
        }

        // 3. Fallback to embedded static_manifest.json asset across assemblies
        try
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly(), typeof(LibraryStorageService).Assembly };
            foreach (var asm in assemblies.Where(a => a != null))
            {
                var resourceName = asm!.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("static_manifest.json", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    using var resourceStream = asm.GetManifestResourceStream(resourceName);
                    if (resourceStream != null)
                    {
                        var staticManifest = await JsonSerializer.DeserializeAsync<ManifestModel>(resourceStream);
                        if (staticManifest != null) return staticManifest;
                    }
                }
            }
        }
        catch
        {
        }

        return new ManifestModel();
    }
}
