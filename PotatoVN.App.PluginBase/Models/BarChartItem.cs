using System;
using System.Collections.Generic;
using GalgameManager.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PotatoVN.App.PluginBase.Models;

/// <summary>
/// Represents a single bar in the horizontal bar chart.
/// Each bar aggregates play time for a time segment (a day, a week, or a month).
/// </summary>
public class BarChartItem
{
    /// <summary>Display label for the Y-axis (e.g. "8/23", "Week 34", "2026/08").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Sub-label for secondary display (e.g. date range).</summary>
    public string SubLabel { get; set; } = string.Empty;

    /// <summary>Total play time in minutes for this segment.</summary>
    public int TotalMinutes { get; set; }

    /// <summary>Formatted display string for the total minutes.</summary>
    public string DisplayTime { get; set; } = string.Empty;

    /// <summary>Width ratio (0–1) relative to the longest bar.</summary>
    public double WidthRatio { get; set; }

    /// <summary>Star-sized GridLength for the filled portion of the bar.</summary>
    public GridLength FillWidth { get; set; } = new(0, GridUnitType.Star);

    /// <summary>Star-sized GridLength for the empty portion of the bar.</summary>
    public GridLength EmptyWidth { get; set; } = new(1, GridUnitType.Star);

    /// <summary>Brush colour for the bar (varies by index for visual variety).</summary>
    public SolidColorBrush BarBrush { get; set; } = new(Microsoft.UI.Colors.DodgerBlue);

    /// <summary>Start date of this segment (inclusive).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End date of this segment (inclusive).</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Per-game minutes within this segment, used when the user clicks a bar.</summary>
    public List<GameSegmentTime> GameTimes { get; set; } = new();
}

/// <summary>
/// Play time for a single game within a time segment.
/// </summary>
public class GameSegmentTime
{
    public Galgame Game { get; set; } = null!;
    public int Minutes { get; set; }
    public string DisplayTime { get; set; } = string.Empty;
}