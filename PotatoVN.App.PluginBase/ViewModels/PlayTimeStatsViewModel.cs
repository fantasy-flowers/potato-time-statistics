using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalgameManager.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;
using Windows.UI;

namespace PotatoVN.App.PluginBase.ViewModels;

public partial class PlayTimeStatsViewModel : ObservableObject
{
    private const int DayViewCount = 7;      // last 7 days
    private const int WeekViewCount = 4;     // last 4 weeks
    private const int MonthViewCount = 6;    // last 6 months

    // Accent colour palette for bars — high-contrast on dark theme
    private static readonly Color[] Palette =
    {
        Color.FromArgb(255, 0, 120, 212),    // blue
        Color.FromArgb(255, 0, 153, 136),    // teal
        Color.FromArgb(255, 118, 185, 0),    // green
        Color.FromArgb(255, 255, 140, 0),    // orange
        Color.FromArgb(255, 239, 68, 111),   // pink
        Color.FromArgb(255, 142, 124, 195),  // purple
    };

    [ObservableProperty] private TimePeriod _selectedPeriod = TimePeriod.Week;
    [ObservableProperty] private string _totalDisplayTime = string.Empty;
    [ObservableProperty] private string _periodDescription = string.Empty;
    [ObservableProperty] private string _filterHint = string.Empty;
    [ObservableProperty] private bool _hasData;

    public ObservableCollection<BarChartItem> BarItems { get; } = new();
    public ObservableCollection<GameRankItem> RankItems { get; } = new();

    private List<Galgame> _allGames = new();
    private BarChartItem? _selectedBar;

    public void LoadData()
    {
        try
        {
            _allGames = Plugin.HostApi.GetAllGames();
            Recompute();
        }
        catch (Exception ex)
        {
            Plugin.HostApi.DeveloperEvent(InfoBarSeverity.Warning, "Failed to load play time data", ex);
        }
    }

    partial void OnSelectedPeriodChanged(TimePeriod value) => Recompute();

    [RelayCommand]
    private void SwitchPeriod(TimePeriod period) => SelectedPeriod = period;

    [RelayCommand]
    private void ClearFilter()
    {
        _selectedBar = null;
        FilterHint = string.Empty;
        Recompute();
    }

    public void SelectBar(BarChartItem bar)
    {
        _selectedBar = bar;
        FilterHint = string.Format(CultureInfo.CurrentCulture,
            "FilterBarHint".GetLoc("Filtered: {0}"), bar.Label);
        RebuildRankItems(bar.GameTimes);
    }

    private void Recompute()
    {
        BarItems.Clear();
        RankItems.Clear();
        _selectedBar = null;
        FilterHint = string.Empty;

        if (_allGames.Count == 0)
        {
            HasData = false;
            TotalDisplayTime = FormatMinutes(0);
            PeriodDescription = string.Empty;
            return;
        }

        var bars = SelectedPeriod switch
        {
            TimePeriod.Day => BuildDayBars(),
            TimePeriod.Week => BuildWeekBars(),
            TimePeriod.Month => BuildMonthBars(),
            _ => BuildWeekBars()
        };

        var totalMinutes = bars.Sum(b => b.TotalMinutes);
        TotalDisplayTime = FormatMinutes(totalMinutes);
        HasData = totalMinutes > 0;

        PeriodDescription = SelectedPeriod switch
        {
            TimePeriod.Day => "PeriodDay".GetLoc("Last 7 days"),
            TimePeriod.Week => "PeriodWeek".GetLoc("Last 4 weeks"),
            TimePeriod.Month => "PeriodMonth".GetLoc("Last 6 months"),
            _ => string.Empty
        };

        var maxMinutes = bars.Max(b => b.TotalMinutes);
        if (maxMinutes <= 0) maxMinutes = 1;

        foreach (var bar in bars)
        {
            bar.WidthRatio = (double)bar.TotalMinutes / maxMinutes;
            bar.DisplayTime = FormatMinutes(bar.TotalMinutes);
            SetGridLengths(bar, bar.WidthRatio);
            BarItems.Add(bar);
        }

        // Default: show all games ranked by total play time within the full period
        var allGameTimes = bars.SelectMany(b => b.GameTimes)
            .GroupBy(g => g.Game, GalgameComparer.Instance)
            .Select(g => new GameSegmentTime
            {
                Game = g.Key,
                Minutes = g.Sum(x => x.Minutes),
                DisplayTime = FormatMinutes(g.Sum(x => x.Minutes))
            })
            .OrderByDescending(g => g.Minutes)
            .ToList();

        RebuildRankItems(allGameTimes);
    }

    private List<BarChartItem> BuildDayBars()
    {
        var today = DateTime.Today;
        var bars = new List<BarChartItem>();
        for (var i = DayViewCount - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            bars.Add(CreateBar(date, date, date.ToString("M/d", CultureInfo.CurrentCulture),
                date.ToString("dddd", CultureInfo.CurrentCulture)));
        }
        PopulateBars(bars);
        return bars;
    }

    private List<BarChartItem> BuildWeekBars()
    {
        var today = DateTime.Today;
        var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-7);

        var bars = new List<BarChartItem>();
        for (var i = WeekViewCount - 1; i >= 0; i--)
        {
            var start = monday.AddDays(-7 * i);
            var end = start.AddDays(6);
            var weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                start, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            bars.Add(CreateBar(start, end,
                $"W{weekNum}", $"{start:M/d} - {end:M/d}"));
        }
        PopulateBars(bars);
        return bars;
    }

    private List<BarChartItem> BuildMonthBars()
    {
        var now = DateTime.Today;
        var bars = new List<BarChartItem>();
        for (var i = MonthViewCount - 1; i >= 0; i--)
        {
            var first = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var last = first.AddMonths(1).AddDays(-1);
            bars.Add(CreateBar(first, last,
                first.ToString("yyyy/MM", CultureInfo.CurrentCulture),
                first.ToString("MMMM", CultureInfo.CurrentCulture)));
        }
        PopulateBars(bars);
        return bars;
    }

    private static BarChartItem CreateBar(DateTime start, DateTime end, string label, string subLabel)
    {
        var idx = Math.Abs(start.DayOfYear) % Palette.Length;
        return new BarChartItem
        {
            StartDate = start,
            EndDate = end,
            Label = label,
            SubLabel = subLabel,
            BarBrush = new SolidColorBrush(Palette[idx])
        };
    }

    private void PopulateBars(List<BarChartItem> bars)
    {
        foreach (var game in _allGames)
        {
            if (game.PlayedTime == null || game.PlayedTime.Count == 0) continue;
            foreach (var (dateStr, minutes) in game.PlayedTime)
            {
                if (minutes <= 0) continue;
                if (!TryParseDate(dateStr, out var date)) continue;
                foreach (var bar in bars)
                {
                    if (date >= bar.StartDate && date <= bar.EndDate)
                    {
                        bar.TotalMinutes += minutes;
                        bar.GameTimes.Add(new GameSegmentTime
                        {
                            Game = game,
                            Minutes = minutes,
                            DisplayTime = FormatMinutes(minutes)
                        });
                        break;
                    }
                }
            }
        }
    }

    private void RebuildRankItems(List<GameSegmentTime> gameTimes)
    {
        RankItems.Clear();
        if (gameTimes.Count == 0) return;

        var total = gameTimes.Sum(g => g.Minutes);
        if (total <= 0) total = 1;
        var maxMinutes = gameTimes.Max(g => g.Minutes);
        if (maxMinutes <= 0) maxMinutes = 1;

        var sorted = gameTimes.OrderByDescending(g => g.Minutes).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var gt = sorted[i];
            var ratio = (double)gt.Minutes / maxMinutes;
            var item = new GameRankItem
            {
                Game = gt.Game,
                Name = gt.Game.Name.Value ?? gt.Game.CnName ?? "Unknown",
                ImagePath = gt.Game.ImagePath.Value ?? Galgame.DefaultImagePath,
                Minutes = gt.Minutes,
                DisplayTime = FormatMinutes(gt.Minutes),
                Percentage = Math.Round((double)gt.Minutes / total * 100, 1),
                WidthRatio = ratio,
                BarBrush = new SolidColorBrush(Palette[i % Palette.Length])
            };
            SetGridLengths(item, ratio);
            RankItems.Add(item);
        }
    }

    private static void SetGridLengths(BarChartItem item, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        item.FillWidth = new GridLength(ratio * 100, GridUnitType.Star);
        item.EmptyWidth = new GridLength((1 - ratio) * 100, GridUnitType.Star);
    }

    private static void SetGridLengths(GameRankItem item, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        item.FillWidth = new GridLength(ratio * 100, GridUnitType.Star);
        item.EmptyWidth = new GridLength((1 - ratio) * 100, GridUnitType.Star);
    }

    private static readonly string[] DateFormats = { "yyyy/M/d", "yyyy/MM/dd", "yyyy-M-d", "yyyy-MM-dd" };

    private static bool TryParseDate(string dateStr, out DateTime date)
    {
        return DateTime.TryParseExact(dateStr, DateFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);
    }

    public static string FormatMinutes(int minutes)
    {
        if (minutes <= 0) return "0m";
        var h = minutes / 60;
        var m = minutes % 60;
        if (h > 0 && m > 0) return $"{h}h {m}m";
        if (h > 0) return $"{h}h";
        return $"{m}m";
    }

    private sealed class GalgameComparer : IEqualityComparer<Galgame>
    {
        public static readonly GalgameComparer Instance = new();
        public bool Equals(Galgame? x, Galgame? y) => ReferenceEquals(x, y);
        public int GetHashCode(Galgame obj) => obj.Uuid.GetHashCode();
    }
}