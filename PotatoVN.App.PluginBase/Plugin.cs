using System;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Contracts;
using GalgameManager.WinApp.Base.Contracts.PluginUi;
using GalgameManager.WinApp.Base.Models;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase
{
    public partial class Plugin : IPlugin, IPluginSetting
    {
        public static IPotatoVnApi HostApi { get; private set; } = null!;
        /// <summary>当前插件数据（无参页面构造函数用）</summary>
        public static PluginData CurrentData { get; private set; } = new();
        private IPotatoVnApi _hostApi = null!;
        private PluginData _data = new ();

        public PluginInfo Info { get; } = new()
        {
            Id = new Guid("9c3f7d21-4b8a-4e6c-8f2d-7a5b1e0c9d43"),
            Name = "统计",
            Description = "游戏时长统计与游戏库分析：按日/周/月查看游玩时长、游戏排行与近7日趋势，" +
                          "并提供游戏库分布、总时长排行与年度游玩强度热力图。",
        };

        public async Task InitializeAsync(IPotatoVnApi hostApi)
        {
            _hostApi = hostApi;
            HostApi = hostApi;
            XamlResourceLocatorFactory.PackagePath = _hostApi.GetPluginPath();
            PluginLocalization.Initialize(hostApi);
            ResourceLoader.Initialize();
            var dataJson = await _hostApi.GetDataAsync();
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                try
                {
                    _data = System.Text.Json.JsonSerializer.Deserialize<PluginData>(dataJson) ?? new PluginData();
                }
                catch
                {
                    _data = new PluginData();
                }
            }
            _data.Normalize();
            CurrentData = _data;
            _data.PropertyChanged += (_, _) => SaveData();
            InitUi();
        }

        public Task OnUninstallAsync(bool deleteData, Action<TimeSpan> extendWaitHandler, CancellationToken cts)
        {
            if (cts.IsCancellationRequested) return Task.FromCanceled(cts);
            ResourceLoader.Unload();
            return Task.CompletedTask;
        }

        private void SaveData()
        {
            var dataJson = System.Text.Json.JsonSerializer.Serialize(_data);
            _ = _hostApi.SaveDataAsync(dataJson);
        }

        protected Guid Id => Info.Id;
    }
}
