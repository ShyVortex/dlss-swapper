using System;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using DLSS_Swapper.Core.Services;
using DLSS_Swapper.Helpers;
using HtmlAgilityPack;

namespace DLSS_Swapper.Avalonia.Views;

public partial class AddGameStep2Window : Window
{
    public bool UserProceeded { get; private set; }

    public AddGameStep2Window()
    {
        InitializeComponent();
        UpdateTranslations();
        LinuxLanguageService.Instance.OnLanguageChanged += UpdateTranslations;
    }

    private void UpdateTranslations()
    {
        Title = ResourceHelper.GetString("GamesPage_ManuallyAdding_AnotherNoteTitle", "Another note for manually adding games");
        TitleTextBlock.Text = ResourceHelper.GetString("GamesPage_ManuallyAdding_AnotherNoteTitle", "Another note for manually adding games");
        AddGameButton.Content = ResourceHelper.GetString("GamesPage_AddGame", "Add Game");
        CloseButton.Content = ResourceHelper.GetString("General_Close", "Close");

        PopulateInfoHtml(ResourceHelper.GetString("GamesPage_ManuallyAdding_InfoHtml", string.Empty));
    }

    private void PopulateInfoHtml(string htmlExpression)
    {
        if (MessageBodyTextBlock == null) return;

        MessageBodyTextBlock.Inlines?.Clear();

        if (string.IsNullOrWhiteSpace(htmlExpression))
        {
            return;
        }

        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlExpression);

            foreach (var node in htmlDoc.DocumentNode.ChildNodes)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "#text":
                        MessageBodyTextBlock.Inlines?.Add(new Run { Text = System.Net.WebUtility.HtmlDecode(node.InnerText) });
                        break;
                    case "i":
                        MessageBodyTextBlock.Inlines?.Add(new Run
                        {
                            Text = System.Net.WebUtility.HtmlDecode(node.InnerText),
                            FontStyle = FontStyle.Italic
                        });
                        break;
                    case "b":
                        MessageBodyTextBlock.Inlines?.Add(new Run
                        {
                            Text = System.Net.WebUtility.HtmlDecode(node.InnerText),
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White
                        });
                        break;
                    case "br":
                        MessageBodyTextBlock.Inlines?.Add(new LineBreak());
                        break;
                    default:
                        MessageBodyTextBlock.Inlines?.Add(new Run { Text = System.Net.WebUtility.HtmlDecode(node.InnerText) });
                        break;
                }
            }
        }
        catch
        {
            MessageBodyTextBlock.Text = htmlExpression;
        }
    }

    private void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = true;
        Close();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        UserProceeded = false;
        Close();
    }
}
