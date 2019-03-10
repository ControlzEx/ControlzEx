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
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(nameof(Direction), typeof(GlowDirection), typeof(Glow), new UIPropertyMetadata(GlowDirection.Top));

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

        public GlowDirection Direction
        {
            get => (GlowDirection)this.GetValue(DirectionProperty);
            set => this.SetValue(DirectionProperty, value);
        }
    }
}
