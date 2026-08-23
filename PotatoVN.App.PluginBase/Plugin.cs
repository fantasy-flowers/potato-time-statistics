using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Contracts;
using GalgameManager.WinApp.Base.Contracts.PluginUi;
using GalgameManager.WinApp.Base.Models;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;

namespace PotatoVN.App.PluginBase
{
    public partial class Plugin : IPlugin, IPluginSetting
    {
        public static IPotatoVnApi HostApi { get; private set; } = null!;
        private IPotatoVnApi _hostApi = null!;
        private PluginData _data = new ();

        public PluginInfo Info { get; } = new()
        {
            Id = new Guid("23eeebfe-a1be-420a-a4c3-46bfea99e0ab"),
            Name = "PlayTimeStats",
            Description = "Game play time statistics with bar chart visualization and game ranking.",
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

        public async Task DevReportInfo(Exception? ex, string? msg)
        {
            var body = new
            {
                message = msg ?? ex?.Message,
                stack_trace = ex?.StackTrace,
            };
            using var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            try
            {
                await new HttpClient().PostAsync($"https://plugin.potatovn.net/api/vibe/plugins/{Id}/runtime-errors", content);
            }
            catch { /* ignore */ }
        }
    }
}
