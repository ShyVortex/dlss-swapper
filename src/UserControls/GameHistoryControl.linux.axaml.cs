using System.Collections.Generic;
using Avalonia.Controls;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class GameHistoryControl : UserControl
{
    public GameHistoryControl()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        if (HistoryDataGrid != null && HistoryDataGrid.Columns.Count >= 4)
        {
            HistoryDataGrid.Columns[0].Header = ResourceHelper.GetString("General_Date", "Event Time");
            HistoryDataGrid.Columns[1].Header = ResourceHelper.GetString("General_Status", "Event Type");
            HistoryDataGrid.Columns[2].Header = ResourceHelper.GetString("General_Name", "Asset Type");
            HistoryDataGrid.Columns[3].Header = ResourceHelper.GetString("General_Version", "Version");
        }
        EmptyTextBlock.Text = ResourceHelper.GetString("GamePage_History_NoRecords", "No history records found for this game.");
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
