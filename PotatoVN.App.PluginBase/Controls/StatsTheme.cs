using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// 统计页配色：深色主题沿用原型（Steam 风 #1b2838），浅色主题为其适配版。
/// </summary>
internal sealed class StatsPalette
{
    public Color BgPrimary { get; init; }
    public Color BgSecondary { get; init; }
    public Color Card { get; init; }
    public Color Hover { get; init; }
    public Color Border { get; init; }
    public Color TextPrimary { get; init; }
    public Color TextSecondary { get; init; }
    public Color TextMuted { get; init; }
    public Color Accent { get; init; }
    public Color AccentBright { get; init; }
    public Color AccentDark { get; init; }
    public Color Success { get; init; }
    public Color Danger { get; init; }

    /// <summary>热力图 5 级颜色（少 → 多）</summary>
    public IReadOnlyList<Color> HeatLevels { get; init; } = Array.Empty<Color>();

    private readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

    public SolidColorBrush Brush(Color color)
    {
        if (!_brushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidColorBrush(color);
            _brushCache[color] = brush;
        }

        return brush;
    }

    public SolidColorBrush BgPrimaryBrush => Brush(BgPrimary);
    public SolidColorBrush BgSecondaryBrush => Brush(BgSecondary);
    public SolidColorBrush CardBrush => Brush(Card);
    public SolidColorBrush HoverBrush => Brush(Hover);
    public SolidColorBrush BorderBrush => Brush(Border);
    public SolidColorBrush TextPrimaryBrush => Brush(TextPrimary);
    public SolidColorBrush TextSecondaryBrush => Brush(TextSecondary);
    public SolidColorBrush TextMutedBrush => Brush(TextMuted);
    public SolidColorBrush AccentBrush => Brush(Accent);
    public SolidColorBrush AccentBrightBrush => Brush(AccentBright);
    public SolidColorBrush AccentDarkBrush => Brush(AccentDark);
    public SolidColorBrush SuccessBrush => Brush(Success);
    public SolidColorBrush DangerBrush => Brush(Danger);

    /// <summary>accent 带 alpha 的半透明版本（用于周高亮等）</summary>
    public SolidColorBrush AccentAlphaBrush(byte alpha)
    {
        var c = Accent;
        return Brush(Color.FromArgb(alpha, c.R, c.G, c.B));
    }
}

internal static class StatsTheme
{
    private static readonly Color[] SeriesColors =
    {
        Color.FromArgb(0xFF, 0x66, 0xc0, 0xf4), Color.FromArgb(0xFF, 0x5c, 0xb8, 0x5c),
        Color.FromArgb(0xFF, 0xd9, 0x53, 0x4f), Color.FromArgb(0xFF, 0xf0, 0xad, 0x4e),
        Color.FromArgb(0xFF, 0x9b, 0x59, 0xb6), Color.FromArgb(0xFF, 0x1a, 0xbc, 0x9c),
        Color.FromArgb(0xFF, 0xe6, 0x7e, 0x22), Color.FromArgb(0xFF, 0x34, 0x98, 0xdb),
        Color.FromArgb(0xFF, 0xe7, 0x4c, 0x3c), Color.FromArgb(0xFF, 0x2e, 0xcc, 0x71),
        Color.FromArgb(0xFF, 0xf1, 0xc4, 0x0f), Color.FromArgb(0xFF, 0x94, 0x67, 0xbd),
        Color.FromArgb(0xFF, 0x17, 0xa2, 0xb8), Color.FromArgb(0xFF, 0xfd, 0x7e, 0x14),
        Color.FromArgb(0xFF, 0x6f, 0x42, 0xc1), Color.FromArgb(0xFF, 0xa3, 0xbe, 0x8c),
    };

    private static readonly StatsPalette DarkPalette = new()
    {
        BgPrimary = Color.FromArgb(0xFF, 0x1b, 0x28, 0x38),
        BgSecondary = Color.FromArgb(0xFF, 0x16, 0x20, 0x2d),
        Card = Color.FromArgb(0xFF, 0x1f, 0x2d, 0x3d),
        Hover = Color.FromArgb(0xFF, 0x2a, 0x47, 0x5e),
        Border = Color.FromArgb(0xFF, 0x3c, 0x4d, 0x5e),
        TextPrimary = Color.FromArgb(0xFF, 0xc7, 0xd5, 0xe0),
        TextSecondary = Color.FromArgb(0xFF, 0x8f, 0x98, 0xa0),
        TextMuted = Color.FromArgb(0xFF, 0x6b, 0x7a, 0x8c),
        Accent = Color.FromArgb(0xFF, 0x66, 0xc0, 0xf4),
        AccentBright = Color.FromArgb(0xFF, 0x1a, 0x9f, 0xff),
        AccentDark = Color.FromArgb(0xFF, 0x0d, 0x6f, 0xb8),
        Success = Color.FromArgb(0xFF, 0x5c, 0xb8, 0x5c),
        Danger = Color.FromArgb(0xFF, 0xd9, 0x53, 0x4f),
        HeatLevels = new[]
        {
            Color.FromArgb(0xFF, 0x24, 0x34, 0x49), Color.FromArgb(0xFF, 0x12, 0x3c, 0x27),
            Color.FromArgb(0xFF, 0x0e, 0x5a, 0x2e), Color.FromArgb(0xFF, 0x19, 0x90, 0x48),
            Color.FromArgb(0xFF, 0x2f, 0xbf, 0x5f),
        },
    };

    private static readonly StatsPalette LightPalette = new()
    {
        BgPrimary = Color.FromArgb(0xFF, 0xf3, 0xf7, 0xfa),
        BgSecondary = Color.FromArgb(0xFF, 0xe9, 0xef, 0xf5),
        Card = Color.FromArgb(0xFF, 0xff, 0xff, 0xff),
        Hover = Color.FromArgb(0xFF, 0xdb, 0xe7, 0xf1),
        Border = Color.FromArgb(0xFF, 0xcd, 0xd9, 0xe4),
        TextPrimary = Color.FromArgb(0xFF, 0x1f, 0x2d, 0x3d),
        TextSecondary = Color.FromArgb(0xFF, 0x54, 0x68, 0x7a),
        TextMuted = Color.FromArgb(0xFF, 0x82, 0x91, 0xa0),
        Accent = Color.FromArgb(0xFF, 0x1a, 0x7f, 0xc0),
        AccentBright = Color.FromArgb(0xFF, 0x1a, 0x9f, 0xff),
        AccentDark = Color.FromArgb(0xFF, 0x0d, 0x6f, 0xb8),
        Success = Color.FromArgb(0xFF, 0x3d, 0x9e, 0x4f),
        Danger = Color.FromArgb(0xFF, 0xd6, 0x45, 0x3d),
        HeatLevels = new[]
        {
            Color.FromArgb(0xFF, 0xe4, 0xe9, 0xee), Color.FromArgb(0xFF, 0xbe, 0xe2, 0xc2),
            Color.FromArgb(0xFF, 0x82, 0xcd, 0x91), Color.FromArgb(0xFF, 0x46, 0xb0, 0x5e),
            Color.FromArgb(0xFF, 0x1f, 0x9e, 0x40),
        },
    };

    public static StatsPalette For(ElementTheme theme) => theme == ElementTheme.Dark ? DarkPalette : LightPalette;

    public static StatsPalette For(FrameworkElement element) => For(element.ActualTheme);

    /// <summary>按序号取系列色（图表/图标用）</summary>
    public static Color SeriesColor(int index) => SeriesColors[Math.Abs(index) % SeriesColors.Length];

    /// <summary>按 Guid 稳定取系列色</summary>
    public static Color SeriesColor(Guid id)
    {
        var hash = 0;
        foreach (var b in id.ToByteArray()) hash = unchecked(hash * 31 + b);
        return SeriesColors[Math.Abs(hash) % SeriesColors.Length];
    }
}
