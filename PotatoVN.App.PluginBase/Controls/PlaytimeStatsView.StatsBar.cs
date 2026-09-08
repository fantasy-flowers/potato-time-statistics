using System;
using System.Collections.Generic;
using System.Linq;
using GalgameManager.Models;
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
/// 概览指标条：总时长 / 游戏数 / 最常玩游戏 / 平均时长四张卡片，及按日/周/月的聚合统计。
/// </summary>
public sealed partial class PlaytimeStatsView
{
    #region 概览指标条

    private FrameworkElement BuildStatsBar(StatsPalette palette)
    {
        var stats = ComputeStats();
        var grid = UiKit.EqualColumns(new FrameworkElement[]
        {
            BuildStatCard(palette, UiKit.L("Stat_TotalTime", "总游玩时长"),
                UiKit.FormatHours(stats.TotalHours), UiKit.L("Unit_Hours", "小时"), stats.TotalSub, fontSize: 26),
            BuildStatCard(palette, UiKit.L("Stat_GameCount", "游玩游戏数"),
                stats.GameCount.ToString(), UiKit.L("Unit_Games", "款"), stats.CountSub, fontSize: 26),
            BuildStatCard(palette, UiKit.L("Stat_AvgTime", "平均时长"),
                UiKit.FormatHours(stats.AvgHours), UiKit.L("Unit_Hours", "小时"), stats.AvgSub, fontSize: 26),
        });
        grid.Margin = new Thickness(0, 0, 0, 20);

        // 整行英雄卡（最常玩游戏）置于其余概览卡上方，随日/周/月维度切换刷新
        var hero = BuildTopGameHero(palette, stats);
        hero.Margin = new Thickness(0, 20, 0, 20);

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(hero);
        root.Children.Add(grid);
        Grid.SetRow(grid, 1);
        return root;
    }

    /// <summary>「最常玩游戏」整行英雄卡：左竖版封面 + 右信息（该时段时长/占比/统计周期）；无记录时显示空态</summary>
    private FrameworkElement BuildTopGameHero(StatsPalette palette, StatsInfo stats)
    {
        if (stats.Top is null)
        {
            return UiKit.HeroCard(palette,
                UiKit.GameCover(null, string.Empty, Guid.Empty),
                UiKit.L("Stat_TopGame", "最常玩游戏"), string.Empty, Array.Empty<(string, string)>(),
                UiKit.L("Stats_NoTop", "暂无游玩记录"));
        }

        var rows = new List<(string, string)>
        {
            (UiKit.L("Chart_Time", "游玩时长"), UiKit.FormatTime(stats.Top.Hours)),
            (UiKit.L("Stat_Percent", "占比"), Percent(stats.Top.Minutes, stats.TotalMinutes) + "%"),
            (UiKit.L("Stat_Period", "统计周期"), stats.PeriodLabel),
        };
        return UiKit.HeroCard(palette,
            UiKit.GameCover(stats.Top.ImagePath, stats.Top.Name, stats.Top.Id),
            UiKit.L("Stat_TopGame", "最常玩游戏"), stats.Top.Name, rows);
    }

    private static FrameworkElement BuildStatCard(StatsPalette palette, string label, string value, string? unit,
        string sub, double fontSize, string? tooltip = null)
    {
        var valueText = new TextBlock
        {
            Text = value,
            FontSize = fontSize,
            FontWeight = FontWeights.Bold,
            Foreground = palette.AccentBrush,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        if (tooltip is not null) ToolTipService.SetToolTip(valueText, tooltip);

        var valueRow = new StackPanel { Orientation = Orientation.Horizontal };
        valueRow.Children.Add(valueText);
        if (unit is not null)
        {
            valueRow.Children.Add(new TextBlock
            {
                Text = unit,
                FontSize = 13,
                Foreground = palette.TextSecondaryBrush,
                Margin = new Thickness(4, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Bottom,
            });
        }

        var content = new StackPanel { Orientation = Orientation.Vertical };
        content.Children.Add(UiKit.Text(label, palette.TextSecondary, 12));
        content.Children.Add(valueRow);
        content.Children.Add(UiKit.Text(sub, palette.TextMuted, 11, margin: new Thickness(0, 6, 0, 0)));
        return UiKit.Card(palette, content, new Thickness(16, 18, 16, 18));
    }

    private sealed class StatsInfo
    {
        public double TotalHours { get; init; }
        public int TotalMinutes { get; init; }
        public int GameCount { get; init; }
        /// <summary>该时段最常玩游戏（含封面路径），无记录为 null</summary>
        public GamePeriodTime? Top { get; init; }
        /// <summary>当前统计周期文本（如 8/10-8/16），英雄卡展示用</summary>
        public string PeriodLabel { get; init; } = "";
        public double AvgHours { get; init; }
        public string TotalSub { get; init; } = "";
        public string CountSub { get; init; } = "";
        public string AvgSub { get; init; } = "";
    }

    private StatsInfo ComputeStats()
    {
        switch (_period)
        {
            case StatsPeriod.Day:
            {
                var todayGames = StatsService.GetDayGames(_snapshot, _selectedDate);
                var totalMinutes = todayGames.Sum(g => g.Minutes);
                var top = todayGames.FirstOrDefault();
                var recent7 = StatsService.GetRecentDays(_selectedDate);
                var avg7 = recent7.Average(d => StatsService.GetDayTotal(_snapshot, d) / 60.0);
                return new StatsInfo
                {
                    TotalHours = totalMinutes / 60.0,
                    TotalMinutes = totalMinutes,
                    GameCount = todayGames.Count,
                    Top = top,
                    PeriodLabel = UiKit.FormatYMD(_selectedDate),
                    AvgHours = avg7,
                    TotalSub = $"{UiKit.FormatYMD(_selectedDate)} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{UiKit.FormatMD(_selectedDate)} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    AvgSub = UiKit.L("Sub_Avg7Days", "近7日日均时长"),
                };
            }
            case StatsPeriod.Week:
            {
                var days = StatsService.GetWeekDays(_selectedDate);
                var gameTotals = StatsService.GetPeriodGames(_snapshot, days);
                var totalMinutes = days.Sum(d => StatsService.GetDayTotal(_snapshot, d));
                var top = gameTotals.FirstOrDefault();
                var periodLabel = UiKit.FormatWeekRange(StatsService.GetMonday(_selectedDate));
                return new StatsInfo
                {
                    TotalHours = totalMinutes / 60.0,
                    TotalMinutes = totalMinutes,
                    GameCount = gameTotals.Count,
                    Top = top,
                    PeriodLabel = periodLabel,
                    AvgHours = totalMinutes / 60.0 / 7,
                    TotalSub = $"{periodLabel} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{periodLabel} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    AvgSub = UiKit.L("Sub_AvgPerDay", "每日平均时长"),
                };
            }
            default:
            {
                var weeks = StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1);
                var totalMinutes = weeks.Sum(w => w.TotalMinutes);
                var merged = new Dictionary<Guid, int>();
                foreach (var week in weeks)
                {
                    foreach (var (id, minutes) in week.GameMinutes)
                    {
                        if (!merged.TryAdd(id, minutes)) merged[id] += minutes;
                    }
                }

                var topEntry = merged.OrderByDescending(kv => kv.Value).FirstOrDefault();
                var topGame = _snapshot.Games.FirstOrDefault(g => g.Uuid == topEntry.Key);
                var periodLabel = UiKit.FormatYM(_selectedYear, _selectedMonth + 1);

                GamePeriodTime? top = null;
                if (topGame is not null)
                {
                    var imagePath = topGame.ImagePath.Value;
                    if (string.IsNullOrEmpty(imagePath) || imagePath == Galgame.DefaultImagePath) imagePath = null;
                    top = new GamePeriodTime
                    {
                        Id = topGame.Uuid,
                        Name = topGame.Name.Value ?? string.Empty,
                        ImagePath = imagePath,
                        Minutes = topEntry.Value,
                    };
                }

                return new StatsInfo
                {
                    TotalHours = totalMinutes / 60.0,
                    TotalMinutes = totalMinutes,
                    GameCount = merged.Count,
                    Top = top,
                    PeriodLabel = periodLabel,
                    AvgHours = weeks.Count > 0 ? totalMinutes / 60.0 / weeks.Count : 0,
                    TotalSub = $"{periodLabel} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{periodLabel} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    AvgSub = UiKit.L("Sub_AvgPerWeek", "每周平均时长"),
                };
            }
        }
    }

    private static string Percent(int part, int total)
        => total > 0 ? (part * 100.0 / total).ToString("F1") : "0.0";

    #endregion
}
