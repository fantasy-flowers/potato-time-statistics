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
/// 模块二：游戏统计（游戏库规模、游玩状态/引擎/制作公司分布、总时长排行、年度游玩强度热力图）。
/// </summary>
public sealed class GameStatsView : Grid
{
    private readonly PluginData _data;
    private StatsSnapshot _snapshot = new();
    private int _year = DateTime.Today.Year;
    private DistTab _tab = DistTab.Status;

    public GameStatsView(PluginData data)
    {
        _data = data;
        _tab = data.DistTab switch { "engine" => DistTab.Engine, "developer" => DistTab.Developer, _ => DistTab.Status };
        ActualThemeChanged += (_, _) => BuildUi();
        RefreshData();
    }

    public void RefreshData()
    {
        _snapshot = StatsService.BuildSnapshot();
        BuildUi();
    }

    public void Rebuild() => BuildUi();

    #region 整体布局

    private void BuildUi()
    {
        var palette = StatsTheme.For(this);
        Children.Clear();

        RowDefinitions.Clear();
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var headerView = BuildHeader(palette);
        Children.Add(headerView);
        Grid.SetRow(headerView, 0);

        var overviewView = BuildOverview(palette);
        Children.Add(overviewView);
        Grid.SetRow(overviewView, 1);

        var distView = BuildDistributionCard(palette);
        Children.Add(distView);
        Grid.SetRow(distView, 2);

        var mainView = BuildMainGrid(palette);
        Children.Add(mainView);
        Grid.SetRow(mainView, 3);
    }

    private FrameworkElement BuildHeader(StatsPalette palette)
    {
        var titlePanel = new StackPanel();
        titlePanel.Children.Add(UiKit.Text(UiKit.L("Stats_Title", "游戏统计"), palette.TextPrimary, 22, FontWeights.Bold));
        titlePanel.Children.Add(UiKit.Text(
            UiKit.L("Stats_Subtitle", "游戏库规模 · 游玩状态 · 时长排行 · 年度游玩强度"), palette.TextSecondary, 12.5));

        var yearNav = BuildYearNav(palette);

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(titlePanel);
        root.Children.Add(yearNav);
        Grid.SetColumn(yearNav, 1);
        return root;
    }

    private FrameworkElement BuildYearNav(StatsPalette palette)
    {
        var yearText = UiKit.Text(_year.ToString(), palette.TextPrimary, 14, FontWeights.SemiBold,
            textAlignment: TextAlignment.Center);
        yearText.MinWidth = 56;

        var prevButton = BuildYearButton(palette, "\uE76B");
        prevButton.IsEnabled = _year > _snapshot.MinYear;
        prevButton.Click += (_, _) =>
        {
            _year--;
            BuildUi();
        };

        var nextButton = BuildYearButton(palette, "\uE76C");
        nextButton.IsEnabled = _year < _snapshot.MaxYear;
        nextButton.Click += (_, _) =>
        {
            _year++;
            BuildUi();
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        panel.Children.Add(prevButton);
        panel.Children.Add(yearText);
        panel.Children.Add(nextButton);

        return new Border
        {
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Child = panel,
        };
    }

    private static Button BuildYearButton(StatsPalette palette, string glyph)
        => new()
        {
            Width = 32,
            Height = 32,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(6),
            Content = new FontIcon { Glyph = glyph, FontSize = 14, Foreground = palette.TextSecondaryBrush },
        };

    #endregion

    #region 概览卡片

    private FrameworkElement BuildOverview(StatsPalette palette)
    {
        var games = _snapshot.Games;
        var localCount = games.Count(g => g.IsLocalGame);
        var totalMinutes = StatsService.GetLibraryTotalMinutes(_snapshot);
        var recent30 = StatsService.GetRecentDaysTotalMinutes(_snapshot, 30);
        var top = games.Where(g => g.TotalPlayTime > 0).OrderByDescending(g => g.TotalPlayTime).FirstOrDefault();

        var recentDeltaText = (recent30 >= 0 ? "+" : "-") +
                              UiKit.FormatHours(Math.Abs(recent30 / 60.0)) + " " + UiKit.L("Unit_Hours", "小时");
        var topSub = top is null
            ? UiKit.L("Stats_NoTop", "暂无游玩记录")
            : $"{UiKit.FormatHoursSmart(top.TotalPlayTime / 60.0)} {UiKit.L("Unit_Hours", "小时")} · " +
              $"{top.PlayCount} {UiKit.L("Unit_Plays", "次游玩")}";

        var grid = UiKit.EqualColumns(new FrameworkElement[]
        {
            BuildStatCard(palette, UiKit.L("Stats_GameCount", "库中游戏"),
                games.Count.ToString(), UiKit.L("Unit_Games", "款"),
                UiKit.Lf("Sub_LocalAndVirtual", "本地安装 {0} · 虚拟游戏 {1}", localCount, games.Count - localCount), 24),
            BuildStatCard(palette, UiKit.L("Stats_TotalTime", "累计游玩时长"),
                UiKit.FormatHoursSmart(totalMinutes / 60.0), UiKit.L("Unit_Hours", "小时"),
                UiKit.Lf("Sub_RecentDays", "近 {0} 天 {1}", 30, recentDeltaText), 24),
            BuildStatCard(palette, UiKit.L("Stats_TopGame", "时长最长"),
                top?.Name.Value ?? "—", null, topSub, 18, top?.Name.Value),
        });
        grid.Margin = new Thickness(0, 20, 0, 20);
        return grid;
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

        var content = new StackPanel();
        content.Children.Add(UiKit.Text(label, palette.TextSecondary, 12));
        content.Children.Add(valueRow);
        content.Children.Add(UiKit.Text(sub, palette.TextMuted, 11, margin: new Thickness(0, 6, 0, 0)));
        return UiKit.Card(palette, content, new Thickness(16, 18, 16, 18));
    }

    #endregion

    #region 游戏分布

    private FrameworkElement BuildDistributionCard(StatsPalette palette)
    {
        var items = StatsService.GetDistribution(_snapshot.Games, _tab, PlayTypeDisplayName);
        var maxCount = items.Count > 0 ? items.Max(i => i.Count) : 1;
        var visuals = items.Select(item => BuildDistItem(palette, item, maxCount)).ToList();

        var repeater = new ItemsRepeater
        {
            ItemsSource = visuals,
            Layout = new UniformGridLayout
            {
                MinItemWidth = 130,
                MinItemHeight = 96,
                MinColumnSpacing = 10,
                MinRowSpacing = 10,
            },
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(UiKit.Text(UiKit.L("Stats_DistTitle", "游戏分布"), palette.TextPrimary, 15, FontWeights.SemiBold));
        var tabs = UiKit.PillTabs(palette,
            new[]
            {
                UiKit.L("Stats_Tab_Status", "游玩状态"),
                UiKit.L("Stats_Tab_Engine", "游戏引擎"),
                UiKit.L("Stats_Tab_Developer", "制作公司"),
            },
            _tab switch { DistTab.Engine => 1, DistTab.Developer => 2, _ => 0 },
            index =>
            {
                _tab = index switch { 1 => DistTab.Engine, 2 => DistTab.Developer, _ => DistTab.Status };
                _data.DistTab = _tab switch { DistTab.Engine => "engine", DistTab.Developer => "developer", _ => "status" };
                BuildUi();
            });
        header.Children.Add(tabs);
        Grid.SetColumn(tabs, 1);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.Children.Add(header);
        content.Children.Add(repeater);
        Grid.SetRow(repeater, 1);
        repeater.Margin = new Thickness(0, 10, 0, 0);

        var card = UiKit.Card(palette, content, new Thickness(20));
        card.Margin = new Thickness(0, 0, 0, 20);
        return card;
    }

    private static FrameworkElement BuildDistItem(StatsPalette palette, DistItem item, int maxCount)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };

        if (string.IsNullOrEmpty(item.Icon))
        {
            // 引擎/制作公司：显示名称前两字
            var iconText = item.Name.Length <= 2 ? item.Name : item.Name[..2];
            panel.Children.Add(UiKit.Text(iconText, palette.TextSecondary, 16, FontWeights.SemiBold,
                textAlignment: TextAlignment.Center));
        }
        else
        {
            panel.Children.Add(new FontIcon
            {
                Glyph = item.Icon,
                FontSize = 20,
                Foreground = palette.AccentBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }

        var name = UiKit.Text(item.Name, palette.TextPrimary, 12.5, trimming: TextTrimming.CharacterEllipsis,
            textAlignment: TextAlignment.Center);
        ToolTipService.SetToolTip(name, item.Name);
        panel.Children.Add(name);
        panel.Children.Add(UiKit.Text(item.Count.ToString(), palette.Accent, 19, FontWeights.Bold,
            textAlignment: TextAlignment.Center, margin: new Thickness(0, 2, 0, 0)));

        // 占比条（相对最大分类）
        var track = new Border
        {
            Background = palette.HoverBrush,
            CornerRadius = new CornerRadius(2),
            Height = 4,
            Margin = new Thickness(6, 8, 6, 0),
        };
        var trackGrid = new Grid();
        var percent = maxCount > 0 ? Math.Clamp(item.Count * 100.0 / maxCount, 0, 100) : 0;
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
        trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - percent, GridUnitType.Star) });
        trackGrid.Children.Add(new Border
        {
            Background = palette.AccentBrightBrush,
            CornerRadius = new CornerRadius(2),
            Height = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        });
        track.Child = trackGrid;
        panel.Children.Add(track);

        return new Border
        {
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 14, 12, 12),
            Child = panel,
        };
    }

    private static string PlayTypeDisplayName(PlayType type)
        => UiKit.L($"PlayType_{type}", type.ToString());

    #endregion

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
