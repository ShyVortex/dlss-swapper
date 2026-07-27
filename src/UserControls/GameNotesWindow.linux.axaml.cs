using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameNotesWindow : Window
{
    private readonly string _gameId = string.Empty;
    private string _gameName = string.Empty;
    private readonly GameMetadataStorageService _storageService = new();

    public GameNotesWindow()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    public GameNotesWindow(string gameId, string gameName) : this()
    {
        _gameId = gameId;
        _gameName = gameName;
        UpdateTranslations();
        NotesTextBox.Text = _storageService.LoadNote(_gameId);
    }

    private void UpdateTranslations()
    {
        var notesTitle = ResourceHelper.GetString("GamePage_Notes", "Notes");
        Title = string.IsNullOrEmpty(_gameName) ? notesTitle : $"{notesTitle} - {_gameName}";
        TitleTextBlock.Text = Title;
        SaveButton.Content = ResourceHelper.GetString("General_Save", "Save");
        CancelButton.Content = ResourceHelper.GetString("General_Cancel", "Cancel");
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        _storageService.SaveNote(_gameId, NotesTextBox.Text ?? string.Empty);
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowPointerPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
    {
        FocusManager?.ClearFocus();
    }
}
