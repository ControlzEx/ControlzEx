namespace ControlzEx.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using ControlzEx.Controls.Internal;

    public static class GlowWindowBitmapGenerator
    {
        public static RenderTargetBitmap GenerateBitmapSource(GlowBitmapPart part, int glowDepth)
        {
            var size = GetSize(part, glowDepth);

            var gradientBrush = CreateGradientBrush(part);

            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();
            try
            {
                drawingContext.DrawRectangle(gradientBrush, null, new Rect(0, 0, size.Width, size.Height));
                drawingContext.DrawRectangle(Brushes.Black, null, GetBlackRect(part, glowDepth));
            }
            finally
            {
                drawingContext.Close();
            }

            var targetBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            targetBitmap.Render(drawingVisual);
            return targetBitmap;
        }

        private static Rect GetBlackRect(GlowBitmapPart part, int glowDepth)
        {
            switch (part)
            {
                case GlowBitmapPart.CornerTopLeft:
                    return new Rect(new Point(glowDepth - 1, glowDepth - 1), new Size(1, 1));

                case GlowBitmapPart.CornerTopRight:
                    return new Rect(new Point(0, glowDepth - 1), new Size(1, 1));

                case GlowBitmapPart.CornerBottomLeft:
                    return new Rect(new Point(glowDepth - 1, 0), new Size(1, 1));

                case GlowBitmapPart.CornerBottomRight:
                    return new Rect(new Point(0, 0), new Size(1, 1));

                case GlowBitmapPart.TopLeft:
                case GlowBitmapPart.Top:
                case GlowBitmapPart.TopRight:
                    return new Rect(new Point(0, glowDepth - 1), new Size(glowDepth, 1));

                case GlowBitmapPart.LeftTop:
                case GlowBitmapPart.Left:
                case GlowBitmapPart.LeftBottom:
                    return new Rect(new Point(glowDepth - 1, 0), new Size(1, glowDepth));

                case GlowBitmapPart.BottomLeft:
                case GlowBitmapPart.Bottom:
                case GlowBitmapPart.BottomRight:
                    return new Rect(new Point(0, 0), new Size(glowDepth, 1));

                case GlowBitmapPart.RightTop:
                case GlowBitmapPart.Right:
                case GlowBitmapPart.RightBottom:
                    return new Rect(new Point(0, 0), new Size(1, glowDepth));

                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }
        }

        private static Size GetSize(GlowBitmapPart part, int glowDepth)
        {
            switch (part)
            {
                case GlowBitmapPart.CornerTopLeft:
                case GlowBitmapPart.CornerTopRight:
                case GlowBitmapPart.CornerBottomLeft:
                case GlowBitmapPart.CornerBottomRight:
                    return new Size(glowDepth, glowDepth);

                case GlowBitmapPart.Top:
                case GlowBitmapPart.Bottom:
                    return new Size(1, glowDepth);

                case GlowBitmapPart.TopLeft:
                case GlowBitmapPart.TopRight:
                case GlowBitmapPart.BottomLeft:
                case GlowBitmapPart.BottomRight:
                    return new Size(6, glowDepth);

                case GlowBitmapPart.Left:
                case GlowBitmapPart.Right:
                    return new Size(glowDepth, 1);

                case GlowBitmapPart.LeftTop:
                case GlowBitmapPart.LeftBottom:
                case GlowBitmapPart.RightTop:
                case GlowBitmapPart.RightBottom:
                    return new Size(glowDepth, 6);

                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }
        }

        private static GradientBrush CreateGradientBrush(GlowBitmapPart part)
        {
            var startAndEndPoint = GetStartAndEndPoint(part);
            return new LinearGradientBrush(new GradientStopCollection(GetGradientStops(part)), startAndEndPoint.Start, startAndEndPoint.End);
        }

        private static StartAndEndPoint GetStartAndEndPoint(GlowBitmapPart part)
        {
            switch (part)
            {
                case GlowBitmapPart.CornerTopLeft:
                    return new(new Point(1, 1), new Point(0, 0));

                case GlowBitmapPart.CornerTopRight:
                    return new(new Point(0, 1), new Point(1, 0));

                case GlowBitmapPart.CornerBottomLeft:
                    return new(new Point(1, 0), new Point(0, 1));

                case GlowBitmapPart.CornerBottomRight:
                    return new(new Point(0, 0), new Point(1, 1));

                case GlowBitmapPart.TopLeft:
                    return new(new Point(0.6, 1), new Point(0.5, 0));
                case GlowBitmapPart.Top:
                    return new(new Point(0.5, 1), new Point(0.5, 0));
                case GlowBitmapPart.TopRight:
                    return new(new Point(0.4, 1), new Point(0.5, 0));

                case GlowBitmapPart.LeftTop:
                    return new(new Point(1, 0.1), new Point(0, 0));
                case GlowBitmapPart.Left:
                    return new(new Point(1, 0), new Point(0, 0));
                case GlowBitmapPart.LeftBottom:
                    return new(new Point(1, 0), new Point(0, 0.1));

                case GlowBitmapPart.BottomLeft:
                    return new(new Point(0.6, 0), new Point(0.5, 1));
                case GlowBitmapPart.Bottom:
                    return new(new Point(0.5, 0), new Point(0.5, 1));
                case GlowBitmapPart.BottomRight:
                    return new(new Point(0.4, 0), new Point(0.5, 1));

                case GlowBitmapPart.RightTop:
                    return new(new Point(0, 0.1), new Point(1, 0));
                case GlowBitmapPart.Right:
                    return new(new Point(0, 0), new Point(1, 0));
                case GlowBitmapPart.RightBottom:
                    return new(new Point(0, 0), new Point(1, 0.1));

                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }
        }

        private static IEnumerable<GradientStop> GetGradientStops(GlowBitmapPart part)
        {
            // yield return new GradientStop(ColorFromString("#FF000000"), 0);
            // yield return new GradientStop(ColorFromString("#FF000000"), 0.1);
            // yield return new GradientStop(ColorFromString("#2F838383"), 0.125);
            // yield return new GradientStop(ColorFromString("#21838383"), 0.250);
            // yield return new GradientStop(ColorFromString("#16838383"), 0.375);
            // yield return new GradientStop(ColorFromString("#0C838383"), 0.5);
            // yield return new GradientStop(ColorFromString("#07838383"), 0.625);
            // yield return new GradientStop(ColorFromString("#05838383"), 0.75);
            // yield return new GradientStop(ColorFromString("#03838383"), 0.875);
            // yield return new GradientStop(ColorFromString("#01838383"), 1);

            // yield return new GradientStop(ColorFromString("#FF000000"), 0);
            // yield return new GradientStop(ColorFromString("#FF000000"), 0.125);
            // yield return new GradientStop(ColorFromString("#41838383"), 0.13);
            // yield return new GradientStop(ColorFromString("#2E838383"), 0.250);
            // yield return new GradientStop(ColorFromString("#1E838383"), 0.375);
            // yield return new GradientStop(ColorFromString("#13838383"), 0.5);
            // yield return new GradientStop(ColorFromString("#08838383"), 0.625);
            // yield return new GradientStop(ColorFromString("#05838383"), 0.75);
            // yield return new GradientStop(ColorFromString("#03838383"), 0.875);
            // yield return new GradientStop(ColorFromString("#01838383"), 1);

            // yield return new GradientStop(ColorFromString("#FF000000"), 0);
            // yield return new GradientStop(ColorFromString("#FF000000"), 0.11);

            // yield return new GradientStop(ColorFromString("#55838383"), 0);
            // yield return new GradientStop(ColorFromString("#02838383"), 0.6);
            // yield return new GradientStop(ColorFromString("#02838383"), 1);

            //yield return new GradientStop(ColorFromString("#00000000"), 1);

            switch (part)
            {
                case GlowBitmapPart.CornerBottomLeft:
                case GlowBitmapPart.CornerBottomRight:
                case GlowBitmapPart.CornerTopLeft:
                case GlowBitmapPart.CornerTopRight:
                    yield return new GradientStop(ColorFromString("#55838383"), 0);
                    yield return new GradientStop(ColorFromString("#02838383"), 0.3);
                    yield return new GradientStop(ColorFromString("#00000000"), 1);
                    break;

                default:
                    yield return new GradientStop(ColorFromString("#55838383"), 0);
                    yield return new GradientStop(ColorFromString("#02838383"), 0.6);
                    yield return new GradientStop(ColorFromString("#00000000"), 1);
                    break;
            }
        }

        private static Color ColorFromString(string input)
        {
            return (Color)ColorConverter.ConvertFromString(input);
        }

        private struct StartAndEndPoint
        {
            public StartAndEndPoint(Point start, Point end)
            {
                this.Start = start;
                this.End = end;
            }

            public Point Start { get; }

            public Point End { get; }
        }
    }
}