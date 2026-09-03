using CommunityToolkit.Mvvm.ComponentModel;

namespace PotatoVN.App.PluginBase.Models;

/// <summary>
/// 插件持久化数据：仅保存用户偏好，统计数据每次从宿主 GetAllGames() 快照实时计算。
/// Version 字段用于将来数据结构变化时的兼容性迁移。
/// </summary>
public partial class PluginData : ObservableRecipient
{
    public int Version { get; set; } = 1;

    /// <summary>时长统计模块默认维度：day / week / month</summary>
    [ObservableProperty] private string _defaultPeriod = "day";

    /// <summary>游戏排行默认排序：time / name</summary>
    [ObservableProperty] private string _rankSort = "time";

    /// <summary>游戏统计模块默认分布标签：status / engine / developer</summary>
    [ObservableProperty] private string _distTab = "status";

    /// <summary>数据规范化：把不合法的持久化值修正为默认值</summary>
    public void Normalize()
    {
        if (DefaultPeriod is not ("day" or "week" or "month")) DefaultPeriod = "day";
        if (RankSort is not ("time" or "name")) RankSort = "time";
        if (DistTab is not ("status" or "engine" or "developer")) DistTab = "status";
    }
}
