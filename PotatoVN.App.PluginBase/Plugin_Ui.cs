using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Models.Plugin;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.PluginBase.Controls;
using PotatoVN.App.PluginBase.Controls.Prefabs;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase;

public partial class Plugin
{
    private bool _uiInit;

    private static string L(string key, string fallback) => PluginLocalization.GetStringOr(key, fallback);

    private void InitUi()
    {
        if (_uiInit) return;
        _hostApi.RegisterSidebarButton(new SidebarButtonInfo
        {
            Id = "open-stats",
            Text = L("Sidebar_Text", "游戏统计"),
            Placement = SidebarButtonPlacement.Menu,
            FluentGlyph = "&#xE9D9;",
            FallbackGlyph = "\uE9D9",
        }, () =>
        {
            _hostApi.NavigateTo(typeof(StatsPage), L("Page_Title", "游戏统计"));
            return Task.CompletedTask;
        });
        _uiInit = true;
    }

    public FrameworkElement CreateSettingUi()
    {
        var panel = new StdStackPanel();

        var periodBox = BuildPreferenceBox(_data.DefaultPeriod,
            new[] { ("day", L("Period_Day", "日")), ("week", L("Period_Week", "周")), ("month", L("Period_Month", "月")) },
            value => _data.DefaultPeriod = value);
        var sortBox = BuildPreferenceBox(_data.RankSort,
            new[] { ("time", L("Rank_SortTime", "按时长")), ("name", L("Rank_SortName", "按名称")) },
            value => _data.RankSort = value);
        var tabBox = BuildPreferenceBox(_data.DistTab,
            new[]
            {
                ("status", L("Stats_Tab_Status", "游玩状态")),
                ("engine", L("Stats_Tab_Engine", "游戏引擎")),
                ("developer", L("Stats_Tab_Developer", "制作公司")),
            },
            value => _data.DistTab = value);

        panel.Children.Add(new StdSetting(
            L("Setting_Period_Title", "默认统计维度"),
            L("Setting_Period_Desc", "打开插件时默认展示的时长统计维度"),
            periodBox));
        panel.Children.Add(new StdSetting(
            L("Setting_Sort_Title", "排行默认排序"),
            L("Setting_Sort_Desc", "游戏时长排行列表的默认排序方式"),
            sortBox));
        panel.Children.Add(new StdSetting(
            L("Setting_Tab_Title", "分布默认标签"),
            L("Setting_Tab_Desc", "游戏统计模块默认展示的分布标签"),
            tabBox));
        return panel;
    }

    private static ComboBox BuildPreferenceBox(string current, IReadOnlyList<(string Value, string Label)> options,
        Action<string> onChanged)
    {
        var box = new ComboBox { MinWidth = 140, HorizontalAlignment = HorizontalAlignment.Right };
        var index = 0;
        for (var i = 0; i < options.Count; i++)
        {
            box.Items.Add(new ComboBoxItem { Content = options[i].Label });
            if (options[i].Value == current) index = i;
        }

        box.SelectedIndex = index;
        box.SelectionChanged += (_, _) =>
        {
            if (box.SelectedIndex >= 0 && box.SelectedIndex < options.Count)
                onChanged(options[box.SelectedIndex].Value);
        };
        return box;
    }
}
