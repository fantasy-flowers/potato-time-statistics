using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase.Controls;

/// <summary>
/// 插件主页面：顶部模块切换（游戏时长统计 / 游戏统计）+ 数据刷新按钮。
/// 宿主通过 Activator.CreateInstance 创建页面，因此必须保留无参构造函数。
/// </summary>
public sealed class StatsPage : Page
{
    private readonly PluginData _data;
    private readonly PlaytimeStatsView _playtimeView;
    private readonly GameStatsView _gameStatsView;
    private Button? _playtimeButton;
    private Button? _statsButton;
    private Border? _moduleSwitchBorder;

    public StatsPage() : this(Plugin.CurrentData)
    {
    }

    public StatsPage(PluginData data)
    {
        _data = data;
        _playtimeView = new PlaytimeStatsView(data);
        _gameStatsView = new GameStatsView(data);
        ActualThemeChanged += (_, _) => BuildPage();
        BuildPage();
    }

    public void RefreshData()
    {
        _playtimeView.RefreshData();
        _gameStatsView.RefreshData();
    }

    private void BuildPage()
    {
        var palette = StatsTheme.For(this);
        Content = null;

        var moduleSwitch = BuildModuleSwitch(palette);
        var refreshButton = BuildRefreshButton(palette);

        var topBar = new Grid();
        topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topBar.Children.Add(moduleSwitch);
        topBar.Children.Add(refreshButton);
        Grid.SetColumn(refreshButton, 1);

        var body = new StackPanel { Spacing = 0 };
        body.Children.Add(topBar);
        body.Children.Add(_playtimeView);
        _playtimeView.Margin = new Thickness(0, 20, 0, 0);
        body.Children.Add(_gameStatsView);
        _gameStatsView.Margin = new Thickness(0, 20, 0, 0);
        _gameStatsView.Visibility = Visibility.Collapsed;

        var container = new Grid { MaxWidth = 1400, HorizontalAlignment = HorizontalAlignment.Stretch };
        container.Children.Add(body);

        var scrollViewer = new ScrollViewer
        {
            Content = container,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(24),
        };

        Content = scrollViewer;
    }

    private FrameworkElement BuildModuleSwitch(StatsPalette palette)
    {
        _playtimeButton = BuildModuleButton(palette, UiKit.L("Module_Playtime", "游戏时长统计"));
        _statsButton = BuildModuleButton(palette, UiKit.L("Module_Stats", "游戏统计"));
        _playtimeButton.Click += (_, _) => SwitchModule(palette, playtime: true);
        _statsButton.Click += (_, _) => SwitchModule(palette, playtime: false);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(_playtimeButton);
        panel.Children.Add(_statsButton);

        _moduleSwitchBorder = new Border { Child = panel };
        StyleModuleButtons(palette, playtime: true);
        return _moduleSwitchBorder;
    }

    private void SwitchModule(StatsPalette palette, bool playtime)
    {
        _playtimeView.Visibility = playtime ? Visibility.Visible : Visibility.Collapsed;
        _gameStatsView.Visibility = playtime ? Visibility.Collapsed : Visibility.Visible;
        StyleModuleButtons(palette, playtime);
        if (playtime)
        {
            _playtimeView.Rebuild();
        }
        else
        {
            _gameStatsView.Rebuild();
        }
    }

    private void StyleModuleButtons(StatsPalette palette, bool playtime)
    {
        if (_playtimeButton is null || _statsButton is null) return;
        StyleModuleButton(_playtimeButton, palette, playtime);
        StyleModuleButton(_statsButton, palette, !playtime);
    }

    private static void StyleModuleButton(Button button, StatsPalette palette, bool active)
    {
        button.Background = active ? palette.AccentDarkBrush : palette.BgSecondaryBrush;
        button.BorderBrush = active ? palette.AccentBrush : palette.BorderBrush;
        button.Foreground = active ? new SolidColorBrush(Colors.White) : palette.TextSecondaryBrush;
    }

    private static Button BuildModuleButton(StatsPalette palette, string text)
        => new()
        {
            Content = text,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 8, 20, 8),
        };

    private Button BuildRefreshButton(StatsPalette palette)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        content.Children.Add(new FontIcon { Glyph = "\uE72C", FontSize = 13, Foreground = palette.TextSecondaryBrush });
        content.Children.Add(UiKit.Text(UiKit.L("Refresh", "刷新数据"), palette.TextSecondary, 12.5, FontWeights.Medium));

        var button = new Button
        {
            Content = content,
            Background = palette.BgSecondaryBrush,
            BorderBrush = palette.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 7, 12, 7),
        };
        button.Click += (_, _) => RefreshData();
        return button;
    }
}
