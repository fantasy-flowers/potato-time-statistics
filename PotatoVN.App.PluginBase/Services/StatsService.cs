using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GalgameManager.Enums;
using GalgameManager.Models;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase.Services;

/// <summary>
/// 宿主游戏库快照 + 解析后的游玩数据（单位均为分钟）
/// </summary>
public sealed class StatsSnapshot
{
    public List<Galgame> Games { get; init; } = new();

    /// <summary>每个游戏：日期 → 分钟</summary>
    public Dictionary<Guid, Dictionary<DateTime, int>> PerGameDaily { get; init; } = new();

    /// <summary>全库：日期 → 总分钟</summary>
    public Dictionary<DateTime, int> DailyTotals { get; init; } = new();

    public DateTime MinDate { get; init; } = DateTime.MaxValue;
    public DateTime MaxDate { get; init; } = DateTime.MinValue;

    public bool HasData => DailyTotals.Count > 0;

    /// <summary>有游玩记录的最早年份；无数据时返回当前年</summary>
    public int MinYear => MinDate == DateTime.MaxValue ? DateTime.Today.Year : MinDate.Year;

    /// <summary>有游玩记录的最晚年份（不早于当前年）</summary>
    public int MaxYear => Math.Max(DateTime.Today.Year, MaxDate == DateTime.MinValue ? DateTime.Today.Year : MaxDate.Year);
}

/// <summary>
/// 统计计算服务：从宿主 GetAllGames() 快照聚合游玩时长数据。
/// Galgame.PlayedTime 的键为 "yyyy/M/d" 等 ShortDateString 格式，值为分钟。
/// </summary>
public static class StatsService
{
    private static readonly string[] DateFormats = { "yyyy/M/d", "yyyy/MM/dd", "yyyy-M-d", "yyyy-MM-dd" };

    #region 快照构建

    /// <summary>从宿主获取游戏列表并解析每日游玩数据</summary>
    public static StatsSnapshot BuildSnapshot()
    {
        var games = Plugin.HostApi.GetAllGames() ?? new List<Galgame>();
        var perGameDaily = new Dictionary<Guid, Dictionary<DateTime, int>>();
        var dailyTotals = new Dictionary<DateTime, int>();
        var minDate = DateTime.MaxValue;
        var maxDate = DateTime.MinValue;

        foreach (var game in games)
        {
            if (game.PlayedTime is not { Count: > 0 }) continue;
            var daily = new Dictionary<DateTime, int>();
            foreach (var (key, value) in game.PlayedTime)
            {
                if (value <= 0) continue;
                var date = ParseDate(key);
                if (date == DateTime.MinValue) continue;
                if (!daily.TryAdd(date, value)) daily[date] += value;
                if (date < minDate) minDate = date;
                if (date > maxDate) maxDate = date;
            }

            if (daily.Count == 0) continue;
            perGameDaily[game.Uuid] = daily;
            foreach (var (date, value) in daily)
            {
                if (!dailyTotals.TryAdd(date, value)) dailyTotals[date] += value;
            }
        }

        return new StatsSnapshot
        {
            Games = games,
            PerGameDaily = perGameDaily,
            DailyTotals = dailyTotals,
            MinDate = minDate,
            MaxDate = maxDate,
        };
    }

    private static DateTime ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
        if (DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
        return DateTime.MinValue;
    }

    #endregion

    #region 通用日期工具

    /// <summary>返回 date 所在周的周一</summary>
    public static DateTime GetMonday(DateTime date)
    {
        var d = date.Date;
        var diff = ((int)d.DayOfWeek + 6) % 7; // 周一=0
        return d.AddDays(-diff);
    }

    /// <summary>以 selectedDate 为终点的近 N 天（升序）。注意 Enumerable.Range(start,count) 是递增序列，不能直接当倒序偏移用</summary>
    public static List<DateTime> GetRecentDays(DateTime selectedDate, int count = 7)
        => Enumerable.Range(0, count).Select(i => selectedDate.Date.AddDays(i - (count - 1))).ToList();

    /// <summary>选中日期所在周的 7 天（周一开头）</summary>
    public static List<DateTime> GetWeekDays(DateTime selectedDate)
        => Enumerable.Range(0, 7).Select(i => GetMonday(selectedDate).AddDays(i)).ToList();

    /// <summary>某年某月的各个自然周（从该月 1 号所在周的周一开始，只统计月内天数）</summary>
    public static List<MonthWeekInfo> GetMonthWeeks(StatsSnapshot snapshot, int year, int month)
    {
        var firstDay = new DateTime(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var monday = GetMonday(firstDay);
        var weeks = new List<MonthWeekInfo>();
        var weekNum = 1;

        while (monday <= lastDay)
        {
            var gameMinutes = new Dictionary<Guid, int>();
            var total = 0;
            for (var i = 0; i < 7; i++)
            {
                var d = monday.AddDays(i);
                if (d.Month != month || d.Year != year) continue;
                foreach (var (gameId, daily) in snapshot.PerGameDaily)
                {
                    if (!daily.TryGetValue(d, out var v)) continue;
                    if (!gameMinutes.TryAdd(gameId, v)) gameMinutes[gameId] += v;
                    total += v;
                }
            }

            weeks.Add(new MonthWeekInfo { WeekNum = weekNum, Start = monday, TotalMinutes = total, GameMinutes = gameMinutes });
            monday = monday.AddDays(7);
            weekNum++;
        }

        return weeks;
    }

    #endregion

    #region 模块一：时长统计

    /// <summary>某天全库总时长（分钟）</summary>
    public static int GetDayTotal(StatsSnapshot snapshot, DateTime date)
        => snapshot.DailyTotals.TryGetValue(date.Date, out var v) ? v : 0;

    /// <summary>某天玩过的游戏列表（按时长降序）</summary>
    public static List<GamePeriodTime> GetDayGames(StatsSnapshot snapshot, DateTime date)
    {
        var result = new List<GamePeriodTime>();
        foreach (var game in snapshot.Games)
        {
            if (!snapshot.PerGameDaily.TryGetValue(game.Uuid, out var daily)) continue;
            if (!daily.TryGetValue(date.Date, out var minutes) || minutes <= 0) continue;
            result.Add(ToGamePeriodTime(game, minutes));
        }

        return result.OrderByDescending(g => g.Minutes).ToList();
    }

    /// <summary>若干天内玩过的游戏列表（按时长降序）；filterIndex 非空时只统计第 filterIndex 天</summary>
    public static List<GamePeriodTime> GetPeriodGames(StatsSnapshot snapshot, List<DateTime> days, int? filterIndex = null)
    {
        var targetDays = filterIndex is { } idx && idx >= 0 && idx < days.Count
            ? new List<DateTime> { days[idx] }
            : days;
        var totals = new Dictionary<Guid, int>();

        foreach (var (gameId, daily) in snapshot.PerGameDaily)
        {
            foreach (var d in targetDays)
            {
                if (!daily.TryGetValue(d.Date, out var v)) continue;
                if (!totals.TryAdd(gameId, v)) totals[gameId] += v;
            }
        }

        var byId = snapshot.Games.Where(g => totals.ContainsKey(g.Uuid))
            .ToDictionary(g => g.Uuid);
        return totals.Where(kv => kv.Value > 0 && byId.ContainsKey(kv.Key))
            .Select(kv => ToGamePeriodTime(byId[kv.Key], kv.Value))
            .OrderByDescending(g => g.Minutes)
            .ToList();
    }

    /// <summary>近 7 日趋势（可只统计某个游戏）</summary>
    public static List<TrendDay> GetTrendDays(StatsSnapshot snapshot, List<DateTime> days, Guid? gameId = null)
    {
        var result = new List<TrendDay>();
        foreach (var d in days)
        {
            var minutes = 0;
            if (gameId is { } id)
            {
                if (snapshot.PerGameDaily.TryGetValue(id, out var daily))
                    daily.TryGetValue(d.Date, out minutes);
            }
            else
            {
                minutes = GetDayTotal(snapshot, d);
            }

            result.Add(new TrendDay { Date = d, Minutes = minutes });
        }

        return result;
    }

    /// <summary>某月玩过的游戏列表（按时长降序）；weekIndex 非空时只统计该自然周</summary>
    public static List<GamePeriodTime> GetMonthGames(StatsSnapshot snapshot, int year, int month, int? weekIndex = null)
    {
        var weeks = GetMonthWeeks(snapshot, year, month);
        if (weekIndex is { } wi && wi >= 0 && wi < weeks.Count)
        {
            return ToGamePeriodTimes(snapshot, weeks[wi].GameMinutes);
        }

        var merged = new Dictionary<Guid, int>();
        foreach (var week in weeks)
        {
            foreach (var (id, minutes) in week.GameMinutes)
            {
                if (!merged.TryAdd(id, minutes)) merged[id] += minutes;
            }
        }

        return ToGamePeriodTimes(snapshot, merged);
    }

    private static List<GamePeriodTime> ToGamePeriodTimes(StatsSnapshot snapshot, Dictionary<Guid, int> minutesById)
    {
        var byId = snapshot.Games.ToDictionary(g => g.Uuid);
        return minutesById.Where(kv => kv.Value > 0 && byId.ContainsKey(kv.Key))
            .Select(kv => ToGamePeriodTime(byId[kv.Key], kv.Value))
            .OrderByDescending(g => g.Minutes)
            .ToList();
    }

    #endregion

    #region 模块二：游戏统计

    /// <summary>库内游戏总时长（分钟，取 TotalPlayTime）</summary>
    public static int GetLibraryTotalMinutes(StatsSnapshot snapshot)
        => snapshot.Games.Sum(g => Math.Max(0, g.TotalPlayTime));

    /// <summary>近 N 天总时长（分钟）</summary>
    public static int GetRecentDaysTotalMinutes(StatsSnapshot snapshot, int days)
    {
        var today = DateTime.Today;
        var total = 0;
        for (var i = 0; i < days; i++)
        {
            total += GetDayTotal(snapshot, today.AddDays(-i));
        }

        return total;
    }

    /// <summary>按分布标签统计游戏数量；playTypeName 用于把游玩状态映射为本地化名称</summary>
    public static List<DistItem> GetDistribution(List<Galgame> games, DistTab tab, Func<PlayType, string>? playTypeName = null)
    {
        playTypeName ??= t => t.ToString();
        IEnumerable<DistItem> items = tab switch
        {
            DistTab.Status => games
                .GroupBy(g => g.PlayType)
                .Select(g => new DistItem
                {
                    Name = playTypeName(g.Key),
                    Icon = PlayTypeIcon(g.Key),
                    Count = g.Count(),
                    Order = (int)g.Key,
                })
                .OrderBy(i => i.Order),
            DistTab.Engine => games
                .Where(g => !IsDefaultString(g.Engine.Value))
                .GroupBy(g => g.Engine.Value!.Trim())
                .Select(g => new DistItem { Name = g.Key, Count = g.Count() })
                .OrderByDescending(i => i.Count),
            _ => games
                .Where(g => !IsDefaultString(g.Developer.Value))
                .GroupBy(g => g.Developer.Value!.Trim())
                .Select(g => new DistItem { Name = g.Key, Count = g.Count() })
                .OrderByDescending(i => i.Count),
        };

        return items.ToList();
    }

    private static bool IsDefaultString(string? s)
        => string.IsNullOrWhiteSpace(s) || s == Galgame.DefaultString;

    /// <summary>游玩状态图标（Segoe Fluent Icons）</summary>
    public static string PlayTypeIcon(PlayType type)
        => type switch
        {
            PlayType.Played => "\uE73E",
            PlayType.Playing => "\uE7FC",
            PlayType.Shelved => "\uE769",
            PlayType.Abandoned => "\uE74D",
            PlayType.WantToPlay => "\uE735",
            _ => "\uE8B7",
        };

    /// <summary>按总时长排行 TOP N（分钟）</summary>
    public static List<GamePeriodTime> GetTopGamesByTotal(List<Galgame> games, int top = 10)
        => games.Where(g => g.TotalPlayTime > 0)
            .OrderByDescending(g => g.TotalPlayTime)
            .Take(top)
            .Select(g => ToGamePeriodTime(g, g.TotalPlayTime))
            .ToList();

    /// <summary>某年每日总时长（分钟），含该年 1 月 1 日所在周的所有日期</summary>
    public static Dictionary<DateTime, int> GetYearDaily(StatsSnapshot snapshot, int year)
    {
        var result = new Dictionary<DateTime, int>();
        foreach (var (date, value) in snapshot.DailyTotals)
        {
            if (date.Year == year) result[date] = value;
        }

        return result;
    }

    /// <summary>热力图颜色层级：0 无游玩 / 1: 1-59分 / 2: 1-3时 / 3: 3-6时 / 4: ≥6时</summary>
    public static int HeatLevel(int minutes)
    {
        if (minutes <= 0) return 0;
        if (minutes < 60) return 1;
        if (minutes < 180) return 2;
        if (minutes < 360) return 3;
        return 4;
    }

    #endregion

    private static GamePeriodTime ToGamePeriodTime(Galgame game, int minutes)
    {
        var imagePath = game.ImagePath.Value;
        if (string.IsNullOrEmpty(imagePath) || imagePath == Galgame.DefaultImagePath) imagePath = null;
        return new GamePeriodTime
        {
            Id = game.Uuid,
            Name = game.Name.Value ?? string.Empty,
            ImagePath = imagePath,
            Minutes = minutes,
        };
    }
}
