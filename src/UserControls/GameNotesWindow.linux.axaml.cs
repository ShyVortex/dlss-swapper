using Avalonia.Controls;
using Avalonia.Interactivity;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameNotesWindow : Window
{
    private readonly string _gameId = string.Empty;
    private readonly GameMetadataStorageService _storageService = new();

    public GameNotesWindow()
    {
        InitializeComponent();
    }

    public GameNotesWindow(string gameId, string gameName) : this()
    {
        _gameId = gameId;
        TitleTextBlock.Text = $"Notes - {gameName}";
        NotesTextBox.Text = _storageService.LoadNote(_gameId);
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
