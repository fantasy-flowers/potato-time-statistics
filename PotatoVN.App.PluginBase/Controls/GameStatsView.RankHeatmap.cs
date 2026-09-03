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

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(header);
        content.Children.Add(list);
        Grid.SetRow(list, 1);
        list.Margin = new Thickness(0, 12, 0, 0);

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
        var fillColor = index == 0
            ? Color.FromArgb(0xFF, 0xb8, 0x86, 0x0b)
            : palette.AccentDark;
        var trackGrid = new Grid();
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - percent, GridUnitType.Star) });
        trackGrid.Children.Add(new Border
        {
            Background = UiKit.VerticalGradient(fillColor, palette.AccentBright),
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
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(UiKit.Text(
            UiKit.Lf("Fmt_YearHeatTitle", "{0} 年游玩强度热力图", _year), palette.TextPrimary, 15, FontWeights.SemiBold));
        var heatHint = UiKit.Text(
            UiKit.L("Stats_HeatHint", "GitHub 贡献图风格 · 颜色越深当日游玩越久"), palette.TextMuted, 11);
        header.Children.Add(heatHint);
        Grid.SetColumn(heatHint, 1);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(header);

        var heat = BuildHeatmap(palette, _year);
        content.Children.Add(heat);
        Grid.SetRow(heat, 1);
        heat.Margin = new Thickness(0, 12, 0, 0);

        var heatLegend = BuildHeatLegend(palette);
        content.Children.Add(heatLegend);
        Grid.SetRow(heatLegend, 2);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    private FrameworkElement BuildHeatmap(StatsPalette palette, int year)
    {
        const int cellSize = 13;
        const int cellGap = 3;
        var daily = StatsService.GetYearDaily(_snapshot, year);

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

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(dayLabels);
        root.Children.Add(scroller);
        Grid.SetColumn(scroller, 1);
        return root;
    }

    private FrameworkElement BuildHeatLegend(StatsPalette palette)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 12, 0, 0) };
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
        return panel;
    }

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
