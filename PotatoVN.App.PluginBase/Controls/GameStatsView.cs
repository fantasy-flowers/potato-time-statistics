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
/// 按功能拆分为多个 partial 文件，本文件只含状态字段、构造、整体布局、头部/年份导航与概览卡片。
/// 其余模块：Distribution（游戏分布）、RankHeatmap（排行 + 热力图）。
/// </summary>
public sealed partial class GameStatsView : Grid
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
}
