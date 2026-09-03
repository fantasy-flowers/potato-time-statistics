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
        // 共享视图重新挂载前必须先从旧父级移除，否则抛 COMException 0x800F1000
        // （Element is already the child of another element）
        DetachFromParent(_playtimeView);
        DetachFromParent(_gameStatsView);
        body.Children.Add(_playtimeView);
        _playtimeView.Margin = new Thickness(0, 20, 0, 0);
        body.Children.Add(_gameStatsView);
        _gameStatsView.Margin = new Thickness(0, 20, 0, 0);
        _gameStatsView.Visibility = Visibility.Collapsed;

        // 容器本身撑满视口；内容列用 Star+MaxWidth=1400 封顶：
        // 若直接给容器设 MaxWidth+Stretch，窗口超过 1400 后元素被截断时按"居中"排列，
        // 导致窗口越大整体内容越往右漂。改为列级 MaxWidth 后内容靠左封顶，超宽部分留在右侧。
        var container = new Grid();
        container.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
            MaxWidth = 1400,
        });
        container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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

    /// <summary>把元素从当前逻辑父级上摘下来（Content=null 只断开根节点，孙级仍保留父级引用）</summary>
    private static void DetachFromParent(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Border border:
                border.Child = null;
                break;
            case ContentControl contentControl:
                contentControl.Content = null;
                break;
        }
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
