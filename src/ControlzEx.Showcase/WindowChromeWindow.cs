namespace ControlzEx.Showcase
{
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Media;
    using ControlzEx.Behaviors;

    public class WindowChromeWindow : Window
    {
        public WindowChromeWindow()
        {
            this.InitializeBehaviors();
        }

        private void InitializeBehaviors()
        {
            this.InitializeWindowChromeBehavior();
            this.InitializeGlowWindowBehavior();
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeWindowChromeBehavior()
        {
            var behavior = new WindowChromeBehavior();
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.TryToBeFlickerFreeProperty, new Binding { Path = new PropertyPath(TryToBeFlickerFreeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty, new Binding { Path = new PropertyPath(IgnoreTaskbarOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.GlowBrushProperty, new Binding { Path = new PropertyPath(GlowBrushProperty), Source = this });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeGlowWindowBehavior()
        {
            var behavior = new GlowWindowBehavior();
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.GlowBrushProperty, new Binding { Path = new PropertyPath(GlowBrushProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.NonActiveGlowBrushProperty, new Binding { Path = new PropertyPath(NonActiveGlowBrushProperty), Source = this });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        public static readonly DependencyProperty TryToBeFlickerFreeProperty = DependencyProperty.Register(nameof(TryToBeFlickerFree), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(true));

        public bool TryToBeFlickerFree
        {
            get { return (bool)this.GetValue(TryToBeFlickerFreeProperty); }
            set { this.SetValue(TryToBeFlickerFreeProperty, value); }
        }

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResizeBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.ResizeBorderThicknessProperty.DefaultMetadata.DefaultValue));

        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty = DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty.DefaultMetadata.DefaultValue));

        public bool IgnoreTaskbarOnMaximize
        {
            get { return (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty); }
            set { this.SetValue(IgnoreTaskbarOnMaximizeProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is active.
        /// </summary>
        public Brush GlowBrush
        {
            get { return (Brush)this.GetValue(GlowBrushProperty); }
            set { this.SetValue(GlowBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="NonActiveGlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is not active.
        /// </summary>
        public Brush NonActiveGlowBrush
        {
            get { return (Brush)this.GetValue(NonActiveGlowBrushProperty); }
            set { this.SetValue(NonActiveGlowBrushProperty, value); }
        }
    }
}