using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PotatoVN.App.PluginBase.Controls.Charts;
using PotatoVN.App.PluginBase.Models;
using PotatoVN.App.PluginBase.Services;
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// 主内容区：日维度为上下两张整行卡片（今日游戏构成 + 近7日趋势）；
/// 周/月维度为左侧柱形图卡片 + 右侧排行侧栏。
/// </summary>
public sealed partial class PlaytimeStatsView
{
    #region 主内容区（图表 + 侧栏）

    private FrameworkElement BuildMainContent(StatsPalette palette)
    {
        if (_period == StatsPeriod.Day)
            return BuildDayMainContent(palette);

        // 周/月：左右卡片同高对齐且高度固定（比旧版 ~494 更高），
        // 否则行高会被右侧长排行列表撑开、左侧图表卡跟着被拉高；
        // 高度受限后右侧列表的 ScrollViewer（Star 行）自动出现滚动条。
        var root = new Grid { Height = 600 };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(BuildChartCard(palette));
        var sideCard = BuildSideCard(palette);
        root.Children.Add(sideCard);
        Grid.SetColumn(sideCard, 2);
        return root;
    }

    /// <summary>
    /// 日维度整行布局：上行「今日游戏构成」（左环形图 + 右图例滚动区），
    /// 下行「近7日游玩趋势」（左柱形图 + 右统计内容）。
    /// </summary>
    private FrameworkElement BuildDayMainContent(StatsPalette palette)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildDayCompositionCard(palette));
        var trendCard = BuildDayTrendCard(palette);
        root.Children.Add(trendCard);
        Grid.SetRow(trendCard, 2);
        return root;
    }

    /// <summary>周/月维度的时长分布柱形图卡片（高度由外层固定，图表随卡片拉伸填充）</summary>
    private FrameworkElement BuildChartCard(StatsPalette palette)
    {
        var chartHost = new Grid();
        chartHost.Children.Add(BuildBarChart(palette));

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(BuildCardHeader(palette,
            UiKit.L("Chart_DistTitle", "时长分布"),
            UiKit.L("Chart_BarHint", "点击柱形可筛选对应时段的游戏排行")));
        content.Children.Add(chartHost);
        Grid.SetRow(chartHost, 1);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    private FrameworkElement BuildCardHeader(StatsPalette palette, string title, string hint)
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(UiKit.Text(title, palette.TextPrimary, 15, FontWeights.SemiBold));
        var hintText = UiKit.Text(hint, palette.TextMuted, 11);
        root.Children.Add(hintText);
        Grid.SetColumn(hintText, 1);
        return root;
    }

    /// <summary>
    /// 日维度「今日游戏构成」卡片：占据整行，左半环形图、右半游戏图例。
    /// 卡片高度固定（图例不再挤占图表高度）；图例超出右半高度时滚动。
    /// </summary>
    private FrameworkElement BuildDayCompositionCard(StatsPalette palette)
    {
        var todayGames = StatsService.GetDayGames(_snapshot, _selectedDate);
        var totalMinutes = todayGames.Sum(g => g.Minutes);

        var donut = new DonutChart();
        donut.SetData(todayGames, _selectedGameId, palette,
            UiKit.FormatHours(totalMinutes / 60.0), UiKit.L("Chart_DayCenterSub", "今日总时长（小时）"));
        donut.SegmentClicked += (_, id) =>
        {
            _selectedGameId = _selectedGameId == id ? null : id;
            BuildUi();
        };

        // 图例
        var legendItems = new List<Border>();
        for (var i = 0; i < todayGames.Count; i++)
        {
            var game = todayGames[i];
            var index = i;
            var chip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            chip.Children.Add(UiKit.Dot(StatsTheme.SeriesColor(index)));
            chip.Children.Add(UiKit.Text(game.Name, palette.TextPrimary, 12, trimming: TextTrimming.CharacterEllipsis, maxWidth: 110));
            chip.Children.Add(UiKit.Text(Percent(game.Minutes, totalMinutes) + "%", palette.TextMuted, 11));

            var container = new Border
            {
                Background = palette.BgSecondaryBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = _selectedGameId == game.Id ? palette.AccentBrush : new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1),
                Child = chip,
            };
            var id = game.Id;
            container.Tapped += (_, _) =>
            {
                _selectedGameId = _selectedGameId == id ? null : id;
                BuildUi();
            };
            legendItems.Add(container);
        }

        var legend = new ItemsRepeater
        {
            ItemsSource = legendItems,
            Layout = new UniformGridLayout
            {
                MinItemWidth = 160,
                MinItemHeight = 28,
                MinRowSpacing = 6,
                MinColumnSpacing = 8,
            },
            // 与左侧环形图纵向居中对齐
            VerticalAlignment = VerticalAlignment.Center,
        };
        var legendScroll = new ScrollViewer
        {
            Content = legend,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(20, 0, 0, 0),
        };

        // 主体：左右等宽两列，高度固定（环形图大小不再受图例条目数影响）
        var body = new Grid { Height = 420 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(donut);
        body.Children.Add(legendScroll);
        Grid.SetColumn(legendScroll, 1);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(BuildCardHeader(palette,
            UiKit.L("Chart_TodayTitle", "今日游戏构成"),
            UiKit.L("Chart_TodayHint", "点击扇区查看该游戏近7日趋势")));
        content.Children.Add(body);
        Grid.SetRow(body, 1);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    private FrameworkElement BuildBarChart(StatsPalette palette)
    {
        var chart = new BarChart();
        List<string> labels;
        List<double> values;
        List<string> tooltips;

        if (_period == StatsPeriod.Week)
        {
            var days = StatsService.GetWeekDays(_selectedDate);
            labels = days.Select(UiKit.FormatDayLabel).ToList();
            values = days.Select(d => StatsService.GetDayTotal(_snapshot, d) / 60.0).ToList();
            tooltips = days.Select((d, i) => BuildBarTooltip(UiKit.FormatDayLabel(d), StatsService.GetPeriodGames(_snapshot, days, i))).ToList();
        }
        else
        {
            var weeks = StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1);
            labels = weeks.Select(w => UiKit.FormatWeekLabel(_selectedMonth + 1, w.WeekNum)).ToList();
            values = weeks.Select(w => w.TotalMinutes / 60.0).ToList();
            tooltips = weeks.Select(w => BuildBarTooltip(
                UiKit.FormatWeekLabel(_selectedMonth + 1, w.WeekNum),
                w.GameMinutes.Where(kv => kv.Value > 0)
                    .OrderByDescending(kv => kv.Value)
                    .Take(3)
                    .Select(kv => (
                        Name: _snapshot.Games.FirstOrDefault(g => g.Uuid == kv.Key)?.Name.Value ?? "?",
                        Minutes: kv.Value))
                    .ToList())).ToList();
        }

        chart.SetData(labels, values, palette, tooltips, _selectedIndex);
        chart.BarClicked += (_, index) =>
        {
            _selectedIndex = _selectedIndex == index ? null : index;
            BuildUi();
        };
        return chart;
    }

    private static string BuildBarTooltip(string label, List<GamePeriodTime> topGames)
    {
        var text = label;
        foreach (var game in topGames.Take(3))
            text += $"\n{game.Name}  {UiKit.FormatTimeShort(game.Hours)}";
        return text;
    }

    private static string BuildBarTooltip(string label, List<(string Name, int Minutes)> topGames)
    {
        var text = label;
        foreach (var game in topGames)
            text += $"\n{game.Name}  {UiKit.FormatTimeShort(game.Minutes / 60.0)}";
        return text;
    }

    #endregion
}
