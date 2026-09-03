using System;
using System.Collections.Generic;

namespace PotatoVN.App.PluginBase.Models;

/// <summary>统计维度</summary>
public enum StatsPeriod
{
    Day,
    Week,
    Month,
}

/// <summary>排行排序方式</summary>
public enum RankSort
{
    Time,
    Name,
}

/// <summary>游戏分布标签</summary>
public enum DistTab
{
    Status,
    Engine,
    Developer,
}

/// <summary>某时段内单个游戏的游玩时长（分钟）</summary>
public sealed class GamePeriodTime
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public int Minutes { get; init; }

    public double Hours => Minutes / 60.0;
}

/// <summary>月维度中的一个自然周（仅统计该月内的天数）</summary>
public sealed class MonthWeekInfo
{
    public int WeekNum { get; init; }
    public DateTime Start { get; init; }
    public int TotalMinutes { get; init; }
    public Dictionary<Guid, int> GameMinutes { get; init; } = new();
}

/// <summary>分布统计项</summary>
public sealed class DistItem
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int Count { get; init; }

    /// <summary>排序键（游玩状态按枚举顺序展示）</summary>
    public int Order { get; init; }
}

/// <summary>趋势面板单日数据</summary>
public sealed class TrendDay
{
    public DateTime Date { get; init; }
    public int Minutes { get; init; }

    public double Hours => Minutes / 60.0;
}
