using GalgameManager.Models;
using Microsoft.UI.Xaml.Media;

namespace PotatoVN.App.PluginBase.Models;

/// <summary>
/// Represents a game entry in the ranking list.
/// </summary>
public class GameRankItem
{
    public Galgame Game { get; set; } = null!;

    /// <summary>Game display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Game icon image path.</summary>
    public string ImagePath { get; set; } = Galgame.DefaultImagePath;

    /// <summary>Total play time in minutes (within the current filter scope).</summary>
    public int Minutes { get; set; }

    /// <summary>Formatted display string for minutes.</summary>
    public string DisplayTime { get; set; } = string.Empty;

    /// <summary>Percentage of total play time (0–100).</summary>
    public double Percentage { get; set; }

    /// <summary>Width ratio for the percentage bar (0–1).</summary>
    public double WidthRatio { get; set; }

    /// <summary>Brush for the percentage bar.</summary>
    public SolidColorBrush BarBrush { get; set; } = new(Microsoft.UI.Colors.DodgerBlue);
}