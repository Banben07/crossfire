using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using CrossfireCrosshair.Models;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace CrossfireCrosshair.Controls;

public sealed class CrosshairPreviewControl : FrameworkElement
{
    public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(
        nameof(Profile),
        typeof(CrosshairProfile),
        typeof(CrosshairPreviewControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnProfileChanged));

    public static readonly DependencyProperty TemporarySpreadProperty = DependencyProperty.Register(
        nameof(TemporarySpread),
        typeof(double),
        typeof(CrosshairPreviewControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private CrosshairProfile? _observedProfile;

    public CrosshairProfile? Profile
    {
        get => (CrosshairProfile?)GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    public double TemporarySpread
    {
        get => (double)GetValue(TemporarySpreadProperty);
        set => SetValue(TemporarySpreadProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Profile is null)
        {
            return;
        }

        DrawCrosshair(drawingContext, Profile, TemporarySpread);
    }

    private static void OnProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        CrosshairPreviewControl control = (CrosshairPreviewControl)d;
        control.UnsubscribeProfile(e.OldValue as CrosshairProfile);
        control.SubscribeProfile(e.NewValue as CrosshairProfile);
        control.InvalidateVisual();
    }

    private void SubscribeProfile(CrosshairProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        _observedProfile = profile;
        _observedProfile.PropertyChanged += OnObservedProfileChanged;
    }

    private void UnsubscribeProfile(CrosshairProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        profile.PropertyChanged -= OnObservedProfileChanged;
        if (ReferenceEquals(_observedProfile, profile))
        {
            _observedProfile = null;
        }
    }

    private void OnObservedProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void DrawCrosshair(DrawingContext drawingContext, CrosshairProfile profile, double temporarySpread)
    {
        WpfColor mainColor = WithOpacity(ParseColor(profile.ColorHex, Colors.Lime), profile.Opacity);
        WpfColor outlineColor = WithOpacity(ParseColor(profile.OutlineColorHex, Colors.Black), profile.Opacity);

        double lineThickness = Math.Max(0.5, profile.LineThickness);
        double lineLength = Math.Max(0.0, profile.LineLength);
        double gap = profile.Gap + profile.DynamicSpread + Math.Max(0.0, temporarySpread);
        double dotSize = Math.Max(0.0, profile.DotSize);
        double outlineThickness = Math.Max(0.0, profile.OutlineThickness);

        WpfPoint center = new((ActualWidth / 2.0) + profile.OffsetX, (ActualHeight / 2.0) + profile.OffsetY);

        SolidColorBrush mainBrush = new(mainColor);
        mainBrush.Freeze();
        WpfPen mainPen = new(mainBrush, lineThickness)
        {
            StartLineCap = PenLineCap.Square,
            EndLineCap = PenLineCap.Square
        };
        mainPen.Freeze();

        WpfPen? outlinePen = null;
        if (profile.ShowOutline && outlineThickness > 0)
        {
            SolidColorBrush outlineBrush = new(outlineColor);
            outlineBrush.Freeze();
            outlinePen = new WpfPen(outlineBrush, lineThickness + (outlineThickness * 2.0))
            {
                StartLineCap = PenLineCap.Square,
                EndLineCap = PenLineCap.Square
            };
            outlinePen.Freeze();
        }

        if (profile.ShowLines && lineLength > 0)
        {
            DrawSegment(
                drawingContext,
                center.X - gap - lineLength,
                center.Y,
                center.X - gap,
                center.Y,
                mainPen,
                outlinePen);

            DrawSegment(
                drawingContext,
                center.X + gap,
                center.Y,
                center.X + gap + lineLength,
                center.Y,
                mainPen,
                outlinePen);

            if (!profile.TStyle)
            {
                DrawSegment(
                    drawingContext,
                    center.X,
                    center.Y - gap - lineLength,
                    center.X,
                    center.Y - gap,
                    mainPen,
                    outlinePen);
            }

            DrawSegment(
                drawingContext,
                center.X,
                center.Y + gap,
                center.X,
                center.Y + gap + lineLength,
                mainPen,
                outlinePen);
        }

        if (profile.ShowCenterDot && dotSize > 0)
        {
            Rect dotRect = new(
                center.X - (dotSize / 2.0),
                center.Y - (dotSize / 2.0),
                dotSize,
                dotSize);

            if (profile.ShowOutline && outlineThickness > 0)
            {
                Rect outlineRect = new(
                    dotRect.X - outlineThickness,
                    dotRect.Y - outlineThickness,
                    dotRect.Width + (outlineThickness * 2),
                    dotRect.Height + (outlineThickness * 2));
                drawingContext.DrawRectangle(new SolidColorBrush(outlineColor), null, outlineRect);
            }

            drawingContext.DrawRectangle(mainBrush, null, dotRect);
        }
    }

    private static void DrawSegment(
        DrawingContext drawingContext,
        double x1,
        double y1,
        double x2,
        double y2,
        WpfPen mainPen,
        WpfPen? outlinePen)
    {
        WpfPoint start = new(x1, y1);
        WpfPoint end = new(x2, y2);

        if (outlinePen is not null)
        {
            drawingContext.DrawLine(outlinePen, start, end);
        }

        drawingContext.DrawLine(mainPen, start, end);
    }

    private static WpfColor ParseColor(string? hex, WpfColor fallback)
    {
        try
        {
            object? parsed = WpfColorConverter.ConvertFromString(hex ?? string.Empty);
            if (parsed is WpfColor color)
            {
                return color;
            }
        }
        catch
        {
        }

        return fallback;
    }

    private static WpfColor WithOpacity(WpfColor color, double opacity)
    {
        byte alpha = (byte)Math.Clamp((int)Math.Round(opacity * 255.0), 0, 255);
        return WpfColor.FromArgb(alpha, color.R, color.G, color.B);
    }
}
