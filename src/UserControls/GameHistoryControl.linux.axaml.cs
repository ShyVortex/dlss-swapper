using System.Collections.Generic;
using Avalonia.Controls;
using DLSS_Swapper.Core.Services;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameHistoryControl : UserControl
{
    public GameHistoryControl()
    {
        InitializeComponent();
    }

    public async void LoadHistory(string gameId)
    {
        var service = new GameHistoryService();
        var history = await service.LoadHistoryAsync(gameId);

        if (history != null && history.Count > 0)
        {
            HistoryDataGrid.ItemsSource = history;
            HistoryDataGrid.IsVisible = true;
            EmptyTextBlock.IsVisible = false;
        }
        else
        {
            HistoryDataGrid.IsVisible = false;
            EmptyTextBlock.IsVisible = true;
        }
    }
}
