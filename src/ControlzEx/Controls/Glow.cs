namespace ControlzEx.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    public class Glow : Control
    {
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(Glow), new UIPropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(Glow), new UIPropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty IsGlowProperty = DependencyProperty.Register(nameof(IsGlow), typeof(bool), typeof(Glow), new UIPropertyMetadata(true));
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(Glow), new UIPropertyMetadata(Orientation.Vertical));
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(nameof(Direction), typeof(GlowDirection), typeof(Glow), new UIPropertyMetadata(GlowDirection.Top));
        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(Glow), new PropertyMetadata(default(Thickness), OnResizeBorderThicknessChanged));
        public static readonly DependencyProperty GlowBlurEffectProperty = DependencyProperty.Register(nameof(GlowBlurEffect), typeof(Effect), typeof(Glow), new PropertyMetadata(default(Effect)));

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

        public Effect GlowBlurEffect
        {
            get => (Effect)this.GetValue(GlowBlurEffectProperty);
            set => this.SetValue(GlowBlurEffectProperty, value);
        }

        private static void OnResizeBorderThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var glow = (Glow)d;

            // Add padding to the edges, otherwise the borders/glows overlap
            switch (glow.Direction)
            {
                case GlowDirection.Left:
                case GlowDirection.Right:
                    glow.Padding = new Thickness(0, glow.ResizeBorderThickness.Top / 2, 0, glow.ResizeBorderThickness.Bottom / 2);
                    break;

                case GlowDirection.Top:
                case GlowDirection.Bottom:
                    glow.Padding = new Thickness(glow.ResizeBorderThickness.Left / 2, 0, glow.ResizeBorderThickness.Right / 2, 0);
                    break;
            }

            glow.GlowBlurEffect = new BlurEffect
                              {
                                  KernelType = KernelType.Box,
                                  RenderingBias = RenderingBias.Performance,
                                  // The blur radius has to be the same as the resize border thickness.
                                  // Otherwise all pixels in the resize border are fully transparent which would disable hittests.
                                  Radius = GetRelevantResizeBorderThickness(glow)                                  
                              };
            glow.GlowBlurEffect.Freeze();
        }

        private static double GetRelevantResizeBorderThickness(Glow glow)
        {
            switch (glow.Direction)
            {
                case GlowDirection.Left:
                    return glow.ResizeBorderThickness.Left;
                    
                case GlowDirection.Right:
                    return glow.ResizeBorderThickness.Right;

                case GlowDirection.Top:
                    return glow.ResizeBorderThickness.Top;

                case GlowDirection.Bottom:
                    return glow.ResizeBorderThickness.Bottom;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
