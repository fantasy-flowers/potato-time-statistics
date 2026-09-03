using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls.Charts;

/// <summary>
/// 原生柱形图（WinUI 自绘）。
/// 点击柱形触发 <see cref="BarClicked"/>；compact 模式用于趋势迷你图（无坐标轴）。
/// </summary>
internal sealed class BarChart : Grid
{
    /// <summary>柱形被点击（参数为数据下标）</summary>
    public event EventHandler<int>? BarClicked;

    private List<string> _labels = new();
    private List<double> _values = new(); // 单位：小时
    private List<string> _tooltips = new();
    private int? _selectedIndex;
    private int _highlightIndex = -1;
    private Color? _highlightColor;
    private bool _compact;
    private StatsPalette _palette = StatsTheme.For(ElementTheme.Dark);
    private readonly Canvas _plotArea = new();

    public BarChart()
    {
        SizeChanged += (_, _) => Render();
    }

    /// <summary>
    /// 设置数据。
    /// </summary>
    /// <param name="labels">X 轴标签</param>
    /// <param name="values">数值（小时）</param>
    /// <param name="tooltips">悬浮提示（可空，默认用标签+时长）</param>
    /// <param name="selectedIndex">选中柱下标（高亮）</param>
    /// <param name="highlightIndex">强调柱下标（迷你图"今天"用）</param>
    /// <param name="highlightColor">强调柱颜色</param>
    /// <param name="compact">迷你模式：不显示坐标轴/网格线/X 标签</param>
    public void SetData(List<string> labels, List<double> values, StatsPalette palette, List<string>? tooltips = null,
        int? selectedIndex = null, int highlightIndex = -1, Color? highlightColor = null, bool compact = false)
    {
        _labels = labels;
        _values = values;
        _palette = palette;
        _tooltips = tooltips ?? labels.Select((l, i) => $"{l}\n{UiKit.FormatTime(values[i])}").ToList();
        _selectedIndex = selectedIndex;
        _highlightIndex = highlightIndex;
        _highlightColor = highlightColor;
        _compact = compact;
        Render();
    }

    private void Render()
    {
        Children.Clear();
        _plotArea.Children.Clear();

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 10 || height <= 10 || _values.Count == 0)
        {
            if (_values.Count == 0)
                Children.Add(UiKit.EmptyState(UiKit.L("Chart_NoData", "暂无游戏记录"), _palette.TextMuted));
            return;
        }

        RowDefinitions.Clear();
        ColumnDefinitions.Clear();

        var allZero = _values.All(v => v <= 0);

        // 布局：左 y 轴标签列 + 右绘图区；下 x 轴标签行
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        const double yAxisWidth = 40;
        ColumnDefinitions[0].Width = new GridLength(_compact ? 0 : yAxisWidth);
        RowDefinitions[1].Height = new GridLength(_compact ? 0 : 22);

        var plotWidth = Math.Max(1, width - (_compact ? 0 : yAxisWidth));
        var plotHeight = Math.Max(1, height - (_compact ? 0 : 22));

        // 网格线与 y 轴标签
        var niceMax = NiceMax(_values.Max());
        if (!_compact && !allZero)
        {
            const int tickCount = 5;
            for (var i = 0; i < tickCount; i++)
            {
                var value = niceMax * i / (tickCount - 1);
                var y = plotHeight - plotHeight * value / niceMax;
                if (y < 0) y = 0;
                if (y > plotHeight) y = plotHeight;

                if (i > 0)
                {
                    _plotArea.Children.Add(new Line
                    {
                        X1 = 0,
                        Y1 = y,
                        X2 = plotWidth,
                        Y2 = y,
                        Stroke = _palette.Brush(Color.FromArgb(0x40, _palette.TextMuted.R, _palette.TextMuted.G, _palette.TextMuted.B)),
                        StrokeDashArray = { 3, 4 },
                    });
                }

                Children.Add(new TextBlock
                {
                    Text = FormatAxisValue(value),
                    FontSize = 11,
                    Foreground = _palette.TextMutedBrush,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, y - 9, 6, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = yAxisWidth - 6,
                });
            }
        }

        // 柱形
        var count = _values.Count;
        var slotWidth = plotWidth / count;
        var barWidth = Math.Min(slotWidth * 0.48, 46);

        for (var i = 0; i < count; i++)
        {
            var value = _values[i];
            var barHeight = allZero || niceMax <= 0 ? 0.0 : Math.Max(0, plotHeight * Math.Min(1, value / niceMax));
            var x = slotWidth * i + (slotWidth - barWidth) / 2;
            var y = plotHeight - barHeight;

            Brush fill;
            if (_selectedIndex == i)
            {
                fill = UiKit.VerticalGradient(_palette.AccentBright, _palette.AccentDark);
            }
            else if (_highlightIndex == i && _highlightColor is { } hc)
            {
                fill = new SolidColorBrush(hc);
            }
            else
            {
                fill = _compact
                    ? UiKit.VerticalGradient(_palette.Accent, Color.FromArgb(0xFF, _palette.Hover.R, _palette.Hover.G, _palette.Hover.B))
                    : UiKit.VerticalGradient(_palette.Accent, _palette.AccentDark);
            }

            var bar = new Rectangle
            {
                Width = Math.Max(1, barWidth),
                Height = Math.Max(0, barHeight),
                RadiusX = 3,
                RadiusY = 3,
                Fill = fill,
            };
            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            _plotArea.Children.Add(bar);

            var index = i;
            bar.Tapped += (_, _) => BarClicked?.Invoke(this, index);
            if (index >= 0 && index < _tooltips.Count)
                ToolTipService.SetToolTip(bar, _tooltips[index]);

            // X 轴标签
            if (!_compact)
            {
                var label = new TextBlock
                {
                    Text = _labels[i],
                    FontSize = 11,
                    Foreground = _palette.TextMutedBrush,
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 0, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Width = slotWidth,
                    MaxLines = 1,
                };
                Children.Add(label);
                Grid.SetColumn(label, 1);
                Grid.SetRow(label, 1);
            }
        }

        var plotGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        plotGrid.Children.Add(_plotArea);
        Children.Add(plotGrid);
        Grid.SetColumn(plotGrid, 1);
        Grid.SetRow(plotGrid, 0);
    }

    private static string FormatAxisValue(double hours)
    {
        if (hours < 1) return $"{Math.Round(hours * 60)}{UiKit.L("Unit_MinShort", "分")}";
        return $"{hours.ToString("F0", CultureInfo.CurrentCulture)}{UiKit.L("Unit_HourShort", "时")}";
    }

    /// <summary>取整的坐标轴上限（1/2/2.5/5 × 10^k）</summary>
    private static double NiceMax(double maxValue)
    {
        if (maxValue <= 0) return 1;
        var exponent = Math.Floor(Math.Log10(maxValue));
        var fraction = maxValue / Math.Pow(10, exponent);
        double niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
        return niceFraction * Math.Pow(10, exponent);
    }
}
