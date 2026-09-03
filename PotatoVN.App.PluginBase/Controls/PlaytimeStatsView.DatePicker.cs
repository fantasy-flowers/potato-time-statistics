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
/// 日期选择器：日历面板（周模式）+ 月份面板（月模式），含导航、今天/本月快捷按钮与周悬停高亮。
/// </summary>
public sealed partial class PlaytimeStatsView
{
    #region 日期选择器

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
}
