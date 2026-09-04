using System;
using System.Collections.Generic;
using System.Linq;
using GalgameManager.Enums;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PotatoVN.App.PluginBase.Controls.Charts;
using PotatoVN.App.PluginBase.Models;
using PotatoVN.App.PluginBase.Services;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// 游戏分布：游玩状态 / 游戏引擎 / 制作公司 三个维度的分布卡片（左图右内容布局）。
/// 布局与日维度「今日游戏构成」一致：主体定高 420、左右等宽两列，
/// 避免右侧明细条目多时撑开行高、左侧图表被挤压（日维度旧布局"环形图被图例挤没"的同款根因）。
/// 游玩状态 = DonutChart 环形图（按款数）+ 右侧图例；游戏引擎 / 制作公司 = TreemapChart 矩形树图 + 右侧排行明细。
/// </summary>
public sealed partial class GameStatsView
{
    #region 游戏分布

    /// <summary>引擎/公司树图只取 Top N，其余并入「其他」一格，否则树图太碎</summary>
    private const int TreemapTopN = 12;

    private FrameworkElement BuildDistributionCard(StatsPalette palette)
    {
        var items = StatsService.GetDistribution(_snapshot.Games, _tab, PlayTypeDisplayName);

        FrameworkElement chart;
        FrameworkElement side;
        if (_tab == DistTab.Status)
        {
            var total = items.Sum(i => i.Count);
            var donut = new DonutChart();
            donut.SetData(
                items.Select(i => new DonutDatum(null, i.Name, i.Count, i.Icon)).ToList(),
                null, palette,
                total.ToString(), UiKit.L("Stats_DistCenterSub", "游戏数量（款）"));
            chart = donut;
            side = BuildStatusLegend(palette, items, total);
        }
        else
        {
            var aggregated = AggregateTop(items, TreemapTopN);
            var treemap = new TreemapChart();
            treemap.SetData(aggregated.Select(i => new TreemapDatum(i.Name, i.Count)).ToList(), palette);
            chart = treemap;
            side = BuildRankList(palette, aggregated);
        }

        // 主体：左右等宽两列，高度固定（图表大小不再受右侧条目数影响）
        var body = new Grid { Height = 420, Margin = new Thickness(0, 10, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(chart);
        var sideScroll = new ScrollViewer
        {
            Content = side,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(20, 0, 0, 0),
        };
        body.Children.Add(sideScroll);
        Grid.SetColumn(sideScroll, 1);

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(UiKit.Text(UiKit.L("Stats_DistTitle", "游戏分布"), palette.TextPrimary, 15, FontWeights.SemiBold));
        var tabs = UiKit.PillTabs(palette,
            new[]
            {
                UiKit.L("Stats_Tab_Status", "游玩状态"),
                UiKit.L("Stats_Tab_Engine", "游戏引擎"),
                UiKit.L("Stats_Tab_Developer", "制作公司"),
            },
            _tab switch { DistTab.Engine => 1, DistTab.Developer => 2, _ => 0 },
            index =>
            {
                _tab = index switch { 1 => DistTab.Engine, 2 => DistTab.Developer, _ => DistTab.Status };
                _data.DistTab = _tab switch { DistTab.Engine => "engine", DistTab.Developer => "developer", _ => "status" };
                BuildUi();
            });
        header.Children.Add(tabs);
        Grid.SetColumn(tabs, 1);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(header);
        content.Children.Add(body);
        Grid.SetRow(body, 1);

        var card = UiKit.Card(palette, content, new Thickness(20));
        card.Margin = new Thickness(0, 0, 0, 20);
        return card;
    }

    /// <summary>Top N + 其余并入「其他」一格；N 项以内原样返回</summary>
    private static List<DistItem> AggregateTop(List<DistItem> items, int topN)
    {
        if (items.Count <= topN) return items;
        var result = items.Take(topN).ToList();
        var rest = items.Skip(topN).ToList();
        result.Add(new DistItem
        {
            Name = UiKit.L("Stats_Other", "其他"),
            Count = rest.Sum(i => i.Count),
            Order = int.MaxValue,
        });
        return result;
    }

    /// <summary>游玩状态右侧图例：每状态一行（图标 + 色点 + 名称 + 数量 + 占比 %）</summary>
    private static FrameworkElement BuildStatusLegend(StatsPalette palette, List<DistItem> items, int total)
    {
        var panel = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var chip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            if (!string.IsNullOrEmpty(item.Icon))
            {
                chip.Children.Add(new FontIcon
                {
                    Glyph = item.Icon,
                    FontSize = 13,
                    Foreground = palette.AccentBrush,
                });
            }

            chip.Children.Add(UiKit.Dot(StatsTheme.SeriesColor(i)));
            chip.Children.Add(UiKit.Text(item.Name, palette.TextPrimary, 12,
                trimming: TextTrimming.CharacterEllipsis, maxWidth: 140));
            chip.Children.Add(UiKit.Text(item.Count.ToString(), palette.Accent, 12, FontWeights.SemiBold));
            chip.Children.Add(UiKit.Text(PercentText(item.Count, total), palette.TextMuted, 11));

            panel.Children.Add(new Border
            {
                Background = palette.BgSecondaryBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 5, 10, 5),
                Child = chip,
            });
        }

        return panel;
    }

    /// <summary>引擎/公司右侧排行式明细：序号 + 色块 + 名称 + 数量/占比 + 占比条（色序与树图格子一致）</summary>
    private static FrameworkElement BuildRankList(StatsPalette palette, List<DistItem> items)
    {
        var total = items.Sum(i => i.Count);
        var maxCount = items.Count > 0 ? items.Max(i => i.Count) : 1;

        var panel = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var line = new Grid();
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var rank = UiKit.Text((i + 1).ToString(), palette.TextMuted, 11,
                margin: new Thickness(0, 0, 0, 0));
            line.Children.Add(rank);

            var swatch = new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(StatsTheme.SeriesColor(i)),
                VerticalAlignment = VerticalAlignment.Center,
            };
            line.Children.Add(swatch);
            Grid.SetColumn(swatch, 1);

            var name = UiKit.Text(item.Name, palette.TextPrimary, 12,
                trimming: TextTrimming.CharacterEllipsis, margin: new Thickness(8, 0, 8, 0));
            ToolTipService.SetToolTip(name, item.Name);
            line.Children.Add(name);
            Grid.SetColumn(name, 2);

            var countText = UiKit.Text(
                $"{item.Count} · {PercentText(item.Count, total)}", palette.TextMuted, 11);
            line.Children.Add(countText);
            Grid.SetColumn(countText, 3);

            // 占比条（相对最大分类，星列按比例）
            var trackGrid = new Grid { Height = 4 };
            var barPercent = maxCount > 0 ? Math.Clamp(item.Count * 100.0 / maxCount, 0, 100) : 0;
            trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(barPercent, GridUnitType.Star) });
            trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - barPercent, GridUnitType.Star) });
            trackGrid.Children.Add(new Border
            {
                Background = new SolidColorBrush(StatsTheme.SeriesColor(i)),
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            });

            var cell = new StackPanel { Spacing = 5 };
            cell.Children.Add(line);
            cell.Children.Add(trackGrid);

            panel.Children.Add(new Border
            {
                Background = palette.BgSecondaryBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 8),
                Child = cell,
            });
        }

        return panel;
    }

    private static string PercentText(int part, int total)
        => (total > 0 ? part * 100.0 / total : 0).ToString("F1") + "%";

    private static string PlayTypeDisplayName(PlayType type)
        => UiKit.L($"PlayType_{type}", type.ToString());

    #endregion
}
