namespace ControlzEx.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class Glow : Control
    {
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(Glow), new UIPropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(Glow), new UIPropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty IsGlowProperty = DependencyProperty.Register(nameof(IsGlow), typeof(bool), typeof(Glow), new UIPropertyMetadata(true));
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(Glow), new UIPropertyMetadata(Orientation.Vertical));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(nameof(Direction), typeof(GlowDirection), typeof(Glow), new UIPropertyMetadata(GlowDirection.Top));
        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(Glow), new PropertyMetadata(default(Thickness), OnResizeBorderThicknessChanged));

        static Glow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Glow), new FrameworkPropertyMetadata(typeof(Glow)));
        }

        public Brush GlowBrush
        {
            get => (Brush)this.GetValue(GlowBrushProperty);
            set => this.SetValue(GlowBrushProperty, value);
        }

        public Brush NonActiveGlowBrush
        {
            get => (Brush)this.GetValue(NonActiveGlowBrushProperty);
            set => this.SetValue(NonActiveGlowBrushProperty, value);
        }

        public bool IsGlow
        {
            get => (bool)this.GetValue(IsGlowProperty);
            set => this.SetValue(IsGlowProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)this.GetValue(OrientationProperty);
            set => this.SetValue(OrientationProperty, value);
        }

        public GlowDirection Direction
        {
            get => (GlowDirection)this.GetValue(DirectionProperty);
            set => this.SetValue(DirectionProperty, value);
        }

        public Thickness ResizeBorderThickness
        {
            get => (Thickness)this.GetValue(ResizeBorderThicknessProperty);
            set => this.SetValue(ResizeBorderThicknessProperty, value);
        }

        private static void OnResizeBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var glow = (Glow)d;

            // Add padding to the edges, otherwise the borders/glows overlap too much
            switch (glow.Direction)
            {
                case GlowDirection.Left:
                case GlowDirection.Right:
                    glow.Padding = new Thickness(0, glow.ResizeBorderThickness.Top / 4, 0, glow.ResizeBorderThickness.Bottom / 4);
                    break;

                case GlowDirection.Top:
                case GlowDirection.Bottom:
                    glow.Padding = new Thickness(glow.ResizeBorderThickness.Left / 4, 0, glow.ResizeBorderThickness.Right / 4, 0);
                    break;
            }
        }
    }
}
