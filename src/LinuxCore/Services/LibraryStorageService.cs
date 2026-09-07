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

    public static string DetectCategoryFromFilename(string filename, string? fallbackCategory = null)
    {
        var lower = filename.ToLowerInvariant();
        if (lower.Contains("nvngx_dlssg")) return "dlss_g";
        if (lower.Contains("nvngx_dlssd") || lower.Contains("nvngx_dlssnr")) return "dlss_d";
        if (lower.Contains("nvngx_dlss") && !lower.Contains("dlssg") && !lower.Contains("dlssd")) return "dlss";
        if (lower.Contains("amd_fidelityfx_vk") || lower.Contains("ffx_fsr31_vk")) return "fsr_31_vk";
        if (lower.Contains("amd_fidelityfx_dx12") || lower.Contains("ffx_fsr31") || lower.Contains("ffx_fsr3") || lower.Contains("ffx_fsr2")) return "fsr_31_dx12";
        if (lower.Contains("libxess_dx11")) return "xess_dx11";
        if (lower.Contains("libxess_fg")) return "xess_fg";
        if (lower.Contains("libxell")) return "xell";
        if (lower.Contains("libxess") && !lower.Contains("dx11") && !lower.Contains("fg")) return "xess";

        if (!string.IsNullOrWhiteSpace(fallbackCategory))
        {
            var fb = fallbackCategory.ToLowerInvariant();
            if (fb is "dlss" or "dlss_g" or "dlss_d" or "fsr_31_dx12" or "fsr_31_vk" or "xess" or "xess_dx11" or "xess_fg" or "xell")
                return fb;
        }

        return "dlss";
    }

    public static string ComputeFileMd5(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    public static DllRecordModel CreateRecordFromDll(string filePath, string category)
    {
        var version = DLSS_Swapper.Core.Helpers.PeVersionReader.GetFileVersion(filePath);
        var versionStr = version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
            : "1.0.0.0";
        ulong versionNum = version != null
            ? (((ulong)version.Major << 48) | ((ulong)version.Minor << 32) | ((ulong)version.Build << 16) | (ulong)version.Revision)
            : 0;

        var fileInfo = new FileInfo(filePath);
        var md5 = ComputeFileMd5(filePath);

        return new DllRecordModel
        {
            Version = versionStr,
            VersionNumber = versionNum,
            Md5Hash = md5,
            FileSize = fileInfo.Length,
            IsImported = true
        };
    }

    private bool ProcessAndStoreDllFile(string sourceDllPath, string category, ManifestModel importedManifest)
    {
        try
        {
            if (!File.Exists(sourceDllPath)) return false;
            var record = CreateRecordFromDll(sourceDllPath, category);
            var targetFolder = GetExpectedRecordFolder(category, record);
            Directory.CreateDirectory(targetFolder);

            var targetFileName = GetDllFilenameForType(category);
            var targetFilePath = Path.Combine(targetFolder, targetFileName);

            File.Copy(sourceDllPath, targetFilePath, true);

            var list = importedManifest.GetRecordsForCategory(category);
            if (!list.Any(r => string.Equals(r.Md5Hash, record.Md5Hash, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(record);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, int ImportedCount, string ErrorMessage)> ImportLocalFileAsync(string filePath, string? categoryHint = null)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return (false, 0, "File not found.");

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var importedManifest = await LoadImportedManifestAsync();
                int importedCount = 0;

                if (ext == ".zip")
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), "dlss_swapper_import_" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        Directory.CreateDirectory(tempDir);
                        ZipFile.ExtractToDirectory(filePath, tempDir, true);

                        var dllFiles = Directory.GetFiles(tempDir, "*.dll", SearchOption.AllDirectories);
                        if (dllFiles.Length == 0)
                        {
                            return (false, 0, "No DLL files found in ZIP archive.");
                        }

                        foreach (var dll in dllFiles)
                        {
                            var category = DetectCategoryFromFilename(Path.GetFileName(dll), categoryHint);
                            if (ProcessAndStoreDllFile(dll, category, importedManifest))
                            {
                                importedCount++;
                            }
                        }
                    }
                    finally
                    {
                        if (Directory.Exists(tempDir))
                        {
                            try { Directory.Delete(tempDir, true); } catch { }
                        }
                    }
                }
                else if (ext == ".dll")
                {
                    var category = DetectCategoryFromFilename(Path.GetFileName(filePath), categoryHint);
                    if (ProcessAndStoreDllFile(filePath, category, importedManifest))
                    {
                        importedCount++;
                    }
                }
                else
                {
                    return (false, 0, "Unsupported file format. Please select a .dll or .zip file.");
                }

                if (importedCount > 0)
                {
                    await SaveImportedManifestAsync(importedManifest);
                    return (true, importedCount, string.Empty);
                }

                return (false, 0, "Could not import any valid DLLs.");
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        });
    }

    public bool ImportLocalFile(string filePath, string type)
    {
        return Task.Run(() => ImportLocalFileAsync(filePath, type)).GetAwaiter().GetResult().Success;
    }

    public bool DeleteRecord(string type, DllRecordModel record)
    {
        try
        {
            var folder = GetExpectedRecordFolder(type, record);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            _ = Task.Run(async () =>
            {
                var importedManifest = await LoadImportedManifestAsync();
                var list = importedManifest.GetRecordsForCategory(type);
                int removed = list.RemoveAll(r => string.Equals(r.Md5Hash, record.Md5Hash, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    await SaveImportedManifestAsync(importedManifest);
                }
            });

            return true;
        }
        catch
        {
            return false;
        }
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

    public static string ImportedManifestPath => Path.Combine(StorageFolder, "json", "imported_manifest.json");

    public async Task<ManifestModel> LoadImportedManifestAsync()
    {
        if (File.Exists(ImportedManifestPath))
        {
            try
            {
                using var stream = File.OpenRead(ImportedManifestPath);
                var imported = await JsonSerializer.DeserializeAsync<ManifestModel>(stream);
                if (imported != null) return imported;
            }
            catch
            {
            }
        }
        return new ManifestModel();
    }

    public async Task SaveImportedManifestAsync(ManifestModel importedManifest)
    {
        try
        {
            var jsonDir = Path.Combine(StorageFolder, "json");
            Directory.CreateDirectory(jsonDir);
            var options = new JsonSerializerOptions { WriteIndented = true };
            using var stream = File.Create(ImportedManifestPath);
            await JsonSerializer.SerializeAsync(stream, importedManifest, options);
        }
        catch
        {
        }
    }

    public async Task CleanAndMergeImportedManifestAsync(ManifestModel mainManifest, ManifestModel importedManifest)
    {
        var categories = new[] { "dlss", "dlss_g", "dlss_d", "fsr_31_dx12", "fsr_31_vk", "xess", "xess_dx11", "xess_fg", "xell" };
        bool changed = false;

        foreach (var category in categories)
        {
            var importedList = importedManifest.GetRecordsForCategory(category);
            var mainList = mainManifest.GetRecordsForCategory(category);
            var toRemove = new List<DllRecordModel>();

            foreach (var item in importedList)
            {
                // 1. If file does not exist on disk, remove it from imported manifest
                if (!IsDownloaded(category, item))
                {
                    toRemove.Add(item);
                    continue;
                }

                // 2. If item is already part of the official manifest for this category, remove it from imported manifest
                if (mainList.Any(m => string.Equals(m.Md5Hash, item.Md5Hash, StringComparison.OrdinalIgnoreCase)))
                {
                    toRemove.Add(item);
                    continue;
                }

                // 3. If the actual DLL in its folder belongs to a different category, remove it
                var expectedDll = GetExpectedDllPath(category, item);
                if (File.Exists(expectedDll))
                {
                    var actualFilename = Path.GetFileName(expectedDll);
                    var detectedCat = DetectCategoryFromFilename(actualFilename, category);
                    if (!string.Equals(detectedCat, category, StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove.Add(item);
                        continue;
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                importedList.RemoveAll(r => toRemove.Contains(r));
                changed = true;
            }
        }

        if (changed)
        {
            await SaveImportedManifestAsync(importedManifest);
        }

        mainManifest.Merge(importedManifest);
    }

    public async Task<ManifestModel?> LoadManifestAsync()
    {
        var jsonDir = Path.Combine(StorageFolder, "json");
        Directory.CreateDirectory(jsonDir);
        var cachedManifestPath = Path.Combine(jsonDir, "manifest.json");
        ManifestModel? manifest = null;

        // 1. Try to download latest manifest online
        try
        {
            using var response = await HttpClient.GetAsync("https://beeradmoore.github.io/dlss-swapper/manifest.json");
            if (response.IsSuccessStatusCode)
            {
                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(cachedManifestPath, contentBytes);
                using var onlineStream = new MemoryStream(contentBytes);
                manifest = await JsonSerializer.DeserializeAsync<ManifestModel>(onlineStream);
            }
        }
        catch
        {
        }

        // 2. Check for cached manifest.json on disk
        if (manifest == null && File.Exists(cachedManifestPath))
        {
            try
            {
                using var stream = File.OpenRead(cachedManifestPath);
                manifest = await JsonSerializer.DeserializeAsync<ManifestModel>(stream);
            }
            catch
            {
            }
        }

        // 3. Fallback to embedded static_manifest.json asset across assemblies
        if (manifest == null)
        {
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
                            manifest = await JsonSerializer.DeserializeAsync<ManifestModel>(resourceStream);
                            if (manifest != null) break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        manifest ??= new ManifestModel();

        // 4. Merge imported manifest cleanly without duplicates
        var importedManifest = await LoadImportedManifestAsync();
        await CleanAndMergeImportedManifestAsync(manifest, importedManifest);

        return manifest;
    }
}
