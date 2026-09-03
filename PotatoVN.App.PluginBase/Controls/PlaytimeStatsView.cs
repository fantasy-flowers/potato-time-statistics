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
/// </summary>
public sealed class PlaytimeStatsView : Grid
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

    private FrameworkElement BuildDatePickerButton(StatsPalette palette)
    {
        var text = new TextBlock { FontSize = 13, FontWeight = FontWeights.Medium };
        var button = new Button
        {
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 7, 12, 7),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        content.Children.Add(new FontIcon { Glyph = "\uE787", FontSize = 14, Foreground = palette.AccentBrush });
        content.Children.Add(text);
        content.Children.Add(new FontIcon { Glyph = "\uE70E", FontSize = 10, Foreground = palette.TextMutedBrush });
        button.Content = content;

        var flyout = new Flyout { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        flyout.Opening += (_, _) =>
        {
            _panelYear = _period == StatsPeriod.Month ? _selectedYear : _selectedDate.Year;
            _panelMonth = _period == StatsPeriod.Month ? _selectedMonth : _selectedDate.Month - 1;
            flyout.Content = BuildPickerPanel(palette, flyout);
        };
        button.Flyout = flyout;

        text.Text = CurrentDateText();
        return button;
    }

    private string CurrentDateText()
    {
        return _period switch
        {
            StatsPeriod.Week => UiKit.FormatWeekRange(StatsService.GetMonday(_selectedDate)),
            StatsPeriod.Month => UiKit.FormatYM(_selectedYear, _selectedMonth + 1),
            _ => UiKit.FormatYMD(_selectedDate),
        };
    }

    #endregion

    #region 日期选择器面板

    private FrameworkElement BuildPickerPanel(StatsPalette palette, Flyout flyout)
        => _period == StatsPeriod.Month ? BuildMonthPanel(palette, flyout) : BuildCalendarPanel(palette, flyout);

    private FrameworkElement BuildCalendarPanel(StatsPalette palette, Flyout flyout)
    {
        var panel = new StackPanel { MinWidth = 300 };
        panel.Children.Add(BuildCalendarHeader(palette, isMonthMode: false, flyout));

        // 星期表头
        var weekdayGrid = new Grid();
        for (var i = 0; i < 7; i++) weekdayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 7; i++)
        {
            var dayName = UiKit.WeekDayName((DayOfWeek)((i + 1) % 7));
            var label = UiKit.Text(dayName, palette.TextMuted, 11, textAlignment: TextAlignment.Center);
            weekdayGrid.Children.Add(label);
            Grid.SetColumn(label, i);
        }

        panel.Children.Add(weekdayGrid);

        // 日期网格（42 格）
        var firstDay = new DateTime(_panelYear, _panelMonth + 1, 1);
        var startMonday = StatsService.GetMonday(firstDay);
        var today = DateTime.Today;
        var selectedMonday = StatsService.GetMonday(_selectedDate);
        var cells = new List<CalendarCell>();

        var daysGrid = new Grid();
        for (var i = 0; i < 7; i++) daysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var r = 0; r < 6; r++) daysGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var index = 0; index < 42; index++)
        {
            var date = startMonday.AddDays(index);
            var otherMonth = date.Month != _panelMonth + 1;
            var isToday = date == today;
            var isSelected = date == _selectedDate;
            var disabled = date > today;
            var inSelectedWeek = _period == StatsPeriod.Week && StatsService.GetMonday(date) == selectedMonday;

            var cell = new CalendarCell { Date = date, OtherMonth = otherMonth, Today = isToday, Selected = isSelected, Disabled = disabled, InSelectedWeek = inSelectedWeek };
            var border = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 2),
            };
            border.Child = UiKit.Text(date.Day.ToString(), palette.TextSecondary, 12, textAlignment: TextAlignment.Center);

            var targetDate = date;
            border.Tapped += (_, _) =>
            {
                if (cell.Disabled) return;
                _selectedDate = targetDate;
                flyout.Hide();
                BuildUi();
            };

            if (_period == StatsPeriod.Week)
            {
                border.PointerEntered += (_, _) => ApplyWeekHover(cells, cell, palette, hovered: true);
                border.PointerExited += (_, _) => ApplyWeekHover(cells, cell, palette, hovered: false);
            }

            cell.Border = border;
            cells.Add(cell);
            daysGrid.Children.Add(border);
            Grid.SetColumn(border, index % 7);
            Grid.SetRow(border, index / 7);
        }

        ApplyCalendarStyles(cells, palette);
        panel.Children.Add(daysGrid);
        panel.Children.Add(BuildCalendarFooter(palette, flyout, monthMode: false));
        return panel;
    }

    private FrameworkElement BuildMonthPanel(StatsPalette palette, Flyout flyout)
    {
        var panel = new StackPanel { MinWidth = 300 };
        panel.Children.Add(BuildCalendarHeader(palette, isMonthMode: true, flyout));

        var now = DateTime.Today;
        var grid = new Grid();
        for (var i = 0; i < 3; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var month = 0; month < 12; month++)
        {
            var isSelected = _panelYear == _selectedYear && month == _selectedMonth;
            var isCurrent = _panelYear == now.Year && month == now.Month - 1;
            var isFuture = _panelYear > now.Year || (_panelYear == now.Year && month > now.Month - 1);

            var button = new Button
            {
                Content = UiKit.MonthName(month + 1),
                FontSize = 13,
                Margin = new Thickness(3),
                Padding = new Thickness(0, 12, 0, 12),
                CornerRadius = new CornerRadius(6),
                Background = isSelected ? palette.AccentBrightBrush : palette.CardBrush,
                BorderBrush = isCurrent && !isSelected ? palette.AccentBrush : new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1),
                Foreground = isSelected ? new SolidColorBrush(Colors.White)
                    : isCurrent ? palette.AccentBrush
                    : isFuture ? palette.TextMutedBrush
                    : palette.TextSecondaryBrush,
                IsEnabled = !isFuture,
                Opacity = isFuture ? 0.45 : 1,
            };
            var targetMonth = month;
            button.Click += (_, _) =>
            {
                _selectedMonth = targetMonth;
                _selectedYear = _panelYear;
                flyout.Hide();
                BuildUi();
            };
            grid.Children.Add(button);
            Grid.SetColumn(button, month % 3);
            Grid.SetRow(button, month / 3);
        }

        panel.Children.Add(grid);
        panel.Children.Add(BuildCalendarFooter(palette, flyout, monthMode: true));
        return panel;
    }

    private FrameworkElement BuildCalendarHeader(StatsPalette palette, bool isMonthMode, Flyout flyout)
    {
        var title = UiKit.Text(isMonthMode ? _panelYear.ToString() : UiKit.FormatYM(_panelYear, _panelMonth + 1),
            palette.TextPrimary, 14, FontWeights.SemiBold, textAlignment: TextAlignment.Center);

        var prevButton = BuildNavButton(palette, "\uE76B");
        prevButton.Click += (_, _) =>
        {
            if (isMonthMode) _panelYear--;
            else
            {
                _panelMonth--;
                if (_panelMonth < 0) { _panelMonth = 11; _panelYear--; }
            }

            flyout.Content = BuildPickerPanel(palette, flyout);
        };

        var nextButton = BuildNavButton(palette, "\uE76C");
        nextButton.Click += (_, _) =>
        {
            var now = DateTime.Today;
            if (isMonthMode)
            {
                if (_panelYear >= now.Year) return;
                _panelYear++;
            }
            else
            {
                var nextMonth = _panelMonth + 1;
                var nextYear = _panelYear;
                if (nextMonth > 11) { nextMonth = 0; nextYear++; }
                if (nextYear > now.Year || (nextYear == now.Year && nextMonth > now.Month - 1)) return;
                _panelMonth = nextMonth;
                _panelYear = nextYear;
            }

            flyout.Content = BuildPickerPanel(palette, flyout);
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(prevButton);
        header.Children.Add(title);
        Grid.SetColumn(title, 1);
        header.Children.Add(nextButton);
        Grid.SetColumn(nextButton, 2);
        return header;
    }

    private static Button BuildNavButton(StatsPalette palette, string glyph)
        => new()
        {
            Width = 28,
            Height = 28,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Content = new FontIcon { Glyph = glyph, FontSize = 12, Foreground = palette.TextSecondaryBrush },
        };

    private FrameworkElement BuildCalendarFooter(StatsPalette palette, Flyout flyout, bool monthMode)
    {
        var button = new Button
        {
            Content = monthMode ? UiKit.L("Picker_ThisMonth", "本月") : UiKit.L("Picker_Today", "今天"),
            FontSize = 12,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = palette.AccentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Foreground = palette.AccentBrush,
            Padding = new Thickness(16, 5, 16, 5),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0),
        };
        button.Click += (_, _) =>
        {
            var now = DateTime.Today;
            _selectedDate = now;
            _selectedYear = now.Year;
            _selectedMonth = now.Month - 1;
            flyout.Hide();
            BuildUi();
        };
        return button;
    }

    private static void ApplyCalendarStyles(List<CalendarCell> cells, StatsPalette palette)
    {
        foreach (var cell in cells) ApplyCalendarCellStyle(cell, palette, hoveredWeek: false);
    }

    private static void ApplyCalendarCellStyle(CalendarCell cell, StatsPalette palette, bool hoveredWeek)
    {
        var border = cell.Border!;
        var text = (TextBlock)border.Child;

        if (cell.Selected)
        {
            border.Background = palette.AccentBrightBrush;
            text.Foreground = new SolidColorBrush(Colors.White);
            text.FontWeight = FontWeights.SemiBold;
        }
        else if (cell.InSelectedWeek)
        {
            border.Background = palette.AccentAlphaBrush(0x40);
            text.Foreground = palette.AccentBrush;
            text.FontWeight = FontWeights.Normal;
        }
        else if (hoveredWeek && !cell.Disabled)
        {
            border.Background = palette.AccentAlphaBrush(0x1E);
            text.Foreground = palette.AccentBrush;
            text.FontWeight = FontWeights.Normal;
        }
        else
        {
            border.Background = new SolidColorBrush(Colors.Transparent);
            text.Foreground = cell.OtherMonth ? palette.TextMutedBrush : palette.TextSecondaryBrush;
            text.FontWeight = cell.Today ? FontWeights.SemiBold : FontWeights.Normal;
            if (cell.OtherMonth) border.Opacity = 0.4;
        }

        if (cell.Today && !cell.Selected)
        {
            border.BorderBrush = palette.AccentBrush;
            border.BorderThickness = new Thickness(1);
            text.Foreground = palette.AccentBrush;
        }
        else
        {
            border.BorderThickness = new Thickness(0);
        }

        if (cell.Disabled)
        {
            border.Opacity = 0.3;
        }
    }

    private static void ApplyWeekHover(List<CalendarCell> cells, CalendarCell target, StatsPalette palette, bool hovered)
    {
        if (target.Disabled) return;
        var targetMonday = StatsService.GetMonday(target.Date);
        foreach (var cell in cells)
        {
            var sameWeek = StatsService.GetMonday(cell.Date) == targetMonday;
            ApplyCalendarCellStyle(cell, palette, hoveredWeek: hovered && sameWeek && !cell.InSelectedWeek && !cell.Selected);
        }
    }

    private sealed class CalendarCell
    {
        public DateTime Date { get; init; }
        public bool OtherMonth { get; init; }
        public bool Today { get; init; }
        public bool Selected { get; init; }
        public bool Disabled { get; init; }
        public bool InSelectedWeek { get; init; }
        public Border? Border { get; set; }
    }

    #endregion

    #region 概览指标条

    private FrameworkElement BuildStatsBar(StatsPalette palette)
    {
        var stats = ComputeStats();
        var grid = UiKit.EqualColumns(new FrameworkElement[]
        {
            BuildStatCard(palette, UiKit.L("Stat_TotalTime", "总游玩时长"),
                UiKit.FormatHours(stats.TotalHours), UiKit.L("Unit_Hours", "小时"), stats.TotalSub, fontSize: 26),
            BuildStatCard(palette, UiKit.L("Stat_GameCount", "游玩游戏数"),
                stats.GameCount.ToString(), UiKit.L("Unit_Games", "款"), stats.CountSub, fontSize: 26),
            BuildStatCard(palette, UiKit.L("Stat_TopGame", "最常玩游戏"),
                stats.TopGame, null, stats.TopSub, fontSize: 17, tooltip: stats.TopGame),
            BuildStatCard(palette, UiKit.L("Stat_AvgTime", "平均时长"),
                UiKit.FormatHours(stats.AvgHours), UiKit.L("Unit_Hours", "小时"), stats.AvgSub, fontSize: 26),
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

        var content = new StackPanel { Orientation = Orientation.Vertical };
        content.Children.Add(UiKit.Text(label, palette.TextSecondary, 12));
        content.Children.Add(valueRow);
        content.Children.Add(UiKit.Text(sub, palette.TextMuted, 11, margin: new Thickness(0, 6, 0, 0)));
        return UiKit.Card(palette, content, new Thickness(16, 18, 16, 18));
    }

    private sealed class StatsInfo
    {
        public double TotalHours { get; init; }
        public int GameCount { get; init; }
        public string TopGame { get; init; } = "—";
        public double AvgHours { get; init; }
        public string TotalSub { get; init; } = "";
        public string CountSub { get; init; } = "";
        public string TopSub { get; init; } = "";
        public string AvgSub { get; init; } = "";
    }

    private StatsInfo ComputeStats()
    {
        switch (_period)
        {
            case StatsPeriod.Day:
            {
                var todayGames = StatsService.GetDayGames(_snapshot, _selectedDate);
                var totalHours = todayGames.Sum(g => g.Hours);
                var top = todayGames.FirstOrDefault();
                var recent7 = StatsService.GetRecentDays(_selectedDate);
                var avg7 = recent7.Average(d => StatsService.GetDayTotal(_snapshot, d) / 60.0);
                var topSub = top is null
                    ? UiKit.L("Stat_NoPlay", "当日暂无游玩")
                    : $"{UiKit.FormatTime(top.Hours)} · {UiKit.L("Stat_Percent", "占比")} " +
                      $"{Percent(top.Minutes, (int)Math.Round(totalHours * 60))}%";
                return new StatsInfo
                {
                    TotalHours = totalHours,
                    GameCount = todayGames.Count,
                    TopGame = top?.Name ?? "—",
                    AvgHours = avg7,
                    TotalSub = $"{UiKit.FormatYMD(_selectedDate)} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{UiKit.FormatMD(_selectedDate)} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    TopSub = topSub,
                    AvgSub = UiKit.L("Sub_Avg7Days", "近7日日均时长"),
                };
            }
            case StatsPeriod.Week:
            {
                var days = StatsService.GetWeekDays(_selectedDate);
                var gameTotals = StatsService.GetPeriodGames(_snapshot, days);
                var totalMinutes = days.Sum(d => StatsService.GetDayTotal(_snapshot, d));
                var top = gameTotals.FirstOrDefault();
                var periodLabel = UiKit.FormatWeekRange(StatsService.GetMonday(_selectedDate));
                var topSub = top is null
                    ? UiKit.L("Stat_NoPlay", "当日暂无游玩")
                    : $"{UiKit.FormatTime(top.Hours)} · {UiKit.L("Stat_Percent", "占比")} {Percent(top.Minutes, totalMinutes)}%";
                return new StatsInfo
                {
                    TotalHours = totalMinutes / 60.0,
                    GameCount = gameTotals.Count,
                    TopGame = top?.Name ?? "—",
                    AvgHours = totalMinutes / 60.0 / 7,
                    TotalSub = $"{periodLabel} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{periodLabel} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    TopSub = topSub,
                    AvgSub = UiKit.L("Sub_AvgPerDay", "每日平均时长"),
                };
            }
            default:
            {
                var weeks = StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1);
                var totalMinutes = weeks.Sum(w => w.TotalMinutes);
                var merged = new Dictionary<Guid, int>();
                foreach (var week in weeks)
                {
                    foreach (var (id, minutes) in week.GameMinutes)
                    {
                        if (!merged.TryAdd(id, minutes)) merged[id] += minutes;
                    }
                }

                var topEntry = merged.OrderByDescending(kv => kv.Value).FirstOrDefault();
                var topGame = _snapshot.Games.FirstOrDefault(g => g.Uuid == topEntry.Key);
                var periodLabel = UiKit.FormatYM(_selectedYear, _selectedMonth + 1);
                var topSub = topGame is null
                    ? UiKit.L("Stat_NoPlay", "当日暂无游玩")
                    : $"{UiKit.FormatTime(topEntry.Value / 60.0)} · {UiKit.L("Stat_Percent", "占比")} {Percent(topEntry.Value, totalMinutes)}%";
                return new StatsInfo
                {
                    TotalHours = totalMinutes / 60.0,
                    GameCount = merged.Count,
                    TopGame = topGame?.Name.Value ?? "—",
                    AvgHours = weeks.Count > 0 ? totalMinutes / 60.0 / weeks.Count : 0,
                    TotalSub = $"{periodLabel} {UiKit.L("Sub_Cumulative", "累计游玩")}",
                    CountSub = $"{periodLabel} {UiKit.L("Sub_GamesPlayed", "启动过的游戏")}",
                    TopSub = topSub,
                    AvgSub = UiKit.L("Sub_AvgPerWeek", "每周平均时长"),
                };
            }
        }
    }

    private static string Percent(int part, int total)
        => total > 0 ? (part * 100.0 / total).ToString("F1") : "0.0";

    #endregion

    #region 主内容区（图表 + 侧栏）

    private FrameworkElement BuildMainContent(StatsPalette palette)
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(BuildChartCard(palette));
        var sideCard = BuildSideCard(palette);
        root.Children.Add(sideCard);
        Grid.SetColumn(sideCard, 2);
        return root;
    }

    private FrameworkElement BuildChartCard(StatsPalette palette)
    {
        var chartHost = new Grid { Height = 430 };
        if (_period == StatsPeriod.Day)
            chartHost.Children.Add(BuildDayChart(palette));
        else
            chartHost.Children.Add(BuildBarChart(palette));

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.Children.Add(BuildCardHeader(palette,
            _period == StatsPeriod.Day
                ? UiKit.L("Chart_TodayTitle", "今日游戏构成")
                : UiKit.L("Chart_DistTitle", "时长分布"),
            _period == StatsPeriod.Day
                ? UiKit.L("Chart_TodayHint", "点击扇区查看该游戏近7日趋势")
                : UiKit.L("Chart_BarHint", "点击柱形可筛选对应时段的游戏排行")));
        content.Children.Add(chartHost);
        Grid.SetRow(chartHost, 1);

        return UiKit.Card(palette, content, new Thickness(20));
    }

    private FrameworkElement BuildCardHeader(StatsPalette palette, string title, string hint)
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.Children.Add(UiKit.Text(title, palette.TextPrimary, 15, FontWeights.SemiBold));
        var hintText = UiKit.Text(hint, palette.TextMuted, 11);
        root.Children.Add(hintText);
        Grid.SetColumn(hintText, 1);
        return root;
    }

    private FrameworkElement BuildDayChart(StatsPalette palette)
    {
        var todayGames = StatsService.GetDayGames(_snapshot, _selectedDate);
        var totalMinutes = todayGames.Sum(g => g.Minutes);

        var donut = new DonutChart();
        donut.SetData(todayGames, _selectedGameId, palette,
            UiKit.FormatHours(totalMinutes / 60.0), UiKit.L("Chart_DayCenterSub", "今日总时长（小时）"));
        donut.SegmentClicked += (_, id) =>
        {
            _selectedGameId = _selectedGameId == id ? null : id;
            BuildUi();
        };

        // 图例
        var legendItems = new List<Border>();
        for (var i = 0; i < todayGames.Count; i++)
        {
            var game = todayGames[i];
            var index = i;
            var chip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            chip.Children.Add(UiKit.Dot(StatsTheme.SeriesColor(index)));
            chip.Children.Add(UiKit.Text(game.Name, palette.TextPrimary, 12, trimming: TextTrimming.CharacterEllipsis, maxWidth: 110));
            chip.Children.Add(UiKit.Text(Percent(game.Minutes, totalMinutes) + "%", palette.TextMuted, 11));

            var container = new Border
            {
                Background = palette.BgSecondaryBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = _selectedGameId == game.Id ? palette.AccentBrush : new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(1),
                Child = chip,
            };
            var id = game.Id;
            container.Tapped += (_, _) =>
            {
                _selectedGameId = _selectedGameId == id ? null : id;
                BuildUi();
            };
            legendItems.Add(container);
        }

        var legend = new ItemsRepeater
        {
            ItemsSource = legendItems,
            Layout = new UniformGridLayout
            {
                MinItemWidth = 160,
                MinItemHeight = 28,
                MinRowSpacing = 6,
                MinColumnSpacing = 8,
            },
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(donut);
        root.Children.Add(legend);
        Grid.SetRow(legend, 1);
        return root;
    }

    private FrameworkElement BuildBarChart(StatsPalette palette)
    {
        var chart = new BarChart();
        List<string> labels;
        List<double> values;
        List<string> tooltips;

        if (_period == StatsPeriod.Week)
        {
            var days = StatsService.GetWeekDays(_selectedDate);
            labels = days.Select(UiKit.FormatDayLabel).ToList();
            values = days.Select(d => StatsService.GetDayTotal(_snapshot, d) / 60.0).ToList();
            tooltips = days.Select((d, i) => BuildBarTooltip(UiKit.FormatDayLabel(d), StatsService.GetPeriodGames(_snapshot, days, i))).ToList();
        }
        else
        {
            var weeks = StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1);
            labels = weeks.Select(w => UiKit.FormatWeekLabel(_selectedMonth + 1, w.WeekNum)).ToList();
            values = weeks.Select(w => w.TotalMinutes / 60.0).ToList();
            tooltips = weeks.Select(w => BuildBarTooltip(
                UiKit.FormatWeekLabel(_selectedMonth + 1, w.WeekNum),
                w.GameMinutes.Where(kv => kv.Value > 0)
                    .OrderByDescending(kv => kv.Value)
                    .Take(3)
                    .Select(kv => (
                        Name: _snapshot.Games.FirstOrDefault(g => g.Uuid == kv.Key)?.Name.Value ?? "?",
                        Minutes: kv.Value))
                    .ToList())).ToList();
        }

        chart.SetData(labels, values, palette, tooltips, _selectedIndex);
        chart.BarClicked += (_, index) =>
        {
            _selectedIndex = _selectedIndex == index ? null : index;
            BuildUi();
        };
        return chart;
    }

    private static string BuildBarTooltip(string label, List<GamePeriodTime> topGames)
    {
        var text = label;
        foreach (var game in topGames.Take(3))
            text += $"\n{game.Name}  {UiKit.FormatTimeShort(game.Hours)}";
        return text;
    }

    private static string BuildBarTooltip(string label, List<(string Name, int Minutes)> topGames)
    {
        var text = label;
        foreach (var game in topGames)
            text += $"\n{game.Name}  {UiKit.FormatTimeShort(game.Minutes / 60.0)}";
        return text;
    }

    #endregion

    #region 侧栏（排行 / 趋势）

    private FrameworkElement BuildSideCard(StatsPalette palette)
        => _period == StatsPeriod.Day ? BuildTrendPanel(palette) : BuildRankPanel(palette);

    private FrameworkElement BuildRankPanel(StatsPalette palette)
    {
        List<GamePeriodTime> games;
        if (_period == StatsPeriod.Week)
        {
            var days = StatsService.GetWeekDays(_selectedDate);
            games = StatsService.GetPeriodGames(_snapshot, days, _selectedIndex);
        }
        else
        {
            games = StatsService.GetMonthGames(_snapshot, _selectedYear, _selectedMonth + 1, _selectedIndex);
        }

        var sorted = UiKit.SortGames(games, _sort);
        var maxMinutes = sorted.Count > 0 ? sorted.Max(g => g.Minutes) : 0;
        var grandTotal = sorted.Sum(g => g.Minutes);

        // 头部
        var header = new StackPanel();
        var headerTop = new Grid();
        headerTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerTop.Children.Add(UiKit.Text(UiKit.L("Rank_Title", "游戏时长排行"), palette.TextPrimary, 15, FontWeights.SemiBold));
        var sortToggle = UiKit.SortToggle(palette,
            new[] { UiKit.L("Rank_SortTime", "按时长"), UiKit.L("Rank_SortName", "按名称") },
            _sort == RankSort.Time ? 0 : 1,
            index =>
            {
                _sort = index == 0 ? RankSort.Time : RankSort.Name;
                _data.RankSort = _sort == RankSort.Time ? "time" : "name";
                BuildUi();
            });
        headerTop.Children.Add(sortToggle);
        Grid.SetColumn(sortToggle, 1);
        header.Children.Add(headerTop);

        // 筛选状态
        if (_selectedIndex is { } idx)
        {
            var filterLabel = _period == StatsPeriod.Week
                ? UiKit.FormatDayLabel(StatsService.GetWeekDays(_selectedDate)[idx])
                : UiKit.FormatWeekLabel(_selectedMonth + 1,
                    StatsService.GetMonthWeeks(_snapshot, _selectedYear, _selectedMonth + 1)[idx].WeekNum);
            var filterRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            filterRow.Children.Add(UiKit.Text($"{UiKit.L("Rank_Filtered", "已筛选")}：{filterLabel}", palette.Accent, 11.5));
            var clearButton = new Button
            {
                Content = UiKit.L("Rank_ClearFilter", "清除筛选"),
                FontSize = 11.5,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = palette.TextMutedBrush,
                Padding = new Thickness(0),
            };
            clearButton.Click += (_, _) =>
            {
                _selectedIndex = null;
                BuildUi();
            };
            filterRow.Children.Add(clearButton);
            header.Children.Add(filterRow);
        }

        // 列表
        FrameworkElement body;
        if (sorted.Count == 0)
        {
            body = UiKit.EmptyState(UiKit.L("Rank_Empty", "该时段暂无游戏记录"), palette.TextMuted);
        }
        else
        {
            var list = new StackPanel();
            for (var i = 0; i < sorted.Count; i++)
            {
                list.Children.Add(BuildRankItem(palette, sorted[i], i, maxMinutes, grandTotal));
            }

            body = new ScrollViewer
            {
                Content = list,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
        }

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(header);
        root.Children.Add(body);
        Grid.SetRow(body, 1);
        return WrapSideCard(palette, root);
    }

    private FrameworkElement BuildRankItem(StatsPalette palette, GamePeriodTime game, int index, int maxMinutes, int grandTotal)
    {
        var barPercent = maxMinutes > 0 ? game.Minutes * 100.0 / maxMinutes : 0;
        var sharePercent = grandTotal > 0 ? game.Minutes * 100.0 / grandTotal : 0;

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

        var rankText = UiKit.Text((index + 1).ToString(), index < 3 ? palette.Accent : palette.TextMuted, 12,
            FontWeights.SemiBold, textAlignment: TextAlignment.Center);
        row.Children.Add(rankText);

        var icon = UiKit.GameIcon(game, index);
        row.Children.Add(icon);
        Grid.SetColumn(icon, 1);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        info.Children.Add(UiKit.Text(game.Name, palette.TextPrimary, 13.5, trimming: TextTrimming.CharacterEllipsis));
        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 3, 0, 0) };
        meta.Children.Add(UiKit.Text(UiKit.FormatTime(game.Hours), palette.TextSecondary, 11.5));
        meta.Children.Add(UiKit.Text(sharePercent.ToString("F1") + "%", palette.TextMuted, 10.5));
        info.Children.Add(meta);
        row.Children.Add(info);
        Grid.SetColumn(info, 2);

        var barTrack = new Border
        {
            Background = palette.BgSecondaryBrush,
            CornerRadius = new CornerRadius(2),
            Width = 56,
            Height = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        barTrack.Child = new Border
        {
            Background = new SolidColorBrush(StatsTheme.SeriesColor(index)),
            CornerRadius = new CornerRadius(2),
            Width = Math.Max(2, 56 * barPercent / 100),
            Height = 4,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        row.Children.Add(barTrack);
        Grid.SetColumn(barTrack, 3);

        var container = new Border { Padding = new Thickness(0, 9, 0, 9), CornerRadius = new CornerRadius(4) };
        UiKit.AttachHover(container, new SolidColorBrush(Colors.Transparent), palette.HoverBrush);
        container.Child = row;
        return container;
    }

    private FrameworkElement BuildTrendPanel(StatsPalette palette)
    {
        var selectedGame = _snapshot.Games.FirstOrDefault(g => g.Uuid == _selectedGameId);
        var recent7 = StatsService.GetRecentDays(_selectedDate);
        var trend = StatsService.GetTrendDays(_snapshot, recent7, _selectedGameId);
        var total7 = trend.Sum(t => t.Minutes);
        var avg7 = trend.Average(t => t.Minutes);
        var maxValue = trend.Count > 0 ? trend.Max(t => t.Minutes) : 0;
        var maxIndex = trend.FindIndex(t => t.Minutes == maxValue);
        var todayValue = trend.LastOrDefault()?.Minutes ?? 0;

        // 头部
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = selectedGame is null
            ? UiKit.L("Trend_Title", "近7日游玩趋势")
            : $"{selectedGame.Name.Value} · {UiKit.L("Trend_Title", "近7日游玩趋势")}";
        header.Children.Add(UiKit.Text(title, palette.TextPrimary, 15, FontWeights.SemiBold, trimming: TextTrimming.CharacterEllipsis));
        if (selectedGame is not null)
        {
            var backButton = new Button
            {
                Content = $"{UiKit.L("Trend_Back", "返回总览")} ←",
                FontSize = 12,
                Background = palette.BgSecondaryBrush,
                BorderBrush = palette.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Foreground = palette.TextSecondaryBrush,
                Padding = new Thickness(8, 4, 8, 4),
            };
            backButton.Click += (_, _) =>
            {
                _selectedGameId = null;
                BuildUi();
            };
            header.Children.Add(backButton);
            Grid.SetColumn(backButton, 1);
        }

        // 迷你图
        var miniChart = new BarChart { Height = 110 };
        miniChart.SetData(
            trend.Select(t => UiKit.FormatMD(t.Date)).ToList(),
            trend.Select(t => t.Hours).ToList(),
            palette,
            trend.Select(t => $"{UiKit.FormatMD(t.Date)} {UiKit.WeekDayName(t.Date.DayOfWeek)}\n{UiKit.FormatTime(t.Hours)}").ToList(),
            highlightIndex: 6,
            highlightColor: selectedGame is not null ? (Color?)StatsTheme.SeriesColor(selectedGame.Uuid) : null,
            compact: true);

        // 摘要 2×2
        var summary = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        summary.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var maxDayLabel = maxIndex >= 0 && maxIndex < trend.Count ? UiKit.FormatMD(trend[maxIndex].Date) : "—";
        AddSummaryItem(summary, palette, UiKit.L("Trend_Total7", "7日总计"), UiKit.FormatTime(total7 / 60.0), 0, 0);
        AddSummaryItem(summary, palette, UiKit.L("Trend_Avg", "日均"), UiKit.FormatTime(avg7 / 60.0), 1, 0);
        AddSummaryItem(summary, palette, UiKit.L("Trend_MaxDay", "最高单日"),
            $"{maxDayLabel} · {UiKit.FormatTimeShort(maxValue / 60.0)}", 0, 1);
        AddSummaryItem(summary, palette, UiKit.L("Trend_ActiveDays", "活跃天数"),
            $"{trend.Count(t => t.Minutes > 0)} / 7 {UiKit.L("Unit_Days", "天")}", 1, 1);

        // 每日列表
        var list = new StackPanel();
        for (var i = 0; i < trend.Count; i++)
        {
            list.Children.Add(BuildTrendRow(palette, trend[i], maxValue, todayValue, i == trend.Count - 1,
                selectedGame is not null ? (Color?)StatsTheme.SeriesColor(selectedGame.Uuid) : null));
        }

        var body = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(header);
        root.Children.Add(miniChart);
        Grid.SetRow(miniChart, 1);
        root.Children.Add(summary);
        Grid.SetRow(summary, 2);
        root.Children.Add(body);
        Grid.SetRow(body, 3);
        return WrapSideCard(palette, root);
    }

    private static void AddSummaryItem(Grid grid, StatsPalette palette, string label, string value, int column, int row)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 6) };
        panel.Children.Add(UiKit.Text(label, palette.TextMuted, 10.5));
        panel.Children.Add(UiKit.Text(value, palette.TextPrimary, 14.5, FontWeights.SemiBold, margin: new Thickness(0, 2, 0, 0)));
        grid.Children.Add(panel);
        Grid.SetColumn(panel, column);
        Grid.SetRow(panel, row);
    }

    private FrameworkElement BuildTrendRow(StatsPalette palette, TrendDay day, int maxValue, int todayValue, bool isLast, Color? gameColor)
    {
        var barPercent = maxValue > 0 ? day.Minutes * 100.0 / maxValue : 0;
        var deltaText = UiKit.L("Trend_Flat", "持平");
        var deltaColor = palette.TextMuted;
        if (isLast)
        {
            deltaText = UiKit.L("Trend_Today", "当天");
        }
        else
        {
            var diff = day.Minutes - todayValue;
            if (diff > 0)
            {
                deltaText = "+" + UiKit.FormatTimeShort(diff / 60.0);
                deltaColor = palette.Success;
            }
            else if (diff < 0)
            {
                deltaText = UiKit.FormatTimeShort(diff / 60.0);
                deltaColor = palette.Danger;
            }
        }

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        datePanel.Children.Add(UiKit.Text(UiKit.FormatMD(day.Date), palette.TextSecondary, 12.5));
        datePanel.Children.Add(UiKit.Text(UiKit.WeekDayName(day.Date.DayOfWeek), palette.TextMuted, 10.5));
        row.Children.Add(datePanel);

        var barTrack = new Border
        {
            Background = palette.BgSecondaryBrush,
            CornerRadius = new CornerRadius(3),
            Height = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
        };
        // 填充宽度用百分比星列实现，随轨道宽度伸缩（旧实现把 0-100 的百分数当像素值用了）
        var fillPercent = Math.Clamp(barPercent, 0, 100);
        var fillHost = new Grid();
        fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fillPercent, GridUnitType.Star) });
        fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - fillPercent, GridUnitType.Star) });
        fillHost.Children.Add(new Border
        {
            Background = isLast
                ? new SolidColorBrush(gameColor ?? palette.Accent)
                : palette.AccentBrush,
            CornerRadius = new CornerRadius(3),
            Height = 6,
        });
        barTrack.Child = fillHost;
        row.Children.Add(barTrack);
        Grid.SetColumn(barTrack, 1);

        var timeText = UiKit.Text(UiKit.FormatTimeShort(day.Hours), palette.TextPrimary, 12.5,
            FontWeights.Medium, textAlignment: TextAlignment.Right);
        row.Children.Add(timeText);
        Grid.SetColumn(timeText, 2);

        var deltaTextBlock = UiKit.Text(deltaText, deltaColor, 10.5, textAlignment: TextAlignment.Right);
        row.Children.Add(deltaTextBlock);
        Grid.SetColumn(deltaTextBlock, 3);

        var container = new Border
        {
            Padding = new Thickness(0, 8, 0, 8),
            CornerRadius = new CornerRadius(4),
            Background = isLast ? palette.AccentAlphaBrush(0x12) : new SolidColorBrush(Colors.Transparent),
        };
        if (!isLast) UiKit.AttachHover(container, new SolidColorBrush(Colors.Transparent), palette.HoverBrush);
        container.Child = row;
        return container;
    }

    /// <summary>侧栏卡片外壳（与图表卡片等高）</summary>
    private static FrameworkElement WrapSideCard(StatsPalette palette, UIElement content)
        => UiKit.Card(palette, content, new Thickness(18, 16, 18, 16));

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
