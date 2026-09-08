using System;
using System.Collections.Generic;
using System.Linq;
using GalgameManager.Enums;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PotatoVN.App.PluginBase.Models;
using PotatoVN.App.PluginBase.Services;
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// 排行 + 热力图：总时长排行 TOP 10 与年度游玩强度热力图（GitHub 贡献图风格）。
/// </summary>
public sealed partial class GameStatsView
{
    #region 排行 + 热力图

    private FrameworkElement BuildMainGrid(StatsPalette palette)
    {
        // 两卡同高：行高由较高一方决定（通常为排行卡），热力图卡默认 Stretch 填满；
        // 热力图卡内部把余量分给热力图行（Star），图例贴底。
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

        root.Children.Add(BuildRankCard(palette));
        var heatCard = BuildHeatmapCard(palette);
        root.Children.Add(heatCard);
        Grid.SetColumn(heatCard, 2);
        return root;
    }

    private FrameworkElement BuildRankCard(StatsPalette palette)
    {
        var topGames = StatsService.GetTopGamesByTotal(_snapshot.Games, 10);
        var maxMinutes = topGames.Count > 0 ? topGames[0].Minutes : 1;

        var list = new StackPanel();
        if (topGames.Count == 0)
        {
            list.Children.Add(UiKit.EmptyState(UiKit.L("Stats_RankEmpty", "暂无游玩时长记录"), palette.TextMuted));
        }
        else
        {
            for (var i = 0; i < topGames.Count; i++)
            {
                list.Children.Add(BuildRankItem(palette, topGames[i], i, maxMinutes));
            }
        }

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(UiKit.Text(UiKit.L("Stats_RankTitle", "总时长排行 TOP 10"), palette.TextPrimary, 15, FontWeights.SemiBold));
        var rankHint = UiKit.Text(UiKit.L("Stats_RankHint", "单位：小时"), palette.TextMuted, 11);
        header.Children.Add(rankHint);
        Grid.SetColumn(rankHint, 1);

        var listScroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(header);
        content.Children.Add(listScroll);
        Grid.SetRow(listScroll, 1);
        listScroll.Margin = new Thickness(0, 12, 0, 0);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    private FrameworkElement BuildRankItem(StatsPalette palette, GamePeriodTime game, int index, int maxMinutes)
    {
        var percent = maxMinutes > 0 ? Math.Min(100, game.Minutes * 100.0 / maxMinutes) : 0;

        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

        row.Children.Add(UiKit.Text((index + 1).ToString(), index == 0 ? palette.AccentBright : palette.TextMuted, 12,
            index == 0 ? FontWeights.Bold : FontWeights.Normal, textAlignment: TextAlignment.Center));
        var nameText = UiKit.Text(game.Name, palette.TextPrimary, 12.5, trimming: TextTrimming.CharacterEllipsis);
        row.Children.Add(nameText);
        Grid.SetColumn(nameText, 1);

        var track = new Border
        {
            Background = palette.BgSecondaryBrush,
            CornerRadius = new CornerRadius(4),
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var trackGrid = new Grid();
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - percent, GridUnitType.Star) });
        // 彩色时间条：按排名取模块系列色（TOP 10 互不相同），第 1 名保留金色；纵向渐变 暗→亮
        var barColor = index == 0 ? Color.FromArgb(0xFF, 0xb8, 0x86, 0x0b) : StatsTheme.SeriesColor(index);
        trackGrid.Children.Add(new Border
        {
            Background = UiKit.VerticalGradient(Darken(barColor, 0.55), barColor),
            CornerRadius = new CornerRadius(4),
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        });
        track.Child = trackGrid;
        row.Children.Add(track);
        Grid.SetColumn(track, 2);

        var timeText = UiKit.Text(UiKit.FormatHoursSmart(game.Hours) + "h", palette.TextSecondary, 12,
            textAlignment: TextAlignment.Right);
        row.Children.Add(timeText);
        Grid.SetColumn(timeText, 3);

        return row;
    }

    private FrameworkElement BuildHeatmapCard(StatsPalette palette)
    {
        // 年度数据只算一次，年度摘要与热力图共用
        var daily = StatsService.GetYearDaily(_snapshot, _year);

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(UiKit.Text(
            UiKit.Lf("Fmt_YearHeatTitle", "{0} 年游玩强度热力图", _year), palette.TextPrimary, 15, FontWeights.SemiBold));
        // 年份选择器放在卡 header 右侧（原 hint 位置；hint 移到图例行右侧）
        var yearNav = BuildYearNav(palette);
        header.Children.Add(yearNav);
        Grid.SetColumn(yearNav, 1);

        // 行序固定：header(Auto) / 年度摘要(Auto) / 热力图(Star) / 图例(Auto)
        // 热力图行用 Star 吃掉卡片余量（两卡同高时由排行卡撑起的差值），图例贴底
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(header);

        var summary = BuildYearSummary(palette, daily);
        content.Children.Add(summary);
        Grid.SetRow(summary, 1);
        summary.Margin = new Thickness(0, 12, 0, 0);

        var heat = BuildHeatmap(palette, _year, daily);
        content.Children.Add(heat);
        Grid.SetRow(heat, 2);
        heat.Margin = new Thickness(0, 12, 0, 0);

        var heatLegend = BuildHeatLegend(palette);
        content.Children.Add(heatLegend);
        Grid.SetRow(heatLegend, 3);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    /// <summary>
    /// 年度摘要：总游玩天数 / 最热的一天 / 连续游玩，三张并排等宽小卡
    /// （样式对齐「近7日趋势」摘要项：label 小字 + 数值大字）。
    /// </summary>
    private FrameworkElement BuildYearSummary(StatsPalette palette, Dictionary<DateTime, int> daily)
    {
        var activeDays = daily.Where(kv => kv.Value > 0).Select(kv => kv.Key.Date).Distinct()
            .OrderBy(d => d).ToList();

        // 最热的一天
        var maxText = "—";
        if (activeDays.Count > 0)
        {
            var maxDay = activeDays.Select(d => (Date: d, Minutes: daily[d])).OrderByDescending(x => x.Minutes).First();
            maxText = $"{UiKit.FormatMD(maxDay.Date)} · {UiKit.FormatHours(maxDay.Minutes / 60.0)}h";
        }

        // 最长连续游玩天数
        var streak = 0;
        var current = 0;
        DateTime? previous = null;
        foreach (var day in activeDays)
        {
            current = previous.HasValue && (day - previous.Value).Days == 1 ? current + 1 : 1;
            streak = Math.Max(streak, current);
            previous = day;
        }

        var unit = UiKit.L("Unit_Days", "天");
        var grid = UiKit.EqualColumns(new FrameworkElement[]
        {
            BuildYearSummaryCard(palette, UiKit.L("Heat_SumDays", "总游玩天数"), $"{activeDays.Count} {unit}"),
            BuildYearSummaryCard(palette, UiKit.L("Heat_MaxDay", "最热的一天"), maxText),
            BuildYearSummaryCard(palette, UiKit.L("Heat_Streak", "连续游玩"), $"{streak} {unit}"),
        }, columnSpacing: 12);
        return grid;
    }

    private static FrameworkElement BuildYearSummaryCard(StatsPalette palette, string label, string value)
    {
        var panel = new StackPanel();
        panel.Children.Add(UiKit.Text(label, palette.TextMuted, 10.5));
        panel.Children.Add(UiKit.Text(value, palette.TextPrimary, 14.5, FontWeights.SemiBold,
            margin: new Thickness(0, 2, 0, 0), trimming: TextTrimming.CharacterEllipsis));
        return new Border
        {
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            Child = panel,
        };
    }

    private FrameworkElement BuildHeatmap(StatsPalette palette, int year, Dictionary<DateTime, int> daily)
    {
        const int cellSize = 13;
        const int cellGap = 3;

        var jan1 = new DateTime(year, 1, 1);
        var start = StatsService.GetMonday(jan1);
        var end = new DateTime(year, 12, 31);
        var lastSunday = end.AddDays((7 - (int)end.DayOfWeek) % 7);
        var totalDays = (lastSunday - start).Days + 1;

        var monthsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = cellGap };
        var columnsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = cellGap };

        var previousMonth = -1;
        for (var dayIndex = 0; dayIndex < totalDays; dayIndex += 7)
        {
            var weekStart = start.AddDays(dayIndex);
            var month = weekStart.Month;
            var monthLabel = UiKit.Text(month != previousMonth ? UiKit.MonthName(month) : "",
                palette.TextMuted, 9);
            monthLabel.Width = cellSize;
            monthLabel.TextTrimming = TextTrimming.CharacterEllipsis;
            monthsRow.Children.Add(monthLabel);
            previousMonth = month;

            var column = new StackPanel { Spacing = cellGap };
            for (var row = 0; row < 7; row++)
            {
                var date = weekStart.AddDays(row);
                var inYear = date.Year == year;
                var minutes = daily.TryGetValue(date, out var v) ? v : 0;
                var level = StatsService.HeatLevel(minutes);

                var cell = new Border
                {
                    Width = cellSize,
                    Height = cellSize,
                    CornerRadius = new CornerRadius(3),
                    Background = palette.Brush(palette.HeatLevels[level]),
                    Opacity = inYear ? 1 : 0.25,
                };
                ToolTipService.SetToolTip(cell,
                    UiKit.FormatDateTooltip(date) + " · " +
                    (minutes > 0 ? UiKit.FormatMinutes(minutes) : UiKit.L("Heat_NoPlay", "未游玩")));
                column.Children.Add(cell);
            }

            columnsRow.Children.Add(column);
        }

        var heatRoot = new StackPanel();
        heatRoot.Children.Add(monthsRow);
        heatRoot.Children.Add(columnsRow);

        var scroller = new ScrollViewer
        {
            Content = heatRoot,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollMode = ScrollMode.Auto,
        };

        // 行标签（一/三/五）
        var dayLabels = new StackPanel { Margin = new Thickness(0, 17, 8, 0) };
        for (var i = 0; i < 7; i++)
        {
            var label = i is 0 or 2 or 4 ? UiKit.WeekDayName((DayOfWeek)((i + 1) % 7)) : "";
            dayLabels.Children.Add(UiKit.Text(label, palette.TextMuted, 9.5,
                maxWidth: cellSize, margin: new Thickness(0, 0, 0, cellGap)));
        }

        // 防御性居中：所在行现为 Auto（紧贴内容），若日后改回 Star 行则余量上下平分
        var root = new Grid { VerticalAlignment = VerticalAlignment.Center };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(dayLabels);
        root.Children.Add(scroller);
        Grid.SetColumn(scroller, 1);
        return root;
    }

    private FrameworkElement BuildHeatLegend(StatsPalette palette)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(UiKit.Text(UiKit.L("Heat_Less", "少"), palette.TextMuted, 11));
        for (var level = 0; level < palette.HeatLevels.Count; level++)
        {
            var cell = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(3),
                Background = palette.Brush(palette.HeatLevels[level]),
            };
            ToolTipService.SetToolTip(cell, HeatLevelName(level));
            panel.Children.Add(cell);
        }

        panel.Children.Add(UiKit.Text(UiKit.L("Heat_More", "多"), palette.TextMuted, 11));
        panel.Children.Add(UiKit.Text(UiKit.L("Heat_Tip", "每日总游玩时长"), palette.TextMuted, 11,
            margin: new Thickness(6, 0, 0, 0)));

        // 原卡 header 右侧的 hint 移到图例行右侧（对齐原型"每日总游玩时长"位置）
        var hint = UiKit.Text(
            UiKit.L("Stats_HeatHint", "GitHub 贡献图风格 · 颜色越深当日游玩越久"), palette.TextMuted, 11);
        hint.VerticalAlignment = VerticalAlignment.Center;

        var root = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(panel);
        root.Children.Add(hint);
        Grid.SetColumn(hint, 1);
        return root;
    }

    /// <summary>按比例缩小 RGB（a>1 为增亮），用于彩色条的渐变暗端</summary>
    private static Color Darken(Color c, double f)
        => Color.FromArgb(c.A,
            (byte)Math.Clamp(c.R * f, 0, 255),
            (byte)Math.Clamp(c.G * f, 0, 255),
            (byte)Math.Clamp(c.B * f, 0, 255));

    private static string HeatLevelName(int level)
        => level switch
        {
            0 => UiKit.L("Heat_L0", "0 分钟"),
            1 => UiKit.L("Heat_L1", "1–59 分钟"),
            2 => UiKit.L("Heat_L2", "1–3 小时"),
            3 => UiKit.L("Heat_L3", "3–6 小时"),
            _ => UiKit.L("Heat_L4", "≥6 小时"),
        };

    #endregion
}
