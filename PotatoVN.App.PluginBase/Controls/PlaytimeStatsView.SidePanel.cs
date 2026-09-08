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
/// 侧栏：周/月维度显示游戏时长排行（含排序、柱形筛选联动）。
/// 日维度的近7日趋势已移出侧栏，见 <see cref="BuildDayTrendCard"/>（整行卡片）。
/// </summary>
public sealed partial class PlaytimeStatsView
{
    #region 侧栏（排行）

    private FrameworkElement BuildSideCard(StatsPalette palette)
        => BuildRankPanel(palette);

    private FrameworkElement BuildRankPanel(StatsPalette palette)
    {
        // 下钻状态：排行卡内容整体替换为该游戏的逐日明细（卡片位置与高度不变）
        if (_rankDrillGameId is { } drillId)
            return BuildRankDrillPanel(palette, drillId);

        List<GamePeriodTime> games;
        if (_period == StatsPeriod.Week)
        {
            var days = StatsService.GetWeekDays(_selectedDate);
            games = StatsService.GetPeriodGames(_snapshot, days, _selectedIndex);
        }
        else
        {
            games = StatsService.GetMonthGames(_snapshot, _selectedYear, _selectedMonth + 1, _selectedIndex);
        }

        var sorted = UiKit.SortGames(games, _sort);
        var maxMinutes = sorted.Count > 0 ? sorted.Max(g => g.Minutes) : 0;
        var grandTotal = sorted.Sum(g => g.Minutes);

        // 头部
        var header = new StackPanel();
        var headerTop = new Grid();
        headerTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerTop.Children.Add(UiKit.Text(UiKit.L("Rank_Title", "游戏时长排行"), palette.TextPrimary, 15, FontWeights.SemiBold));
        var sortToggle = UiKit.SortToggle(palette,
            new[] { UiKit.L("Rank_SortTime", "按时长"), UiKit.L("Rank_SortName", "按名称") },
            _sort == RankSort.Time ? 0 : 1,
            index =>
            {
                _sort = index == 0 ? RankSort.Time : RankSort.Name;
                _data.RankSort = _sort == RankSort.Time ? "time" : "name";
                BuildUi();
            });
        headerTop.Children.Add(sortToggle);
        Grid.SetColumn(sortToggle, 1);
        header.Children.Add(headerTop);

        // 筛选状态
        if (_selectedIndex is { } idx)
        {
            var filterLabel = _period == StatsPeriod.Week
                ? UiKit.FormatDayLabel(StatsService.GetWeekDays(_selectedDate)[idx])
                : UiKit.FormatWeekLabel(_selectedMonth + 1,
                    StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1)[idx].WeekNum);
            var filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            filterRow.Children.Add(UiKit.Text($"{UiKit.L("Rank_Filtered", "已筛选")}：{filterLabel}", palette.Accent, 11.5));
            var clearButton = new Button
            {
                Content = UiKit.L("Rank_ClearFilter", "清除筛选"),
                FontSize = 11.5,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = palette.TextMutedBrush,
                Padding = new Thickness(0),
            };
            clearButton.Click += (_, _) =>
            {
                _selectedIndex = null;
                BuildUi();
            };
            filterRow.Children.Add(clearButton);
            header.Children.Add(filterRow);
        }

        // 列表
        FrameworkElement body;
        if (sorted.Count == 0)
        {
            body = UiKit.EmptyState(UiKit.L("Rank_Empty", "该时段暂无游戏记录"), palette.TextMuted);
        }
        else
        {
            var list = new StackPanel();
            for (var i = 0; i < sorted.Count; i++)
            {
                list.Children.Add(BuildRankItem(palette, sorted[i], i, maxMinutes, grandTotal));
            }

            body = new ScrollViewer
            {
                Content = list,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
        }

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(header);
        root.Children.Add(body);
        Grid.SetRow(body, 1);
        return WrapSideCard(palette, root);
    }

    private FrameworkElement BuildRankItem(StatsPalette palette, GamePeriodTime game, int index, int maxMinutes, int grandTotal)
    {
        var barPercent = maxMinutes > 0 ? game.Minutes * 100.0 / maxMinutes : 0;
        var sharePercent = grandTotal > 0 ? game.Minutes * 100.0 / grandTotal : 0;

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

        var rankText = UiKit.Text((index + 1).ToString(), index < 3 ? palette.Accent : palette.TextMuted, 12,
            FontWeights.SemiBold, textAlignment: TextAlignment.Center);
        row.Children.Add(rankText);

        var icon = UiKit.GameIcon(game, index);
        row.Children.Add(icon);
        Grid.SetColumn(icon, 1);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        info.Children.Add(UiKit.Text(game.Name, palette.TextPrimary, 13.5, trimming: TextTrimming.CharacterEllipsis));
        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 3, 0, 0) };
        meta.Children.Add(UiKit.Text(UiKit.FormatTime(game.Hours), palette.TextSecondary, 11.5));
        meta.Children.Add(UiKit.Text(sharePercent.ToString("F1") + "%", palette.TextMuted, 10.5));
        info.Children.Add(meta);
        row.Children.Add(info);
        Grid.SetColumn(info, 2);

        var barTrack = new Border
        {
            Background = palette.BgSecondaryBrush,
            CornerRadius = new CornerRadius(2),
            Width = 56,
            Height = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        barTrack.Child = new Border
        {
            Background = new SolidColorBrush(StatsTheme.SeriesColor(index)),
            CornerRadius = new CornerRadius(2),
            Width = Math.Max(2, 56 * barPercent / 100),
            Height = 4,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        row.Children.Add(barTrack);
        Grid.SetColumn(barTrack, 3);

        var container = new Border { Padding = new Thickness(0, 9, 0, 9), CornerRadius = new CornerRadius(4) };
        UiKit.AttachHover(container, new SolidColorBrush(Colors.Transparent), palette.HoverBrush);
        container.Child = row;

        // 点击游戏 → 卡内下钻为逐日明细；进入明细时清除柱形筛选（明细展示完整时段）
        var gameId = game.Id;
        container.Tapped += (_, _) =>
        {
            _rankDrillGameId = gameId;
            _rankDrillDesc = true;
            _selectedIndex = null;
            BuildUi();
        };
        return container;
    }

    /// <summary>
    /// 排行卡下钻视图：单个游戏在当前周/月时段内的逐日游玩时长明细（列表式，每天一行：
    /// 日期 + 时长进度条 + 时长文本，0 分钟行无条），支持按时长倒序/正序重排，可返回排行列表。
    /// </summary>
    private FrameworkElement BuildRankDrillPanel(StatsPalette palette, Guid gameId)
    {
        var game = _snapshot.Games.FirstOrDefault(g => g.Uuid == gameId);
        if (game is null)
        {
            // 数据刷新后游戏已不在库中 → 复位回排行
            _rankDrillGameId = null;
            return BuildRankPanel(palette);
        }

        // 时段内全部自然日：周=当周 7 天，月=当月全部自然日
        List<DateTime> days;
        string rangeText;
        if (_period == StatsPeriod.Week)
        {
            days = StatsService.GetWeekDays(_selectedDate);
            rangeText = UiKit.FormatWeekRange(days[0]);
        }
        else
        {
            var year = _selectedYear;
            var month = _selectedMonth + 1;
            days = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(d => new DateTime(year, month, d))
                .ToList();
            rangeText = UiKit.FormatYM(year, month);
        }

        _snapshot.PerGameDaily.TryGetValue(gameId, out var daily);
        var entries = days
            .Select(d => new TrendDay { Date = d, Minutes = daily is not null && daily.TryGetValue(d, out var v) ? v : 0 })
            .ToList();
        var total = entries.Sum(e => e.Minutes);

        // 头部：标题（游戏名 · 时段范围）+ 倒序/正序切换 + 返回排行
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = UiKit.Text($"{game.Name.Value} · {rangeText}", palette.TextPrimary, 15,
            FontWeights.SemiBold, trimming: TextTrimming.CharacterEllipsis,
            margin: new Thickness(0, 0, 8, 0));
        ToolTipService.SetToolTip(titleText, $"{game.Name.Value} · {rangeText}");
        header.Children.Add(titleText);

        var sortToggle = UiKit.SortToggle(palette,
            new[] { UiKit.L("Drill_SortDesc", "倒序"), UiKit.L("Drill_SortAsc", "正序") },
            _rankDrillDesc ? 0 : 1,
            index =>
            {
                _rankDrillDesc = index == 0;
                BuildUi();
            });
        header.Children.Add(sortToggle);
        Grid.SetColumn(sortToggle, 1);

        var backButton = new Button
        {
            Content = UiKit.L("Drill_Back", "返回排行"),
            FontSize = 12,
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Foreground = palette.TextSecondaryBrush,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(8, 0, 0, 0),
        };
        backButton.Click += (_, _) =>
        {
            _rankDrillGameId = null;
            BuildUi();
        };
        header.Children.Add(backButton);
        Grid.SetColumn(backButton, 2);

        // 主体：逐日明细列表（每天一行：日期 + 时长条 + 时长文本）；无记录时空态（卡片不塌陷）
        FrameworkElement body;
        if (total <= 0)
        {
            body = UiKit.EmptyState(UiKit.L("Drill_Empty", "该游戏在该时段暂无游玩记录"), palette.TextMuted);
        }
        else
        {
            // 日期与时长成对排序；同值按日期升序保证稳定
            var ordered = _rankDrillDesc
                ? entries.OrderByDescending(e => e.Minutes).ThenBy(e => e.Date).ToList()
                : entries.OrderBy(e => e.Minutes).ThenBy(e => e.Date).ToList();
            var maxMinutes = ordered.Max(e => e.Minutes);

            var list = new StackPanel();
            for (var i = 0; i < ordered.Count; i++)
            {
                list.Children.Add(BuildDrillRow(palette, ordered[i], maxMinutes));
            }

            body = new ScrollViewer
            {
                Content = list,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
        }

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(header);
        root.Children.Add(body);
        Grid.SetRow(body, 1);
        return WrapSideCard(palette, root);
    }

    /// <summary>
    /// 逐日明细行：日期（M/D + 周几）+ 时长进度条 + 时长文本；0 分钟时不画条、文本紧跟日期。
    /// 进度条填充用百分比星列实现，随轨道宽度伸缩（不要把 0-100 百分数当像素值）。
    /// </summary>
    private static FrameworkElement BuildDrillRow(StatsPalette palette, TrendDay day, int maxMinutes)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        datePanel.Children.Add(UiKit.Text(UiKit.FormatMD(day.Date), palette.TextSecondary, 12.5));
        datePanel.Children.Add(UiKit.Text(UiKit.WeekDayName(day.Date.DayOfWeek), palette.TextMuted, 10.5));
        row.Children.Add(datePanel);

        if (day.Minutes > 0)
        {
            var fillPercent = maxMinutes > 0 ? Math.Clamp(day.Minutes * 100.0 / maxMinutes, 0, 100) : 0;
            var barTrack = new Border
            {
                Background = palette.BgSecondaryBrush,
                CornerRadius = new CornerRadius(3),
                Height = 6,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
            };
            var fillHost = new Grid();
            fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fillPercent, GridUnitType.Star) });
            fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - fillPercent, GridUnitType.Star) });
            fillHost.Children.Add(new Border
            {
                Background = palette.AccentBrush,
                CornerRadius = new CornerRadius(3),
                Height = 6,
            });
            barTrack.Child = fillHost;
            row.Children.Add(barTrack);
            Grid.SetColumn(barTrack, 1);

            var timeText = UiKit.Text(UiKit.FormatTime(day.Hours), palette.TextPrimary, 12.5, FontWeights.Medium);
            timeText.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(timeText);
            Grid.SetColumn(timeText, 2);
        }
        else
        {
            // 0 分钟：不画条，文本紧跟日期（对齐参考样式）
            var zeroText = UiKit.Text(UiKit.L("Drill_Zero", "0分钟"), palette.TextMuted, 12.5);
            zeroText.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(zeroText);
            Grid.SetColumn(zeroText, 1);
        }

        var container = new Border { Padding = new Thickness(0, 9, 0, 9), CornerRadius = new CornerRadius(4) };
        UiKit.AttachHover(container, new SolidColorBrush(Colors.Transparent), palette.HoverBrush);
        container.Child = row;
        return container;
    }

    /// <summary>
    /// 日维度「近7日游玩趋势」卡片：占据整行，位于今日游戏构成卡片下方。
    /// 左侧柱形图（带坐标轴完整版），右侧统计摘要 2×2 + 每日列表（超出滚动）。
    /// </summary>
    private FrameworkElement BuildDayTrendCard(StatsPalette palette)
    {
        var selectedGame = _snapshot.Games.FirstOrDefault(g => g.Uuid == _selectedGameId);
        var recent7 = StatsService.GetRecentDays(_selectedDate);
        var trend = StatsService.GetTrendDays(_snapshot, recent7, _selectedGameId);
        var total7 = trend.Sum(t => t.Minutes);
        var avg7 = trend.Average(t => t.Minutes);
        var maxValue = trend.Count > 0 ? trend.Max(t => t.Minutes) : 0;
        var maxIndex = trend.FindIndex(t => t.Minutes == maxValue);
        var todayValue = trend.LastOrDefault()?.Minutes ?? 0;
        var gameColor = selectedGame is not null ? (Color?)StatsTheme.SeriesColor(selectedGame.Uuid) : null;

        // 头部（标题 + 选中游戏时的返回按钮）
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = selectedGame is null
            ? UiKit.L("Trend_Title", "近7日游玩趋势")
            : $"{selectedGame.Name.Value} · {UiKit.L("Trend_Title", "近7日游玩趋势")}";
        header.Children.Add(UiKit.Text(title, palette.TextPrimary, 15, FontWeights.SemiBold, trimming: TextTrimming.CharacterEllipsis));
        if (selectedGame is not null)
        {
            var backButton = new Button
            {
                Content = $"{UiKit.L("Trend_Back", "返回总览")} ←",
                FontSize = 12,
                Background = palette.BgSecondaryBrush,
                BorderBrush = palette.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Foreground = palette.TextSecondaryBrush,
                Padding = new Thickness(8, 4, 8, 4),
            };
            backButton.Click += (_, _) =>
            {
                _selectedGameId = null;
                BuildUi();
            };
            header.Children.Add(backButton);
            Grid.SetColumn(backButton, 1);
        }

        // 左侧：柱形图（带坐标轴的完整版，末根柱为选中日高亮）
        var chart = new BarChart();
        chart.SetData(
            trend.Select(t => UiKit.FormatMD(t.Date)).ToList(),
            trend.Select(t => t.Hours).ToList(),
            palette,
            trend.Select(t => $"{UiKit.FormatMD(t.Date)} {UiKit.WeekDayName(t.Date.DayOfWeek)}\n{UiKit.FormatTime(t.Hours)}").ToList(),
            highlightIndex: trend.Count - 1,
            highlightColor: gameColor,
            compact: false);

        // 右侧：摘要 2×2
        var summary = new Grid();
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summary.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var maxDayLabel = maxIndex >= 0 && maxIndex < trend.Count ? UiKit.FormatMD(trend[maxIndex].Date) : "—";
        AddSummaryItem(summary, palette, UiKit.L("Trend_Total7", "7日总计"), UiKit.FormatTime(total7 / 60.0), 0, 0);
        AddSummaryItem(summary, palette, UiKit.L("Trend_Avg", "日均"), UiKit.FormatTime(avg7 / 60.0), 1, 0);
        AddSummaryItem(summary, palette, UiKit.L("Trend_MaxDay", "最高单日"),
            $"{maxDayLabel} · {UiKit.FormatTimeShort(maxValue / 60.0)}", 0, 1);
        AddSummaryItem(summary, palette, UiKit.L("Trend_ActiveDays", "活跃天数"),
            $"{trend.Count(t => t.Minutes > 0)} / 7 {UiKit.L("Unit_Days", "天")}", 1, 1);

        // 右侧：每日列表（超出滚动）
        var list = new StackPanel();
        for (var i = 0; i < trend.Count; i++)
        {
            list.Children.Add(BuildTrendRow(palette, trend[i], maxValue, todayValue, i == trend.Count - 1, gameColor));
        }

        var listScroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var rightPanel = new Grid { Margin = new Thickness(20, 0, 0, 0) };
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rightPanel.Children.Add(summary);
        rightPanel.Children.Add(listScroll);
        Grid.SetRow(listScroll, 1);

        // 主体：左图表 + 右统计，高度固定
        var body = new Grid { Height = 280, Margin = new Thickness(0, 12, 0, 0) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(chart);
        body.Children.Add(rightPanel);
        Grid.SetColumn(rightPanel, 1);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(header);
        content.Children.Add(body);
        Grid.SetRow(body, 1);
        return UiKit.Card(palette, content, new Thickness(20));
    }

    private static void AddSummaryItem(Grid grid, StatsPalette palette, string label, string value, int column, int row)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 6) };
        panel.Children.Add(UiKit.Text(label, palette.TextMuted, 10.5));
        panel.Children.Add(UiKit.Text(value, palette.TextPrimary, 14.5, FontWeights.SemiBold, margin: new Thickness(0, 2, 0, 0)));
        grid.Children.Add(panel);
        Grid.SetColumn(panel, column);
        Grid.SetRow(panel, row);
    }

    private FrameworkElement BuildTrendRow(StatsPalette palette, TrendDay day, int maxValue, int todayValue, bool isLast, Color? gameColor)
    {
        var barPercent = maxValue > 0 ? day.Minutes * 100.0 / maxValue : 0;
        var deltaText = UiKit.L("Trend_Flat", "持平");
        var deltaColor = palette.TextMuted;
        if (isLast)
        {
            deltaText = UiKit.L("Trend_Today", "当天");
        }
        else
        {
            var diff = day.Minutes - todayValue;
            if (diff > 0)
            {
                deltaText = "+" + UiKit.FormatTimeShort(diff / 60.0);
                deltaColor = palette.Success;
            }
            else if (diff < 0)
            {
                deltaText = UiKit.FormatTimeShort(diff / 60.0);
                deltaColor = palette.Danger;
            }
        }

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        datePanel.Children.Add(UiKit.Text(UiKit.FormatMD(day.Date), palette.TextSecondary, 12.5));
        datePanel.Children.Add(UiKit.Text(UiKit.WeekDayName(day.Date.DayOfWeek), palette.TextMuted, 10.5));
        row.Children.Add(datePanel);

        var barTrack = new Border
        {
            Background = palette.BgSecondaryBrush,
            CornerRadius = new CornerRadius(3),
            Height = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
        };
        // 填充宽度用百分比星列实现，随轨道宽度伸缩（旧实现把 0-100 的百分数当像素值用了）
        var fillPercent = Math.Clamp(barPercent, 0, 100);
        var fillHost = new Grid();
        fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fillPercent, GridUnitType.Star) });
        fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - fillPercent, GridUnitType.Star) });
        fillHost.Children.Add(new Border
        {
            Background = isLast
                ? new SolidColorBrush(gameColor ?? palette.Accent)
                : palette.AccentBrush,
            CornerRadius = new CornerRadius(3),
            Height = 6,
        });
        barTrack.Child = fillHost;
        row.Children.Add(barTrack);
        Grid.SetColumn(barTrack, 1);

        var timeText = UiKit.Text(UiKit.FormatTimeShort(day.Hours), palette.TextPrimary, 12.5,
            FontWeights.Medium, textAlignment: TextAlignment.Right);
        row.Children.Add(timeText);
        Grid.SetColumn(timeText, 2);

        var deltaTextBlock = UiKit.Text(deltaText, deltaColor, 10.5, textAlignment: TextAlignment.Right);
        row.Children.Add(deltaTextBlock);
        Grid.SetColumn(deltaTextBlock, 3);

        var container = new Border
        {
            Padding = new Thickness(0, 8, 0, 8),
            CornerRadius = new CornerRadius(4),
            Background = isLast ? palette.AccentAlphaBrush(0x12) : new SolidColorBrush(Colors.Transparent),
        };
        if (!isLast) UiKit.AttachHover(container, new SolidColorBrush(Colors.Transparent), palette.HoverBrush);
        container.Child = row;
        return container;
    }

    /// <summary>侧栏卡片外壳（与图表卡片等高）</summary>
    private static FrameworkElement WrapSideCard(StatsPalette palette, UIElement content)
        => UiKit.Card(palette, content, new Thickness(18, 16, 18, 16));

    #endregion
}
