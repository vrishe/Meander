using Meander.Signals;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Meander.Controls;

internal sealed class SignalGraphView : SKCanvasView
{
    public static readonly BindableProperty SignalIdProperty =
        BindableProperty.Create(nameof(SignalId), typeof(Guid?), typeof(SignalGraphView), default(Guid?),
            propertyChanged: (bo, _, newValue) => (bo as SignalGraphView)!.RequestSignalUpdate((Guid?)newValue));

    public Guid? SignalId
    {
        get => (Guid?)GetValue(SignalIdProperty);
        set => SetValue(SignalIdProperty, value);
    }

    public static readonly BindableProperty GraphColorProperty =
        BindableProperty.Create(nameof(GraphColor), typeof(Color), typeof(SignalGraphView), Colors.Magenta,
            propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateGraphPaint());

    public Color GraphColor
    {
        get => (Color)GetValue(GraphColorProperty);
        set => SetValue(GraphColorProperty, value);
    }

    public static readonly BindableProperty GraphThicknessProperty =
        BindableProperty.Create(nameof(GraphThickness), typeof(double), typeof(SignalGraphView), 1d,
            propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateGraphPaint());

    public double GraphThickness
    {
        get => (double)GetValue(GraphThicknessProperty);
        set => SetValue(GraphThicknessProperty, value);
    }

    private ISignalsEvaluator _evaluator;
    private ISignalInterpolator _interpolator;
    private IDisposable _requestHandle;

    private SKPaint _graphSignalPaint;
    private SKPoint[] _graphPoints = Array.Empty<SKPoint>();
    private bool _graphPointsFilled;

    private SKPaint GraphSignalPaint => _graphSignalPaint ??= new SKPaint
    {
        ColorF = GraphColor.ToSKColorF(),
        IsAntialias = true,
        StrokeJoin = SKStrokeJoin.Round,
        StrokeCap = SKStrokeCap.Round,
        StrokeWidth = (float)GraphThickness,
        Style = SKPaintStyle.Stroke,
    };

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent != null)
        {
            _evaluator = App.FindMauiContext(this).Services
                .GetRequiredService<ISignalsEvaluator>();

            RequestSignalUpdate(SignalId);
        }
        else
        {
            _requestHandle?.Dispose();
            _requestHandle = null;
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var (info, surface) = (e.Info, e.Surface);
        var canvas = surface.Canvas;

        canvas.Clear();

        // TODO: draw graph chart decor (axes, marks, etc.)

        DrawSignalGraph(e);
    }

    private void DrawSignalGraph(SKPaintSurfaceEventArgs e)
    {
        if (_interpolator == null) return;

        var (info, surface) = (e.Info, e.Surface);
        var canvas = surface.Canvas;

        if (_graphPoints.Length != info.Width)
        {
            _graphPointsFilled = false;
            Array.Resize(ref _graphPoints, info.Width);
        }

        if (!_graphPointsFilled)
        {
            _graphPointsFilled = true;
            for (int i = 0, imax = _graphPoints.Length; i < imax; ++i)
            {
                ref var p = ref _graphPoints[i];
                p.X = i;
                p.Y = (float)(.5 * info.Height * (1 - _interpolator.Interpolate(i / (double)imax)));
            }
        }

        canvas.DrawPoints(SKPointMode.Polygon, _graphPoints, GraphSignalPaint);
    }

    private void RequestSignalUpdate(Guid? signalId)
    {
        if (!signalId.HasValue)
        {
            InvalidateSurface();
            return;
        }

        _requestHandle?.Dispose();
        _requestHandle = _evaluator.SubscribeSignalInterpolatorUpdates(signalId.Value,
            interpolator => {
                _interpolator = interpolator;
                _graphPointsFilled = false;

                InvalidateSurface();
            });
    }

    private void UpdateGraphPaint()
    {
        if (_graphSignalPaint == null) return;

        _graphSignalPaint.ColorF = GraphColor.ToSKColorF();
        _graphSignalPaint.StrokeWidth = (float)GraphThickness;

        InvalidateSurface();
    }
}
