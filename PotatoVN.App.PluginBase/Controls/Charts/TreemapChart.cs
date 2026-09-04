using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls.Charts;

/// <summary>矩形树图数据项（Value 决定面积占比）</summary>
public sealed record TreemapDatum(string Name, double Value);

/// <summary>
/// 原生矩形树图（WinUI 自绘，无外部图表依赖）：squarified 算法把面积占比排布为近正方形色块。
/// 每格 = 色块（SeriesColor 序号取色 + 卡片底色描边）+ 居中「名称 + 数量」；
/// 格子过小时只显示数量或不显示，tooltip 补全名 / 数量 / 占比。
/// 数据量大时调用方应先聚合 Top N + 「其他」，避免格子太碎。
/// </summary>
internal sealed class TreemapChart : Grid
{
    private List<TreemapDatum> _items = new();
    private StatsPalette _palette = StatsTheme.For(ElementTheme.Dark);

    public TreemapChart()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        SizeChanged += (_, _) => Render();
    }

    public void SetData(List<TreemapDatum> items, StatsPalette palette)
    {
        _items = items.Where(i => i.Value > 0).OrderByDescending(i => i.Value).ToList();
        _palette = palette;
        Render();
    }

    private void Render()
    {
        // 每次整棵重建，避免共享元素重复挂载（COMException 0x800F1000）
        Children.Clear();
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 20 || height <= 20)
        {
            if (_items.Count == 0)
                Children.Add(UiKit.EmptyState(UiKit.L("Chart_NoData", "暂无游戏记录"), _palette.TextMuted));
            return;
        }

        var total = _items.Sum(i => i.Value);
        if (_items.Count == 0 || total <= 0)
        {
            Children.Add(UiKit.EmptyState(UiKit.L("Chart_NoData", "暂无游戏记录"), _palette.TextMuted));
            return;
        }

        // 树图格子不能用 UniformGrid（WinUI 3 无此控件），用 Canvas 绝对定位
        var canvas = new Canvas { Width = width, Height = height };
        var rects = Squarify(_items.Select(i => i.Value).ToList(), new Rect(0, 0, width, height));

        for (var i = 0; i < _items.Count; i++)
        {
            var rect = rects[i];
            if (rect.Width < 2 || rect.Height < 2) continue;

            var item = _items[i];
            var percent = item.Value / total * 100.0;

            var tile = new Border
            {
                Width = rect.Width,
                Height = rect.Height,
                Background = new SolidColorBrush(StatsTheme.SeriesColor(i)),
                BorderBrush = _palette.CardBrush, // 与环形图一致：卡片底色描边分隔
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
            };

            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            // 格子够大才放名称，再小只放数量，太小什么都不放（tooltip 兜底）
            if (rect.Width >= 70 && rect.Height >= 40)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = item.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    MaxWidth = rect.Width - 12,
                });
                panel.Children.Add(new TextBlock
                {
                    Text = item.Value.ToString("F0"),
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                    TextAlignment = TextAlignment.Center,
                });
            }
            else if (rect.Width >= 36 && rect.Height >= 20)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = item.Value.ToString("F0"),
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                    TextAlignment = TextAlignment.Center,
                });
            }

            tile.Child = panel;
            ToolTipService.SetToolTip(tile,
                $"{item.Name}\n{UiKit.L("Chart_Count", "数量")}：{item.Value.ToString("F0")}\n" +
                $"{UiKit.L("Chart_Percent", "占比")}：{percent.ToString("F1")}%");

            Canvas.SetLeft(tile, rect.X);
            Canvas.SetTop(tile, rect.Y);
            canvas.Children.Add(tile);
        }

        Children.Add(canvas);
    }

    #region squarified 布局（Bruls et al.：保持长宽比接近 1）

    private static List<Rect> Squarify(IReadOnlyList<double> values, Rect bounds)
    {
        var result = new Rect[values.Count];
        var total = values.Sum();
        if (total <= 0) return result.ToList();

        // 面积归一化到可用矩形
        var scaled = values.Select(v => v / total * bounds.Width * bounds.Height).ToList();

        var x = bounds.X;
        var y = bounds.Y;
        var w = bounds.Width;
        var h = bounds.Height;

        var i = 0;
        while (i < scaled.Count)
        {
            // 贪心地往当前行里加，直到长宽比变差
            var row = new List<int> { i };
            var side = Math.Min(w, h);
            var j = i + 1;
            while (j < scaled.Count && side > 0 && Worst(row, scaled, side) >= Worst(With(row, j), scaled, side))
            {
                row.Add(j);
                j++;
            }

            // 沿短边铺一行：横铺（当前区域比高宽）或竖铺
            var rowSum = row.Sum(k => scaled[k]);
            if (w >= h)
            {
                var colWidth = rowSum / h;
                var offset = y;
                foreach (var k in row)
                {
                    var itemHeight = colWidth > 0 ? scaled[k] / colWidth : 0;
                    result[k] = new Rect(x, offset, colWidth, itemHeight);
                    offset += itemHeight;
                }

                x += colWidth;
                w -= colWidth;
            }
            else
            {
                var rowHeight = rowSum / w;
                var offset = x;
                foreach (var k in row)
                {
                    var itemWidth = rowHeight > 0 ? scaled[k] / rowHeight : 0;
                    result[k] = new Rect(offset, y, itemWidth, rowHeight);
                    offset += itemWidth;
                }

                y += rowHeight;
                h -= rowHeight;
            }

            i = j;
        }

        return result.ToList();
    }

    private static List<int> With(List<int> row, int index)
    {
        var copy = new List<int>(row) { index };
        return copy;
    }

    /// <summary>一行内最差长宽比（越大越差）</summary>
    private static double Worst(List<int> row, List<double> scaled, double side)
    {
        var sum = row.Sum(k => scaled[k]);
        var max = row.Max(k => scaled[k]);
        var min = row.Min(k => scaled[k]);
        if (sum <= 0 || min <= 0 || side <= 0) return double.MaxValue;
        var sum2 = sum * sum;
        var side2 = side * side;
        return Math.Max(side2 * max / sum2, sum2 / (side2 * min));
    }

    #endregion
}
