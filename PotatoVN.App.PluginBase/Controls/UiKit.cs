using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// UI 常用构建工具：本地化快捷方式、时长/日期格式化、卡片/文本等通用控件。
/// </summary>
internal static class UiKit
{
    #region 本地化

    public static string L(string key, string fallback) => PluginLocalization.GetStringOr(key, fallback);

    public static string Lf(string key, string fallback, params object[] args)
        => PluginLocalization.GetStringOrFormat(key, fallback, args);

    /// <summary>逗号分隔的名称表（如星期/月份），key 对应 Strings 中的数组式字符串</summary>
    private static string[] SplitList(string key, string fallback) => L(key, fallback).Split(',');

    /// <summary>星期名（0=周一 ... 6=周日）</summary>
    public static string WeekDayName(DayOfWeek dayOfWeek)
    {
        var names = SplitList("WeekDayNames", "周一,周二,周三,周四,周五,周六,周日");
        var index = ((int)dayOfWeek + 6) % 7;
        return index >= 0 && index < names.Length ? names[index] : dayOfWeek.ToString();
    }

    /// <summary>月份短名（1-12）</summary>
    public static string MonthName(int month)
    {
        var names = SplitList("MonthNames", "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月");
        return month >= 1 && month <= names.Length ? names[month - 1] : month.ToString();
    }

    #endregion

    #region 时长格式化

    /// <summary>小时数 → 文本（如 "3.5"），用于卡片大数字</summary>
    public static string FormatHours(double hours) => hours.ToString("F1", CultureInfo.CurrentCulture);

    /// <summary>小时数 → 文本（≥10 取整，否则 1 位小数），用于概览</summary>
    public static string FormatHoursSmart(double hours)
        => (hours >= 10 ? Math.Round(hours) : hours).ToString(hours >= 10 ? "F0" : "F1", CultureInfo.CurrentCulture);

    /// <summary>小时数 → 完整时长文本（"3 小时 20 分"/"45 分钟"/"0 分钟"）</summary>
    public static string FormatTime(double hours)
    {
        if (hours <= 0) return $"0 {L("Unit_Minutes", "分钟")}";
        if (hours < 1) return $"{Math.Round(hours * 60)} {L("Unit_Minutes", "分钟")}";
        var h = (int)hours;
        var m = (int)Math.Round((hours - h) * 60);
        if (m == 0) return $"{h} {L("Unit_Hours", "小时")}";
        if (m == 60) return $"{h + 1} {L("Unit_Hours", "小时")}";
        return $"{h} {L("Unit_Hours", "小时")} {m} {L("Unit_Minutes", "分")}";
    }

    /// <summary>小时数 → 短文本（"3.2时"/"45分"/"0分"），支持负号</summary>
    public static string FormatTimeShort(double hours)
    {
        var sign = hours < 0 ? "-" : "";
        var abs = Math.Abs(hours);
        if (abs < 1) return $"{sign}{Math.Round(abs * 60)}{L("Unit_MinShort", "分")}";
        return $"{sign}{abs.ToString("F1", CultureInfo.CurrentCulture)}{L("Unit_HourShort", "时")}";
    }

    /// <summary>分钟数 → 热力图提示文本（"2时30分"/"45分"/"0分"）</summary>
    public static string FormatMinutes(int minutes)
    {
        if (minutes <= 0) return $"0{L("Unit_MinShort", "分")}";
        var h = minutes / 60;
        var m = minutes % 60;
        return h > 0
            ? m > 0 ? $"{h}{L("Unit_HourShort", "时")}{m}{L("Unit_MinShort", "分")}" : $"{h}{L("Unit_HourShort", "时")}"
            : $"{m}{L("Unit_MinShort", "分")}";
    }

    #endregion

    #region 日期格式化

    public static string FormatMD(DateTime date) => $"{date.Month}/{date.Day}";

    public static string FormatYMD(DateTime date) => Lf("Fmt_YMD", "{0}年{1}月{2}日", date.Year, date.Month, date.Day);

    public static string FormatYM(int year, int month) => Lf("Fmt_YM", "{0}年{1}月", year, month);

    public static string FormatWeekRange(DateTime monday)
    {
        var sunday = monday.AddDays(6);
        return Lf("Fmt_WeekRange", "{0}-{1}", FormatMD(monday), FormatMD(sunday));
    }

    public static string FormatDateTooltip(DateTime date) => Lf("Fmt_DateTooltip", "{0}-{1}-{2}", date.Year, date.Month, date.Day);

    /// <summary>"9/1 周一" 式标签</summary>
    public static string FormatDayLabel(DateTime date) => $"{FormatMD(date)} {WeekDayName(date.DayOfWeek)}";

    /// <summary>"9月 第1周" 式标签</summary>
    public static string FormatWeekLabel(int month, int weekNum) => Lf("Fmt_MonthWeek", "{0}月 第{1}周", month, weekNum);

    #endregion

    #region 控件构建

    public static TextBlock Text(string content, Color? color = null, double fontSize = 13,
        FontWeight? fontWeight = null, TextTrimming trimming = TextTrimming.None, TextWrapping wrapping = TextWrapping.NoWrap,
        TextAlignment? textAlignment = null, Thickness? margin = null, double? maxWidth = null)
    {
        var textBlock = new TextBlock
        {
            Text = content,
            FontSize = fontSize,
            TextTrimming = trimming,
            TextWrapping = wrapping,
        };
        if (color is { } c) textBlock.Foreground = new SolidColorBrush(c);
        if (fontWeight is { } w) textBlock.FontWeight = w;
        if (textAlignment is { } ta) textBlock.TextAlignment = ta;
        if (margin is { } m) textBlock.Margin = m;
        if (maxWidth is { } mw) textBlock.MaxWidth = mw;
        return textBlock;
    }

    /// <summary>卡片容器：圆角 8、1px 边框、hover 时边框变强调色</summary>
    public static Border Card(StatsPalette palette, UIElement child, Thickness? padding = null)
    {
        var border = new Border
        {
            Background = palette.CardBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = padding ?? new Thickness(20),
            Child = child,
        };
        AttachBorderHover(border, palette.BorderBrush, palette.AccentBrush);
        return border;
    }

    /// <summary>hover 换背景（行 hover 效果）</summary>
    public static void AttachHover(Border border, Brush normal, Brush hover)
    {
        border.Background = normal;
        border.PointerEntered += (_, _) => border.Background = hover;
        border.PointerExited += (_, _) => border.Background = normal;
    }

    /// <summary>hover 换边框颜色（卡片效果）</summary>
    public static void AttachBorderHover(Border border, Brush normal, Brush hover)
    {
        border.PointerEntered += (_, _) => border.BorderBrush = hover;
        border.PointerExited += (_, _) => border.BorderBrush = normal;
    }

    /// <summary>色点（图例用）</summary>
    public static Ellipse Dot(Color color, double size = 8)
        => new() { Width = size, Height = size, Fill = new SolidColorBrush(color) };

    /// <summary>游戏图标：优先真实封面图，失败/缺省时用首字母色块</summary>
    public static FrameworkElement GameIcon(GamePeriodTime game, int colorIndex, double size = 40)
    {
        var container = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(StatsTheme.SeriesColor(colorIndex)),
        };

        if (!string.IsNullOrWhiteSpace(game.ImagePath))
        {
            try
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(game.ImagePath)),
                    Stretch = Stretch.UniformToFill,
                    Width = size,
                    Height = size,
                };
                var clip = new Border { Width = size, Height = size, CornerRadius = new CornerRadius(6), Child = image };
                container.Background = null;
                container.Child = clip;
                return container;
            }
            catch (Exception)
            {
                // 图片加载失败 → 回退首字母
            }
        }

        var initials = string.IsNullOrEmpty(game.Name) ? "?" : game.Name.Substring(0, Math.Min(2, game.Name.Length));
        container.Child = new TextBlock
        {
            Text = initials,
            FontSize = size * 0.35,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        return container;
    }

    /// <summary>横向渐变刷（柱形图用）</summary>
    public static LinearGradientBrush VerticalGradient(Color top, Color bottom)
        => new()
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop { Color = top, Offset = 0 },
                new GradientStop { Color = bottom, Offset = 1 },
            },
        };

    /// <summary>居中的空状态提示</summary>
    public static TextBlock EmptyState(string text, Color mutedColor)
        => new()
        {
            Text = text,
            FontSize = 13,
            Foreground = new SolidColorBrush(mutedColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 40, 0, 0),
        };

    #endregion

    #region 标签组与排序切换

    /// <summary>等宽多列网格（替代 UniformGrid，WinUI 3 无此控件）</summary>
    public static Grid EqualColumns(IReadOnlyList<FrameworkElement> children, double columnSpacing = 16)
    {
        var grid = new Grid();
        for (var i = 0; i < children.Count; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (i > 0) children[i].Margin = new Thickness(columnSpacing, 0, 0, 0);
            grid.Children.Add(children[i]);
            Grid.SetColumn(children[i], i);
        }

        return grid;
    }

    /// <summary>药丸式标签组（维度/分布切换用）；点击后自动切换激活样式并回调</summary>
    public static Border PillTabs(StatsPalette palette, IReadOnlyList<string> labels, int activeIndex, Action<int> onSelected)
    {
        var buttons = new List<Button>();
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        for (var i = 0; i < labels.Count; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = labels[i],
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(14, 6, 14, 6),
            };
            button.Click += (_, _) =>
            {
                for (var j = 0; j < buttons.Count; j++)
                    StylePillButton(buttons[j], palette, j == index);
                onSelected(index);
            };
            StylePillButton(button, palette, i == activeIndex);
            buttons.Add(button);
            panel.Children.Add(button);
        }

        return new Border
        {
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(3),
            Child = panel,
        };
    }

    /// <summary>小型排序切换按钮组（按时长/按名称）</summary>
    public static Border SortToggle(StatsPalette palette, IReadOnlyList<string> labels, int activeIndex, Action<int> onSelected)
    {
        var buttons = new List<Button>();
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

        for (var i = 0; i < labels.Count; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = labels[i],
                FontSize = 12,
                Background = palette.BgSecondaryBrush,
                BorderBrush = palette.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 4, 12, 4),
            };
            button.Click += (_, _) =>
            {
                for (var j = 0; j < buttons.Count; j++)
                    StyleSmallButton(buttons[j], palette, j == index);
                onSelected(index);
            };
            StyleSmallButton(button, palette, i == activeIndex);
            buttons.Add(button);
            panel.Children.Add(button);
        }

        return new Border { Child = panel };
    }

    private static void StylePillButton(Button button, StatsPalette palette, bool active)
    {
        button.Background = active ? palette.AccentBrightBrush : new SolidColorBrush(Colors.Transparent);
        button.Foreground = active ? new SolidColorBrush(Colors.White) : palette.TextSecondaryBrush;
    }

    private static void StyleSmallButton(Button button, StatsPalette palette, bool active)
    {
        button.Background = active ? palette.AccentBrightBrush : palette.BgSecondaryBrush;
        button.BorderBrush = active ? palette.AccentBrightBrush : palette.BorderBrush;
        button.Foreground = active ? new SolidColorBrush(Colors.White) : palette.TextSecondaryBrush;
    }

    #endregion

    #region 排序

    public static List<GamePeriodTime> SortGames(List<GamePeriodTime> games, RankSort sort)
    {
        var result = new List<GamePeriodTime>(games);
        if (sort == RankSort.Name)
            result.Sort((a, b) => string.Compare(a.Name, b.Name, CultureInfo.CurrentCulture, CompareOptions.None));
        return result;
    }

    #endregion
}
