using Meander.Signals;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Meander.Controls;

internal sealed class SignalGraphView : SKCanvasView
{
    public static readonly BindableProperty AnnotationsColorProperty =
        BindableProperty.Create(nameof(AnnotationsColor), typeof(Color), typeof(SignalGraphView), Colors.LightGray,
            propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateAnnotationsPaint());

    public Color AnnotationsColor
    {
        get => (Color)GetValue(AnnotationsColorProperty);
        set => SetValue(AnnotationsColorProperty, value);
    }

    public static readonly BindableProperty AnnotationsFontFamilyProperty =
    BindableProperty.Create(nameof(AnnotationsFontFamily), typeof(string), typeof(SignalGraphView), default(string),
        propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateAnnotationsTextPaint(true));


    public string AnnotationsFontFamily
    {
        get => (string)GetValue(AnnotationsFontFamilyProperty);
        set => SetValue(AnnotationsFontFamilyProperty, value);
    }

    public static readonly BindableProperty AnnotationsFontSizeProperty =
    BindableProperty.Create(nameof(AnnotationsFontSize), typeof(float), typeof(SignalGraphView), 14f,
        propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateAnnotationsTextPaint(),
        coerceValue: (_, v) => Math.Max((float)v, 1));

    public float AnnotationsFontSize
    {
        get => (float)GetValue(AnnotationsFontSizeProperty);
        set => SetValue(AnnotationsFontSizeProperty, value);
    }

    public static readonly BindableProperty AnnotationsTextColorProperty =
    BindableProperty.Create(nameof(AnnotationsTextColor), typeof(Color), typeof(SignalGraphView), Colors.White,
        propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateAnnotationsTextPaint());

    public Color AnnotationsTextColor
    {
        get => (Color)GetValue(AnnotationsTextColorProperty);
        set => SetValue(AnnotationsTextColorProperty, value);
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

    public static readonly BindableProperty SignalIdProperty =
        BindableProperty.Create(nameof(SignalId), typeof(Guid?), typeof(SignalGraphView), default(Guid?),
            propertyChanged: (bo, _, newValue) => (bo as SignalGraphView)!.RequestSignalUpdate((Guid?)newValue));

    public Guid? SignalId
    {
        get => (Guid?)GetValue(SignalIdProperty);
        set => SetValue(SignalIdProperty, value);
    }

    public static readonly BindablePropertyKey SignalStatsProperty =
        BindableProperty.CreateReadOnly(nameof(SignalStats), typeof(SignalStatsInfo), typeof(SignalGraphView), default(SignalStatsInfo));

    public SignalStatsInfo SignalStats
    {
        get => (SignalStatsInfo)GetValue(SignalStatsProperty.BindableProperty);
        private set => SetValue(SignalStatsProperty, value);
    }

    public static readonly BindableProperty ZeroLabelTextProperty =
        BindableProperty.Create(nameof(ZeroLabelText), typeof(string), typeof(SignalGraphView), "0",
            propertyChanged: (bo, _, _) => (bo as SignalGraphView)!.UpdateZeroLabelText());

    public string ZeroLabelText
    {
        get => (string)GetValue(ZeroLabelTextProperty);
        set => SetValue(ZeroLabelTextProperty, value);
    }

    private ISignalsEvaluator _evaluator;
    private ISignalInterpolator _interpolator;
    private SKPoint[] _graphPoints = Array.Empty<SKPoint>();
    private bool _graphPointsFilled;
    private IDisposable _requestHandle;

    private double _signalAvg;
    private double _signalMax;
    private double _signalMin;
    private double _signalRms;

    private SKPaint _annotationsPaint;
    private SKPaint _annotationsTextPaint;
    private SKPaint _graphSignalPaint;
    private SKPaint _graphSignalAvgPaint;
    private SKPaint _graphSignalRmsPaint;

    private LabelDrawingCommand _maxLabel;
    private LabelDrawingCommand _minLabel;
    private LabelDrawingCommand _zeroLabel;

    private float _canvasHeightLast = float.NaN;
    private SKRect _tempBounds;

    private SKPaint AnnotationsPaint => _annotationsPaint
        ??= SetupAnnotationsPaint(new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = .5f,
            SubpixelText = true,
            //PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0),
        });

    private SKPaint AnnotationsTextPaint => _annotationsTextPaint
        ??= SetupAnnotationsTextPaint(new SKPaint(new SKFont(SKTypeface.FromFamilyName(AnnotationsFontFamily)))
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            SubpixelText = true,
        });

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

        var heightChanged = _canvasHeightLast != info.Height;
        _canvasHeightLast = info.Height;

        var yCenter = .5f * info.Height;
        float GetY(double v) => (float)(yCenter * (1 - .95 * v));

        if (!_graphPointsFilled)
        {
            _graphPointsFilled = true;

            _signalAvg = 0;
            _signalMax = float.MinValue;
            _signalMin = float.MaxValue;
            _signalRms = 0;

            var div = (double)_graphPoints.Length;
            for (int i = 0, imax = _graphPoints.Length; i < imax; ++i)
            {
                var v = _interpolator.Interpolate(i / div);

                if (v > _signalMax) _signalMax = v;
                if (v < _signalMin) _signalMin = v;

                var v_div = v / div;
                _signalAvg += v_div;
                _signalRms += v * v_div;

                ref var p = ref _graphPoints[i];
                p.X = i;
                p.Y = GetY(v);
            }

            _signalRms = Math.Sqrt(_signalRms);

            SignalStats = new SignalStatsInfo(_signalAvg, _signalMax, _signalMin, _signalRms);

            _maxLabel = _maxLabel with { IsValid = false };
            _minLabel = _minLabel with { IsValid = false };
        }

        float yMax, yMin;
        var drawMaxAnnotations = _signalMax != 0;
        var drawMinAnnotations = _signalMin != 0 && _signalMin != _signalMax;

        // Draw back
        var yTemp = GetY(_signalRms);
        var paint = GraphSignalRmsPaint;
        canvas.DrawRect(new SKRect(-paint.StrokeWidth, yTemp, info.Width + paint.StrokeWidth, yCenter), paint);

        // Annotation Lines

        canvas.DrawLine(0, yCenter, info.Width, yCenter, AnnotationsPaint);

        if (drawMaxAnnotations)
        {
            yMax = GetY(_signalMax);
            canvas.DrawLine(0, yMax, info.Width, yMax, AnnotationsPaint);
        }

        if (drawMinAnnotations)
        {
            yMin = GetY(_signalMin);
            canvas.DrawLine(0, yMin, info.Width, yMin, AnnotationsPaint);
        }

        // Draw front
        yTemp = GetY(_signalAvg);
        canvas.DrawLine(0, yTemp, info.Width, yTemp, GraphSignalAvgPaint);
        canvas.DrawPoints(SKPointMode.Polygon, _graphPoints, GraphSignalPaint);

        if (heightChanged | !_zeroLabel.IsValid)
        {
            CalculateFloatingLabel(ZeroLabelText, AnnotationsTextPaint,
                8, yCenter, info.Width, info.Height, ref _zeroLabel);
        }

        _zeroLabel.Draw(canvas);

        if (drawMaxAnnotations)
        {
            if (heightChanged | !_maxLabel.IsValid)
            {
                var text = _signalMax.ToString();
                var yPosRef = GetY(1);
                var yPos = GetY(_signalMax);
                if (yPos < yPosRef)
                {
                    text = "▲" + text;
                    yPos = yPosRef;
                }

                CalculateFloatingLabel(text, AnnotationsTextPaint,
                    8, yPos, info.Width, info.Height, ref _maxLabel);
            }

            _maxLabel.Draw(canvas);
        }

        if (drawMinAnnotations)
        {
            if (heightChanged | !_minLabel.IsValid)
            {
                if (_signalMin != 0)
                {
                    var text = _signalMin.ToString();
                    var yPosRef = GetY(-1);
                    var yPos = GetY(_signalMin);
                    if (yPos > yPosRef)
                    {
                        text = "▼" + text;
                        yPos = yPosRef;
                    }

                    CalculateFloatingLabel(text, AnnotationsTextPaint,
                        8, yPos, info.Width, info.Height, ref _minLabel);
                }
                else _minLabel = _minLabel with { IsNonEmpty = false, IsValid = true };
            }

            _minLabel.Draw(canvas);
        }
    }

    private void CalculateFloatingLabel(string text, SKPaint textPaint, float x, float cy, float w, float h, ref LabelDrawingCommand cmd)
    {
        if (string.IsNullOrEmpty(text))
        {
            cmd = cmd with { IsValid = true, IsNonEmpty = false };
            return;
        }

        textPaint.MeasureText(text, ref _tempBounds);

        var textBounds = _tempBounds;
        var cWidth = .5f * Math.Max(textPaint.FontMetrics.AverageCharacterWidth, .5f * textPaint.FontMetrics.MaxCharacterWidth);
        textBounds.Inflate(new SKSize(cWidth, .5f * textBounds.Height));
        textBounds.Location = new SKPoint(
            Math.Min(Math.Max(x, 0), w - textBounds.Width),
            Math.Min(Math.Max(cy - .5f * textBounds.Height, 0), h - textBounds.Height));
        var textLocation = new SKPoint(textBounds.Left + .5f * (textBounds.Width - _tempBounds.Width),
            textBounds.Top + .5f * (textBounds.Height - _tempBounds.Height)) - _tempBounds.Location;

        cmd = new(true, true, textBounds, textLocation, text, textPaint);
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

    private SKPaint SetupAnnotationsPaint(SKPaint p)
    {
        p.ColorF = AnnotationsColor.ToSKColorF();
        return p;
    }

    private SKPaint SetupAnnotationsTextPaint(SKPaint p)
    {
        p.ColorF = AnnotationsTextColor.ToSKColorF();
        p.TextSize = AnnotationsFontSize;
        return p;
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

    private void UpdateAnnotationsPaint()
    {
        SetupAnnotationsPaint(AnnotationsPaint);

        InvalidateSurface();
    }

    private void UpdateAnnotationsTextPaint(bool fontChanged = false)
    {
        if (!fontChanged)
        {
            SetupAnnotationsTextPaint(AnnotationsTextPaint);
        }
        else
        {
            _annotationsTextPaint = null;
        }

        _maxLabel = _maxLabel with { IsValid = false };
        _minLabel = _minLabel with { IsValid = false };
        _zeroLabel = _zeroLabel with { IsValid = false };

        InvalidateSurface();
    }

    private void UpdateGraphPaint()
    {
        SetupGraphSignalPaint(GraphSignalPaint);
        SetupGraphSignalAvgPaint(GraphSignalAvgPaint);

        InvalidateSurface();
    }

    private void UpdateZeroLabelText()
    {
        _zeroLabel = _zeroLabel with { IsValid = false };
        InvalidateSurface();
    }

    public readonly record struct SignalStatsInfo(double Avg, double Max, double Min, double Rms);

    private readonly record struct LabelDrawingCommand(bool IsValid, bool IsNonEmpty, SKRect Bounds, SKPoint TextLocation, string Text, SKPaint Paint)
    {
        public void Draw(SKCanvas canvas)
        {
            if (!IsValid || !IsNonEmpty) return;

            using var _ = new SavedCountScope(canvas);

            canvas.ClipRect(Bounds);
            canvas.Clear();
            canvas.DrawText(Text, TextLocation, Paint);
        }
    }

    private readonly struct SavedCountScope : IDisposable
    {
        private readonly SKCanvas _canvas;
        private readonly int _count;

        public SavedCountScope(SKCanvas c)
        {
            _canvas = c;
            _count = c.Save();
        }

        public void Dispose()
        {
            _canvas.RestoreToCount(_count);
        }
    }
}
