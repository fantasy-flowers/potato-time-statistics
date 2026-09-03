using System;
using System.Collections.Generic;
using System.Linq;
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
/// 模块一：游戏时长统计（日/周/月维度 + 日期选择 + 图表联动），全部用 C# 描述 UI。
/// 按功能拆分为多个 partial 文件，本文件只含状态字段、构造、整体布局、头部与交互回调。
/// 其余模块：DatePicker（日期选择器）、StatsBar（概览指标条）、Chart（图表区）、SidePanel（侧栏）。
/// </summary>
public sealed partial class PlaytimeStatsView : Grid
{
    private readonly PluginData _data;
    private StatsSnapshot _snapshot = new();

    // 界面状态
    private StatsPeriod _period = StatsPeriod.Day;
    private RankSort _sort = RankSort.Time;
    private DateTime _selectedDate = DateTime.Today;
    private int _selectedYear;
    private int _selectedMonth;   // 0-11
    private int? _selectedIndex;  // 柱形图选中下标
    private Guid? _selectedGameId; // 环形图选中游戏

    // 日历面板状态
    private int _panelYear;
    private int _panelMonth;

    public PlaytimeStatsView(PluginData data)
    {
        _data = data;
        _selectedYear = _selectedDate.Year;
        _selectedMonth = _selectedDate.Month - 1;
        _period = data.DefaultPeriod switch { "week" => StatsPeriod.Week, "month" => StatsPeriod.Month, _ => StatsPeriod.Day };
        _sort = data.RankSort == "name" ? RankSort.Name : RankSort.Time;
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
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var headerView = BuildHeader(palette);
        Children.Add(headerView);
        Grid.SetRow(headerView, 0);

        var statsBarView = BuildStatsBar(palette);
        Children.Add(statsBarView);
        Grid.SetRow(statsBarView, 1);

        var mainView = BuildMainContent(palette);
        Children.Add(mainView);
        Grid.SetRow(mainView, 2);
    }

    private FrameworkElement BuildHeader(StatsPalette palette)
    {
        var title = UiKit.Text(UiKit.L("Playtime_Title", "游戏时长统计"), palette.TextPrimary, 22, FontWeights.Bold);
        var subtitle = UiKit.Text(UiKit.L("Playtime_Subtitle", "追踪你的游戏习惯，合理安排游戏时间"), palette.TextSecondary, 12.5);
        var titlePanel = new StackPanel();
        titlePanel.Children.Add(title);
        titlePanel.Children.Add(subtitle);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right };

        // 日期选择按钮
        var dateButton = BuildDatePickerButton(palette);
        right.Children.Add(dateButton);

        // 维度切换
        var periodLabels = new[] { UiKit.L("Period_Day", "日"), UiKit.L("Period_Week", "周"), UiKit.L("Period_Month", "月") };
        var periodIndex = _period switch { StatsPeriod.Week => 1, StatsPeriod.Month => 2, _ => 0 };
        right.Children.Add(UiKit.PillTabs(palette, periodLabels, periodIndex, OnPeriodSelected));

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(titlePanel);
        root.Children.Add(right);
        Grid.SetColumn(right, 1);
        return root;
    }

    #endregion

    #region 交互回调

    private void OnPeriodSelected(int index)
    {
        _period = index switch { 1 => StatsPeriod.Week, 2 => StatsPeriod.Month, _ => StatsPeriod.Day };
        if (_period == StatsPeriod.Month)
        {
            _selectedYear = _selectedDate.Year;
            _selectedMonth = _selectedDate.Month - 1;
        }

        _selectedIndex = null;
        _selectedGameId = null;
        _data.DefaultPeriod = _period switch { StatsPeriod.Week => "week", StatsPeriod.Month => "month", _ => "day" };
        BuildUi();
    }

    #endregion
}
