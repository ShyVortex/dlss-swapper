using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DLSS_Swapper.Data;
using SQLite;

namespace DLSS_Swapper.Core.Services;

public class GameHistoryService
{
    public static string DatabasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLSS Swapper", "dlss-swapper.db");

    private static readonly object _lock = new();
    private static SQLiteAsyncConnection? _cachedConnection;
    private static bool _isInitialized = false;

    private static SQLiteAsyncConnection GetConnection()
    {
        if (_cachedConnection == null)
        {
            lock (_lock)
            {
                if (_cachedConnection == null)
                {
                    var dir = Path.GetDirectoryName(DatabasePath)!;
                    Directory.CreateDirectory(dir);
                    _cachedConnection = new SQLiteAsyncConnection(DatabasePath);
                }
            }
        }
        return _cachedConnection;
    }

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            var conn = GetConnection();
            await conn.CreateTableAsync<GameHistory>().ConfigureAwait(false);
            _isInitialized = true;
        }
    }

    public async Task<List<GameHistory>> LoadHistoryAsync(string gameId)
    {
        if (string.IsNullOrEmpty(gameId)) return new List<GameHistory>();

        try
        {
            await InitializeAsync();
            var conn = GetConnection();
            var items = await conn.Table<GameHistory>().Where(x => x.GameId == gameId).ToListAsync();
            return items.FindAll(x => x != null).ConvertAll(x => x!);
        }
        catch
        {
            return new List<GameHistory>();
        }
    }

    public async Task AddEventAsync(string gameId, GameHistoryEventType eventType, GameAssetType? assetType = null, string? version = null, string? assetPath = null)
    {
        if (string.IsNullOrEmpty(gameId)) return;

        try
        {
            await InitializeAsync();
            var conn = GetConnection();

            var historyItem = new GameHistory
            {
                GameId = gameId,
                EventType = eventType,
                AssetType = assetType,
                AssetVersion = version,
                AssetPath = assetPath,
                EventTime = DateTime.Now
            };

            await conn.InsertAsync(historyItem);
        }
        catch
        {
        }
    }

    public async Task LogDetectedDllAsync(string gameId, GameAssetType assetType, string? version)
    {
        if (string.IsNullOrEmpty(version) || version == "N/A" || version == "Not found") return;

        try
        {
            await InitializeAsync();
            var conn = GetConnection();

            // Avoid inserting duplicate detection records if already exists for this game and version
            var existing = await conn.Table<GameHistory>()
                .Where(x => x.GameId == gameId && x.EventType == GameHistoryEventType.DLLDetected && x.AssetVersion == version)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                await AddEventAsync(gameId, GameHistoryEventType.DLLDetected, assetType, version);
            }
        }
        catch
        {
        }
    }
}
