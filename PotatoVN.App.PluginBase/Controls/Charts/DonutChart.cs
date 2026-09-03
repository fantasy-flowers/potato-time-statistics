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
using Windows.UI;

namespace PotatoVN.App.PluginBase.Controls.Charts;

/// <summary>
/// 原生环形图（WinUI 自绘，无外部图表依赖）。
/// 样式对齐 sample/_html_full.html 的 ECharts 饼图：
/// radius 48%-72%、相邻扇区间隙 2° + 卡片底色描边、占比 ≥5% 的外部标签带引导线。
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
        var half = Math.Min(width, height) / 2;
        var outerRadius = half * 0.72; // 原型 series.radius = ['48%','72%']
        var innerRadius = half * 0.48;

        var separated = _items.Count > 1;
        var startAngle = -90.0; // 从 12 点方向开始顺时针
        var labels = new List<DonutLabel>();
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var sweep = item.Minutes / (double)_totalMinutes * 360.0;

            // 原型 padAngle: 2 → 相邻扇区间共 2° 间隙（每侧收 1°）；单扇区满圆时不收
            var pad = separated ? Math.Min(1.0, sweep / 6.0) : 0.0;
            var drawSweep = Math.Max(0.5, sweep - pad * 2);

            var segment = BuildRingSegment(center, innerRadius, outerRadius, startAngle + pad, drawSweep);
            segment.Fill = new SolidColorBrush(StatsTheme.SeriesColor(i));
            if (separated)
            {
                segment.Stroke = _palette.CardBrush; // 原型 itemStyle.borderColor=#1f2d3d（卡片底色）
                segment.StrokeThickness = 2;
            }

            var isSelected = _selectedId == item.Id;
            if (_selectedId is not null && !isSelected)
                segment.Opacity = 0.3;
            else if (isSelected)
                segment.Stroke = _palette.AccentBrightBrush;

            ToolTipService.SetToolTip(segment, BuildTooltip(item, _totalMinutes));
            var index = i;
            segment.Tapped += (_, _) => SegmentClicked?.Invoke(this, _items[index].Id);
            Children.Add(segment);

            // 外部标签：原型 label.formatter 对占比 <5% 返回空
            var percent = item.Minutes / (double)_totalMinutes * 100.0;
            if (percent >= 5)
                labels.Add(new DonutLabel(item.Name, percent, startAngle + sweep / 2.0));

            startAngle += sweep;
        }

        AddOutsideLabels(labels, center, outerRadius, width, height);

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

    /// <summary>
    /// 环形扇区 = 外弧(a0→a1 顺时针) + 径向线 + 内弧(a1→a0 逆时针)，闭合边补最后一条径向线。
    /// 注意外圈边界必须是 ArcSegment 而非直线弦，径向连接必须是 LineSegment 而非弧，否则会画成月牙。
    /// 弧必须按 ≤180° 分段：单条 ArcSegment 不允许起点=终点（360° 满圆时两点重合，
    /// 退化弧会整段不渲染，导致 100% 单扇区时整个环消失）。
    /// </summary>
    private static Path BuildRingSegment(Point center, double rInner, double rOuter, double startDeg, double sweepDeg)
    {
        var figure = new PathFigure
        {
            StartPoint = PointAt(center, rOuter, startDeg * Math.PI / 180.0),
            IsClosed = true,
        };

        // 外弧：a0 → a1 顺时针，分段 ≤180°
        var angle = startDeg;
        var remaining = sweepDeg;
        while (remaining > 0)
        {
            var step = Math.Min(remaining, 180.0);
            angle += step;
            figure.Segments.Add(new ArcSegment
            {
                Point = PointAt(center, rOuter, angle * Math.PI / 180.0),
                Size = new Size(rOuter, rOuter),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false, // step ≤ 180，永远取小弧
            });
            remaining -= step;
        }

        // 径向线：外弧终点 → 内弧终点
        figure.Segments.Add(new LineSegment { Point = PointAt(center, rInner, angle * Math.PI / 180.0) });

        // 内弧：a1 → a0 逆时针，分段 ≤180°
        remaining = sweepDeg;
        while (remaining > 0)
        {
            var step = Math.Min(remaining, 180.0);
            angle -= step;
            figure.Segments.Add(new ArcSegment
            {
                Point = PointAt(center, rInner, angle * Math.PI / 180.0),
                Size = new Size(rInner, rInner),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = false,
            });
            remaining -= step;
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return new Path { Data = geometry };
    }

    #region 外部标签（原型 label + labelLine）

    private sealed record DonutLabel(string Name, double Percent, double MidAngleDeg);

    private sealed class LabelSlot
    {
        public required DonutLabel Label;
        public Point LineStart;
        public Point LineElbow;
        public double TipX;
        public double TipY;
        public int Side; // 1=右侧, -1=左侧
        public double FinalY;
    }

    private void AddOutsideLabels(List<DonutLabel> labels, Point center, double outerRadius, double width, double height)
    {
        if (labels.Count == 0) return;

        const double lineLen1 = 8;    // 原型 labelLine.length
        const double lineLen2 = 6;    // 原型 labelLine.length2
        const double textGap = 4;
        const double blockWidth = 130;
        const double blockHeight = 34; // 两行 11px × lineHeight 16 ≈ 32
        const double minGap = 38;

        var slots = new List<LabelSlot>();
        foreach (var label in labels)
        {
            var rad = label.MidAngleDeg * Math.PI / 180.0;
            var side = Math.Cos(rad) >= 0 ? 1 : -1;
            slots.Add(new LabelSlot
            {
                Label = label,
                LineStart = PointAt(center, outerRadius + 2, rad),
                LineElbow = PointAt(center, outerRadius + 2 + lineLen1, rad),
                TipX = 0,
                TipY = 0,
                Side = side,
                FinalY = 0,
            });
        }

        foreach (var slot in slots)
        {
            slot.TipX = slot.LineElbow.X + lineLen2 * slot.Side;
            slot.TipY = slot.LineElbow.Y;
        }

        // 同侧标签自上而下推开防重叠，超出下边界时整体回推
        foreach (var side in new[] { 1, -1 })
        {
            var group = slots.Where(s => s.Side == side).OrderBy(s => s.TipY).ToList();
            if (group.Count == 0) continue;

            var ys = group.Select(s => s.TipY).ToList();
            for (var i = 1; i < ys.Count; i++)
            {
                if (ys[i] < ys[i - 1] + minGap) ys[i] = ys[i - 1] + minGap;
            }

            var overflow = ys[^1] + blockHeight / 2 - (height - 6);
            if (overflow > 0)
            {
                for (var i = ys.Count - 1; i >= 0; i--)
                {
                    ys[i] -= overflow;
                    if (i > 0 && ys[i - 1] > ys[i] - minGap) ys[i - 1] = ys[i] - minGap;
                }
            }

            for (var i = 0; i < group.Count; i++)
            {
                group[i].FinalY = Math.Max(blockHeight / 2 + 4, ys[i]);
            }
        }

        foreach (var slot in slots)
        {
            var tip = new Point(slot.TipX, slot.FinalY);
            var line = new Polyline
            {
                Stroke = _palette.BorderBrush, // 原型 labelLine.lineStyle.color=#3c4d5e
                StrokeThickness = 1,
            };
            line.Points.Add(slot.LineStart);
            line.Points.Add(slot.LineElbow);
            line.Points.Add(tip);
            Children.Add(line);

            var alignment = slot.Side > 0 ? TextAlignment.Left : TextAlignment.Right;
            var block = new StackPanel
            {
                Width = blockWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            block.Children.Add(new TextBlock
            {
                Text = slot.Label.Name,
                FontSize = 11,
                Foreground = _palette.TextSecondaryBrush, // 原型 label.color=#8f98a0
                TextAlignment = alignment,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
            });
            block.Children.Add(new TextBlock
            {
                Text = slot.Label.Percent.ToString("F1") + "%",
                FontSize = 11,
                Foreground = _palette.TextSecondaryBrush,
                TextAlignment = alignment,
            });

            var left = slot.Side > 0 ? tip.X + textGap : tip.X - textGap - blockWidth;
            left = Math.Clamp(left, 2, Math.Max(2, width - blockWidth - 2));
            block.Margin = new Thickness(left, tip.Y - blockHeight / 2, 0, 0);
            Children.Add(block);
        }
    }

    #endregion

    private static Point PointAt(Point center, double radius, double angleRad)
        => new(center.X + radius * Math.Cos(angleRad), center.Y + radius * Math.Sin(angleRad));
}
