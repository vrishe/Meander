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
    private SKPoint[] _graphPoints = Array.Empty<SKPoint>();
    private bool _graphPointsFilled;
    private SKPaint _graphSignalPaint;
    private SKPaint _graphSignalAvgPaint;
    private SKPaint _graphSignalRmsPaint;
    private ISignalInterpolator _interpolator;
    private IDisposable _requestHandle;
    private double _signalAvg;
    private double _signalRms;

    private SKPaint GraphSignalPaint => _graphSignalPaint
        ??= SetupGraphSignalPaint(new SKPaint
        {
            IsAntialias = true,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
        });

    private SKPaint GraphSignalAvgPaint => _graphSignalAvgPaint
        ??= SetupGraphSignalAvgPaint(new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 16, 8 }, 0),
        });

    private SKPaint GraphSignalRmsPaint => _graphSignalRmsPaint
        ??= SetupGraphSignalRmsPaint(new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        });

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

        if (_interpolator == null) return;

        if (_graphPoints.Length != info.Width)
        {
            _graphPointsFilled = false;
            Array.Resize(ref _graphPoints, info.Width);
        }

        var yCenter = .5f * info.Height;
        float GetY(double v) => (float)(yCenter * (1 - .95 * v));

        if (!_graphPointsFilled)
        {
            _graphPointsFilled = true;

            _signalAvg = 0;
            _signalRms = 0;

            var div = (double)_graphPoints.Length;
            for (int i = 0, imax = _graphPoints.Length; i < imax; ++i)
            {
                var v = _interpolator.Interpolate(i / div);
                var v_div = v / div;
                _signalAvg += v_div;
                _signalRms += v * v_div;

                ref var p = ref _graphPoints[i];
                p.X = i;
                p.Y = GetY(v);
            }

            _signalRms = Math.Sqrt(_signalRms);
        }

        // Draw back
        var yTemp = GetY(_signalRms);
        var paint = GraphSignalRmsPaint;
        canvas.DrawRect(new SKRect(-paint.StrokeWidth, yTemp, info.Width + paint.StrokeWidth, yCenter), paint);

        // TODO: draw decor here

        // Draw front
        yTemp = GetY(_signalAvg);
        canvas.DrawLine(0, yTemp, info.Width, yTemp, GraphSignalAvgPaint);
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

    private SKPaint SetupGraphSignalPaint(SKPaint p)
    {
        p.ColorF = GraphColor.ToSKColorF().WithAlpha(1);
        p.StrokeWidth = (float)GraphThickness;
        return p;
    }

    private SKPaint SetupGraphSignalAvgPaint(SKPaint p)
    {
        p.ColorF = GraphColor.ToSKColorF().WithAlpha(.45f);
        p.StrokeWidth = (float)Math.Max(GraphThickness - 1, 1);
        return p;
    }

    private SKPaint SetupGraphSignalRmsPaint(SKPaint p)
    {
        p.ColorF = GraphColor.ToSKColorF().WithAlpha(.15f);
        return p;
    }

    private void UpdateGraphPaint()
    {
        SetupGraphSignalPaint(GraphSignalPaint);
        SetupGraphSignalAvgPaint(GraphSignalAvgPaint);

        InvalidateSurface();
    }
}
