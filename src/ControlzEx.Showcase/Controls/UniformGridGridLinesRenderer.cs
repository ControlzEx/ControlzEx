namespace ControlzEx.Showcase
{
    using System.Windows;
    using System.Windows.Media;

    public abstract class GridLinesRendererBase : DrawingVisual
    {
        public virtual void UpdateRenderBounds(Size boundsSize, int columns, double columnWidth, int rows, double rowHeight)
        {
            using var drawingContext = this.RenderOpen();

            for (var index = 1; index < columns; ++index)
            {
                this.DrawGridLine(drawingContext, index * columnWidth, 0.0, index * columnWidth, boundsSize.Height);
            }

            for (var index = 1; index < rows; ++index)
            {
                this.DrawGridLine(drawingContext, 0.0, index * rowHeight, boundsSize.Width, index * rowHeight);
            }
        }

        protected abstract void DrawGridLine(DrawingContext drawingContext,
                                             double startX,
                                             double startY,
                                             double endX,
                                             double endY);
    }

    public class UniformGridGridLinesRenderer : GridLinesRendererBase
    {
        private const double DashLength = 4.0;
        private const double PenWidth = 1.0;
        private static readonly Pen oddDashPen;
        private static readonly Pen evenDashPen;

        static UniformGridGridLinesRenderer()
        {
            oddDashPen = new Pen(Brushes.Blue, PenWidth)
            {
                DashStyle = new DashStyle(new DoubleCollection
                {
                    DashLength,
                    DashLength
                }, 0.0),
                DashCap = PenLineCap.Flat
            };
            oddDashPen.Freeze();
            evenDashPen = new Pen(Brushes.Yellow, PenWidth)
            {
                DashStyle = new DashStyle(new DoubleCollection
                {
                    DashLength,
                    DashLength
                }, DashLength),
                DashCap = PenLineCap.Flat
            };
            evenDashPen.Freeze();
        }

        protected override void DrawGridLine(DrawingContext drawingContext,
                                             double startX,
                                             double startY,
                                             double endX,
                                             double endY)
        {
            var point0 = new Point(startX, startY);
            var point1 = new Point(endX, endY);
            drawingContext.DrawLine(oddDashPen, point0, point1);
            drawingContext.DrawLine(evenDashPen, point0, point1);
        }
    }
}