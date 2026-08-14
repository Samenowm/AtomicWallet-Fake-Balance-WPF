using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AtomicWallet.Controls;

/// <summary>Filled area + line chart for the asset price view.</summary>
public sealed class AreaChart : FrameworkElement
{
    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IEnumerable), typeof(AreaChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(AreaChart),
        new FrameworkPropertyMetadata(Brushes.Orange, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Values
    {
        get => (IEnumerable?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Values == null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var pts = Values.Cast<object>().Select(v => (double)System.Convert.ToDecimal(v)).ToList();
        if (pts.Count < 2)
        {
            return;
        }

        var min = pts.Min();
        var max = pts.Max();
        var range = max - min;
        if (range <= 0)
        {
            range = 1;
        }

        const double pad = 2;
        var w = ActualWidth - (pad * 2);
        var h = ActualHeight - (pad * 2);
        var stepX = w / (pts.Count - 1);

        var line = new StreamGeometry();
        var fill = new StreamGeometry();
        using (var lc = line.Open())
        using (var fc = fill.Open())
        {
            var first = new Point(pad, pad + (h - ((pts[0] - min) / range * h)));
            lc.BeginFigure(first, false, false);
            fc.BeginFigure(new Point(pad, ActualHeight), true, true);
            fc.LineTo(first, true, true);

            for (var i = 1; i < pts.Count; i++)
            {
                var p = new Point(pad + (i * stepX), pad + (h - ((pts[i] - min) / range * h)));
                lc.LineTo(p, true, true);
                fc.LineTo(p, true, true);
            }

            fc.LineTo(new Point(pad + ((pts.Count - 1) * stepX), ActualHeight), true, true);
        }

        line.Freeze();
        fill.Freeze();

        var baseColor = (Stroke as SolidColorBrush)?.Color ?? Colors.Orange;
        var grad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        grad.GradientStops.Add(new GradientStop(Color.FromArgb(90, baseColor.R, baseColor.G, baseColor.B), 0));
        grad.GradientStops.Add(new GradientStop(Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B), 1));
        grad.Freeze();

        dc.DrawGeometry(grad, null, fill);
        dc.DrawGeometry(null,
            new Pen(Stroke, 1.6) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
            line);
    }
}
