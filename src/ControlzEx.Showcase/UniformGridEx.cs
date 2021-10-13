namespace ControlzEx.Showcase
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class UniformGridEx : Panel
    {
        public static readonly DependencyProperty FirstColumnProperty = DependencyProperty.Register(nameof(FirstColumn), typeof(int), typeof(UniformGridEx), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateFirstColumn);
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int), typeof(UniformGridEx), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateColumns);
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(UniformGridEx), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure), ValidateRows);
        public static readonly DependencyProperty ShowGridLinesProperty = DependencyProperty.Register(nameof(ShowGridLines), typeof(bool), typeof(UniformGridEx), new FrameworkPropertyMetadata(false, OnShowGridLinesChanged));
        public static readonly DependencyProperty GridLinesRendererProperty = DependencyProperty.Register(nameof(GridLinesRenderer), typeof(GridLinesRendererBase), typeof(UniformGridEx), new PropertyMetadata(default(UniformGridGridLinesRenderer), OnGridLinesRendererChanged));

        private int columns;
        private int rows;

        public int FirstColumn
        {
            get => (int)this.GetValue(FirstColumnProperty);
            set => this.SetValue(FirstColumnProperty, value);
        }

        public int Columns
        {
            get => (int)this.GetValue(ColumnsProperty);
            set => this.SetValue(ColumnsProperty, value);
        }

        public int Rows
        {
            get => (int)this.GetValue(RowsProperty);
            set => this.SetValue(RowsProperty, value);
        }

        public bool ShowGridLines
        {
            get => (bool)this.GetValue(ShowGridLinesProperty);
            set => this.SetValue(ShowGridLinesProperty, value);
        }

        public GridLinesRendererBase GridLinesRenderer
        {
            get => (GridLinesRendererBase)this.GetValue(GridLinesRendererProperty);
            set => this.SetValue(GridLinesRendererProperty, value);
        }

        protected override int VisualChildrenCount => base.VisualChildrenCount + (this.GridLinesRenderer is not null ? 1 : 0);

        private static bool ValidateFirstColumn(object o)
        {
            return (int)o >= 0;
        }

        private static bool ValidateColumns(object o)
        {
            return (int)o >= 0;
        }

        private static bool ValidateRows(object o)
        {
            return (int)o >= 0;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != base.VisualChildrenCount)
            {
                return base.GetVisualChild(index);
            }

            return this.GridLinesRenderer is not null
                ? (Visual)this.GridLinesRenderer
                : throw new ArgumentOutOfRangeException(nameof(index), index, null);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            this.UpdateComputedValues();

            var availableSize = new Size(constraint.Width / this.columns, constraint.Height / this.rows);
            var minChildWidth = 0.0;
            var minChildHeight = 0.0;
            var index = 0;

            for (var count = this.InternalChildren.Count; index < count; ++index)
            {
                var internalChild = this.InternalChildren[index];
                internalChild.Measure(availableSize);
                var desiredSize = internalChild.DesiredSize;

                if (minChildWidth < desiredSize.Width)
                {
                    minChildWidth = desiredSize.Width;
                }

                if (minChildHeight < desiredSize.Height)
                {
                    minChildHeight = desiredSize.Height;
                }
            }

            return new Size(minChildWidth * this.columns, minChildHeight * this.rows);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var finalRect = new Rect(0.0, 0.0, arrangeSize.Width / this.columns, arrangeSize.Height / this.rows);
            var columnWidth = finalRect.Width;
            var rowHeight = finalRect.Height;
            var adjustedWidth = arrangeSize.Width - 1.0;

            finalRect.X += finalRect.Width * this.FirstColumn;

            foreach (UIElement internalChild in this.InternalChildren)
            {
                internalChild.Arrange(finalRect);

                if (internalChild.Visibility != Visibility.Collapsed)
                {
                    finalRect.X += columnWidth;

                    if (finalRect.X >= adjustedWidth)
                    {
                        finalRect.Y += finalRect.Height;
                        finalRect.X = 0.0;
                    }
                }
            }

            this.EnsureGridLinesRenderer()?.UpdateRenderBounds(arrangeSize, this.columns, columnWidth, this.rows, rowHeight);

            return arrangeSize;
        }

        private void UpdateComputedValues()
        {
            this.columns = this.Columns;
            this.rows = this.Rows;

            if (this.FirstColumn >= this.columns)
            {
                this.FirstColumn = 0;
            }

            if (this.rows != 0
                && this.columns != 0)
            {
                return;
            }

            var visibleChildrenCount = 0;
            var index = 0;
            for (var count = this.InternalChildren.Count; index < count; ++index)
            {
                if (this.InternalChildren[index].Visibility != Visibility.Collapsed)
                {
                    ++visibleChildrenCount;
                }
            }

            if (visibleChildrenCount == 0)
            {
                visibleChildrenCount = 1;
            }

            if (this.rows == 0)
            {
                if (this.columns > 0)
                {
                    this.rows = (visibleChildrenCount + this.FirstColumn + (this.columns - 1)) / this.columns;
                }
                else
                {
                    this.rows = (int)Math.Sqrt(visibleChildrenCount);
                    if (this.rows * this.rows < visibleChildrenCount)
                    {
                        ++this.rows;
                    }

                    this.columns = this.rows;
                }
            }
            else
            {
                if (this.columns != 0)
                {
                    return;
                }

                this.columns = (visibleChildrenCount + (this.rows - 1)) / this.rows;
            }
        }

        private GridLinesRendererBase EnsureGridLinesRenderer()
        {
            if (this.ShowGridLines)
            {
                this.GridLinesRenderer ??= this.CreateGridLinesRenderer();
                this.AddVisualChild(this.GridLinesRenderer);
            }
            else if (this.ShowGridLines == false
                     && this.GridLinesRenderer is not null)
            {
                this.RemoveVisualChild(this.GridLinesRenderer);
            }

            return this.GridLinesRenderer;
        }

        protected virtual GridLinesRendererBase CreateGridLinesRenderer()
        {
            return new UniformGridGridLinesRenderer();
        }

        private static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (UniformGridEx)d;
            grid.InvalidateVisual();
        }

        private static void OnGridLinesRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (UniformGridEx)d;
            grid.InvalidateVisual();
        }
    }
}