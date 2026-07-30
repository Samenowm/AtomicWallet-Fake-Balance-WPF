using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AtomicWallet.Controls;

/// <summary>One arc of the donut: a value weight and a color.</summary>
public sealed class DonutSegment
{
    public double Value { get; init; }
    public Brush Color { get; init; } = Brushes.Gray;
}

/// <summary>A ring chart drawn from a set of weighted, colored segments.</summary>
public sealed class DonutChart : FrameworkElement
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments), typeof(IEnumerable), typeof(DonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(DonutChart),
        new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Segments
    {
        get => (IEnumerable?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = (System.Math.Min(ActualWidth, ActualHeight) / 2) - (Thickness / 2);
        if (radius <= 0)
        {
            return;
        }

        var segs = Segments?.Cast<DonutSegment>().Where(s => s.Value > 0).ToList() ?? new List<DonutSegment>();
        var total = segs.Sum(s => s.Value);

        var track = new Pen(new SolidColorBrush(Color.FromRgb(0x2A, 0x35, 0x52)), Thickness);
        if (total <= 0)
        {
            dc.DrawEllipse(null, track, center, radius, radius);
            return;
        }

        const double gap = 2.0; // degrees between segments
        var angle = -90.0;       // start at top
        foreach (var s in segs)
        {
            var sweep = (s.Value / total) * 360.0;
            DrawArc(dc, center, radius, angle + (gap / 2), angle + sweep - (gap / 2),
                new Pen(s.Color, Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round });
            angle += sweep;
        }
    }

    private static void DrawArc(DrawingContext dc, Point c, double r, double a0Deg, double a1Deg, Pen pen)
    {
        if (a1Deg <= a0Deg)
        {
            return;
        }

        var a0 = a0Deg * System.Math.PI / 180.0;
        var a1 = a1Deg * System.Math.PI / 180.0;
        var p0 = new Point(c.X + (r * System.Math.Cos(a0)), c.Y + (r * System.Math.Sin(a0)));
        var p1 = new Point(c.X + (r * System.Math.Cos(a1)), c.Y + (r * System.Math.Sin(a1)));

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(p0, false, false);
            ctx.ArcTo(p1, new Size(r, r), 0, (a1Deg - a0Deg) > 180, SweepDirection.Clockwise, true, false);
        }

        geo.Freeze();
        dc.DrawGeometry(null, pen, geo);
    }
}
