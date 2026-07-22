using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Avalonia.ViewModels;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameDetailsWindow : Window
{
    public GameCardItem? SelectedGame { get; }

    public GameDetailsWindow() : this(new GameCardItem())
    {
    }

    public GameDetailsWindow(GameCardItem game)
    {
        InitializeComponent();
        SelectedGame = game;
        DataContext = game;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnDlssClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss", SelectedGame?.DLSSVersion);
    private void OnDlssRrClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss_d", SelectedGame?.DLSSDVersion);
    private void OnDlssFgClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("dlss_g", SelectedGame?.DLSSGVersion);
    private void OnFsrDx12Click(object? sender, RoutedEventArgs e) => OpenVersionPicker("fsr_31_dx12", SelectedGame?.Fsr31Dx12Version);
    private void OnFsrVkClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("fsr_31_vk", SelectedGame?.Fsr31VkVersion);
    private void OnXessClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess", SelectedGame?.XessVersion);
    private void OnXessDx11Click(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess_dx11", SelectedGame?.XessDx11Version);
    private void OnXessFgClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xess_fg", SelectedGame?.XessFgVersion);
    private void OnXellClick(object? sender, RoutedEventArgs e) => OpenVersionPicker("xell", SelectedGame?.XellVersion);

    private async void OpenVersionPicker(string categoryType, string? currentVersion)
    {
        if (SelectedGame == null || string.IsNullOrEmpty(SelectedGame.InstallPath)) return;
        if (string.IsNullOrEmpty(currentVersion) || currentVersion == "Not found" || currentVersion == "N/A") return;

        var possibleFilenames = LibraryStorageService.GetPossibleDllFilenamesForType(categoryType);
        var targetDllPath = string.Empty;

        try
        {
            if (Directory.Exists(SelectedGame.InstallPath))
            {
                foreach (var fname in possibleFilenames)
                {
                    var files = Directory.GetFiles(SelectedGame.InstallPath, fname, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        targetDllPath = files[0];
                        break;
                    }
                }
            }
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(targetDllPath))
        {
            targetDllPath = Path.Combine(SelectedGame.InstallPath, possibleFilenames[0]);
        }

        var storageService = new LibraryStorageService();
        var vm = new SelectDllVersionViewModel(categoryType, targetDllPath, currentVersion, storageService);

        var dialog = new SelectDllVersionWindow
        {
            DataContext = vm
        };

        await dialog.ShowDialog(this);

        if (!string.IsNullOrEmpty(vm.ResultSwappedPath) && File.Exists(vm.ResultSwappedPath))
        {
            var scanner = new LinuxSteamLibraryScanner();
            var newVer = scanner.ExtractDllVersionFromFile(vm.ResultSwappedPath);
            if (!string.IsNullOrEmpty(newVer))
            {
                UpdateGameVersion(categoryType, newVer);
            }
        }
    }

    private void UpdateGameVersion(string categoryType, string newVer)
    {
        if (SelectedGame == null) return;
        switch (categoryType.ToLowerInvariant())
        {
            case "dlss": SelectedGame.DLSSVersion = newVer; break;
            case "dlss_g": SelectedGame.DLSSGVersion = newVer; break;
            case "dlss_d": SelectedGame.DLSSDVersion = newVer; break;
            case "fsr_31_dx12": SelectedGame.Fsr31Dx12Version = newVer; break;
            case "fsr_31_vk": SelectedGame.Fsr31VkVersion = newVer; break;
            case "xess": SelectedGame.XessVersion = newVer; break;
            case "xess_dx11": SelectedGame.XessDx11Version = newVer; break;
            case "xess_fg": SelectedGame.XessFgVersion = newVer; break;
            case "xell": SelectedGame.XellVersion = newVer; break;
        }
    }
}
