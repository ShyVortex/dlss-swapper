using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper;

public partial class NetworkTesterWindow : Window
{
    private readonly List<(int Id, string TitleKey, string DefaultTitle, Func<CancellationToken, Task<string>> TestFunc)> _tests = new();
    private CancellationTokenSource? _cts;

    public NetworkTesterWindow()
    {
        InitializeComponent();
        RegisterTests();
        BuildTestRows();
        UpdateTranslations();
        AppendLog($"Init: DLSS Swapper version: {GetAppVersion()}");
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private string GetAppVersion()
    {
        return DLSS_Swapper.LinuxCore.Helpers.AppVersionHelper.GetVersionString();
    }

    private void UpdateTranslations()
    {
        var appTitle = ResourceHelper.GetString("ApplicationTitle", "DLSS Swapper");
        var windowTitle = ResourceHelper.GetString("NetworkTesterPage_WindowTitle", "Network Tester");
        Title = $"{appTitle} - {windowTitle}";

        CopyResultsButton.Content = ResourceHelper.GetString("NetworkTesterPage_CopyTestResults", "Copy test results");
        CreateBugReportButton.Content = ResourceHelper.GetString("NetworkTesterPage_CreateBugReport", "Create bug report");
        CancelTestButton.Content = ResourceHelper.GetString("NetworkTesterPage_CancelCurrentTest", "Cancel current test");

        BuildTestRows();
    }

    private void RegisterTests()
    {
        _tests.Add((1, "NetworkTesterPage_DiagnosticsTest1Title", "Accessing google.com from within DLSS Swapper (tests general internet connectivity)", async ct => await TestUrlAsync("https://google.com", ct)));
        _tests.Add((2, "NetworkTesterPage_DiagnosticsTest2Title", "Accessing bing.com from within DLSS Swapper (tests general internet connectivity)", async ct => await TestUrlAsync("https://bing.com", ct)));
        _tests.Add((3, "NetworkTesterPage_DiagnosticsTest3Title", "Downloading DLSS Swapper DLL within DLSS Swapper", async ct => await TestUrlAsync("https://raw.githubusercontent.com/beeradmoore/dlss-swapper/main/Assets/static_manifest.json", ct)));
        _tests.Add((4, "NetworkTesterPage_DiagnosticsTest4Title", "Downloading DLSS Swapper DLL from browser", async ct => await TestUrlAsync("https://raw.githubusercontent.com/beeradmoore/dlss-swapper/main/Assets/static_manifest.json", ct, true)));
        _tests.Add((5, "NetworkTesterPage_DiagnosticsTest5Title", "Downloading game cover from Steam", async ct => await TestUrlAsync("https://cdn.cloudflare.steamstatic.com/steam/apps/1091500/header.jpg", ct)));
        _tests.Add((6, "NetworkTesterPage_DiagnosticsTest6Title", "Downloading game cover from Epic Game Store", async ct => await TestUrlAsync("https://cdn1.epicgames.com/offer/cbd5b059466d4957a0753063f25c7e0f/EGS_Cyberpunk2077_CDPROJEKTRED_S1_03_2560x1440-359e9842a2754668b5a0fb709c00b0f0", ct)));
        _tests.Add((7, "NetworkTesterPage_DiagnosticsTest7Title", "Downloading game cover from DLSS Swapper file server", async ct => await TestUrlAsync("https://dlss-swapper.beeradmoore.com/manifest.json", ct)));
        _tests.Add((8, "NetworkTesterPage_DiagnosticsTest8Title", "Downloading game cover from alternative DLSS Swapper file server", async ct => await TestUrlAsync("https://raw.githubusercontent.com/beeradmoore/dlss-swapper/main/Assets/static_manifest.json", ct)));
        _tests.Add((9, "NetworkTesterPage_DnsLookupTitle", "DNS lookup of DLSS Swapper file server", TestDnsLookupAsync));
        _tests.Add((10, "NetworkTesterPage_DiagnosticsTest10Title", "Downloading DLSS Swapper DLL within DLSS Swapper with custom user agent", async ct => await TestUrlAsync("https://raw.githubusercontent.com/beeradmoore/dlss-swapper/main/Assets/static_manifest.json", ct, customUserAgent: DLSS_Swapper.LinuxCore.Helpers.AppVersionHelper.GetUserAgent())));
        _tests.Add((11, "NetworkTesterPage_DiagnosticsTest11Title", "Downloading DLSS DLL from UploadThing mirror", async ct => await TestUrlAsync("https://uploadthing.com", ct)));
    }

    private void BuildTestRows()
    {
        TestsStackPanel.Children.Clear();
        foreach (var test in _tests)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto, *, Auto"),
                Margin = new Thickness(0, 2)
            };

            var testNumLabel = new TextBlock
            {
                Text = $"Test {test.Id}:",
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var titleText = ResourceHelper.GetString(test.TitleKey, test.DefaultTitle);
            var testTitleLabel = new TextBlock
            {
                Text = titleText,
                Foreground = Brush.Parse("#DDDDDD"),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            var runBtn = new Button
            {
                Content = "Run test",
                Padding = new Thickness(12, 6),
                Background = Brush.Parse("#2B2B2B"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = test.Id
            };
            int testId = test.Id;
            runBtn.Click += async (s, e) => await RunSingleTestAsync(testId);

            Grid.SetColumn(testNumLabel, 0);
            Grid.SetColumn(testTitleLabel, 1);
            Grid.SetColumn(runBtn, 2);

            grid.Children.Add(testNumLabel);
            grid.Children.Add(testTitleLabel);
            grid.Children.Add(runBtn);

            TestsStackPanel.Children.Add(grid);
        }
    }

    private async Task RunSingleTestAsync(int testId)
    {
        var test = _tests.Find(t => t.Id == testId);
        if (test.TestFunc == null) return;

        _cts = new CancellationTokenSource();
        AppendLog($"Running Test {test.Id}: {ResourceHelper.GetString(test.TitleKey, test.DefaultTitle)}...");
        try
        {
            var result = await test.TestFunc(_cts.Token);
            AppendLog($"Test {test.Id} Result: {result}");
        }
        catch (OperationCanceledException)
        {
            AppendLog($"Test {test.Id} Cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog($"Test {test.Id} Error: {ex.Message}");
        }
    }

    private async Task<string> TestUrlAsync(string url, CancellationToken ct, bool isBrowser = false, string? customUserAgent = null)
    {
        var sw = Stopwatch.StartNew();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("User-Agent", customUserAgent ?? (isBrowser ? "Mozilla/5.0 (X11; Linux x86_64)" : "DLSS-Swapper-Linux"));

        var response = await client.GetAsync(url, ct);
        sw.Stop();
        return $"StatusCode: {(int)response.StatusCode} ({response.StatusCode}), Latency: {sw.ElapsedMilliseconds}ms";
    }

    private async Task<string> TestDnsLookupAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var entry = await Dns.GetHostEntryAsync("github.com", ct);
        sw.Stop();
        var ips = string.Join(", ", (IEnumerable<IPAddress>)entry.AddressList);
        return $"Resolved IP(s): {ips} ({sw.ElapsedMilliseconds}ms)";
    }

    private void OnCancelTestClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        AppendLog("Cancellation requested for current test.");
    }

    private async void OnCopyResultsClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(ResultsTextBox.Text ?? string.Empty);
            AppendLog("Copied test results to clipboard.");
        }
    }

    private void OnCreateBugReportClick(object? sender, RoutedEventArgs e)
    {
        var issueUrl = "https://github.com/beeradmoore/dlss-swapper/issues/new?title=Network%20Diagnostics%20Report";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = issueUrl, UseShellExecute = true });
        }
        catch { }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz");
        ResultsTextBox.Text = (ResultsTextBox.Text ?? string.Empty) + $"{timestamp} {message}\n";
    }
}
