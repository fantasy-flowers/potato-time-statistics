using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PotatoVN.App.PluginBase.Helper;
using PotatoVN.App.PluginBase.Models;
using PotatoVN.App.PluginBase.ViewModels;

namespace PotatoVN.App.PluginBase.Controls;

public sealed partial class PlayTimeStatsPage : Page
{
    public PlayTimeStatsViewModel ViewModel { get; } = new();

    public bool ShowFilterHint => !string.IsNullOrEmpty(ViewModel.FilterHint);
    public Visibility NoDataVisibility => ViewModel.HasData ? Visibility.Collapsed : Visibility.Visible;

    public PlayTimeStatsPage()
    {
        XamlResourceLocatorFactory.PluginControlInit(ref _contentLoaded, this);
        InitializeComponent();
        ViewModel.PropertyChanged += (_, _) => UpdateBindings();
        Loaded += PlayTimeStatsPage_Loaded;
    }

    private void PlayTimeStatsPage_Loaded(object sender, RoutedEventArgs e)
    {
        TitleText.Text = "PlayTimeStats_Title".GetLoc("Play Time Stats");
        DayBtn.Content = "PlayTimeStats_Day".GetLoc("Day");
        WeekBtn.Content = "PlayTimeStats_Week".GetLoc("Week");
        MonthBtn.Content = "PlayTimeStats_Month".GetLoc("Month");
        TotalPlayTimeLabel.Text = "PlayTimeStats_TotalPlayTime".GetLoc("Total Play Time");
        TimeDistributionLabel.Text = "PlayTimeStats_TimeDistribution".GetLoc("Time Distribution");
        GameRankingLabel.Text = "PlayTimeStats_GameRanking".GetLoc("Game Ranking");
        NoDataText.Text = "PlayTimeStats_NoData".GetLoc("No play data yet. Go play a game!");
        ViewModel.LoadData();
        UpdateTabStyles();
    }

    private void UpdateBindings()
    {
        Bindings.Update();
        UpdateTabStyles();
    }

    private void UpdateTabStyles()
    {
        var activeBg = Application.Current.Resources["AccentFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;
        var inactiveBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        DayBtn.Background = ViewModel.SelectedPeriod == TimePeriod.Day ? activeBg : inactiveBg;
        WeekBtn.Background = ViewModel.SelectedPeriod == TimePeriod.Week ? activeBg : inactiveBg;
        MonthBtn.Background = ViewModel.SelectedPeriod == TimePeriod.Month ? activeBg : inactiveBg;
    }

    private void DayBtn_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchPeriodCommand.Execute(TimePeriod.Day);
    private void WeekBtn_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchPeriodCommand.Execute(TimePeriod.Week);
    private void MonthBtn_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchPeriodCommand.Execute(TimePeriod.Month);

    private void BarRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is BarChartItem bar)
        {
            ViewModel.SelectBar(bar);
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e) => ViewModel.ClearFilterCommand.Execute(null);
}