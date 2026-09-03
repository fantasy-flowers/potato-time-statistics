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
/// 游戏分布：游玩状态 / 游戏引擎 / 制作公司 三个维度的分布卡片（含占比条与 tab 切换）。
/// </summary>
public sealed partial class GameStatsView
{
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
}
