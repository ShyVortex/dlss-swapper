using Avalonia.Controls;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;

namespace DLSS_Swapper.Avalonia.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        LibraryTitleTextBlock.Text = ResourceHelper.GetString("LibraryPage_Title", "Library");
        ImportButtonTextBlock.Text = ResourceHelper.GetString("General_Import", "Import");
        ImportLocalFilesMenuItem.Header = ResourceHelper.GetString("LibraryPage_ImportFrom_LocalFiles", "From local files");
        ImportNvidiaHeaderMenuItem.Header = "NVIDIA";
        ImportFromDriverMenuItem.Header = ResourceHelper.GetString("LibraryPage_ImportFrom_Driver", "From driver");
        ImportFromServerMenuItem.Header = ResourceHelper.GetString("LibraryPage_ImportFrom_DownloadFromServer", "Download from server");
        ExportAllButtonTextBlock.Text = ResourceHelper.GetString("General_ExportAll", "Export All");
        DownloadLatestButtonTextBlock.Text = ResourceHelper.GetString("LibraryPage_DownloadLatest", "Download Latest");
        RefreshButtonTextBlock.Text = ResourceHelper.GetString("General_Refresh", "Refresh");
        LoadingOverlayTextBlock.Text = ResourceHelper.GetString("General_Loading", "Refreshing library manifest...");
    }
}
