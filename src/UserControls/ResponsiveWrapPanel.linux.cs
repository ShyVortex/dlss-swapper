using System;
using Avalonia;
using Avalonia.Controls;

namespace DLSS_Swapper.Avalonia.Views;

/// <summary>
/// A panel that arranges children in a responsive grid where items stretch
/// to fill each row evenly. The column count is determined automatically
/// from the available width and <see cref="MinItemWidth"/>.
/// Unlike WrapPanel with bound widths, all sizing is computed within the
/// panel's own Measure/Arrange pass, eliminating multi-frame layout flicker.
/// </summary>
public class ResponsiveWrapPanel : Panel
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<ResponsiveWrapPanel, double>(nameof(MinItemWidth), 175.0);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<ResponsiveWrapPanel, double>(nameof(ItemHeight), double.NaN);

    public static readonly StyledProperty<double> ItemAspectRatioProperty =
        AvaloniaProperty.Register<ResponsiveWrapPanel, double>(nameof(ItemAspectRatio), 0.0);

    public static readonly StyledProperty<double> HorizontalSpacingProperty =
        AvaloniaProperty.Register<ResponsiveWrapPanel, double>(nameof(HorizontalSpacing), 16.0);

    public static readonly StyledProperty<double> VerticalSpacingProperty =
        AvaloniaProperty.Register<ResponsiveWrapPanel, double>(nameof(VerticalSpacing), 16.0);

    /// <summary>Minimum width of each item. Columns are added when space allows.</summary>
    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    /// <summary>Fixed height for each item. Ignored when <see cref="ItemAspectRatio"/> is set.</summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Height-to-width ratio (e.g. 1.486 for 260/175). When &gt; 0, overrides ItemHeight.</summary>
    public double ItemAspectRatio
    {
        get => GetValue(ItemAspectRatioProperty);
        set => SetValue(ItemAspectRatioProperty, value);
    }

    /// <summary>Horizontal gap between columns.</summary>
    public double HorizontalSpacing
    {
        get => GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    /// <summary>Vertical gap between rows.</summary>
    public double VerticalSpacing
    {
        get => GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    static ResponsiveWrapPanel()
    {
        AffectsMeasure<ResponsiveWrapPanel>(
            MinItemWidthProperty, ItemHeightProperty, ItemAspectRatioProperty,
            HorizontalSpacingProperty, VerticalSpacingProperty);
    }

    private (int cols, double itemWidth, double itemHeight) ComputeLayout(double availWidth)
    {
        double minW = MinItemWidth;
        double hSpace = HorizontalSpacing;

        if (availWidth <= 0 || double.IsInfinity(availWidth))
        {
            double fallbackH = ItemAspectRatio > 0
                ? minW * ItemAspectRatio
                : (!double.IsNaN(ItemHeight) ? ItemHeight : minW);
            return (1, minW, fallbackH);
        }

        // Max columns: N items of minW with (N-1) gaps of hSpace
        // N * minW + (N-1) * hSpace <= availWidth
        // N <= (availWidth + hSpace) / (minW + hSpace)
        int cols = Math.Max(1, (int)Math.Floor((availWidth + hSpace) / (minW + hSpace)));

        // Distribute width: N items + (N-1) gaps = availWidth
        double itemWidth = Math.Floor((availWidth - (cols - 1) * hSpace) / cols);

        double itemHeight;
        if (ItemAspectRatio > 0)
            itemHeight = Math.Floor(itemWidth * ItemAspectRatio);
        else if (!double.IsNaN(ItemHeight))
            itemHeight = ItemHeight;
        else
            itemHeight = itemWidth;

        return (cols, itemWidth, itemHeight);
    }

    private int CountVisible()
    {
        int count = 0;
        foreach (var child in Children)
            if (child.IsVisible) count++;
        return count;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var (cols, itemWidth, itemHeight) = ComputeLayout(availableSize.Width);
        var childConstraint = new Size(itemWidth, itemHeight);

        foreach (var child in Children)
        {
            if (child.IsVisible)
                child.Measure(childConstraint);
        }

        int visibleCount = CountVisible();
        int rows = visibleCount > 0 ? (int)Math.Ceiling((double)visibleCount / cols) : 0;
        double totalHeight = rows > 0 ? rows * itemHeight + (rows - 1) * VerticalSpacing : 0;

        return new Size(availableSize.Width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (cols, itemWidth, itemHeight) = ComputeLayout(finalSize.Width);
        double hSpace = HorizontalSpacing;
        double vSpace = VerticalSpacing;

        double x = 0, y = 0;
        int col = 0;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));

            col++;
            if (col >= cols)
            {
                col = 0;
                x = 0;
                y += itemHeight + vSpace;
            }
            else
            {
                x += itemWidth + hSpace;
            }
        }

        int visibleCount = CountVisible();
        int rows = visibleCount > 0 ? (int)Math.Ceiling((double)visibleCount / cols) : 0;
        double totalHeight = rows > 0 ? rows * itemHeight + (rows - 1) * vSpace : 0;

        return new Size(finalSize.Width, totalHeight);
    }
}
