using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Contracts.PluginUi;
using GalgameManager.WinApp.Base.Models.Plugin;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.PluginBase.Controls;
using PotatoVN.App.PluginBase.Helper;

namespace PotatoVN.App.PluginBase;

public partial class Plugin : IPluginSetting
{
    private bool _uiInit;

    private void InitUi()
    {
        if (_uiInit) return;
        _hostApi.RegisterSidebarButton(new SidebarButtonInfo
        {
           Id = "play-time-stats",
           Text = "PlayTimeStats_SidebarButton".GetLoc("Play Time"),
           Placement = SidebarButtonPlacement.Menu,
           FluentGlyph = "&#xE7BC;",
           FallbackGlyph = "\uE7BC",
        }, () =>
        {
            _hostApi.NavigateTo(typeof(PlayTimeStatsPage), "PlayTimeStats_Title".GetLoc("Play Time Stats"));
            return Task.CompletedTask;
        });
        _uiInit = true;
    }

    public FrameworkElement CreateSettingUi() => new UserControl1(_data);
}