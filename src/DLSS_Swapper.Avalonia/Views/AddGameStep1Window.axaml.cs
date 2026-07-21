using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DLSS_Swapper.Avalonia.Views;

public partial class AddGameStep1Window : Window
{
    public bool UserProceeded { get; private set; }
    public bool DontShowAgain => DontShowCheckBox?.IsChecked == true;

    public AddGameStep1Window()
    {
        InitializeComponent();
    }

    private void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = true;
        Close();
    }

    private void OnReportIssueClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/beeradmoore/dlss-swapper/issues",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = false;
        Close();
    }
}
