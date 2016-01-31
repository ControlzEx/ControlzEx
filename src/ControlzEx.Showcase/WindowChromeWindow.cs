using System.Windows;

namespace ControlzEx.Showcase
{
    public class WindowChromeWindow : Window
    {
        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResizeBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeWindow), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Gets or sets glass border thickness
        /// </summary>
        public Thickness GlassFrameThickness
        {
            get { return (Thickness)this.GetValue(GlassFrameThicknessProperty); }
            set { this.SetValue(GlassFrameThicknessProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for GlassFrameThickness.  This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty GlassFrameThicknessProperty =
            DependencyProperty.Register(nameof(GlassFrameThickness), typeof(Thickness), typeof(WindowChromeWindow), new UIPropertyMetadata(new Thickness(0D)));
    }
}
