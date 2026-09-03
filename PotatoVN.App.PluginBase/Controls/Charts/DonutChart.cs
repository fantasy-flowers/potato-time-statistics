using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PotatoVN.App.PluginBase.Models;
using Windows.Foundation;

namespace PotatoVN.App.PluginBase.Controls.Charts;

/// <summary>
/// 原生环形图（WinUI 自绘，无外部图表依赖）。
/// 点击扇区触发 <see cref="SegmentClicked"/>；选中项高亮、其余半透明。
/// </summary>
internal sealed class DonutChart : Grid
{
    /// <summary>扇区被点击（参数为游戏 Id）</summary>
    public event EventHandler<Guid>? SegmentClicked;

    public string CenterLabel { get; set; } = string.Empty;
    public string CenterSubLabel { get; set; } = string.Empty;

    private List<GamePeriodTime> _items = new();
    private Guid? _selectedId;
    private StatsPalette _palette = StatsTheme.For(ElementTheme.Dark);
    private double _totalMinutes;

    public DonutChart()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        SizeChanged += (_, _) => Render();
    }

    public void SetData(List<GamePeriodTime> items, Guid? selectedId, StatsPalette palette,
        string centerLabel, string centerSubLabel)
    {
        _items = items;
        _selectedId = selectedId;
        _palette = palette;
        CenterLabel = centerLabel;
        CenterSubLabel = centerSubLabel;
        _totalMinutes = items.Sum(i => (long)i.Minutes);
        Render();
    }

    private void Render()
    {
        Children.Clear();
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 20 || height <= 20)
        {
            if (_items.Count == 0)
                Children.Add(UiKit.EmptyState(UiKit.L("Chart_NoData", "暂无游戏记录"), _palette.TextMuted));
            return;
        }

        if (_items.Count == 0 || _totalMinutes <= 0)
        {
            Children.Add(UiKit.EmptyState(UiKit.L("Chart_NoData", "暂无游戏记录"), _palette.TextMuted));
            return;
        }

        var center = new Point(width / 2, height / 2);
        var outerRadius = Math.Min(width, height) / 2 - 16;
        var innerRadius = outerRadius * 0.62;
        if (innerRadius < 10) innerRadius = Math.Max(2, outerRadius - 14);

        // 从 12 点方向开始顺时针
        var startAngle = -90.0;
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var sweep = item.Minutes / (double)_totalMinutes * 360.0;

            // 单扇区覆盖整圆时拆成两段 180°（ArcSegment 不能画整圆）
            var segments = _items.Count == 1
                ? new[] { (startAngle, 180.0), (startAngle + 180.0, 180.0) }
                : new[] { (startAngle, sweep) };

            foreach (var (segmentStart, segmentSweep) in segments)
            {
                var segment = BuildRingSegment(center, innerRadius, outerRadius, segmentStart, segmentSweep);
                var color = StatsTheme.SeriesColor(i);
                segment.Fill = new SolidColorBrush(color);
                var isSelected = _selectedId == item.Id;
                if (_selectedId is not null && !isSelected)
                    segment.Opacity = 0.3;
                else if (isSelected)
                    segment.Stroke = _palette.AccentBrightBrush;

                ToolTipService.SetToolTip(segment, BuildTooltip(item, _totalMinutes));
                var index = i;
                segment.Tapped += (_, _) => SegmentClicked?.Invoke(this, _items[index].Id);
                Children.Add(segment);
            }

            startAngle += sweep;
        }

        // 中心文本（大数字 + 说明）
        var centerPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = innerRadius * 2 - 12,
        };
        centerPanel.Children.Add(new TextBlock
        {
            Text = CenterLabel,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = _palette.TextPrimaryBrush,
            TextAlignment = TextAlignment.Center,
        });
        centerPanel.Children.Add(new TextBlock
        {
            Text = CenterSubLabel,
            FontSize = 11,
            Foreground = _palette.TextSecondaryBrush,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        Children.Add(centerPanel);
    }

    private static string BuildTooltip(GamePeriodTime item, double total)
    {
        var percent = total > 0 ? item.Minutes / total * 100.0 : 0.0;
        return $"{item.Name}\n{UiKit.L("Chart_Time", "游玩时长")}：{UiKit.FormatTime(item.Hours)}\n" +
               $"{UiKit.L("Chart_Percent", "占比")}：{percent.ToString("F1")}%";
    }

    private static Path BuildRingSegment(Point center, double rInner, double rOuter, double startDeg, double sweepDeg)
    {
        var a0 = startDeg * Math.PI / 180.0;
        var a1 = (startDeg + sweepDeg) * Math.PI / 180.0;

        var pOuter0 = PointAt(center, rOuter, a0);
        var pOuter1 = PointAt(center, rOuter, a1);
        var pInner1 = PointAt(center, rInner, a1);
        var pInner0 = PointAt(center, rInner, a0);

        var figure = new PathFigure
        {
            StartPoint = pOuter0,
            IsClosed = true,
        };
        figure.Segments.Add(new LineSegment { Point = pOuter1 });
        figure.Segments.Add(new ArcSegment
        {
            Point = pInner1,
            Size = new Size(rInner, rInner),
            SweepDirection = SweepDirection.Counterclockwise,
            IsLargeArc = sweepDeg > 180,
        });
        figure.Segments.Add(new LineSegment { Point = pInner0 });
        figure.Segments.Add(new ArcSegment
        {
            Point = pOuter0,
            Size = new Size(rOuter, rOuter),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = sweepDeg > 180,
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return new Path { Data = geometry };
    }

    private static Point PointAt(Point center, double radius, double angleRad)
        => new(center.X + radius * Math.Cos(angleRad), center.Y + radius * Math.Sin(angleRad));
}
